using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Kalem.Application.Dto.V2.IsTakip;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.AuthPolicies;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Exceptions;
using KentOS.Kalem.Web.Services.V2;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// PROJE — çatı, pano ve gantt.
/// </summary>
/// <remarks>
/// <para>
/// Kilitlenen kurallar: <b>proje silmek görevleri silmez</b>, <b>kart taşımak
/// durum akışından geçer</b> ve <b>kilometre taşı düzenlemek bağlı görevleri
/// koparmaz</b>. Üçü de sessiz veri kaybı üretebilecek yerler.
/// </para>
/// </remarks>
[Collection(SunucuKoleksiyonu.Ad)]
public class ProjeTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;
    private readonly SahteMesajServisi _mesajlar = new();

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    /// <summary>Proje ve görev servislerini TEK bağlam üzerinde kurar.</summary>
    private (IProjeServisi Proje, IGorevServisi Gorev, AppDbContext Baglam) Kur(long birimId = 1)
    {
        var baglam = _ortam.Baglam();
        var kullanici = new SahteKullaniciServisi(1, "test", birimId);

        var etkin = new EtkinBirim(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            kullanici,
            new BirimAgaci(_ortam.Baglam(), new MemoryCache(new MemoryCacheOptions())),
            new HerSeyeIzinli());

        var olaylar = new IsOlayServisi(baglam, kullanici, etkin, NullLogger<IsOlayServisi>.Instance);
        var ekler = new IsEkServisi(baglam, new TestServisFabrikasi.SahteDepo(), kullanici,
            NullLogger<IsEkServisi>.Instance);
        var yorumlar = new IsYorumServisi(baglam, kullanici);

        var gorev = new GorevServisi(
            baglam, kullanici, etkin, olaylar, ekler, yorumlar,
            new EkipServisi(baglam, etkin), _mesajlar, TestKapsayici.Bos,
            NullLogger<GorevServisi>.Instance);

        var proje = new ProjeServisi(baglam, kullanici, etkin, olaylar, ekler, yorumlar, gorev,
            _mesajlar, NullLogger<ProjeServisi>.Instance);

        return (proje, gorev, baglam);
    }

    private static ProjeKayitDto Kayit(string? ad = null) => new()
    {
        Ad = ad ?? "Kent Meydanı Düzenlemesi " + Guid.NewGuid().ToString("N")[..6],
        Kod = "KMD-2026",
        Durum = ProjeDurumu.Devam,
        Baslangic = new DateTime(2026, 3, 1),
        Bitis = new DateTime(2026, 9, 30),
    };

    // ── varsayılan pano ────────────────────────────────────────────────

    /// <summary>
    /// Sütun verilmezse VARSAYILAN pano kurulur.
    /// </summary>
    /// <remarks>
    /// Boş bir pano, kanban sekmesini açan kullanıcıya "burada iş yok" değil
    /// "burası bozuk" dedirtirdi.
    /// </remarks>
    [Fact]
    public async Task Sutun_verilmezse_varsayilan_pano_kurulur()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, _, _) = Kur();
        var p = await proje.OlusturAsync(Kayit());

        Assert.Equal(5, p.PanoSutunlari.Count);
        Assert.Equal([1, 2, 3, 4, 5], p.PanoSutunlari.Select(s => s.SiraNo));

        // Her sütun bir GÖREV DURUMUNA eşli — ayrı bir durum kaynağı yok.
        Assert.All(p.PanoSutunlari, s => Assert.False(string.IsNullOrWhiteSpace(s.GorevDurumuAd)));

        /*
          VARSAYILAN PANO NORMAL AKIŞIN TAMAMINI KAPSAR.

          `Basladi` sütunu önce yoktu ve tarayıcıda ölçüldüğünde şu çıktı:
          başlatılan görev hiçbir sütuna eşleşmiyor, "Sütunsuz"a düşüyor ve
          oradan sürüklenemediği için panoda kilitleniyordu.
        */
        GorevDurumu[] normalAkis =
        [
            GorevDurumu.Atandi, GorevDurumu.Basladi, GorevDurumu.DevamEdiyor,
            GorevDurumu.TamamlanmaBekliyor, GorevDurumu.Tamamlandi,
        ];

        Assert.Equal(normalAkis, p.PanoSutunlari.Select(s => s.GorevDurumu));
    }

    /// <summary>
    /// GÜNCELLEMEDE PANO BOŞ KALMAZ.
    /// </summary>
    /// <remarks>
    /// Sütun listesi tam liste olarak yazılıyor; gövdesinde sütun taşımayan
    /// bir güncelleme panoyu siliyordu ve tarayıcıda ölçüldüğünde görüldü:
    /// kanban sekmesi "pano kurulmamış" diyor ama arayüzde sütun ekleme yolu
    /// yok, yani proje kalıcı olarak panosuz kalıyordu.
    /// </remarks>
    [Fact]
    public async Task Guncellemede_bos_sutun_listesi_varsayilana_doner()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, _, _) = Kur();
        var p = await proje.OlusturAsync(Kayit());

        var bos = Kayit(p.Ad);
        bos.PanoSutunlari = [];

        var sonra = await proje.GuncelleAsync(p.Id, bos);

        Assert.Equal(5, sonra.PanoSutunlari.Count);
        Assert.Contains(sonra.PanoSutunlari, s => s.GorevDurumu == GorevDurumu.Basladi);
    }

    /// <summary>Kurum kendi sütunlarını tanımlarsa varsayılan DEVREYE GİRMEZ.</summary>
    [Fact]
    public async Task Ozel_sutunlar_varsayilanla_degistirilmez()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, _, _) = Kur();
        var kayit = Kayit();
        kayit.PanoSutunlari =
        [
            new PanoSutunuDto { Ad = "Sahada", GorevDurumu = GorevDurumu.DevamEdiyor },
            new PanoSutunuDto { Ad = "Atölyede", GorevDurumu = GorevDurumu.DevamEdiyor },
        ];

        var p = await proje.OlusturAsync(kayit);

        Assert.Equal(["Sahada", "Atölyede"], p.PanoSutunlari.Select(s => s.Ad));
    }

    /// <summary>Bitiş başlangıçtan önce olamaz — gantt çubuğu negatif genişlik alırdı.</summary>
    [Fact]
    public async Task Ters_tarih_araligi_REDDEDILIR()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, _, _) = Kur();
        var kayit = Kayit();
        kayit.Baslangic = new DateTime(2026, 9, 1);
        kayit.Bitis = new DateTime(2026, 3, 1);

        await Assert.ThrowsAsync<BusinessRuleException>(() => proje.OlusturAsync(kayit));
    }

    // ── silme ──────────────────────────────────────────────────────────

    /// <summary>
    /// PROJE SİLMEK GÖREVLERİ SİLMEZ.
    /// </summary>
    /// <remarks>
    /// Proje bir çatı, işin sahibi değil. Cascade kursaydık bir projeyi
    /// silmek altındaki bütün işi, aşama kanıtlarını ve zaman çizelgesini de
    /// götürürdü.
    /// </remarks>
    [Fact]
    public async Task Proje_silinince_gorevler_KALIR()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, gorev, baglam) = Kur();
        var p = await proje.OlusturAsync(Kayit());

        var g = await gorev.OlusturAsync(new GorevKayitDto
        {
            Baslik = "Zemin kaplaması",
            ProjeId = p.Id,
        });

        await proje.SilAsync(p.Id);

        var kalan = await baglam.Gorevler.AsNoTracking().FirstOrDefaultAsync(x => x.Id == g.Id);

        Assert.NotNull(kalan);
        Assert.Null(kalan.ProjeId);
        Assert.Null(kalan.KilometreTasiId);
        Assert.False(await baglam.Projeler.AnyAsync(x => x.Id == p.Id));
        Assert.False(await baglam.PanoSutunlari.AnyAsync(x => x.ProjeId == p.Id));
    }

    // ── kilometre taşı ─────────────────────────────────────────────────

    /// <summary>
    /// KİLOMETRE TAŞI DÜZENLEMEK BAĞLI GÖREVLERİ KOPARMAZ.
    /// </summary>
    /// <remarks>
    /// Taşlar sil-yeniden-yaz edilseydi her kayıtta yeni kimlikler üretilir ve
    /// bağlı görevlerin hepsi sahipsiz kalırdı — projeyi düzenlemek,
    /// görevlerin hangi hedefe ait olduğunu silmek olurdu.
    /// </remarks>
    [Fact]
    public async Task Kilometre_tasi_duzenlemek_bagli_gorevi_KOPARMAZ()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, gorev, baglam) = Kur();

        var kayit = Kayit();
        kayit.KilometreTaslari = [new KilometreTasiDto { Ad = "Zemin bitti", HedefTarih = new DateTime(2026, 5, 1) }];
        var p = await proje.OlusturAsync(kayit);

        var tasId = p.KilometreTaslari[0].Id;

        var g = await gorev.OlusturAsync(new GorevKayitDto
        {
            Baslik = "Parke döşeme",
            ProjeId = p.Id,
            KilometreTasiId = tasId,
        });

        // Aynı taş KİMLİĞİYLE geri gönderiliyor; adı değişiyor.
        var guncel = Kayit(p.Ad);
        guncel.KilometreTaslari =
        [
            new KilometreTasiDto { Id = tasId, Ad = "Zemin tamam", HedefTarih = new DateTime(2026, 5, 15) },
        ];

        var sonra = await proje.GuncelleAsync(p.Id, guncel);

        Assert.Equal(tasId, sonra.KilometreTaslari[0].Id);
        Assert.Equal("Zemin tamam", sonra.KilometreTaslari[0].Ad);

        var bagliKalan = await baglam.Gorevler.AsNoTracking().FirstAsync(x => x.Id == g.Id);
        Assert.Equal(tasId, bagliKalan.KilometreTasiId);
    }

    /// <summary>Taş listeden ÇIKARILIRSA bağlı görevler sahipsiz kalır ama SİLİNMEZ.</summary>
    [Fact]
    public async Task Kaldirilan_kilometre_tasinin_gorevleri_silinmez()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, gorev, baglam) = Kur();

        var kayit = Kayit();
        kayit.KilometreTaslari = [new KilometreTasiDto { Ad = "Kaldırılacak", HedefTarih = DateTime.Today }];
        var p = await proje.OlusturAsync(kayit);
        var tasId = p.KilometreTaslari[0].Id;

        var g = await gorev.OlusturAsync(new GorevKayitDto
        {
            Baslik = "Bağlı iş", ProjeId = p.Id, KilometreTasiId = tasId,
        });

        var bos = Kayit(p.Ad);
        bos.KilometreTaslari = [];
        await proje.GuncelleAsync(p.Id, bos);

        var kalan = await baglam.Gorevler.AsNoTracking().FirstAsync(x => x.Id == g.Id);
        Assert.Null(kalan.KilometreTasiId);
        Assert.Equal(p.Id, kalan.ProjeId);
    }

    /// <summary>Tamamlanma elle işaretlenir ve geri alınabilir.</summary>
    [Fact]
    public async Task Kilometre_tasi_tamamlanir_ve_geri_alinir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, _, _) = Kur();
        var kayit = Kayit();
        kayit.KilometreTaslari = [new KilometreTasiDto { Ad = "Hedef", HedefTarih = DateTime.Today }];
        var p = await proje.OlusturAsync(kayit);
        var tasId = p.KilometreTaslari[0].Id;

        var kapali = await proje.KilometreTasiTamamlaAsync(p.Id, tasId, true);
        Assert.True(kapali.Tamamlandi);
        Assert.NotNull(kapali.TamamlanmaTarihi);

        var acik = await proje.KilometreTasiTamamlaAsync(p.Id, tasId, false);
        Assert.False(acik.Tamamlandi);
        Assert.Null(acik.TamamlanmaTarihi);
    }

    // ── kanban ─────────────────────────────────────────────────────────

    /// <summary>
    /// KART TAŞIMAK DURUM AKIŞINDAN GEÇER.
    /// </summary>
    /// <remarks>
    /// Panoyu akışın dışında tutsaydık kartı sürükleyerek onay kapısını
    /// atlamak mümkün olurdu — modülün en önemli kuralı panodan delinirdi.
    /// </remarks>
    [Fact]
    public async Task Kart_tasima_durum_akisindan_gecer()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, gorev, _) = Kur();
        var p = await proje.OlusturAsync(Kayit());

        var g = await gorev.OlusturAsync(new GorevKayitDto
        {
            Baslik = "Yeni iş",
            ProjeId = p.Id,
        });

        var tamamlandiSutunu = p.PanoSutunlari.First(s => s.GorevDurumu == GorevDurumu.Tamamlandi);

        // `Yeni` durumundan doğrudan `Tamamlandi`ya geçilemez — onay kapısı.
        var hata = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            proje.KartTasiAsync(p.Id, new KartTasimaDto
            {
                GorevId = g.Id,
                HedefSutunId = tamamlandiSutunu.Id,
            }));

        Assert.Contains("taşınamaz", hata.Message);
    }

    /// <summary>Geçerli bir taşıma görevin durumunu DEĞİŞTİRİR.</summary>
    [Fact]
    public async Task Gecerli_tasima_gorevin_durumunu_degistirir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, gorev, _) = Kur();
        var p = await proje.OlusturAsync(Kayit());

        var g = await gorev.OlusturAsync(new GorevKayitDto
        {
            Baslik = "Atanacak iş",
            ProjeId = p.Id,
            Atamalar = [new GorevAtamaIstegiDto { KullaniciId = 2 }],
        });

        Assert.Equal(GorevDurumu.Atandi, g.Durum);

        var basladiSutunu = p.PanoSutunlari.First(s => s.GorevDurumu == GorevDurumu.Basladi);
        var devamSutunu = p.PanoSutunlari.First(s => s.GorevDurumu == GorevDurumu.DevamEdiyor);

        // SIRA ATLANAMAZ: atanmış görev doğrudan "devam ediyor"a geçemez,
        // önce başlaması gerekiyor — panonun akışa saygılı olduğunun kanıtı.
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            proje.KartTasiAsync(p.Id, new KartTasimaDto
            {
                GorevId = g.Id, HedefSutunId = devamSutunu.Id,
            }));

        // "Başladı" sütununa taşımak geçerli ve görevi BAŞLATIR.
        var pano = await proje.KartTasiAsync(p.Id, new KartTasimaDto
        {
            GorevId = g.Id, HedefSutunId = basladiSutunu.Id,
        });

        Assert.Contains(
            pano.Sutunlar.First(s => s.Sutun.Id == basladiSutunu.Id).Kartlar,
            k => k.Id == g.Id);

        // Oradan "devam ediyor"a taşımak da geçerli.
        pano = await proje.KartTasiAsync(p.Id, new KartTasimaDto
        {
            GorevId = g.Id, HedefSutunId = devamSutunu.Id,
        });

        Assert.Contains(
            pano.Sutunlar.First(s => s.Sutun.Id == devamSutunu.Id).Kartlar,
            k => k.Id == g.Id);
    }

    /// <summary>
    /// Hiçbir sütuna düşmeyen görev KAYBOLMAZ.
    /// </summary>
    /// <remarks>
    /// Panoda karşılığı olmayan durumdaki görev sessizce kaybolsaydı, pano
    /// yapılmakta olan işin eksik bir resmini gösterirdi — kayıp iş, panoyu
    /// yanlış okutan en sinsi şey.
    /// </remarks>
    [Fact]
    public async Task Sutuna_dusmeyen_gorev_DAGITILMAYANLARDA_gorunur()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, gorev, _) = Kur();
        var p = await proje.OlusturAsync(Kayit());

        // `Yeni` durumunda; varsayılan panoda "Yeni" sütunu YOK.
        var g = await gorev.OlusturAsync(new GorevKayitDto { Baslik = "Sütunsuz", ProjeId = p.Id });

        var pano = await proje.PanoAsync(p.Id);

        Assert.Contains(pano.Dagitilmayanlar, k => k.Id == g.Id);
        Assert.All(pano.Sutunlar, s => Assert.DoesNotContain(s.Kartlar, k => k.Id == g.Id));
    }

    /// <summary>Başka projenin görevi panoya taşınamaz.</summary>
    [Fact]
    public async Task Baska_projenin_gorevi_tasinamaz()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, gorev, _) = Kur();
        var p = await proje.OlusturAsync(Kayit());
        var digerProje = await proje.OlusturAsync(Kayit());

        var g = await gorev.OlusturAsync(new GorevKayitDto
        {
            Baslik = "Öteki projenin işi", ProjeId = digerProje.Id,
        });

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            proje.KartTasiAsync(p.Id, new KartTasimaDto
            {
                GorevId = g.Id,
                HedefSutunId = p.PanoSutunlari[0].Id,
            }));
    }

    // ── gantt ──────────────────────────────────────────────────────────

    /// <summary>
    /// TARİHSİZ SATIR ÇİZİLMEZ.
    /// </summary>
    /// <remarks>
    /// Başlangıcı ve bitişi olmayan bir işi çizmeye çalışmak onu ya bugüne ya
    /// da sonsuza yapıştırırdı; iki durumda da yanlış bilgi verirdi.
    /// </remarks>
    [Fact]
    public async Task Gantt_tarihsiz_satiri_atlar()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, gorev, _) = Kur();

        var kayit = Kayit();
        kayit.KilometreTaslari =
        [
            new KilometreTasiDto { Ad = "Tarihli taş", HedefTarih = new DateTime(2026, 5, 1) },
            new KilometreTasiDto { Ad = "Tarihsiz taş" },
        ];
        var p = await proje.OlusturAsync(kayit);

        // Tarihi olan görev.
        await gorev.OlusturAsync(new GorevKayitDto
        {
            Baslik = "Planlı iş",
            ProjeId = p.Id,
            PlanlananBaslangic = new DateTime(2026, 3, 10),
            PlanlananBitis = new DateTime(2026, 4, 20),
        });

        // Tarihi ve SLA'sı olmayan görev — çizilmemeli.
        await gorev.OlusturAsync(new GorevKayitDto { Baslik = "Tarihsiz iş", ProjeId = p.Id });

        var satirlar = await proje.GanttAsync(p.Id);

        Assert.Contains(satirlar, s => s.Ad == "Tarihli taş" && s.Tur == "kilometreTasi");
        Assert.DoesNotContain(satirlar, s => s.Ad == "Tarihsiz taş");
        Assert.Contains(satirlar, s => s.Ad == "Planlı iş" && s.Tur == "gorev");
        Assert.DoesNotContain(satirlar, s => s.Ad == "Tarihsiz iş");

        // Satırlar başlangıca göre sıralı — çizim sırayı yeniden hesaplamasın.
        var tarihler = satirlar.Select(s => s.Baslangic!.Value).ToList();
        Assert.Equal(tarihler.OrderBy(t => t), tarihler);
    }

    /// <summary>Kilometre taşı bir NOKTA: başlangıç ile bitiş aynı gün.</summary>
    [Fact]
    public async Task Gantt_kilometre_tasi_nokta_olarak_doner()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, _, _) = Kur();
        var kayit = Kayit();
        kayit.KilometreTaslari = [new KilometreTasiDto { Ad = "Nokta", HedefTarih = new DateTime(2026, 6, 1) }];
        var p = await proje.OlusturAsync(kayit);

        var satir = (await proje.GanttAsync(p.Id)).First(s => s.Tur == "kilometreTasi");

        Assert.Equal(satir.Baslangic, satir.Bitis);
    }

    // ── görünürlük ─────────────────────────────────────────────────────

    /// <summary>Başka birimin projesi 403 değil BULUNAMADI döner.</summary>
    [Fact]
    public async Task Baska_birimin_projesi_BULUNAMADI_doner()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (birinci, _, _) = Kur(birimId: 1);
        var p = await birinci.OlusturAsync(Kayit());

        var (ikinci, _, _) = Kur(birimId: 2);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => ikinci.GetirAsync(p.Id));
        await Assert.ThrowsAsync<EntityNotFoundException>(() => ikinci.PanoAsync(p.Id));
        await Assert.ThrowsAsync<EntityNotFoundException>(() => ikinci.GanttAsync(p.Id));
    }

    // ── ilerleme ───────────────────────────────────────────────────────

    /// <summary>
    /// İlerleme PROJEDEN DEĞİL görevlerden okunur.
    /// </summary>
    /// <remarks>
    /// Projede saklanan bir yüzde kolonu olsaydı görevlerle çelişebilen ikinci
    /// bir gerçek doğardı.
    /// </remarks>
    [Fact]
    public async Task Ilerleme_gorevlerden_hesaplanir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, gorev, baglam) = Kur();
        var p = await proje.OlusturAsync(Kayit());

        var bir = await gorev.OlusturAsync(new GorevKayitDto { Baslik = "Bir", ProjeId = p.Id });
        await gorev.OlusturAsync(new GorevKayitDto { Baslik = "İki", ProjeId = p.Id });

        // Bir görevi doğrudan tamamlanmış işaretle (akış testi burada değil).
        await baglam.Gorevler.Where(g => g.Id == bir.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(g => g.Durum, GorevDurumu.Tamamlandi));
        baglam.ChangeTracker.Clear();

        var sonra = await proje.GetirAsync(p.Id);

        Assert.Equal(2, sonra.GorevToplam);
        Assert.Equal(1, sonra.GorevBiten);
    }

    /// <summary>Kapanmış proje GECİKMİŞ sayılmaz — ölçüm bitti.</summary>
    [Fact]
    public async Task Kapanan_proje_GECIKMIS_sayilmaz()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, _, _) = Kur();

        var gecmis = Kayit();
        gecmis.Baslangic = new DateTime(2020, 1, 1);
        gecmis.Bitis = new DateTime(2020, 6, 1);
        var p = await proje.OlusturAsync(gecmis);

        Assert.True(p.Gecikti);

        var kapali = Kayit(p.Ad);
        kapali.Baslangic = gecmis.Baslangic;
        kapali.Bitis = gecmis.Bitis;
        kapali.Durum = ProjeDurumu.Tamamlandi;

        var sonra = await proje.GuncelleAsync(p.Id, kapali);

        Assert.False(sonra.Gecikti);
        Assert.NotNull(sonra.TamamlanmaTarihi);
    }

    /// <summary>Yeniden açılan projede eski tamamlanma damgası KALMAZ.</summary>
    [Fact]
    public async Task Yeniden_acilan_projede_tamamlanma_damgasi_silinir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (proje, _, _) = Kur();
        var p = await proje.OlusturAsync(Kayit());

        var kapat = Kayit(p.Ad);
        kapat.Durum = ProjeDurumu.Tamamlandi;
        var kapali = await proje.GuncelleAsync(p.Id, kapat);
        Assert.NotNull(kapali.TamamlanmaTarihi);

        var ac = Kayit(p.Ad);
        ac.Durum = ProjeDurumu.Devam;
        var acik = await proje.GuncelleAsync(p.Id, ac);

        Assert.Null(acik.TamamlanmaTarihi);
    }

    /// <summary>Bu testlerin konusu yetki değil PROJE; izin kapısı açık tutuluyor.</summary>
    private sealed class HerSeyeIzinli : IIzinServisi
    {
        public Task<IReadOnlySet<string>> IzinleriAsync(long kullaniciId) =>
            Task.FromResult<IReadOnlySet<string>>(Izinler.Adlar.ToHashSet());

        public Task<bool> VarMiAsync(long kullaniciId, string izin) => Task.FromResult(true);
        public void Dusur(long kullaniciId) { }
        public Task RolDegistiAsync(long rolId) => Task.CompletedTask;
    }
}
