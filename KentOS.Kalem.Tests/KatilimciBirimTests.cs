using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Dto.ViewModels;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Exceptions;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// KATILIMCI BİRİM ile GÖREBİLECEK KİŞİ — İKİ AYRI KAVRAM.
///
/// <para>
/// <b>Katılımcı birim</b> etkinliğe katılacak departmandır; kullanıcının kendi
/// seviyesindeki ve altındaki birimlerden seçilir.
/// <b>Görebilecek kişi</b> ise gizli etkinliği kimin görebileceğini belirler ve
/// yalnızca ekleyenin KENDİ biriminden seçilir.
/// </para>
///
/// <para>
/// Bir dönem ikisi birbirine bağlanmıştı: gizli bir toplantıya bir müdürlüğü
/// davet etmek, o müdürlükteki HERKESİ toplantının içeriğine ortak ediyordu.
/// Depodaki en hassas değişmez bu; kurallar burada tek tek kilitlenir.
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class KatilimciBirimTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;

    // SunucuTestOrtami: 1/2/3 → birim 1, 4 → birim 2.
    private const long Birim1Kullanici = 1;
    private const string Birim1KullaniciAdi = "ekleyen";
    private const long Birim2Kullanici = 4;
    private const string Birim2KullaniciAdi = "digerbirim";

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    private async Task TemizleAsync()
    {
        using var b = _ortam.Baglam();
        await b.Database.ExecuteSqlRawAsync(
            "TRUNCATE ajanda_katilimcilar, ajandalar RESTART IDENTITY CASCADE;");
        await b.Database.ExecuteSqlRawAsync("DELETE FROM birimler WHERE id > 2;");
        await _ortam.TemelVerileriKurAsync();
    }

    private async Task<long> EtkinlikYazAsync(
        long birimId,
        string kullaniciAdi,
        bool gizli,
        long? katilimciBirimId = null,
        long? gorebilecekKullaniciId = null)
    {
        using var db = _ortam.Baglam();

        var a = new Ajanda
        {
            Baslik = gizli ? "Gizli toplantı" : "Açık toplantı",
            BaslangicTarihi = DateTime.Now.AddDays(3),
            BitisTarihi = DateTime.Now.AddDays(3).AddMinutes(30),
            OlusturmaTarihi = DateTime.Now,
            BirimId = birimId,
            KullaniciId = kullaniciAdi,
            Gizli = gizli,
            // Şemada NOT NULL — entity isteğe bağlı ilan etse de sütun zorunlu.
            RandevuTipId = 1,
            DurumId = 1,
        };

        if (katilimciBirimId != null)
        {
            a.Katilimcilar.Add(new AjandaKatilimci { BirimId = katilimciBirimId });
        }

        if (gorebilecekKullaniciId != null)
        {
            a.Katilimcilar.Add(new AjandaKatilimci { KullaniciId = gorebilecekKullaniciId });
        }

        db.Ajandalar.Add(a);
        await db.SaveChangesAsync();
        return a.Id;
    }

    private async Task<List<long>> ErisilebilirIdlerAsync(
        long kullaniciId, string kullaniciAdi, long birimId)
    {
        using var db = _ortam.Baglam();
        return await db.Ajandalar
            .AsNoTracking()
            .ErisilebilirOlanlar(kullaniciId, kullaniciAdi, birimId, yalnizcaBasin: false)
            .Select(a => a.Id)
            .ToListAsync();
    }

    // ═══════════════════════════════════════════ katılımcı birim (davet)

    [Fact]
    public async Task Cagrilan_BIRIM_acik_etkinligi_gorur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: false,
            katilimciBirimId: 2);

        // Birim süzgecini tek başına bırakmak, davetin hiçbir işe yaramaması
        // demekti: çağırabiliyorsun ama çağrılan göremiyor.
        var gorunen = await ErisilebilirIdlerAsync(
            Birim2Kullanici, Birim2KullaniciAdi, birimId: 2);

        Assert.Contains(id, gorunen);
    }

    [Fact]
    public async Task Cagrilmayan_birim_GORMEZ()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: false);

        var gorunen = await ErisilebilirIdlerAsync(
            Birim2Kullanici, Birim2KullaniciAdi, birimId: 2);

        Assert.DoesNotContain(id, gorunen);
    }

    /// <summary>
    /// ÇAĞRILAN BİRİM, ETKİNLİĞİ HER OKUMA YOLUNDAN GÖRÜR.
    /// </summary>
    /// <remarks>
    /// Sorgu uzantısı (<c>BirimKapsami</c>) doğruydu ama SERVİSİN üç metodu onu
    /// hiç çağırmıyor, kendi <c>a.BirimId == birimId</c> süzgecini yazıyordu:
    /// gün listesi (mobil ajanda sekmesi), ay sayaçları ve "bugünden itibaren".
    /// Sonuç: davet edilen birim etkinliği ARAMADA ve TAKVİMDE görüyor, kendi
    /// ajandasında göremiyordu. Kullanıcının tarifi buydu — "katılımcı olduğum
    /// etkinlik ajandamda çıkmıyor".
    ///
    /// Bu test uzantıyı değil SERVİSİ çağırır; kapsamı unutan yeni bir okuma
    /// yolu buradan kırmızıya döner.
    /// </remarks>
    [Fact]
    public async Task Cagrilan_birim_TUM_OKUMA_YOLLARINDA_gorur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var tarih = DateTime.Now.AddDays(3);
        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: false,
            katilimciBirimId: 2);

        using var baglam = _ortam.Baglam();
        var kullanici = new SahteKullaniciServisi(Birim2Kullanici, Birim2KullaniciAdi, 2);
        var (servis, _, _) = TestServisFabrikasi.Kur(baglam, kullanici, _ortam.Mapper);

        // 1) Gün listesi — mobilin ajanda sekmesi burayı okuyor.
        var gun = await servis.GetByDateAsync(
            new AjandaDateSearchDto { Date = tarih.Date });
        Assert.Contains(gun, a => a.Id == id);

        // 2) Ay sayaçları — takvimdeki nokta gün listesiyle aynı kümeyi saymalı.
        var sayilar = await servis.GetCountByDayAsync(tarih.Month, tarih.Year);
        Assert.True((sayilar.FirstOrDefault(s => s.Day == tarih.Day)?.Count ?? 0) > 0);

        // 3) Bugünden itibaren.
        var bugunden = await servis.GetAllFromTodayAsync();
        Assert.Contains(bugunden, a => a.Id == id);

        // 4) Arama ve detay zaten kapsamdaydı; birlikte kilitleniyor.
        var arama = await servis.SearchAsync(new AjandaSearchParametersDto());
        Assert.Contains(arama, a => a.Id == id);
        Assert.Equal(id, (await servis.GetAsync(id)).Id);
    }

    // ═══════════════════════════════════════ gizlilik: davet ≠ görme izni

    [Fact]
    public async Task GIZLI_etkinlikte_cagrilan_birim_GORMEZ()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        // Toplantıya çağrılmak, toplantının İÇERİĞİNİ görmek demek değil.
        // Gizli etkinliğin görünürlüğü ayrı bir kişi listesinden gelir.
        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: true,
            katilimciBirimId: 2);

        var gorunen = await ErisilebilirIdlerAsync(
            Birim2Kullanici, Birim2KullaniciAdi, birimId: 2);

        Assert.DoesNotContain(id, gorunen);
    }

    [Fact]
    public async Task GOREBILECEK_kisi_gizli_etkinligi_gorur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: true,
            gorebilecekKullaniciId: 2);

        var gorunen = await ErisilebilirIdlerAsync(2, "katilimci", birimId: 1);

        Assert.Contains(id, gorunen);
    }

    [Fact]
    public async Task Ayni_birimden_bile_olsa_listede_olmayan_GORMEZ()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: true,
            gorebilecekKullaniciId: 2);

        // Rol ayrıcalığı YOK; birim ortaklığı da gizliliği delmez.
        var gorunen = await ErisilebilirIdlerAsync(3, "yabanci", birimId: 1);

        Assert.DoesNotContain(id, gorunen);
    }

    [Fact]
    public async Task Olusturan_kendi_gizli_etkinligini_her_zaman_gorur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        // Görebilecek kimse seçilmemiş: yalnızca oluşturan görür. Bu geçerli
        // bir durum — kişinin kendine ait gizli kaydı.
        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: true);

        var gorunen = await ErisilebilirIdlerAsync(
            Birim1Kullanici, Birim1KullaniciAdi, birimId: 1);

        Assert.Contains(id, gorunen);
    }

    // ═══════════════════════════════════════════════ bildirim alıcıları

    [Fact]
    public async Task GIZLI_bildirimi_katilimci_BIRIME_gitmez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        // Katılımcı birim 2 (içinde 4 numaralı kullanıcı var),
        // görebilecek kişi 2 numaralı kullanıcı.
        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: true,
            katilimciBirimId: 2, gorebilecekKullaniciId: 2);

        using var db = _ortam.Baglam();
        var alicilar = await db.GizliAliciIdleriAsync(id, Birim1KullaniciAdi);

        // Bildirim metni de bir sızıntı yüzeyi: göremeyecek birine etkinliğin
        // BAŞLIĞINI göndermek, gizliliği bildirim üzerinden delmek demek.
        Assert.DoesNotContain(Birim2Kullanici, alicilar);

        // Alıcılar görünürlük kuralının birebir karşılığı olmalı.
        Assert.Contains(2L, alicilar);
        Assert.Contains(Birim1Kullanici, alicilar);
    }

    // ══════════════════════════════════════════════ eşitleme ve kaynak

    [Fact]
    public async Task Birim_katilimcisi_GIZLILIKTEN_bagimsiz_tutulur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: false,
            katilimciBirimId: 2);

        using var db = _ortam.Baglam();

        // Açık bir toplantının davetlilerini kaydedememek anlamsızdı.
        await db.KatilimcilariEsitleAsync(
            id, gizli: false, birimIdler: [2], kullaniciIdler: null,
            kullanicininBirimId: 1);
        await db.SaveChangesAsync();

        using var kontrol = _ortam.Baglam();
        var kalanlar = await kontrol.AjandaKatilimcilar
            .Where(k => k.AjandaId == id)
            .ToListAsync();

        Assert.Single(kalanlar);
        Assert.Equal(2, kalanlar[0].BirimId);
    }

    [Fact]
    public async Task Gizlilik_kapaninca_GOREBILECEKLER_temizlenir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: true,
            katilimciBirimId: 2, gorebilecekKullaniciId: 3);

        using var db = _ortam.Baglam();
        await db.KatilimcilariEsitleAsync(
            id, gizli: false, birimIdler: [2], kullaniciIdler: null,
            kullanicininBirimId: 1);
        await db.SaveChangesAsync();

        using var kontrol = _ortam.Baglam();
        var kalanlar = await kontrol.AjandaKatilimcilar
            .Where(k => k.AjandaId == id)
            .ToListAsync();

        // Görebilecek kişi listesinin TEK işlevi gizli etkinliği görünür
        // kılmak; gizlilik kapanınca anlamı kalmıyor. Davet listesi kalır.
        Assert.Single(kalanlar);
        Assert.Null(kalanlar[0].KullaniciId);
        Assert.Equal(2, kalanlar[0].BirimId);
    }

    [Fact]
    public async Task Null_liste_DOKUNMAZ()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: true,
            katilimciBirimId: 2);

        using var db = _ortam.Baglam();
        await db.KatilimcilariEsitleAsync(
            id, gizli: true, birimIdler: null, kullaniciIdler: null,
            kullanicininBirimId: 1);
        await db.SaveChangesAsync();

        using var kontrol = _ortam.Baglam();
        Assert.Single(await kontrol.AjandaKatilimcilar.Where(k => k.AjandaId == id).ToListAsync());
    }

    [Fact]
    public async Task UST_seviye_birim_katilimci_olarak_EKLENEMEZ()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        using (var kur = _ortam.Baglam())
        {
            // Seviye 0 = üst; test birimleri varsayılan seviyede (0) olduğu
            // için hiyerarşiyi burada kuruyoruz.
            await kur.Database.ExecuteSqlRawAsync(
                "UPDATE birimler SET level = 1 WHERE id IN (1,2);");
            kur.Birimler.Add(new Birim
            {
                Id = 9, Ad = "Başkan Yardımcısı", Yetkili = "Üst", Unvan = "Bşk. Yrd.", Level = 0,
            });
            await kur.SaveChangesAsync();
        }

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: false);

        using var db = _ortam.Baglam();

        // Bir müdürlük başkan yardımcısını kendi toplantısına çağıramaz;
        // o davet yukarıdan gelir. Denetim arayüzdeydi, artık sunucuda.
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            db.KatilimcilariEsitleAsync(
                id, gizli: false, birimIdler: [9], kullaniciIdler: null,
                kullanicininBirimId: 1));
    }

    [Fact]
    public async Task BASKA_birimden_kisi_gorebilecek_olarak_EKLENEMEZ()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: true);

        using var db = _ortam.Baglam();

        // 4 numaralı kullanıcı 2. birimde. Elle kurulmuş bir istek gizli
        // etkinliği kurumdaki herhangi birine açabilirdi.
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            db.KatilimcilariEsitleAsync(
                id, gizli: true, birimIdler: null, kullaniciIdler: [Birim2Kullanici],
                kullanicininBirimId: 1));
    }

    [Fact]
    public async Task Kayitta_ZATEN_duran_satir_denetimden_gecer()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: true,
            gorebilecekKullaniciId: Birim2Kullanici);

        using var db = _ortam.Baglam();

        // Kullanıcının birimi ya da bir birimin hiyerarşideki yeri sonradan
        // değişince eski bir etkinliği açıp kaydetmek imkânsız hâle gelirdi.
        await db.KatilimcilariEsitleAsync(
            id, gizli: true, birimIdler: null, kullaniciIdler: [Birim2Kullanici],
            kullanicininBirimId: 1);
        await db.SaveChangesAsync();

        using var kontrol = _ortam.Baglam();
        Assert.Single(await kontrol.AjandaKatilimcilar.Where(k => k.AjandaId == id).ToListAsync());
    }

    // ═══════════════════════════════ çağrılan birimin YAPABİLDİKLERİ

    /// <summary>
    /// Çağrılan birim etkinliği KENDİ AJANDA LİSTESİNDE de görür.
    /// </summary>
    /// <remarks>
    /// Takvim (<c>ErisilebilirOlanlar</c>) çağrılan birimi gösterirken ajanda
    /// LİSTESİ göstermiyordu: liste sorguları yalnızca <c>BirimId == birimId</c>
    /// diyordu ve aynı kullanıcı iki ekranda iki farklı küme görüyordu.
    /// </remarks>
    [Fact]
    public async Task Cagrilan_birim_etkinligi_LISTEDE_gorur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: false,
            katilimciBirimId: 2);

        using var db = _ortam.Baglam();
        var idler = await db.Ajandalar
            .AsNoTracking()
            .BirimKapsami(2)
            .Select(a => a.Id)
            .ToListAsync();

        Assert.Contains(id, idler);
    }

    /// <summary>Çağrılmayan birim listede DE göremez.</summary>
    [Fact]
    public async Task Cagrilmayan_birim_listede_goremez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: false);

        using var db = _ortam.Baglam();
        var idler = await db.Ajandalar.AsNoTracking().BirimKapsami(2)
            .Select(a => a.Id).ToListAsync();

        Assert.DoesNotContain(id, idler);
    }

    /// <summary>
    /// Çağrılan birim etkinliği DÜZENLEYEMEZ ve SİLEMEZ.
    /// </summary>
    /// <remarks>
    /// Görme kapısı (<c>BirimKapsami</c>) genişledi; yazma kapısı genişlemedi.
    /// İkisi karışsaydı davet edilen müdürlük başkanlık toplantısının saatini
    /// değiştirebilirdi.
    /// </remarks>
    [Fact]
    public async Task Cagrilan_birim_DUZENLEYEMEZ_ve_SILEMEZ()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: false,
            katilimciBirimId: 2);

        using var db = _ortam.Baglam();
        var cagrilan = new SahteKullaniciServisi(Birim2Kullanici, Birim2KullaniciAdi, 2);
        var (ajanda, _, _) = TestServisFabrikasi.Kur(db, cagrilan, _ortam.Mapper);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => ajanda.DeleteAsync(id));

        var dto = await db.Ajandalar.AsNoTracking().FirstAsync(a => a.Id == id);
        Assert.False(dto.IsDeleted);
    }

    /// <summary>
    /// Çağrılan birim NOT EKLEYEBİLİR.
    /// </summary>
    /// <remarks>
    /// Not eklemek kaydı değiştirmiyor; davet edilen müdürlüğün "geleceğiz /
    /// şu kişi katılacak" diye yazması akışın parçası. Kapı sahibi birime
    /// bağlıyken bu mümkün değildi.
    /// </remarks>
    [Fact]
    public async Task Cagrilan_birim_NOT_EKLEYEBILIR()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: false,
            katilimciBirimId: 2);

        using var db = _ortam.Baglam();
        var cagrilan = new SahteKullaniciServisi(Birim2Kullanici, Birim2KullaniciAdi, 2);
        var (ajanda, _, _) = TestServisFabrikasi.Kur(db, cagrilan, _ortam.Mapper);

        var sonuc = await ajanda.CreateNoteAsync(new AjandaNotDto
        {
            AjandaId = id,
            Not = "Müdürlüğümüzden iki kişi katılacaktır.",
        });

        Assert.True(sonuc);
        Assert.Equal(1, await db.AjandaNotlar.CountAsync(n => n.AjandaId == id));
    }

    /// <summary>
    /// Çağrılmayan birim not EKLEYEMEZ.
    /// </summary>
    [Fact]
    public async Task Cagrilmayan_birim_not_ekleyemez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: false);

        using var db = _ortam.Baglam();
        var yabanci = new SahteKullaniciServisi(Birim2Kullanici, Birim2KullaniciAdi, 2);
        var (ajanda, _, _) = TestServisFabrikasi.Kur(db, yabanci, _ortam.Mapper);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ajanda.CreateNoteAsync(new AjandaNotDto { AjandaId = id, Not = "olmaz" }));
    }

    /// <summary>
    /// GİZLİ etkinlik çağrılan birime AÇILMAZ — not da ekleyemez.
    /// </summary>
    /// <remarks>
    /// Görme kapısı genişledi ama gizlilik onun üstünde: iki koşul VE ile
    /// bağlı. Davet etmek ile "içeriği görebilsin" demek aynı şey değil.
    /// </remarks>
    [Fact]
    public async Task Gizli_etkinlik_cagrilan_birime_ACILMAZ()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync(
            birimId: 1, kullaniciAdi: Birim1KullaniciAdi, gizli: true,
            katilimciBirimId: 2);

        using var db = _ortam.Baglam();
        var cagrilan = new SahteKullaniciServisi(Birim2Kullanici, Birim2KullaniciAdi, 2);
        var (ajanda, _, _) = TestServisFabrikasi.Kur(db, cagrilan, _ortam.Mapper);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ajanda.CreateNoteAsync(new AjandaNotDto { AjandaId = id, Not = "olmaz" }));
    }
}
