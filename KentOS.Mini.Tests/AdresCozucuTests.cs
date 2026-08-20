using Microsoft.AspNetCore.Http;
using KentOS.Mini.Web.Options;
using KentOS.Mini.Web.Services;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// DIŞARIYA VERİLEN ADRES isteğin kendisinden gelir.
/// </summary>
/// <remarks>
/// <para>
/// Önce <c>App:BaseUrl</c> okunuyordu ve o TEK bir alan adı. Uygulama başka
/// bir adresten yayınlandığında çiçekçiye giden SMS <b>yanlış adrese</b>
/// götürüyordu: uygulama <c>akillisehir…</c> altında çalışıyor, SMS
/// <c>randevu…</c> yazıyor ve bağlantı hiç açılmıyordu.
/// </para>
/// <para>
/// Aynı hata kimlik sağlayıcı dönüş adresinde de vardı; orada sonuç
/// sağlayıcının <c>redirect_uri mismatch</c> ile isteği reddetmesi olurdu.
/// </para>
/// </remarks>
public class AdresCozucuTests
{
    private static IAdresCozucu Cozucu(string? sema, string? host, string? tabanAyari)
    {
        var erisim = new HttpContextAccessor();

        if (sema is not null && host is not null)
        {
            var baglam = new DefaultHttpContext();
            baglam.Request.Scheme = sema;
            baglam.Request.Host = new HostString(host);
            erisim.HttpContext = baglam;
        }

        return new AdresCozucu(erisim, new ApplicationOptions { BaseUrl = tabanAyari ?? string.Empty });
    }

    [Theory]
    [InlineData("https", "akillisehir.sivas.bel.tr", "https://akillisehir.sivas.bel.tr")]
    [InlineData("https", "baska-kurum.gov.tr", "https://baska-kurum.gov.tr")]
    [InlineData("http", "localhost:5099", "http://localhost:5099")]
    public void Taban_istekten_gelir(string sema, string host, string beklenen)
        => Assert.Equal(beklenen, Cozucu(sema, host, "https://ayardaki-bayat-adres.example").Taban());

    /// <summary>
    /// İstek yoksa ayara düşülür.
    /// </summary>
    /// <remarks>
    /// Arka plan servisleri (<c>FirebaseWorker</c>, <c>TekrarUfkuWorker</c>)
    /// bir HTTP isteğinin içinde değil; orada tahmin edilecek bir alan adı
    /// yok.
    /// </remarks>
    [Fact]
    public void Istek_yokken_ayara_dusulur()
        => Assert.Equal("https://ayar.example", Cozucu(null, null, "https://ayar.example/").Taban());

    [Theory]
    [InlineData("/cicek-teslim/abc", "https://kurum.gov.tr/cicek-teslim/abc")]
    [InlineData("cicek-teslim/abc", "https://kurum.gov.tr/cicek-teslim/abc")]
    [InlineData("/api/v2/openid/geri-donus", "https://kurum.gov.tr/api/v2/openid/geri-donus")]
    public void Mutlak_adres_tek_egik_cizgiyle_birlesir(string goreli, string beklenen)
        => Assert.Equal(beklenen, Cozucu("https", "kurum.gov.tr", null).Mutlak(goreli));

    /// <summary>
    /// Çiçek SMS'i ve OpenID dönüş adresi SABİT ayardan OKUMAZ.
    /// </summary>
    /// <remarks>
    /// Kaynak taranıyor: bir gün biri kolaylık olsun diye
    /// <c>_uygulamaAyari.BaseUrl</c>'e geri dönerse hata çalışma anında
    /// sessizdir — bağlantı üretilir, yalnızca yanlış yere gider.
    /// </remarks>
    [Theory]
    [InlineData("KentOS.Mini.Web/Services/AjandaService.cs", "cicek-teslim")]
    [InlineData("KentOS.Mini.Web/Services/V2/OpenIdServisi.cs", "TamDonusAdresi")]
    public void Disa_verilen_adresler_sabit_ayardan_uretilmez(string dosya, string _)
    {
        var kaynak = File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
            dosya.Replace('/', Path.DirectorySeparatorChar)));

        Assert.Contains("_adresCozucu", kaynak);
        Assert.DoesNotContain("BaseUrl.TrimEnd", kaynak);
    }
}
