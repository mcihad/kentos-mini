using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Tests;

/// <summary>
/// Oturum denetim kaydı.
///
/// <para>
/// Sistem iki yıldır canlıda ve bugüne kadar kimin ne zaman girdiğine dair
/// hiçbir kayıt tutulmuyordu. Buradaki testler iki şeyi kilitler:
/// kayıt GERÇEKTEN yazılıyor, ve kayıt yazılamadığında <b>giriş akışı
/// bozulmuyor</b> — denetim tablosundaki bir sorun tüm kullanıcıları
/// sistemin dışında bırakmamalı.
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class OturumKaydiTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    private OturumKaydiServisi Servis(HttpContext? baglam = null) =>
        new(_ortam.Baglam(), new SahteHttpErisimi(baglam), NullLogger<OturumKaydiServisi>.Instance);

    private static HttpContext BaglamKur(string? ip = null, string? iletilen = null, string? istemci = null)
    {
        var b = new DefaultHttpContext();
        if (ip is not null) b.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
        if (iletilen is not null) b.Request.Headers["X-Forwarded-For"] = iletilen;
        if (istemci is not null) b.Request.Headers.UserAgent = istemci;
        return b;
    }

    [Fact]
    public async Task Basarili_giris_kaydedilir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using (var db = _ortam.Baglam()) await db.OturumKayitlari.ExecuteDeleteAsync();

        await Servis(BaglamKur(ip: "10.1.2.3")).KaydetAsync(
            1, "ekleyen", OturumOlayi.Giris, true);

        using var kontrol = _ortam.Baglam();
        var kayit = await kontrol.OturumKayitlari.SingleAsync();

        Assert.Equal(1, kayit.KullaniciId);
        Assert.Equal("ekleyen", kayit.KullaniciAdi);
        Assert.Equal(OturumOlayi.Giris, kayit.Olay);
        Assert.True(kayit.Basarili);
        Assert.Equal("10.1.2.3", kayit.IpAdresi);
    }

    [Fact]
    public async Task Basarisiz_deneme_de_kaydedilir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using (var db = _ortam.Baglam()) await db.OturumKayitlari.ExecuteDeleteAsync();

        // Arka arkaya başarısız giriş, hesap kilitlenmeden önce görülmesi
        // gereken tek sinyal.
        await Servis().KaydetAsync(null, "olmayan-kullanici", OturumOlayi.Giris, false, "Kullanıcı bulunamadı");

        using var kontrol = _ortam.Baglam();
        var kayit = await kontrol.OturumKayitlari.SingleAsync();

        Assert.Null(kayit.KullaniciId);
        Assert.False(kayit.Basarili);
        Assert.Equal("Kullanıcı bulunamadı", kayit.Aciklama);
    }

    [Fact]
    public async Task Vekil_arkasindaki_gercek_IP_kaydedilir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using (var db = _ortam.Baglam()) await db.OturumKayitlari.ExecuteDeleteAsync();

        // Uygulama ters vekil arkasında; `RemoteIpAddress` hep vekilin adresi.
        // Zincirin EN SOLDAKİ öğesi gerçek istemcidir.
        await Servis(BaglamKur(ip: "127.0.0.1", iletilen: "203.0.113.7, 10.0.0.1"))
            .KaydetAsync(1, "ekleyen", OturumOlayi.Giris, true);

        using var kontrol = _ortam.Baglam();
        Assert.Equal("203.0.113.7", (await kontrol.OturumKayitlari.SingleAsync()).IpAdresi);
    }

    [Fact]
    public async Task Cok_uzun_degerler_kirpilir_ve_HATA_VERMEZ()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using (var db = _ortam.Baglam()) await db.OturumKayitlari.ExecuteDeleteAsync();

        // Elle hazırlanmış bir istek 500 karakterlik kullanıcı adı gönderebilir;
        // veritabanı sınırı 64. Kırpılmazsa denetim kaydı YAZILAMAZ ve
        // asıl olayın izi kaybolur.
        await Servis(BaglamKur(istemci: new string('U', 900))).KaydetAsync(
            null, new string('A', 500), OturumOlayi.Giris, false, new string('N', 900));

        using var kontrol = _ortam.Baglam();
        var kayit = await kontrol.OturumKayitlari.SingleAsync();

        Assert.Equal(64, kayit.KullaniciAdi.Length);
        Assert.Equal(256, kayit.Aciklama!.Length);
        Assert.Equal(256, kayit.Istemci!.Length);
    }

    [Fact]
    public async Task Kayit_yazilamazsa_ISTISNA_FIRLATMAZ()
    {
        PostgresYoksaAtla();

        // Kapatılmış bir bağlam → yazma kesin başarısız.
        var baglam = _ortam.Baglam();
        await baglam.DisposeAsync();

        var servis = new OturumKaydiServisi(
            baglam, new SahteHttpErisimi(null), NullLogger<OturumKaydiServisi>.Instance);

        // Denetim kaydı yazılamadı diye giriş REDDEDİLMEMELİ; tek bir
        // veritabanı sorunu tüm kullanıcıları sistemin dışında bırakırdı.
        await servis.KaydetAsync(1, "ekleyen", OturumOlayi.Giris, true);
    }

    [Fact]
    public async Task Liste_en_yeni_once_doner_ve_sayfalanir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using (var db = _ortam.Baglam()) await db.OturumKayitlari.ExecuteDeleteAsync();

        var servis = Servis();
        for (var i = 0; i < 5; i++)
        {
            await servis.KaydetAsync(1, $"kullanici{i}", OturumOlayi.Giris, i % 2 == 0);
            await Task.Delay(5);
        }

        var sonuc = await Servis().ListeAsync(new OturumKaydiSuzgeci { Boyut = 3 });

        Assert.Equal(5, sonuc.Toplam);
        Assert.Equal(3, sonuc.Veriler.Count);
        Assert.True(sonuc.SonrakiVar);
        // En yeni önce.
        Assert.Equal("kullanici4", sonuc.Veriler[0].KullaniciAdi);
    }

    [Fact]
    public async Task Yalnizca_basarisizlar_suzulebilir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using (var db = _ortam.Baglam()) await db.OturumKayitlari.ExecuteDeleteAsync();

        var servis = Servis();
        await servis.KaydetAsync(1, "a", OturumOlayi.Giris, true);
        await servis.KaydetAsync(1, "b", OturumOlayi.Giris, false, "Parola hatalı");
        await servis.KaydetAsync(1, "c", OturumOlayi.Giris, false, "Hesap kilitli");

        var sonuc = await Servis().ListeAsync(new OturumKaydiSuzgeci { Basarili = false });

        Assert.Equal(2, sonuc.Toplam);
        Assert.All(sonuc.Veriler, k => Assert.False(k.Basarili));
    }

    // ═══════════════════════════════════════════════════ süzgeçler

    [Fact]
    public async Task Ip_adresi_ON_EK_olarak_suzulur()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using (var db = _ortam.Baglam()) await db.OturumKayitlari.ExecuteDeleteAsync();

        await Servis(BaglamKur(ip: "192.168.1.10")).KaydetAsync(1, "ic1", OturumOlayi.Giris, true);
        await Servis(BaglamKur(ip: "192.168.1.44")).KaydetAsync(1, "ic2", OturumOlayi.Giris, true);
        await Servis(BaglamKur(ip: "10.0.0.5")).KaydetAsync(1, "dis", OturumOlayi.Giris, true);

        // "Bu ağ bloğundan kimler girdi" en sık sorulan denetim sorusu;
        // tam eşleşme zorunlu olsaydı 254 sorgu atmak gerekirdi.
        var blok = await Servis().ListeAsync(new OturumKaydiSuzgeci { IpAdresi = "192.168.1." });
        Assert.Equal(2, blok.Toplam);
        Assert.All(blok.Veriler, k => Assert.StartsWith("192.168.1.", k.IpAdresi!));

        var tam = await Servis().ListeAsync(new OturumKaydiSuzgeci { IpAdresi = "10.0.0.5" });
        Assert.Equal(1, tam.Toplam);
        Assert.Equal("dis", tam.Veriler[0].KullaniciAdi);
    }

    [Fact]
    public async Task Kullaniciya_gore_suzulur()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using (var db = _ortam.Baglam()) await db.OturumKayitlari.ExecuteDeleteAsync();

        var servis = Servis();
        await servis.KaydetAsync(1, "birinci", OturumOlayi.Giris, true);
        await servis.KaydetAsync(1, "birinci", OturumOlayi.Cikis, true);
        await servis.KaydetAsync(2, "ikinci", OturumOlayi.Giris, true);

        var sonuc = await Servis().ListeAsync(new OturumKaydiSuzgeci { KullaniciId = 1 });

        Assert.Equal(2, sonuc.Toplam);
        Assert.All(sonuc.Veriler, k => Assert.Equal(1, k.KullaniciId));
    }

    [Fact]
    public async Task Tarih_araligina_gore_suzulur()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using (var db = _ortam.Baglam()) await db.OturumKayitlari.ExecuteDeleteAsync();

        await Servis().KaydetAsync(1, "bugun", OturumOlayi.Giris, true);

        // Damgayı geriye çekmek, testin "dün" ve "bugün" ayrımını gerçek
        // veriyle kurmasının tek yolu — KaydetAsync tarihi kendi koyuyor.
        using (var db = _ortam.Baglam())
        {
            await db.OturumKayitlari
                .Where(k => k.KullaniciAdi == "bugun")
                .ExecuteUpdateAsync(s => s.SetProperty(k => k.Tarih, DateTime.Now.AddDays(-10)));
        }

        await Servis().KaydetAsync(1, "yeni", OturumOlayi.Giris, true);

        var sonGunler = await Servis().ListeAsync(new OturumKaydiSuzgeci
        {
            Baslangic = DateTime.Now.Date.AddDays(-1),
        });
        Assert.Equal(1, sonGunler.Toplam);
        Assert.Equal("yeni", sonGunler.Veriler[0].KullaniciAdi);

        var eskiAralik = await Servis().ListeAsync(new OturumKaydiSuzgeci
        {
            Baslangic = DateTime.Now.Date.AddDays(-11),
            Bitis = DateTime.Now.Date.AddDays(-9),
        });
        Assert.Equal(1, eskiAralik.Toplam);
        Assert.Equal("bugun", eskiAralik.Veriler[0].KullaniciAdi);
    }

    [Fact]
    public async Task Suzgecler_BIRLIKTE_uygulanir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using (var db = _ortam.Baglam()) await db.OturumKayitlari.ExecuteDeleteAsync();

        await Servis(BaglamKur(ip: "192.168.1.10")).KaydetAsync(1, "a", OturumOlayi.Giris, false, "Parola");
        await Servis(BaglamKur(ip: "192.168.1.10")).KaydetAsync(2, "b", OturumOlayi.Giris, false, "Parola");
        await Servis(BaglamKur(ip: "10.0.0.5")).KaydetAsync(1, "c", OturumOlayi.Giris, false, "Parola");
        await Servis(BaglamKur(ip: "192.168.1.10")).KaydetAsync(1, "d", OturumOlayi.Giris, true);

        // Süzgeçler VE'lenmeli. Biri diğerini ezseydi denetim kaydı yanlış
        // güven verirdi: "bu IP'den başarısız deneme yok" derken olurdu.
        var sonuc = await Servis().ListeAsync(new OturumKaydiSuzgeci
        {
            IpAdresi = "192.168.1.",
            KullaniciId = 1,
            Basarili = false,
        });

        Assert.Equal(1, sonuc.Toplam);
        Assert.Equal("a", sonuc.Veriler[0].KullaniciAdi);
    }

    /// <summary>Sabit bir <see cref="HttpContext"/> döndüren yerine geçen.</summary>
    private sealed class SahteHttpErisimi(HttpContext? baglam) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = baglam;
    }
}

