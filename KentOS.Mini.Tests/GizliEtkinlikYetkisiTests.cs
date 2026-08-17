using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Dto.Randevu;
using KentOS.Mini.Web.Exceptions;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// GİZLİ ETKİNLİK OLUŞTURMA YETKİSİ — <c>AppUser.GizliEtkinlikEkleyebilir</c>.
///
/// <para>
/// Görünürlükten AYRI bir kural: bu bayrak kimsenin başkasının gizli
/// etkinliğini görmesini sağlamaz, yalnızca gizli etkinlik <b>oluşturmayı</b>
/// açar. Görünürlük matrisi <see cref="GizliEtkinlikTests"/> içinde.
/// </para>
///
/// <para>
/// Denetim <b>servis katmanında</b>: v1 (mobil) ve v2 aynı
/// <c>AjandaService</c>'i çağırıyor; controller'a konsaydı mobil kuralı
/// atlardı.
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class GizliEtkinlikYetkisiTests : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam;

    public GizliEtkinlikYetkisiTests(SunucuTestOrtami ortam) => _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
        {
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres kullanılamıyor");
        }
    }

    private static AjandaDto Sablon(bool gizli) => new()
    {
        Baslik = gizli ? "Gizli Görüşme" : "Açık Toplantı",
        Konum = "Başkanlık Odası",
        BaslangicTarihi = new DateTime(2026, 10, 5, 11, 0, 0),
        BitisTarihi = new DateTime(2026, 10, 5, 12, 0, 0),
        RandevuTipId = 1,
        DurumId = 1,
        Gizli = gizli,
        KatilimciIdler = gizli ? [2] : [],
    };

    [Fact]
    public async Task Yetkisiz_kullanici_gizli_etkinlik_olusturamaz()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using var baglam = _ortam.Baglam();
        var kullanici = new SahteKullaniciServisi(1, "ekleyen", 1) { GizliEtkinlikEkleyebilir = false };
        var (ajanda, _, _) = TestServisFabrikasi.Kur(baglam, kullanici, _ortam.Mapper);

        var hata = await Assert.ThrowsAsync<BusinessRuleException>(
            () => ajanda.CreateAsync(Sablon(gizli: true)));

        Assert.Contains("yetkiniz yok", hata.Message);
    }

    [Fact]
    public async Task Yetkisiz_kullanici_ACIK_etkinlik_olusturabilir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using var baglam = _ortam.Baglam();
        var kullanici = new SahteKullaniciServisi(1, "ekleyen", 1) { GizliEtkinlikEkleyebilir = false };
        var (ajanda, _, _) = TestServisFabrikasi.Kur(baglam, kullanici, _ortam.Mapper);

        var olusan = await ajanda.CreateAsync(Sablon(gizli: false));

        Assert.NotNull(olusan);
        Assert.False(olusan.Gizli);
    }

    [Fact]
    public async Task Yetkili_kullanici_gizli_etkinlik_olusturabilir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using var baglam = _ortam.Baglam();
        var kullanici = new SahteKullaniciServisi(1, "ekleyen", 1) { GizliEtkinlikEkleyebilir = true };
        var (ajanda, _, _) = TestServisFabrikasi.Kur(baglam, kullanici, _ortam.Mapper);

        var olusan = await ajanda.CreateAsync(Sablon(gizli: true));

        Assert.True(olusan.Gizli);
    }

    /// <summary>
    /// Var olan AÇIK bir etkinliği gizliye çevirmek de yetki ister.
    /// </summary>
    /// <remarks>
    /// Yalnızca oluşturma yolunu korumak yetmez: yetkisiz kullanıcı açık bir
    /// etkinlik oluşturup hemen ardından "gizli" işaretleyerek kuralı
    /// dolanabilirdi.
    /// </remarks>
    [Fact]
    public async Task Yetkisiz_kullanici_var_olan_etkinligi_gizliye_ceviremez()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using var baglam = _ortam.Baglam();
        var kullanici = new SahteKullaniciServisi(1, "ekleyen", 1) { GizliEtkinlikEkleyebilir = false };
        var (ajanda, _, _) = TestServisFabrikasi.Kur(baglam, kullanici, _ortam.Mapper);

        var acik = await ajanda.CreateAsync(Sablon(gizli: false));

        acik.Gizli = true;
        acik.KatilimciIdler = [2];

        var hata = await Assert.ThrowsAsync<BusinessRuleException>(
            () => ajanda.UpdateAsync(acik));

        Assert.Contains("yetkiniz yok", hata.Message);
    }
}
