using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Mini.Application.Dto.V2.IsTakip;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Services.V2;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// GÖREV AKIŞI — kanıt zinciri ve onay kapısı.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="GorevDurumAkisiTests"/> geçiş TABLOSUNU kilitliyor; buradaki
/// testler tablonun servis içinde gerçekten UYGULANDIĞINI ve aşama
/// kurallarının veritabanı üzerinde işlediğini kilitler. İkisi ayrı: doğru
/// bir tabloyu hiç sormayan bir servis de testten geçerdi.
/// </para>
/// <para>
/// Kilitlenen şey kurumun iş kuralı: <b>zorunlu aşama atlanamaz</b>,
/// <b>fotoğraf zorunluysa fotoğrafsız tamamlanmaz</b>, <b>onaysız
/// tamamlanmaz</b>.
/// </para>
/// </remarks>
[Collection(SunucuKoleksiyonu.Ad)]
public class GorevAkisTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;
    private readonly SahteMesajServisi _mesajlar = new();

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    // ── kurulum ────────────────────────────────────────────────────────

    /// <summary>
    /// Üç aşamalı bir görev tipi kurar.
    /// </summary>
    /// <remarks>
    /// 1. Keşif — zorunlu, açıklama zorunlu<br/>
    /// 2. Uygulama — zorunlu, FOTOĞRAF zorunlu<br/>
    /// 3. Bilgilendirme — zorunlu DEĞİL (atlanabilir olmalı)
    /// </remarks>
    private async Task<long> TipKurAsync(int slaSaat = 0)
    {
        using var b = _ortam.Baglam();
        await _ortam.TemelVerileriKurAsync();

        var tip = new TaskType
        {
            Ad = "Yol Onarımı " + Guid.NewGuid().ToString("N")[..6],
            SlaSaat = slaSaat > 0 ? slaSaat : null,
            HizmetStandardiGun = 5,
            Kullanimda = true,
            BirimId = 1,
        };

        b.GorevTipleri.Add(tip);
        await b.SaveChangesAsync();

        b.GorevTipiAsamalari.AddRange(
            new TaskTypeStage
            {
                GorevTipiId = tip.Id, SiraNo = 1, Ad = "Keşif",
                Zorunlu = true, AciklamaZorunlu = true,
            },
            new TaskTypeStage
            {
                GorevTipiId = tip.Id, SiraNo = 2, Ad = "Uygulama",
                Zorunlu = true, FotografZorunlu = true,
            },
            new TaskTypeStage
            {
                GorevTipiId = tip.Id, SiraNo = 3, Ad = "Bilgilendirme",
                Zorunlu = false,
            });

        await b.SaveChangesAsync();
        return tip.Id;
    }

    /// <summary>Servisi tek bir DbContext üzerinde kurar (üretimdeki kapsam gibi).</summary>
    private (IGorevServisi Servis, AppDbContext Baglam) Kur(long birimId = 1)
    {
        var baglam = _ortam.Baglam();
        var kullanici = new SahteKullaniciServisi(1, "test", birimId);

        var etkin = new EtkinBirim(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            kullanici,
            new BirimAgaci(_ortam.Baglam(), new MemoryCache(new MemoryCacheOptions())),
            new HerSeyeIzinli());

        var olaylar = new IsOlayServisi(baglam, kullanici, etkin,
            NullLogger<IsOlayServisi>.Instance);

        var ekler = new IsEkServisi(baglam, new TestServisFabrikasi.SahteDepo(), kullanici,
            NullLogger<IsEkServisi>.Instance);

        var servis = new GorevServisi(
            baglam, kullanici, etkin, olaylar, ekler,
            new IsYorumServisi(baglam, kullanici),
            new EkipServisi(baglam, etkin),
            _mesajlar,
            TestKapsayici.Bos,
            NullLogger<GorevServisi>.Instance);

        return (servis, baglam);
    }

    private static GorevKayitDto Kayit(long tipId) => new()
    {
        Baslik = "Kaldırım çöktü",
        GorevTipiId = tipId,
        Kaynak = GorevKaynagi.Manuel,
        Atamalar = [new GorevAtamaIstegiDto { KullaniciId = 2 }],
    };

    /// <summary>Görevi açar, atar ve BAŞLATIR — aşama testlerinin başlangıcı.</summary>
    private async Task<(IGorevServisi Servis, AppDbContext Baglam, GorevDetayDto Gorev)>
        BaslatilmisGorevAsync(int slaSaat = 0)
    {
        var tipId = await TipKurAsync(slaSaat);
        var (servis, baglam) = Kur();

        var gorev = await servis.OlusturAsync(Kayit(tipId));

        gorev = await servis.DurumDegistirAsync(gorev.Id,
            new GorevDurumIstegiDto { Durum = GorevDurumu.Basladi });

        return (servis, baglam, gorev);
    }

    // ── aşama kopyalama ────────────────────────────────────────────────

    /// <summary>
    /// Görev aşamalarını TİPTEN devralır.
    /// </summary>
    /// <remarks>
    /// Kullanıcının tarifi: "görev tipine göre aşamaları otomatik gelecek".
    /// Kopya, bağ değil — tipe sonradan adım eklenirse tamamlanmış görevler
    /// eksik görünmemeli.
    /// </remarks>
    [Fact]
    public async Task Gorev_asamalari_tipten_KOPYALANIR()
    {
        PostgresYoksaAtla();

        var tipId = await TipKurAsync();
        var (servis, _) = Kur();

        var gorev = await servis.OlusturAsync(Kayit(tipId));

        Assert.Equal(3, gorev.Asamalar.Count);
        Assert.Equal(["Keşif", "Uygulama", "Bilgilendirme"], gorev.Asamalar.Select(a => a.Ad));
        Assert.True(gorev.Asamalar[0].Sirada);
        Assert.All(gorev.Asamalar, a => Assert.Equal(GorevAsamaDurumu.Bekliyor, a.Durum));
    }

    /// <summary>Tipe SONRADAN aşama eklemek açılmış görevi değiştirmez.</summary>
    [Fact]
    public async Task Tipe_sonradan_eklenen_asama_ACIK_goreve_islemez()
    {
        PostgresYoksaAtla();

        var tipId = await TipKurAsync();
        var (servis, _) = Kur();
        var gorev = await servis.OlusturAsync(Kayit(tipId));

        using (var b = _ortam.Baglam())
        {
            b.GorevTipiAsamalari.Add(new TaskTypeStage
            {
                GorevTipiId = tipId, SiraNo = 4, Ad = "Sonradan eklendi", Zorunlu = true,
            });
            await b.SaveChangesAsync();
        }

        var tekrar = await servis.GetirAsync(gorev.Id);

        Assert.Equal(3, tekrar.Asamalar.Count);
        Assert.DoesNotContain(tekrar.Asamalar, a => a.Ad == "Sonradan eklendi");
    }

    // ── aşama sırası ───────────────────────────────────────────────────

    /// <summary>
    /// SIRA ATLANAMAZ.
    /// </summary>
    /// <remarks>
    /// Aşamalar bir işin nasıl yapıldığını anlatıyor; üçüncü adımı ikinciden
    /// önce işaretlemek kanıtı gerçek sıradan koparır.
    /// </remarks>
    [Fact]
    public async Task Sira_ATLANAMAZ()
    {
        PostgresYoksaAtla();

        var (servis, _, gorev) = await BaslatilmisGorevAsync();
        var ikinci = gorev.Asamalar[1];

        var hata = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.AsamaTamamlaAsync(gorev.Id, ikinci.Id, new GorevAsamaIstegiDto()));

        Assert.Contains("Keşif", hata.Message);
    }

    /// <summary>ZORUNLU aşama atlanamaz — kural yazılmış ama uygulanmıyorsa hiç yazılmamış demektir.</summary>
    [Fact]
    public async Task Zorunlu_asama_ATLANAMAZ()
    {
        PostgresYoksaAtla();

        var (servis, _, gorev) = await BaslatilmisGorevAsync();
        var ilk = gorev.Asamalar[0];

        var hata = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.AsamaTamamlaAsync(gorev.Id, ilk.Id,
                new GorevAsamaIstegiDto { Atla = true, Not = "gerek yok" }));

        Assert.Contains("atlanamaz", hata.Message);
    }

    /// <summary>Açıklama zorunluysa notsuz tamamlanmaz.</summary>
    [Fact]
    public async Task Aciklama_zorunluysa_NOTSUZ_tamamlanmaz()
    {
        PostgresYoksaAtla();

        var (servis, _, gorev) = await BaslatilmisGorevAsync();
        var ilk = gorev.Asamalar[0];

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.AsamaTamamlaAsync(gorev.Id, ilk.Id, new GorevAsamaIstegiDto { Not = "   " }));

        var sonra = await servis.AsamaTamamlaAsync(gorev.Id, ilk.Id,
            new GorevAsamaIstegiDto { Not = "Keşif yapıldı." });

        Assert.Equal(GorevAsamaDurumu.Tamamlandi, sonra.Asamalar[0].Durum);
    }

    /// <summary>
    /// FOTOĞRAF ZORUNLUYSA FOTOĞRAFSIZ TAMAMLANMAZ.
    /// </summary>
    /// <remarks>
    /// Sahada yapıldığı iddia edilen işin kanıtı bu. Kanıtsız kapatılabilseydi
    /// "fotoğraf zorunlu" ayarı yalnızca bir temenni olurdu.
    /// </remarks>
    [Fact]
    public async Task Fotograf_zorunluysa_FOTOGRAFSIZ_tamamlanmaz()
    {
        PostgresYoksaAtla();

        var (servis, baglam, gorev) = await BaslatilmisGorevAsync();

        await servis.AsamaTamamlaAsync(gorev.Id, gorev.Asamalar[0].Id,
            new GorevAsamaIstegiDto { Not = "Keşif yapıldı." });

        var uygulama = gorev.Asamalar[1];

        var hata = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.AsamaTamamlaAsync(gorev.Id, uygulama.Id,
                new GorevAsamaIstegiDto { Not = "Bitti." }));

        Assert.Contains("fotoğraf", hata.Message.ToLowerInvariant());

        // Fotoğraf yüklenince aynı çağrı geçmeli.
        baglam.IsEkleri.Add(new WorkAttachment
        {
            VarlikTuru = IsVarligi.GorevAsama,
            VarlikId = uygulama.Id,
            Ad = "kanit.jpg",
            DosyaYolu = "uploads/is/kanit.jpg",
            IcerikTuru = "image/jpeg",
            Boyut = 10,
        });
        await baglam.SaveChangesAsync();

        var sonra = await servis.AsamaTamamlaAsync(gorev.Id, uygulama.Id,
            new GorevAsamaIstegiDto { Not = "Bitti." });

        Assert.Equal(GorevAsamaDurumu.Tamamlandi, sonra.Asamalar[1].Durum);
    }

    /// <summary>Zorunlu OLMAYAN aşama atlanabilir.</summary>
    [Fact]
    public async Task Zorunlu_olmayan_asama_atlanabilir()
    {
        PostgresYoksaAtla();

        var (servis, baglam, gorev) = await BaslatilmisGorevAsync();
        await TumZorunluAsamalariGecAsync(servis, baglam, gorev);

        var son = await servis.GetirAsync(gorev.Id);
        var sonuncu = son.Asamalar[2];

        var sonra = await servis.AsamaTamamlaAsync(gorev.Id, sonuncu.Id,
            new GorevAsamaIstegiDto { Atla = true });

        Assert.Equal(GorevAsamaDurumu.Atlandi, sonra.Asamalar[2].Durum);
    }

    /// <summary>Kapatılmış aşama İKİNCİ KEZ kapatılamaz.</summary>
    [Fact]
    public async Task Kapatilmis_asama_tekrar_kapatilamaz()
    {
        PostgresYoksaAtla();

        var (servis, _, gorev) = await BaslatilmisGorevAsync();
        var ilk = gorev.Asamalar[0];

        await servis.AsamaTamamlaAsync(gorev.Id, ilk.Id, new GorevAsamaIstegiDto { Not = "Bitti." });

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.AsamaTamamlaAsync(gorev.Id, ilk.Id, new GorevAsamaIstegiDto { Not = "Yine bitti." }));
    }

    /// <summary>Başlamamış görevde aşama ilerletilemez — önce sorumlusu olmalı.</summary>
    [Fact]
    public async Task Baslamamis_gorevde_asama_ilerletilemez()
    {
        PostgresYoksaAtla();

        var tipId = await TipKurAsync();
        var (servis, _) = Kur();
        var gorev = await servis.OlusturAsync(Kayit(tipId));

        var hata = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.AsamaTamamlaAsync(gorev.Id, gorev.Asamalar[0].Id,
                new GorevAsamaIstegiDto { Not = "Bitti." }));

        Assert.Contains("BAŞLATIN", hata.Message);
    }

    // ── onay kapısı ────────────────────────────────────────────────────

    /// <summary>
    /// ZORUNLU AŞAMALAR BİTMEDEN ONAYA GÖNDERİLEMEZ.
    /// </summary>
    [Fact]
    public async Task Zorunlu_asamalar_bitmeden_onaya_gonderilemez()
    {
        PostgresYoksaAtla();

        var (servis, _, gorev) = await BaslatilmisGorevAsync();

        var hata = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.DurumDegistirAsync(gorev.Id,
                new GorevDurumIstegiDto { Durum = GorevDurumu.TamamlanmaBekliyor }));

        Assert.Contains("Keşif", hata.Message);
        Assert.Contains("Uygulama", hata.Message);
    }

    /// <summary>
    /// ONAYSIZ TAMAMLANMA YOK — servis düzeyinde.
    /// </summary>
    /// <remarks>
    /// Personelin "bitirdim" beyanı ile kurumun kabulü aynı şey değil. Tek
    /// adımda tamamlanabilseydi, yapılmamış bir iş kimse bakmadan kapanırdı.
    /// </remarks>
    [Fact]
    public async Task Onaysiz_TAMAMLANMAZ()
    {
        PostgresYoksaAtla();

        var (servis, baglam, gorev) = await BaslatilmisGorevAsync();
        await TumZorunluAsamalariGecAsync(servis, baglam, gorev);

        // Aşamalar bitti ama görev hâlâ "devam ediyor": doğrudan tamamlanamaz.
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.DurumDegistirAsync(gorev.Id,
                new GorevDurumIstegiDto { Durum = GorevDurumu.Tamamlandi }));

        var onayda = await servis.DurumDegistirAsync(gorev.Id,
            new GorevDurumIstegiDto { Durum = GorevDurumu.TamamlanmaBekliyor });

        Assert.Equal(GorevDurumu.TamamlanmaBekliyor, onayda.Durum);
        Assert.Null(onayda.TamamlanmaTarihi);

        var tamam = await servis.DurumDegistirAsync(gorev.Id,
            new GorevDurumIstegiDto { Durum = GorevDurumu.Tamamlandi });

        Assert.Equal(GorevDurumu.Tamamlandi, tamam.Durum);
        Assert.NotNull(tamam.TamamlanmaTarihi);
        Assert.False(string.IsNullOrWhiteSpace(tamam.Onaylayan));
    }

    /// <summary>İADE gerekçesiz yapılamaz — personel neyi düzelteceğini bilmeli.</summary>
    [Fact]
    public async Task Iade_GEREKCESIZ_yapilamaz()
    {
        PostgresYoksaAtla();

        var (servis, baglam, gorev) = await BaslatilmisGorevAsync();
        await TumZorunluAsamalariGecAsync(servis, baglam, gorev);
        await servis.DurumDegistirAsync(gorev.Id,
            new GorevDurumIstegiDto { Durum = GorevDurumu.TamamlanmaBekliyor });

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.DurumDegistirAsync(gorev.Id,
                new GorevDurumIstegiDto { Durum = GorevDurumu.IadeEdildi }));

        var iade = await servis.DurumDegistirAsync(gorev.Id, new GorevDurumIstegiDto
        {
            Durum = GorevDurumu.IadeEdildi,
            Gerekce = "Kaldırım hâlâ çökük.",
        });

        Assert.Equal(GorevDurumu.IadeEdildi, iade.Durum);
        Assert.Equal("Kaldırım hâlâ çökük.", iade.Gerekce);
    }

    /// <summary>TAMAMLANAN görev YENİDEN AÇILMAZ ve düzenlenemez.</summary>
    [Fact]
    public async Task Tamamlanan_gorev_YENIDEN_ACILMAZ()
    {
        PostgresYoksaAtla();

        var (servis, baglam, gorev) = await BaslatilmisGorevAsync();
        await TumZorunluAsamalariGecAsync(servis, baglam, gorev);
        await servis.DurumDegistirAsync(gorev.Id,
            new GorevDurumIstegiDto { Durum = GorevDurumu.TamamlanmaBekliyor });
        var tamam = await servis.DurumDegistirAsync(gorev.Id,
            new GorevDurumIstegiDto { Durum = GorevDurumu.Tamamlandi });

        Assert.Empty(tamam.SonrakiDurumlar);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.DurumDegistirAsync(gorev.Id,
                new GorevDurumIstegiDto { Durum = GorevDurumu.DevamEdiyor }));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.GuncelleAsync(gorev.Id, new GorevKayitDto { Baslik = "Yeni başlık" }));
    }

    // ── atama ──────────────────────────────────────────────────────────

    /// <summary>Atama görevi <c>Yeni</c>den çıkarır ve atanana BİLDİRİR.</summary>
    [Fact]
    public async Task Atama_gorevi_Atandi_yapar_ve_bildirir()
    {
        PostgresYoksaAtla();

        var tipId = await TipKurAsync();
        var (servis, _) = Kur();

        _mesajlar.KisilereGidenler.Clear();

        var gorev = await servis.OlusturAsync(Kayit(tipId));

        Assert.Equal(GorevDurumu.Atandi, gorev.Durum);
        Assert.Single(gorev.Atamalar);
        Assert.Equal(2, gorev.Atamalar[0].KullaniciId);

        var bildirim = Assert.Single(_mesajlar.KisilereGidenler);
        Assert.Contains(2L, bildirim.KullaniciIdler);
        Assert.Contains(gorev.TakipNo, bildirim.Icerik);
    }

    /// <summary>İşlemi YAPANA kendi işlemi bildirilmez.</summary>
    [Fact]
    public async Task Kendine_atamada_bildirim_GITMEZ()
    {
        PostgresYoksaAtla();

        var tipId = await TipKurAsync();
        var (servis, _) = Kur();

        _mesajlar.KisilereGidenler.Clear();

        // Sahte kullanıcı 1 numaralı kişi; kendine atıyor.
        await servis.OlusturAsync(new GorevKayitDto
        {
            Baslik = "Kendi işim",
            GorevTipiId = tipId,
            Atamalar = [new GorevAtamaIstegiDto { KullaniciId = 1 }],
        });

        Assert.Empty(_mesajlar.KisilereGidenler);
    }

    /// <summary>Aynı atama İKİNCİ KEZ yazıldığında yeniden bildirilmez.</summary>
    [Fact]
    public async Task Degismeyen_atama_TEKRAR_bildirmez()
    {
        PostgresYoksaAtla();

        var tipId = await TipKurAsync();
        var (servis, _) = Kur();
        var gorev = await servis.OlusturAsync(Kayit(tipId));

        _mesajlar.KisilereGidenler.Clear();

        await servis.AtaAsync(gorev.Id, [new GorevAtamaIstegiDto { KullaniciId = 2 }]);

        Assert.Empty(_mesajlar.KisilereGidenler);
    }

    /// <summary>Boş atama reddedilir — kişi de ekip de yoksa atama değil.</summary>
    [Fact]
    public async Task Bos_atama_REDDEDILIR()
    {
        PostgresYoksaAtla();

        var tipId = await TipKurAsync();
        var (servis, _) = Kur();
        var gorev = await servis.OlusturAsync(Kayit(tipId));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.AtaAsync(gorev.Id, [new GorevAtamaIstegiDto()]));
    }

    // ── SLA ────────────────────────────────────────────────────────────

    /// <summary>
    /// SLA AÇILIŞTA DEĞİL BAŞLANGIÇTA damgalanır.
    /// </summary>
    /// <remarks>
    /// Atanmayı bekleyen bir görevin SLA'sını işletmek, henüz kimseye
    /// verilmemiş işi geciktirdi diye personele yazmak olurdu.
    /// </remarks>
    [Fact]
    public async Task SLA_baslangicta_damgalanir()
    {
        PostgresYoksaAtla();

        var tipId = await TipKurAsync(slaSaat: 48);
        var (servis, _) = Kur();

        var gorev = await servis.OlusturAsync(Kayit(tipId));
        Assert.Null(gorev.SlaBitis);

        var basladi = await servis.DurumDegistirAsync(gorev.Id,
            new GorevDurumIstegiDto { Durum = GorevDurumu.Basladi });

        Assert.NotNull(basladi.SlaBitis);
        Assert.InRange(basladi.KalanSaat!.Value, 47, 48.1);
    }

    /// <summary>
    /// BEKLEYEN İŞİN SLA'SI DURUR — bekleme süresi SLA bitişine EKLENİR.
    /// </summary>
    [Fact]
    public async Task Bekleme_suresi_SLA_bitisine_eklenir()
    {
        PostgresYoksaAtla();

        var (servis, baglam, gorev) = await BaslatilmisGorevAsync(slaSaat: 24);
        var ilkBitis = gorev.SlaBitis;
        Assert.NotNull(ilkBitis);

        await servis.DurumDegistirAsync(gorev.Id,
            new GorevDurumIstegiDto { Durum = GorevDurumu.Beklemede });

        // Gerçek zaman beklemek yerine damga geriye çekiliyor: testin 90
        // dakika sürmesi kabul edilemez ve `DateTime.Now` sahtelenemiyor.
        await baglam.Gorevler
            .Where(g => g.Id == gorev.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(
                g => g.BeklemeBaslangic, DateTime.Now.AddMinutes(-90)));

        // İzleyici TEMİZLENİYOR: `ExecuteUpdateAsync` doğrudan SQL çalıştırıyor
        // ve izlenen nesneyi haberdar etmiyor. Temizlenmezse servisin
        // `FirstOrDefaultAsync`'i kimlik çözümlemesi yüzünden ESKİ nesneyi
        // döndürür ve bekleme sıfır ölçülür. Üretimde sorun değil — her
        // istek kendi bağlamını alıyor.
        baglam.ChangeTracker.Clear();

        var devam = await servis.DurumDegistirAsync(gorev.Id,
            new GorevDurumIstegiDto { Durum = GorevDurumu.DevamEdiyor });

        Assert.InRange(devam.BeklemeDakika, 89, 91);

        var kayit = await baglam.Gorevler.AsNoTracking()
            .FirstAsync(g => g.Id == gorev.Id);

        Assert.Null(kayit.BeklemeBaslangic);
        Assert.InRange((kayit.SlaBitis!.Value - ilkBitis.Value).TotalMinutes, 89, 91);
    }

    /// <summary>Kapanmış görev GECİKMİŞ sayılmaz — ölçüm bitti.</summary>
    [Fact]
    public async Task Kapanan_gorev_GECIKMIS_sayilmaz()
    {
        PostgresYoksaAtla();

        var (servis, baglam, gorev) = await BaslatilmisGorevAsync(slaSaat: 1);

        await baglam.Gorevler
            .Where(g => g.Id == gorev.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(g => g.SlaBitis, DateTime.Now.AddHours(-5)));

        baglam.ChangeTracker.Clear();

        Assert.True((await servis.GetirAsync(gorev.Id)).Gecikti);

        await TumZorunluAsamalariGecAsync(servis, baglam, gorev);
        await servis.DurumDegistirAsync(gorev.Id,
            new GorevDurumIstegiDto { Durum = GorevDurumu.TamamlanmaBekliyor });
        var tamam = await servis.DurumDegistirAsync(gorev.Id,
            new GorevDurumIstegiDto { Durum = GorevDurumu.Tamamlandi });

        Assert.False(tamam.Gecikti);
        Assert.Null(tamam.KalanSaat);
    }

    // ── takip numarası ─────────────────────────────────────────────────

    /// <summary>Takip numarası TEKİL ve okunabilir.</summary>
    [Fact]
    public async Task Takip_numarasi_tekil_ve_okunabilir()
    {
        PostgresYoksaAtla();

        var tipId = await TipKurAsync();
        var (servis, _) = Kur();

        var bir = await servis.OlusturAsync(Kayit(tipId));
        var iki = await servis.OlusturAsync(Kayit(tipId));

        Assert.StartsWith($"GRV-{DateTime.Now.Year}-", bir.TakipNo);
        Assert.NotEqual(bir.TakipNo, iki.TakipNo);
    }

    // ── ağaç ───────────────────────────────────────────────────────────

    /// <summary>Alt görev üst görevin BİRİMİNİ devralır.</summary>
    [Fact]
    public async Task Alt_gorev_ust_gorevin_birimini_devralir()
    {
        PostgresYoksaAtla();

        var tipId = await TipKurAsync();
        var (servis, _) = Kur();
        var ust = await servis.OlusturAsync(Kayit(tipId));

        var alt = await servis.OlusturAsync(new GorevKayitDto
        {
            Baslik = "Malzeme temini",
            UstGorevId = ust.Id,
        });

        Assert.Equal(ust.Id, alt.UstGorevId);
        Assert.Equal(ust.BirimId, alt.BirimId);

        var tekrar = await servis.GetirAsync(ust.Id);
        Assert.Equal(1, tekrar.AltGorevSayisi);
        Assert.Single(tekrar.AltGorevler);
    }

    /// <summary>Görev silindiğinde ALT GÖREVLERİ de gider — sahipsiz kayıt kalmasın.</summary>
    [Fact]
    public async Task Gorev_silinince_alt_gorevleri_de_silinir()
    {
        PostgresYoksaAtla();

        var tipId = await TipKurAsync();
        var (servis, baglam) = Kur();
        var ust = await servis.OlusturAsync(Kayit(tipId));
        var alt = await servis.OlusturAsync(new GorevKayitDto
        {
            Baslik = "Alt iş",
            UstGorevId = ust.Id,
        });

        await servis.SilAsync(ust.Id);

        Assert.False(await baglam.Gorevler.AnyAsync(g => g.Id == ust.Id));
        Assert.False(await baglam.Gorevler.AnyAsync(g => g.Id == alt.Id));
        Assert.False(await baglam.GorevAsamalari.AnyAsync(a => a.GorevId == ust.Id));
        Assert.False(await baglam.IsOlaylari
            .AnyAsync(o => o.VarlikTuru == IsVarligi.Gorev && o.VarlikId == ust.Id));
    }

    // ── görünürlük ─────────────────────────────────────────────────────

    /// <summary>
    /// BAŞKA BİRİMİN görevi 403 DEĞİL 404 döner.
    /// </summary>
    /// <remarks>
    /// "Yetkiniz yok" demek, o kimlikte bir görev OLDUĞUNU söylemek olurdu.
    /// </remarks>
    [Fact]
    public async Task Baska_birimin_gorevi_BULUNAMADI_doner()
    {
        PostgresYoksaAtla();

        var tipId = await TipKurAsync();
        var (birinci, _) = Kur(birimId: 1);
        var gorev = await birinci.OlusturAsync(Kayit(tipId));

        var (ikinci, _) = Kur(birimId: 2);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => ikinci.GetirAsync(gorev.Id));
    }

    /// <summary>Liste yalnızca ETKİN BİRİMİN görevlerini verir.</summary>
    [Fact]
    public async Task Liste_yalnizca_kendi_biriminin_gorevlerini_verir()
    {
        PostgresYoksaAtla();

        var tipId = await TipKurAsync();
        var (birinci, _) = Kur(birimId: 1);
        var gorev = await birinci.OlusturAsync(Kayit(tipId));

        var (ikinci, _) = Kur(birimId: 2);
        var liste = await ikinci.ListeAsync(new GorevSuzgecDto());

        Assert.DoesNotContain(liste.Veriler, g => g.Id == gorev.Id);
        Assert.Contains((await birinci.ListeAsync(new GorevSuzgecDto())).Veriler,
            g => g.Id == gorev.Id);
    }

    // ── zaman çizelgesi ────────────────────────────────────────────────

    /// <summary>Her adım çizelgeye YAZILIR — "ne oldu, kim yaptı" sorulabilmeli.</summary>
    [Fact]
    public async Task Akisin_her_adimi_cizelgeye_yazilir()
    {
        PostgresYoksaAtla();

        var (servis, baglam, gorev) = await BaslatilmisGorevAsync();
        await TumZorunluAsamalariGecAsync(servis, baglam, gorev);
        await servis.DurumDegistirAsync(gorev.Id,
            new GorevDurumIstegiDto { Durum = GorevDurumu.TamamlanmaBekliyor });
        await servis.DurumDegistirAsync(gorev.Id,
            new GorevDurumIstegiDto { Durum = GorevDurumu.Tamamlandi });

        var olaylar = await servis.OlaylarAsync(gorev.Id);
        var tipler = olaylar.Select(o => o.Tip).ToList();

        Assert.Contains(GorevOlayTipi.Olusturuldu, tipler);
        Assert.Contains(GorevOlayTipi.Atandi, tipler);
        Assert.Contains(GorevOlayTipi.DurumDegisti, tipler);
        Assert.Contains(GorevOlayTipi.AsamaTamamlandi, tipler);
        Assert.Contains(GorevOlayTipi.TamamlanmayaGonderildi, tipler);
        Assert.Contains(GorevOlayTipi.Onaylandi, tipler);

        // Durum değişimi YAPISAL fark taşır, serbest metin değil.
        var onay = olaylar.First(o => o.Tip == GorevOlayTipi.Onaylandi);
        var fark = Assert.Single(onay.Degisiklikler);
        Assert.Equal("Durum", fark.Alan);
        Assert.Equal("Onay bekliyor", fark.Eski);
        Assert.Equal("Tamamlandı", fark.Yeni);
    }

    // ── yardımcılar ────────────────────────────────────────────────────

    /// <summary>İlk iki (zorunlu) aşamayı kanıtlarıyla geçer.</summary>
    private async Task TumZorunluAsamalariGecAsync(
        IGorevServisi servis, AppDbContext baglam, GorevDetayDto gorev)
    {
        await servis.AsamaTamamlaAsync(gorev.Id, gorev.Asamalar[0].Id,
            new GorevAsamaIstegiDto { Not = "Keşif yapıldı." });

        baglam.IsEkleri.Add(new WorkAttachment
        {
            VarlikTuru = IsVarligi.GorevAsama,
            VarlikId = gorev.Asamalar[1].Id,
            Ad = "kanit.jpg",
            DosyaYolu = $"uploads/is/{Guid.NewGuid():N}.jpg",
            IcerikTuru = "image/jpeg",
            Boyut = 10,
        });
        await baglam.SaveChangesAsync();

        await servis.AsamaTamamlaAsync(gorev.Id, gorev.Asamalar[1].Id,
            new GorevAsamaIstegiDto { Not = "Uygulandı." });
    }

    /// <summary>Bu testlerin konusu yetki değil AKIŞ; izin kapısı açık tutuluyor.</summary>
    private sealed class HerSeyeIzinli : IIzinServisi
    {
        public Task<IReadOnlySet<string>> IzinleriAsync(long kullaniciId) =>
            Task.FromResult<IReadOnlySet<string>>(Izinler.Adlar.ToHashSet());

        public Task<bool> VarMiAsync(long kullaniciId, string izin) => Task.FromResult(true);
        public void Dusur(long kullaniciId) { }
        public Task RolDegistiAsync(long rolId) => Task.CompletedTask;
    }
}
