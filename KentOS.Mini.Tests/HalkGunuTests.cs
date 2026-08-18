using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Tests;

/// <summary>
/// HALK GÜNÜ — gün, dilimler, havuz, atama, görüşme.
/// </summary>
/// <remarks>
/// Modül üç ayrı kullanıcıya hizmet ediyor (sekreter · salondaki personel ·
/// başkan) ve testler o üçlünün her birinin bozulabileceği yerleri tutuyor:
/// birim izolasyonu, dilim üretimi, sıralama, görüşme damgası, telefon
/// normalleştirmesi ve talebe dönüştürmenin tek seferliği.
/// </remarks>
[Collection(SunucuKoleksiyonu.Ad)]
public class HalkGunuTests : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam;

    public HalkGunuTests(SunucuTestOrtami ortam) => _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
        {
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres kullanılamıyor");
        }
    }

    private async Task TemizleAsync()
    {
        using var b = _ortam.Baglam();
        await b.Database.ExecuteSqlRawAsync(
            "TRUNCATE halk_gunu_katilimlari, halk_gunu_dilimleri, halk_gunu_basvurulari, halk_gunleri RESTART IDENTITY CASCADE;");
        await _ortam.TemelVerileriKurAsync();

        // Talebe dönüştürme için gereken referans kayıtlar.
        //
        // `randevular` şemasında `mahalle_id` ve `randevu_durum_id` NOT NULL
        // (model `long?` gösterse de). Ortak fixture yalnızca `RandevuTip`
        // kuruyor; bu ikisi olmadan dönüştürme testi hizmet kuralına takılır.
        if (!await b.RandevuDurumlar.AnyAsync())
        {
            b.RandevuDurumlar.Add(new RandevuDurum { DurumAd = "Beklemede", Renk = "#B07D2B" });
        }
        if (!await b.Mahalleler.AnyAsync())
        {
            b.Mahalleler.Add(new Mahalle { Ad = "Merkez" });
        }
        await b.SaveChangesAsync();
    }

    private static HalkGunuServisi Servis(AppDbContext b, long birimId = 1) =>
        new(b, new SahteKullaniciServisi(1, "kalem1", birimId));

    private static (HalkGunuServisi gun, HalkGunuIslemServisi islem, SahteMesajServisi mesaj)
        TamServis(AppDbContext b, long birimId = 1)
    {
        var kullanici = new SahteKullaniciServisi(1, "kalem1", birimId);
        var gun = new HalkGunuServisi(b, kullanici);
        var mesaj = new SahteMesajServisi();
        return (gun, new HalkGunuIslemServisi(b, kullanici, gun, mesaj), mesaj);
    }

    private static HalkGunuIstegi GunIstegi(int gunFarki = 7) => new()
    {
        Tarih = DateTime.Now.Date.AddDays(gunFarki),
        Baslik = "Test Halk Günü",
        Konum = "Başkanlık Makamı",
    };

    private static BasvuruIstegi Kisi(string ad, string? telefon = null) => new()
    {
        Ad = ad,
        Soyad = "Test",
        Telefon = telefon,
        Konu = "Deneme konusu",
    };

    // ═══════════════════════════════════════════════ dilim üretimi

    /// <summary>
    /// Toplu dilim üretimi: 14:00–15:00 / 10 dk → altı dilim.
    /// </summary>
    /// <remarks>
    /// Sekreterin on dilimi tek tek girmesi işin en sıkıcı kısmıydı; bu yol
    /// olmadan modül pratikte kullanılmazdı.
    /// </remarks>
    [Fact]
    public async Task Toplu_dilim_uretimi_araligi_boler()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var s = Servis(b);

        var gun = await s.OlusturAsync(GunIstegi());
        var taban = gun.Tarih;

        var detay = await s.DilimEkleAsync(gun.Id, new DilimIstegi
        {
            Baslangic = taban.AddHours(14),
            Bitis = taban.AddHours(15),
            DilimDakika = 10,
        });

        Assert.Equal(6, detay.Dilimler.Count);
        Assert.Equal(taban.AddHours(14), detay.Dilimler[0].Baslangic);
        Assert.Equal(taban.AddHours(15), detay.Dilimler[^1].Bitis);
        // Toplu üretimde varsayılan kapasite 1: tek kişilik randevu dilimleri.
        Assert.Equal(1, detay.Dilimler[0].Kapasite);
    }

    /// <summary>Tek dilim: 14:00–15:00'e sırayla on kişi.</summary>
    [Fact]
    public async Task Tek_dilim_kapasitesiz_olabilir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var s = Servis(b);

        var gun = await s.OlusturAsync(GunIstegi());
        var detay = await s.DilimEkleAsync(gun.Id, new DilimIstegi
        {
            Baslangic = gun.Tarih.AddHours(14),
            Bitis = gun.Tarih.AddHours(15),
        });

        Assert.Single(detay.Dilimler);
        Assert.Null(detay.Dilimler[0].Kapasite);
    }

    [Fact]
    public async Task Bitis_baslangictan_once_olamaz()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var s = Servis(b);
        var gun = await s.OlusturAsync(GunIstegi());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            s.DilimEkleAsync(gun.Id, new DilimIstegi
            {
                Baslangic = gun.Tarih.AddHours(15),
                Bitis = gun.Tarih.AddHours(14),
            }));
    }

    /// <summary>
    /// Dilim silinince atanmış kişiler GÜNDE KALIR.
    /// </summary>
    /// <remarks>
    /// FK <c>SetNull</c>: yanlışlıkla silinen bir saat aralığı yüzünden on
    /// kişinin kaydı yok olmamalı — "atanmamışlar" bölümüne düşerler.
    /// </remarks>
    [Fact]
    public async Task Dilim_silinince_kisiler_atanmamislara_duser()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var s = Servis(b);

        var gun = await s.OlusturAsync(GunIstegi());
        var detay = await s.DilimEkleAsync(gun.Id, new DilimIstegi
        {
            Baslangic = gun.Tarih.AddHours(14),
            Bitis = gun.Tarih.AddHours(15),
        });
        var basvuru = await s.BasvuruOlusturAsync(Kisi("Ahmet"));
        await s.KatilimEkleAsync(gun.Id, [basvuru.Id], detay.Dilimler[0].Id);

        var sonra = await s.DilimSilAsync(detay.Dilimler[0].Id);

        Assert.Empty(sonra.Dilimler);
        Assert.Single(sonra.Atanmamislar);
        Assert.Equal("Ahmet Test", sonra.Atanmamislar[0].AdSoyad);
    }

    // ═══════════════════════════════════════════════ atama ve sıralama

    [Fact]
    public async Task Ayni_kisi_ayni_gune_iki_kez_atanmaz()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var s = Servis(b);

        var gun = await s.OlusturAsync(GunIstegi());
        var basvuru = await s.BasvuruOlusturAsync(Kisi("Ahmet"));

        await s.KatilimEkleAsync(gun.Id, [basvuru.Id], null);
        // İkinci çağrı benzersiz indekse takılmamalı, sessizce yok saymalı:
        // "tümünü ekle" düğmesine ikinci kez basmak hata vermemeli.
        var detay = await s.KatilimEkleAsync(gun.Id, [basvuru.Id], null);

        Assert.Equal(1, detay.KisiSayisi);
    }

    /// <summary>
    /// Sıralama TAMAMEN yeniden yazılır.
    /// </summary>
    /// <remarks>
    /// İstemci yeni diziyi kurup tamamını gönderiyor; tek tek "yukarı taşı"
    /// ucu, iki kullanıcı aynı anda oynadığında sırayı bozardı.
    /// </remarks>
    [Fact]
    public async Task Siralama_dilim_icinde_degistirilebilir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var s = Servis(b);

        var gun = await s.OlusturAsync(GunIstegi());
        var detay = await s.DilimEkleAsync(gun.Id, new DilimIstegi
        {
            Baslangic = gun.Tarih.AddHours(14),
            Bitis = gun.Tarih.AddHours(15),
        });
        var dilimId = detay.Dilimler[0].Id;

        var idler = new List<long>();
        foreach (var ad in new[] { "Ahmet", "Ayşe", "Mehmet" })
        {
            idler.Add((await s.BasvuruOlusturAsync(Kisi(ad))).Id);
        }
        detay = await s.KatilimEkleAsync(gun.Id, [.. idler], dilimId);

        var kisiler = detay.Dilimler[0].Kisiler;
        Assert.Equal(["Ahmet Test", "Ayşe Test", "Mehmet Test"],
            kisiler.Select(k => k.AdSoyad));

        // Ters çevir.
        await s.SiralamaGuncelleAsync(gun.Id,
            [.. kisiler.AsEnumerable().Reverse().Select((k, i) =>
                new SiralamaOgesiDto { Id = k.Id, DilimId = dilimId, SiraNo = i + 1 })]);

        var sonra = await s.DetayAsync(gun.Id);
        Assert.Equal(["Mehmet Test", "Ayşe Test", "Ahmet Test"],
            sonra.Dilimler[0].Kisiler.Select(k => k.AdSoyad));
    }

    /// <summary>Listeden çıkarılan kişi havuza GERİ DÖNER.</summary>
    [Fact]
    public async Task Listeden_cikarilan_kisi_havuza_doner()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var s = Servis(b);

        var gun = await s.OlusturAsync(GunIstegi());
        var basvuru = await s.BasvuruOlusturAsync(Kisi("Ahmet"));
        var detay = await s.KatilimEkleAsync(gun.Id, [basvuru.Id], null);

        // Atanınca havuzdaki durum "Atandı" olur.
        var havuz = await s.BasvuruListeAsync(new BasvuruSuzgeci());
        Assert.Equal(BasvuruDurumu.Atandi, havuz.Veriler[0].Durum);

        await s.KatilimCikarAsync(detay.Atanmamislar[0].Id);

        havuz = await s.BasvuruListeAsync(new BasvuruSuzgeci());
        Assert.Equal(BasvuruDurumu.Bekliyor, havuz.Veriler[0].Durum);
    }

    // ═══════════════════════════════════════════════ görüşme

    /// <summary>
    /// Görüşme tarihi İLK "görüşüldü"de damgalanır, sonraki düzeltmelerde korunur.
    /// </summary>
    [Fact]
    public async Task Gorusme_tarihi_ilk_kayitta_damgalanir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var s = Servis(b);

        var gun = await s.OlusturAsync(GunIstegi());
        var basvuru = await s.BasvuruOlusturAsync(Kisi("Ahmet"));
        var detay = await s.KatilimEkleAsync(gun.Id, [basvuru.Id], null);
        var katilimId = detay.Atanmamislar[0].Id;

        var ilk = await s.GorusmeKaydetAsync(katilimId, new GorusmeIstegi
        {
            Durum = KatilimDurumu.Gorusuldu,
            GorusmeNotu = "Yol talebi.",
        });
        Assert.NotNull(ilk.GorusmeTarihi);

        var damga = ilk.GorusmeTarihi;
        var ikinci = await s.GorusmeKaydetAsync(katilimId, new GorusmeIstegi
        {
            GorusmeNotu = "Yol talebi — düzeltildi.",
        });

        Assert.Equal(damga, ikinci.GorusmeTarihi);
        Assert.Equal("Yol talebi — düzeltildi.", ikinci.GorusmeNotu);
    }

    /// <summary>
    /// "Geldi" ile "Görüşüldü" AYRI durumlar.
    /// </summary>
    /// <remarks>
    /// Salonda sırası gelen kişi gelmiş olabilir ama görüşme bitmemiştir.
    /// Tek alana sıkıştırmak operatörün ekranını anlamsız kılardı.
    /// </remarks>
    [Fact]
    public async Task Geldi_gorusuldu_degil()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var s = Servis(b);

        var gun = await s.OlusturAsync(GunIstegi());
        var basvuru = await s.BasvuruOlusturAsync(Kisi("Ahmet"));
        var detay = await s.KatilimEkleAsync(gun.Id, [basvuru.Id], null);

        var geldi = await s.GorusmeKaydetAsync(detay.Atanmamislar[0].Id,
            new GorusmeIstegi { Durum = KatilimDurumu.Geldi });

        Assert.Equal("Geldi", geldi.DurumAd);
        Assert.Null(geldi.GorusmeTarihi);

        var ozet = await s.DetayAsync(gun.Id);
        Assert.Equal(0, ozet.GorusulenSayisi);
    }

    // ═══════════════════════════════════════════════ birim izolasyonu

    [Fact]
    public async Task Baska_birimin_halk_gunu_GORUNMEZ()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();

        var gun = await Servis(b, birimId: 1).OlusturAsync(GunIstegi());

        var yabanci = Servis(b, birimId: 2);
        var liste = await yabanci.ListeAsync(new HalkGunuSuzgeci());

        Assert.Empty(liste.Veriler);
        await Assert.ThrowsAsync<EntityNotFoundException>(() => yabanci.DetayAsync(gun.Id));
    }

    [Fact]
    public async Task Baska_birimin_basvurusu_atanamaz()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();

        var yabanciBasvuru = await Servis(b, birimId: 2).BasvuruOlusturAsync(Kisi("Yabancı"));

        var s = Servis(b, birimId: 1);
        var gun = await s.OlusturAsync(GunIstegi());
        var detay = await s.KatilimEkleAsync(gun.Id, [yabanciBasvuru.Id], null);

        Assert.Equal(0, detay.KisiSayisi);
    }

    // ═══════════════════════════════════════════════ telefon normalleştirme

    /// <summary>
    /// Aynı numaranın FARKLI yazımları aynı kişiyi bulur.
    /// </summary>
    /// <remarks>
    /// Sistemde vatandaş tablosu yok; eşleştirilebilecek tek doğal anahtar
    /// telefon. Kayıtlardaki numaralar `0541 298 34 50` / `05412983450` /
    /// `+90 541…` karışık ve bugünkü `ILIKE` araması bitişik yazımı
    /// bulmuyordu. Bu test o davranışı kilitliyor.
    /// </remarks>
    [Theory]
    [InlineData("0541 298 34 50")]
    [InlineData("05412983450")]
    [InlineData("+90 541 298 34 50")]
    [InlineData("541 298 34 50")]
    public async Task Telefonun_farkli_yazimlari_ayni_kisiyi_bulur(string yazim)
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (gun, islem, _) = TamServis(b);

        var g = await gun.OlusturAsync(GunIstegi());
        var basvuru = await gun.BasvuruOlusturAsync(Kisi("Ahmet", "0541 298 34 50"));
        await gun.KatilimEkleAsync(g.Id, [basvuru.Id], null);

        var gecmis = await islem.KisiGecmisiAsync(yazim, null);

        Assert.True(gecmis.KayitVar, $"'{yazim}' yazımı kişiyi bulamadı");
        Assert.Equal(1, gecmis.HalkGunuSayisi);
    }

    [Fact]
    public async Task Telefon_ve_ad_yoksa_gecmis_bos_doner()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (_, islem, _) = TamServis(b);

        var gecmis = await islem.KisiGecmisiAsync(null, null);

        // Tüm tabloyu taramanın anlamı yok; boş sorgu boş sonuç.
        Assert.False(gecmis.KayitVar);
        Assert.Empty(gecmis.Son);
    }

    // ═══════════════════════════════════════════════ toplu SMS

    /// <summary>
    /// SMS yer tutucuları doldurulur, telefonsuz kayıt ATLANIR ve SAYILIR.
    /// </summary>
    /// <remarks>
    /// Telefonu olmayanı sessizce geçmek, sekreterin kimin haber almadığını
    /// görmesini engellerdi.
    /// </remarks>
    [Fact]
    public async Task Toplu_sms_yer_tutuculari_doldurur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (gun, islem, mesaj) = TamServis(b);

        var g = await gun.OlusturAsync(GunIstegi());
        var detay = await gun.DilimEkleAsync(g.Id, new DilimIstegi
        {
            Baslangic = g.Tarih.AddHours(14),
            Bitis = g.Tarih.AddHours(15),
        });

        var telefonlu = await gun.BasvuruOlusturAsync(Kisi("Ahmet", "05412983450"));
        var telefonsuz = await gun.BasvuruOlusturAsync(Kisi("Ayşe"));
        await gun.KatilimEkleAsync(g.Id, [telefonlu.Id, telefonsuz.Id], detay.Dilimler[0].Id);

        var sonuc = await islem.SmsGonderAsync(g.Id, new HalkGunuSmsIstegi
        {
            Mesaj = "Sayın {adSoyad}, {tarih} saat {saat} ({sira}. sıra).",
        });

        Assert.Equal(1, sonuc.Gonderilen);
        Assert.Equal(1, sonuc.Telefonsuz);

        var gonderilen = Assert.Single(mesaj.TekKisiyeGidenMesajlar);
        Assert.Contains("Ahmet Test", gonderilen.Icerik);
        Assert.Contains("14:00", gonderilen.Icerik);
        Assert.Contains("1. sıra", gonderilen.Icerik);
        // Yer tutucu kalıntısı olmamalı.
        Assert.DoesNotContain("{", gonderilen.Icerik);
    }

    /// <summary>İkinci gönderimde daha önce SMS gidenler atlanır.</summary>
    [Fact]
    public async Task Ikinci_sms_gonderiminde_tekrar_gonderilmez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (gun, islem, _) = TamServis(b);

        var g = await gun.OlusturAsync(GunIstegi());
        var basvuru = await gun.BasvuruOlusturAsync(Kisi("Ahmet", "05412983450"));
        await gun.KatilimEkleAsync(g.Id, [basvuru.Id], null);

        var istek = new HalkGunuSmsIstegi { Mesaj = "Merhaba {ad}." };
        await islem.SmsGonderAsync(g.Id, istek);
        var ikinci = await islem.SmsGonderAsync(g.Id, istek);

        Assert.Equal(0, ikinci.Gonderilen);
        Assert.Equal(1, ikinci.Atlanan);
    }

    // ═══════════════════════════════════════════════ talebe dönüştürme

    /// <summary>
    /// Görüşme talebe BİR KEZ dönüştürülür.
    /// </summary>
    /// <remarks>
    /// İkinci çağrı hata vermeseydi aynı iş birden çok birime düşerdi.
    /// </remarks>
    [Fact]
    public async Task Talebe_donusturme_tek_seferlik()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (gun, islem, _) = TamServis(b);

        var g = await gun.OlusturAsync(GunIstegi());
        var basvuru = await gun.BasvuruOlusturAsync(Kisi("Ahmet", "05412983450"));
        var detay = await gun.KatilimEkleAsync(g.Id, [basvuru.Id], null);
        var katilimId = detay.Atanmamislar[0].Id;

        await gun.GorusmeKaydetAsync(katilimId, new GorusmeIstegi
        {
            Durum = KatilimDurumu.Gorusuldu,
            GorusmeNotu = "Mahalledeki yol talebi.",
            DegerlendirmeyeEsas = true,
        });

        var talepId = await islem.TalebeDonusturAsync(katilimId, new TalepOlusturIstegi());
        Assert.True(talepId > 0);

        // Görüşme notu talebin açıklamasına taşınmalı: talebi alan birim
        // vatandaşın ne anlattığını okumadan işe başlayamaz.
        var talep = await b.Randevular.AsNoTracking().FirstAsync(r => r.Id == talepId);
        Assert.Contains("Mahalledeki yol talebi.", talep.Aciklama);
        Assert.Equal("Ahmet", talep.Ad);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            islem.TalebeDonusturAsync(katilimId, new TalepOlusturIstegi()));
    }

    // ═══════════════════════════════════════════════ silme kuralı

    /// <summary>
    /// Görüşme kaydı girilmiş gün SİLİNEMEZ.
    /// </summary>
    /// <remarks>
    /// Salonda tutulan notlar tek kopya ve geri getirilemez.
    /// </remarks>
    [Fact]
    public async Task Gorusme_girilmis_gun_silinemez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var s = Servis(b);

        var gun = await s.OlusturAsync(GunIstegi());
        var basvuru = await s.BasvuruOlusturAsync(Kisi("Ahmet"));
        var detay = await s.KatilimEkleAsync(gun.Id, [basvuru.Id], null);

        // Henüz görüşme yok → silinebilir olmalı.
        await s.GorusmeKaydetAsync(detay.Atanmamislar[0].Id,
            new GorusmeIstegi { Durum = KatilimDurumu.Geldi });

        await Assert.ThrowsAsync<BusinessRuleException>(() => s.SilAsync(gun.Id));
    }

    [Fact]
    public async Task Bos_gun_silinebilir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var s = Servis(b);

        var gun = await s.OlusturAsync(GunIstegi());
        await s.SilAsync(gun.Id);

        var liste = await s.ListeAsync(new HalkGunuSuzgeci());
        Assert.Empty(liste.Veriler);
    }

    // ═══════════════════════════════════════════════ özet ve Excel

    [Fact]
    public async Task Ozet_takip_gerektirenleri_listeler()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (gun, islem, _) = TamServis(b);

        var g = await gun.OlusturAsync(GunIstegi());
        var idler = new List<long>();
        foreach (var ad in new[] { "Ahmet", "Ayşe" })
        {
            idler.Add((await gun.BasvuruOlusturAsync(Kisi(ad))).Id);
        }
        var detay = await gun.KatilimEkleAsync(g.Id, [.. idler], null);

        await gun.GorusmeKaydetAsync(detay.Atanmamislar[0].Id, new GorusmeIstegi
        {
            Durum = KatilimDurumu.Gorusuldu,
            DegerlendirmeyeEsas = true,
            DegerlendirmeNotu = "Fen İşleri baksın.",
        });

        var ozet = await islem.OzetAsync(g.Id);

        Assert.Equal(2, ozet.Toplam);
        Assert.Equal(1, ozet.Gorusulen);
        Assert.Equal(1, ozet.TakipGerektiren);
        Assert.Single(ozet.TakipListesi);
        Assert.Equal("Fen İşleri baksın.", ozet.TakipListesi[0].DegerlendirmeNotu);
    }

    /// <summary>
    /// Üç çıktı türü de üretilir — hem Excel hem PDF.
    /// </summary>
    /// <remarks>
    /// Çıktı yolu sessizce kırılabilen bir yer: QuestPDF lisansı HER PDF
    /// sınıfının kendi statik kurucusunda ayarlanmak zorunda ve unutulduğunda
    /// uç nokta 500 dönüyor. Bayt kontrolü de biçimi doğruluyor (XLSX bir ZIP,
    /// PDF "%PDF" ile başlar).
    /// </remarks>
    [Theory]
    [InlineData(HalkGunuCiktiTuru.Program)]
    [InlineData(HalkGunuCiktiTuru.Sonuc)]
    [InlineData(HalkGunuCiktiTuru.Imza)]
    public async Task Cikti_uretilir(HalkGunuCiktiTuru tur)
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (gun, _, _) = TamServis(b);
        var kurum = new SahteKurumServisi("Örnek Belediyesi");
        var cikti = new HalkGunuCiktiServisi(gun, new SahteKullaniciServisi(1, "ekleyen", 1), kurum);

        var g = await gun.OlusturAsync(GunIstegi());
        var detay = await gun.DilimEkleAsync(g.Id, new DilimIstegi
        {
            Baslangic = g.Tarih.AddHours(14),
            Bitis = g.Tarih.AddHours(15),
            Baslik = "İlk oturum",
        });
        var basvuru = await gun.BasvuruOlusturAsync(Kisi("Ahmet", "05412983450"));
        await gun.KatilimEkleAsync(g.Id, [basvuru.Id], detay.Dilimler[0].Id);

        var xlsx = await cikti.ExcelAsync(g.Id, null, tur, null);
        Assert.EndsWith(".xlsx", xlsx.DosyaAdi);
        // XLSX bir ZIP: ilk iki bayt "PK".
        Assert.Equal(0x50, xlsx.Icerik[0]);
        Assert.Equal(0x4B, xlsx.Icerik[1]);

        var pdf = await cikti.PdfAsync(g.Id, null, tur, null);

        // Kurum adı KURUM KAYDINDAN geliyor mu? Koda yazılıyken de PDF
        // üretiliyor ve bu test yeşil geçiyordu.
        Assert.True(kurum.OkumaSayisi > 0, "PDF kurum adını kurum kaydından okumadı.");
        Assert.EndsWith(".pdf", pdf.DosyaAdi);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf.Icerik, 0, 4));
    }

    /// <summary>
    /// Tek grup çıktısı YALNIZCA o grubu içerir.
    /// </summary>
    /// <remarks>
    /// Kapıdaki görevlinin elindeki kâğıt bütün günü değil o saatteki grubu
    /// gösteriyor; süzgeç düşerse listede sırası gelmemiş kişiler de çağrılır.
    /// </remarks>
    [Fact]
    public async Task Tek_grup_ciktisi_yalnizca_o_grubu_icerir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (gun, _, _) = TamServis(b);
        var kurum = new SahteKurumServisi("Örnek Belediyesi");
        var cikti = new HalkGunuCiktiServisi(gun, new SahteKullaniciServisi(1, "ekleyen", 1), kurum);

        var g = await gun.OlusturAsync(GunIstegi());
        var detay = await gun.DilimEkleAsync(g.Id, new DilimIstegi
        {
            Baslangic = g.Tarih.AddHours(14),
            Bitis = g.Tarih.AddHours(16),
            DilimDakika = 60,
        });
        var dilimler = detay.Dilimler.OrderBy(d => d.Baslangic).ToList();

        var a = await gun.BasvuruOlusturAsync(Kisi("Birinci"));
        var c = await gun.BasvuruOlusturAsync(Kisi("İkinci"));
        await gun.KatilimEkleAsync(g.Id, [a.Id], dilimler[0].Id);
        await gun.KatilimEkleAsync(g.Id, [c.Id], dilimler[1].Id);

        var tek = await cikti.ExcelAsync(g.Id, dilimler[0].Id, HalkGunuCiktiTuru.Program, null);
        var tumu = await cikti.ExcelAsync(g.Id, null, HalkGunuCiktiTuru.Program, null);

        Assert.Contains("Birinci", MetinCikar(tek.Icerik));
        Assert.DoesNotContain("İkinci", MetinCikar(tek.Icerik));
        Assert.Contains("İkinci", MetinCikar(tumu.Icerik));
    }

    /// <summary>XLSX içindeki paylaşılan metinleri kabaca çıkarır.</summary>
    private static string MetinCikar(byte[] xlsx)
    {
        using var akis = new MemoryStream(xlsx);
        using var kitap = new ClosedXML.Excel.XLWorkbook(akis);
        var sayfa = kitap.Worksheets.First();
        return string.Join("\n", sayfa.CellsUsed().Select(h => h.GetString()));
    }

    /// <summary>
    /// Kapasite AŞILAMAZ.
    /// </summary>
    /// <remarks>
    /// Kapasite bilgilendirme alanı değil: 10 dakikalık dilime üç kişi
    /// atanınca ekranda "3 / 1 kişi" yazıyor ve liste bozuk görünüyordu.
    /// </remarks>
    [Fact]
    public async Task Dilim_kapasitesi_asilamaz()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var s = Servis(b);

        var gun = await s.OlusturAsync(GunIstegi());
        var detay = await s.DilimEkleAsync(gun.Id, new DilimIstegi
        {
            Baslangic = gun.Tarih.AddHours(14),
            Bitis = gun.Tarih.AddMinutes(14 * 60 + 10),
            Kapasite = 1,
        });
        var dilimId = detay.Dilimler[0].Id;

        var birinci = await s.BasvuruOlusturAsync(Kisi("Ahmet"));
        var ikinci = await s.BasvuruOlusturAsync(Kisi("Ayşe"));

        await s.KatilimEkleAsync(gun.Id, [birinci.Id], dilimId);

        var hata = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            s.KatilimEkleAsync(gun.Id, [ikinci.Id], dilimId));

        Assert.Contains("kapasitesi", hata.Message);
    }

    /// <summary>Kapasitesiz dilime sınırsız kişi atanabilir.</summary>
    [Fact]
    public async Task Kapasitesiz_dilime_sinirsiz_atanir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var s = Servis(b);

        var gun = await s.OlusturAsync(GunIstegi());
        var detay = await s.DilimEkleAsync(gun.Id, new DilimIstegi
        {
            Baslangic = gun.Tarih.AddHours(14),
            Bitis = gun.Tarih.AddHours(15),
        });

        var idler = new List<long>();
        foreach (var ad in new[] { "Ahmet", "Ayşe", "Mehmet", "Fatma", "Ali" })
        {
            idler.Add((await s.BasvuruOlusturAsync(Kisi(ad))).Id);
        }

        var sonra = await s.KatilimEkleAsync(gun.Id, [.. idler], detay.Dilimler[0].Id);

        Assert.Equal(5, sonra.Dilimler[0].Kisiler.Count);
    }

    // ── ret ve taşıma ──────────────────────────────────────────────────

    /// <summary>
    /// Reddedilen kişi ATANMIŞ olsa bile listeden düşer.
    /// </summary>
    /// <remarks>
    /// Ret kararı çoğu zaman liste kurulduktan SONRA geliyor; kayıt yerinde
    /// kalırsa vatandaş salonda çağrılmaya devam ediyordu. Kaydın kendisi
    /// havuzda kalır — "kaç kişi geri çevrildi" ve "neden çevrildi" soruları
    /// ancak öyle cevaplanabiliyor.
    /// </remarks>
    [Fact]
    public async Task Reddedilen_kisi_atandigi_gunden_duser()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (gun, _, _) = TamServis(b);

        var g = await gun.OlusturAsync(GunIstegi());
        var basvuru = await gun.BasvuruOlusturAsync(Kisi("Reddedilecek"));
        await gun.KatilimEkleAsync(g.Id, [basvuru.Id], null);

        var red = await gun.BasvuruReddetAsync(basvuru.Id, "Konu belediyeyi ilgilendirmiyor.");

        Assert.Equal(BasvuruDurumu.Reddedildi, red.Durum);
        Assert.Equal("Konu belediyeyi ilgilendirmiyor.", red.RedNedeni);
        Assert.NotNull(red.RedTarihi);

        var detay = await gun.DetayAsync(g.Id);
        Assert.Empty(detay.Dilimler.SelectMany(d => d.Kisiler).Concat(detay.Atanmamislar));

        // "Bekleyenler" listesi de göstermez: yanlışlıkla yeniden atanmasın.
        var bekleyenler = await gun.BasvuruListeAsync(new BasvuruSuzgeci { Atanmamis = true });
        Assert.Empty(bekleyenler.Veriler);
    }

    /// <summary>Görüşülmüş kayda ret dokunmaz — olan olmuş.</summary>
    [Fact]
    public async Task Gorusulmus_katilim_retten_etkilenmez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (gun, islem, _) = TamServis(b);

        var g = await gun.OlusturAsync(GunIstegi());
        var basvuru = await gun.BasvuruOlusturAsync(Kisi("Görüşülmüş"));
        var detay = await gun.KatilimEkleAsync(g.Id, [basvuru.Id], null);
        var katilim = detay.Atanmamislar[0];

        await gun.GorusmeKaydetAsync(katilim.Id, new GorusmeIstegi
        {
            Durum = KatilimDurumu.Gorusuldu,
            GorusmeNotu = "Konuşuldu.",
        });

        await gun.BasvuruReddetAsync(basvuru.Id, "Bir daha çağrılmasın.");

        var sonrasi = await gun.DetayAsync(g.Id);
        var kalanlar = sonrasi.Dilimler.SelectMany(d => d.Kisiler)
            .Concat(sonrasi.Atanmamislar).ToList();
        Assert.Single(kalanlar);
        Assert.Equal(KatilimDurumu.Gorusuldu, kalanlar[0].Durum);
        _ = islem;
    }

    /// <summary>Havuza geri alma reddi temizler.</summary>
    [Fact]
    public async Task Geri_alma_reddi_temizler()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (gun, _, _) = TamServis(b);

        var basvuru = await gun.BasvuruOlusturAsync(Kisi("Geri alınacak"));
        await gun.BasvuruReddetAsync(basvuru.Id, "Yanlış kişi.");

        var geri = await gun.BasvuruGeriAlAsync(basvuru.Id);

        Assert.Equal(BasvuruDurumu.Bekliyor, geri.Durum);
        Assert.Null(geri.RedNedeni);

        var bekleyenler = await gun.BasvuruListeAsync(new BasvuruSuzgeci { Atanmamis = true });
        Assert.Single(bekleyenler.Veriler);
    }

    /// <summary>
    /// Kişi başka bir DİLİME taşınabilir ve kapasite orada da denetlenir.
    /// </summary>
    /// <remarks>
    /// Sıralama ucu yalnızca "sırayı değiştir" sanılıyordu ama `dilimId` de
    /// yazıyor: taşımanın yolu bu. Kapasite denetimi yalnızca atama ucunda
    /// dururken taşıma sınırı sessizce aşıyordu.
    /// </remarks>
    [Fact]
    public async Task Dilime_tasima_kapasiteyi_asamaz()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (gun, _, _) = TamServis(b);

        var g = await gun.OlusturAsync(GunIstegi());
        var detay = await gun.DilimEkleAsync(g.Id, new DilimIstegi
        {
            Baslangic = g.Tarih.AddHours(14),
            Bitis = g.Tarih.AddHours(16),
            DilimDakika = 60,
            Kapasite = 1,
        });
        var dilimler = detay.Dilimler.OrderBy(d => d.Baslangic).ToList();

        var a = await gun.BasvuruOlusturAsync(Kisi("Birinci"));
        var c = await gun.BasvuruOlusturAsync(Kisi("İkinci"));
        var ilk = await gun.KatilimEkleAsync(g.Id, [a.Id], dilimler[0].Id);
        var ikinci = await gun.KatilimEkleAsync(g.Id, [c.Id], dilimler[1].Id);

        var birinciKatilim = ilk.Dilimler.First(d => d.Id == dilimler[0].Id).Kisiler[0];
        var ikinciKatilim = ikinci.Dilimler.First(d => d.Id == dilimler[1].Id).Kisiler[0];

        // TAŞIMA çalışır: ikinci kişi ilk dilime alınırken birinci boşaltılır.
        await gun.SiralamaGuncelleAsync(g.Id, [
            new SiralamaOgesiDto { Id = ikinciKatilim.Id, DilimId = dilimler[0].Id, SiraNo = 1 },
            new SiralamaOgesiDto { Id = birinciKatilim.Id, DilimId = dilimler[1].Id, SiraNo = 1 },
        ]);

        var sonrasi = await gun.DetayAsync(g.Id);
        Assert.Equal(ikinciKatilim.Id,
            sonrasi.Dilimler.First(d => d.Id == dilimler[0].Id).Kisiler[0].Id);

        // Kapasite AŞILAMAZ: ikisini birden ilk dilime koymak reddedilir.
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            gun.SiralamaGuncelleAsync(g.Id, [
                new SiralamaOgesiDto { Id = ikinciKatilim.Id, DilimId = dilimler[0].Id, SiraNo = 1 },
                new SiralamaOgesiDto { Id = birinciKatilim.Id, DilimId = dilimler[0].Id, SiraNo = 2 },
            ]));
    }
}
