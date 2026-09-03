using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Data;

namespace KentOS.Kalem.Tests;

/// <summary>
/// BASIN AJANDASI — <c>ajanda.basinGoruntule</c> daraltması.
/// </summary>
/// <remarks>
/// <para>
/// Katalogdaki tek daraltan izin. Ötekiler bir kapı açar, bu açılan kapının
/// ardında ne görüleceğini kısar: sahibi ajandayı açar ama listede yalnızca
/// <c>BasinKatilsin</c> işaretli, gizli olmayan kayıtlar döner.
/// </para>
/// <para>
/// Buradaki her iddia bir sızıntı yüzeyini kapatıyor. Daraltmanın atlandığı
/// TEK bir sorgu, basın kullanıcısına makamın bütün gününü gösterir ve bu
/// sessizce olur — ekranda hata değil, fazladan satır çıkar.
/// </para>
/// </remarks>
[Collection(SunucuKoleksiyonu.Ad)]
public class BasinAjandasiTests : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam;

    public BasinAjandasiTests(SunucuTestOrtami ortam) => _ortam = ortam;

    private static string DepoKoku()
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);
        while (dizin is not null && !File.Exists(Path.Combine(dizin.FullName, "KentOS.Kalem.sln")))
        {
            dizin = dizin.Parent;
        }
        return dizin?.FullName ?? throw new InvalidOperationException("Çözüm kökü bulunamadı");
    }

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
        {
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres kullanılamıyor");
        }
    }

    private static AjandaDto Sablon(string baslik, bool basin, bool gizli = false) => new()
    {
        Baslik = baslik,
        Konum = "Başkanlık Odası",
        BaslangicTarihi = new DateTime(2026, 11, 4, 10, 0, 0),
        BitisTarihi = new DateTime(2026, 11, 4, 11, 0, 0),
        RandevuTipId = 1,
        DurumId = 1,
        BasinKatilsin = basin,
        Gizli = gizli,
        KatilimciIdler = gizli ? [1] : [],
    };

    /// <summary>Üç kayıt kurar: basınlı, basınsız ve gizli-basınlı.</summary>
    private async Task<AppDbContext> VeriKurAsync()
    {
        await _ortam.TemelVerileriKurAsync();

        var baglam = _ortam.Baglam();
        var ekleyen = new SahteKullaniciServisi(1, "ekleyen", 1);
        var (ajanda, _, _) = TestServisFabrikasi.Kur(baglam, ekleyen, _ortam.Mapper);

        await ajanda.CreateAsync(Sablon("Basın toplantısı", basin: true));
        await ajanda.CreateAsync(Sablon("İç değerlendirme", basin: false));

        return baglam;
    }

    [Fact]
    public async Task Basin_kullanicisi_YALNIZCA_basin_etkinliklerini_gorur()
    {
        PostgresYoksaAtla();
        using var baglam = await VeriKurAsync();

        var liste = await baglam.Ajandalar
            .GorunurOlanlar(kullaniciId: 2, kullaniciAdi: "basinci", yalnizcaBasin: true)
            .Select(a => a.Baslik)
            .ToListAsync();

        Assert.Contains("Basın toplantısı", liste);
        Assert.DoesNotContain("İç değerlendirme", liste);
    }

    [Fact]
    public async Task Tam_yetkili_kullanici_HEPSINI_gorur()
    {
        PostgresYoksaAtla();
        using var baglam = await VeriKurAsync();

        var liste = await baglam.Ajandalar
            .GorunurOlanlar(kullaniciId: 1, kullaniciAdi: "ekleyen", yalnizcaBasin: false)
            .Select(a => a.Baslik)
            .ToListAsync();

        Assert.Contains("Basın toplantısı", liste);
        Assert.Contains("İç değerlendirme", liste);
    }

    /// <summary>
    /// Daraltma, <b>gizlilik kuralının önüne geçemez</b>.
    /// </summary>
    /// <remarks>
    /// Basın işaretli bir kayıt gizli de olabilseydi (sunucu bunu ayrıca
    /// reddediyor ama veritabanında eski bir satır bulunabilir), basın
    /// kullanıcısı onu "basın kapsamındadır" diye görürdü. İki kural VE ile
    /// bağlı: daraltma neyin görüneceğini kısar, gizlilik neyin
    /// görünmeyeceğini söyler ve gizlilik üstte durur.
    /// </remarks>
    [Fact]
    public async Task Gizli_kayit_basin_isaretli_olsa_bile_gorunmez()
    {
        PostgresYoksaAtla();
        using var baglam = await VeriKurAsync();

        // Kuralı dolanarak doğrudan veritabanına yazılıyor: servis bu bileşimi
        // zaten reddediyor, sınanan şey SORGU kapısının davranışı.
        baglam.Ajandalar.Add(new Ajanda
        {
            Baslik = "Gizli ama basın işaretli",
            BaslangicTarihi = new DateTime(2026, 11, 4, 14, 0, 0),
            BitisTarihi = new DateTime(2026, 11, 4, 15, 0, 0),
            BasinKatilsin = true,
            Gizli = true,
            BirimId = 1,
            KullaniciId = "ekleyen",
            RandevuTipId = 1,
            DurumId = 1,
        });
        await baglam.SaveChangesAsync();

        var liste = await baglam.Ajandalar
            .GorunurOlanlar(kullaniciId: 2, kullaniciAdi: "basinci", yalnizcaBasin: true)
            .Select(a => a.Baslik)
            .ToListAsync();

        Assert.DoesNotContain("Gizli ama basın işaretli", liste);
    }

    /// <summary>
    /// Birim izolasyonu daraltmayla birlikte de çalışır.
    /// </summary>
    /// <remarks>
    /// İki süzgeç ayrı ayrı doğru olup birleştiklerinde biri düşebilir; bu
    /// yüzden bileşim ayrıca sınanıyor. Basın kullanıcısı, BAŞKA bir birimin
    /// basın etkinliğini de görmemeli.
    /// </remarks>
    [Fact]
    public async Task Basin_daraltmasi_birim_izolasyonunu_KALDIRMAZ()
    {
        PostgresYoksaAtla();
        using var baglam = await VeriKurAsync();

        baglam.Ajandalar.Add(new Ajanda
        {
            Baslik = "Başka birimin basın etkinliği",
            BaslangicTarihi = new DateTime(2026, 11, 5, 10, 0, 0),
            BitisTarihi = new DateTime(2026, 11, 5, 11, 0, 0),
            BasinKatilsin = true,
            BirimId = 2,   // fixture'daki "Diğer Birim"
            KullaniciId = "yabanci",
            RandevuTipId = 1,
            DurumId = 1,
        });
        await baglam.SaveChangesAsync();

        var liste = await baglam.Ajandalar
            .ErisilebilirOlanlar(kullaniciId: 2, kullaniciAdi: "basinci",
                                 birimId: 1, yalnizcaBasin: true)
            .Select(a => a.Baslik)
            .ToListAsync();

        Assert.Contains("Basın toplantısı", liste);
        Assert.DoesNotContain("Başka birimin basın etkinliği", liste);
    }

    /// <summary>
    /// Daraltma <b>tek bir kapıda</b> duruyor mu?
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bu test kaynak kodu okur. Amacı: ajandayı okuyan yeni bir sorgu
    /// eklendiğinde daraltmanın atlanmadığından emin olmak. Derleyici zaten
    /// zorluyor (parametrenin varsayılanı yok) ama biri
    /// <c>yalnizcaBasin: false</c> yazıp geçebilir — burada o satırların
    /// GEREKÇELİ olması bekleniyor.
    /// </para>
    /// <para>
    /// Sabit <c>false</c> geçmesine izin verilen tek yer eski MVC arayüzü:
    /// davranışı donduruldu ve <c>Basin</c> rolü o sayfalara zaten giremiyor.
    /// </para>
    /// </remarks>
    [Fact]
    public void Sabit_false_yalnizca_eski_MVCde()
    {
        var kok = DepoKoku();
        var izinliDosyalar = new[] { "AjandaController.cs" };

        var ihlaller = new List<string>();

        foreach (var dosya in Directory.EnumerateFiles(
                     Path.Combine(kok, "KentOS.Kalem.Web"), "*.cs",
                     SearchOption.AllDirectories))
        {
            if (dosya.Contains("/obj/") || dosya.Contains("/Migrations/")) continue;

            var ad = Path.GetFileName(dosya);
            if (izinliDosyalar.Contains(ad)) continue;

            var metin = File.ReadAllText(dosya);
            if (metin.Contains("yalnizcaBasin: false"))
            {
                ihlaller.Add(ad);
            }
        }

        Assert.True(ihlaller.Count == 0,
            "Basın daraltması sabit `false` ile atlanmış: " + string.Join(", ", ihlaller));
    }
}
