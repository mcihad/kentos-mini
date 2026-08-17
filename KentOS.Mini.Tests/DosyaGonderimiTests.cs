using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Services.V2;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// DOSYA GÖNDERİMİ görünürlüğü.
///
/// <para>
/// Kilitlenen sözleşme: bir gönderimi <b>yalnızca gönderen ve alıcı</b>
/// görebilir — listede, detayda, indirmede ve not eklemede. Rol bypass'ı yok.
/// Bu, özelliğin varlık sebebi olduğu için ayrı bir test dosyası hak ediyor.
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class DosyaGonderimiTests : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam;

    // SunucuTestOrtami.TemelVerileriKurAsync ile eşleşir.
    private const long GonderenId = 1;
    private const long AliciId = 2;
    private const long YabanciId = 3;

    public DosyaGonderimiTests(SunucuTestOrtami ortam) => _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
        {
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres kullanılamıyor");
        }
    }

    private (DosyaGonderimiServisi servis, SahteMesajServisi mesaj, AppDbContextSarmali kap) Kur()
    {
        var baglam = _ortam.Baglam();
        var mesaj = new SahteMesajServisi();

        // Her koşum kendi bellek içi deposuyla çalışır; testler birbirinin
        // dosyasını görmez ve diskte artık kalmaz.
        var depo = new TestServisFabrikasi.SahteDepo();

        var servis = new DosyaGonderimiServisi(
            baglam, depo, mesaj, NullLogger<DosyaGonderimiServisi>.Instance);

        return (servis, mesaj, new AppDbContextSarmali(baglam));
    }

    /// <summary>Bellekten üretilmiş küçük bir yükleme dosyası.</summary>
    private static IFormFile Dosya(string ad = "belge.pdf", string icerik = "test icerigi")
    {
        var bayt = System.Text.Encoding.UTF8.GetBytes(icerik);
        return new FormFile(new MemoryStream(bayt), 0, bayt.Length, "dosya", ad)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf",
        };
    }

    private async Task TemizleAsync()
    {
        using var b = _ortam.Baglam();
        await b.Database.ExecuteSqlRawAsync(
            "TRUNCATE dosya_gonderimi_notlari, dosya_gonderimleri RESTART IDENTITY CASCADE;");
        await _ortam.TemelVerileriKurAsync();
    }

    [Fact]
    public async Task Yabanci_gonderimi_ne_listede_ne_detayda_ne_de_indirmede_gorur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, _, kap) = Kur();
        using (kap)
        {
            var g = await servis.GonderAsync(GonderenId, AliciId, "Rapor", "İnceler misiniz?", Dosya());

            // Gönderen ve alıcı görür.
            Assert.NotNull(await servis.DetayAsync(GonderenId, g.Id));
            Assert.NotNull(await servis.DetayAsync(AliciId, g.Id));

            // Yabancı hiçbir yoldan göremez.
            var liste = await servis.ListeAsync(YabanciId, new GonderimSuzgeci());
            Assert.Empty(liste.Veriler);

            await Assert.ThrowsAsync<EntityNotFoundException>(
                () => servis.DetayAsync(YabanciId, g.Id));

            await Assert.ThrowsAsync<EntityNotFoundException>(
                () => servis.DosyaAsync(YabanciId, g.Id));

            await Assert.ThrowsAsync<EntityNotFoundException>(
                () => servis.NotEkleAsync(YabanciId, g.Id, "araya girdim"));
        }
    }

    /// <summary>
    /// Veritabanında YALNIZCA disk adı saklanır, adres değil.
    /// </summary>
    /// <remarks>
    /// Adres saklansaydı arayüz onu doğrudan bağlantı yapmaya heveslenirdi;
    /// oysa dosyanın tek meşru girişi kimlik denetimli
    /// <c>GET /api/v2/gonderim/{id}/dosya</c> ucu. Klasörün HTTP'den
    /// kapalı olduğunu <c>GonderimDosyaKorumasiTests</c> ayrıca bekçiler.
    /// </remarks>
    [Fact]
    public async Task Veritabaninda_adres_degil_disk_adi_saklanir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, _, kap) = Kur();
        using (kap)
        {
            var g = await servis.GonderAsync(GonderenId, AliciId, "Rapor", null, Dosya());

            using var b = _ortam.Baglam();
            var kayit = await b.DosyaGonderimleri.AsNoTracking().FirstAsync(x => x.Id == g.Id);

            // Depolanan değer bir URL değil, yalnızca disk adı olmalı.
            Assert.DoesNotContain("/", kayit.DosyaYolu);
            Assert.DoesNotContain("\\", kayit.DosyaYolu);
            Assert.EndsWith(".pdf", kayit.DosyaYolu);
        }
    }

    [Fact]
    public async Task Indirilen_dosya_gonderilenle_ayni()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, _, kap) = Kur();
        using (kap)
        {
            var g = await servis.GonderAsync(
                GonderenId, AliciId, "Rapor", null, Dosya("yillik.pdf", "birebir ayni icerik"));

            var (akis, ad, tur) = await servis.DosyaAsync(AliciId, g.Id);
            using var okuyucu = new StreamReader(akis);

            Assert.Equal("birebir ayni icerik", await okuyucu.ReadToEndAsync());
            Assert.Equal("yillik.pdf", ad);
            Assert.Equal("application/pdf", tur);
        }
    }

    [Fact]
    public async Task Not_bildirimi_KARSI_tarafa_gider()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, mesaj, kap) = Kur();
        using (kap)
        {
            var g = await servis.GonderAsync(GonderenId, AliciId, "Rapor", null, Dosya());
            mesaj.KisilereGidenler.Clear();

            // Alıcı not yazınca bildirim GÖNDERENE gitmeli.
            await servis.NotEkleAsync(AliciId, g.Id, "aldım, teşekkürler");

            var bildirim = Assert.Single(mesaj.KisilereGidenler);
            Assert.Equal([GonderenId], bildirim.KullaniciIdler);
        }
    }

    [Fact]
    public async Task Yalnizca_gonderen_silebilir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, _, kap) = Kur();
        using (kap)
        {
            var g = await servis.GonderAsync(GonderenId, AliciId, "Rapor", null, Dosya());

            // Alıcının silmesi, gönderenin bilgisi olmadan belgeyi yok etmek olurdu.
            await Assert.ThrowsAnyAsync<Exception>(() => servis.SilAsync(AliciId, g.Id));

            await servis.SilAsync(GonderenId, g.Id);

            using var b = _ortam.Baglam();
            Assert.False(await b.DosyaGonderimleri.AnyAsync(x => x.Id == g.Id));
        }
    }

    [Fact]
    public async Task Calistirilabilir_uzanti_reddedilir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, _, kap) = Kur();
        using (kap)
        {
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => servis.GonderAsync(GonderenId, AliciId, "Kurulum", null, Dosya("kur.exe")));
        }
    }

    [Fact]
    public async Task Kendine_gonderilemez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, _, kap) = Kur();
        using (kap)
        {
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => servis.GonderAsync(GonderenId, GonderenId, "Kendime", null, Dosya()));
        }
    }

    /// <summary>Bağlamı testin sonunda kapatmak için küçük sarmalayıcı.</summary>
    private sealed class AppDbContextSarmali(Web.Data.AppDbContext baglam) : IDisposable
    {
        public void Dispose() => baglam.Dispose();
    }
}