/// <summary>
/// Bildirim merkezinin tekilleştirme davranışı.
/// </summary>
[Collection("SeriPostgres")]
public class BildirimMerkeziTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    private async Task MesajlariKurAsync()
    {
        await _ortam.TemelVerileriKurAsync();

        using var db = _ortam.Baglam();
        await db.Messages.ExecuteDeleteAsync();

        var an = DateTime.Now;

        // AYNI bildirim, iki cihaz için iki satır — gerçek üretim davranışı.
        db.Messages.AddRange(
            new Application.Models.Message
            {
                UserId = 1, Token = "MOBIL", Title = "Yeni Etkinlik", Content = "Toplantı eklendi",
                MessageType = SendMessageType.PushNotification, CreatedAt = an, IsSuccess = true,
                Data = """{"entity":"Ajanda","id":42,"action":"OpenDetails"}""",
            },
            new Application.Models.Message
            {
                UserId = 1, Token = "WEB", Title = "Yeni Etkinlik", Content = "Toplantı eklendi",
                MessageType = SendMessageType.PushNotification, CreatedAt = an, IsSuccess = true,
                Data = """{"entity":"Ajanda","id":42,"action":"OpenDetails"}""",
            },
            // SMS satırı — bildirim merkezine GİRMEMELİ.
            new Application.Models.Message
            {
                UserId = 1, Token = "05551112233", Title = "SMS", Content = "Vatandaşa mesaj",
                MessageType = SendMessageType.SMS, CreatedAt = an, IsSuccess = true,
            });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Ayni_bildirim_iki_cihazda_TEK_satir_gorunur()
    {
        PostgresYoksaAtla();
        await MesajlariKurAsync();

        var sonuc = await new BildirimMerkeziServisi(_ortam.Baglam())
            .ListeAsync(1, new SayfaIstegi(), yalnizcaOkunmamis: false);

        // İki cihaz satırı tek bildirim; SMS hiç yok.
        Assert.Equal(1, sonuc.Toplam);
        Assert.Equal("Yeni Etkinlik", sonuc.Veriler[0].Baslik);
    }

    [Fact]
    public async Task Yonlendirme_bilgisi_sunucuda_cozulur()
    {
        PostgresYoksaAtla();
        await MesajlariKurAsync();

        var sonuc = await new BildirimMerkeziServisi(_ortam.Baglam())
            .ListeAsync(1, new SayfaIstegi(), yalnizcaOkunmamis: false);

        // İstemcinin ham JSON ayrıştırması gerekmesin diye sunucu çözüyor.
        var b = sonuc.Veriler[0];
        Assert.Equal("Ajanda", b.Varlik);
        Assert.Equal(42, b.VarlikId);
        Assert.Equal("OpenDetails", b.Eylem);
    }

    [Fact]
    public async Task Okundu_isaretlemek_TUM_cihaz_satirlarini_kapatir()
    {
        PostgresYoksaAtla();
        await MesajlariKurAsync();

        var servis = new BildirimMerkeziServisi(_ortam.Baglam());
        var liste = await servis.ListeAsync(1, new SayfaIstegi(), false);

        await new BildirimMerkeziServisi(_ortam.Baglam())
            .OkunduIsaretleAsync(1, liste.Veriler[0].Id);

        // Biri okunup diğeri okunmamış kalırsa rozet asla sıfırlanmaz.
        var sayi = await new BildirimMerkeziServisi(_ortam.Baglam()).OkunmamisSayisiAsync(1);
        Assert.Equal(0, sayi);
    }

    [Fact]
    public async Task Okunmamis_sayaci_cihaz_basina_KATLANMAZ()
    {
        PostgresYoksaAtla();
        await MesajlariKurAsync();

        // İki cihaz satırı var ama kullanıcı için bu TEK bir bildirim.
        var sayi = await new BildirimMerkeziServisi(_ortam.Baglam()).OkunmamisSayisiAsync(1);
        Assert.Equal(1, sayi);
    }
}
