using KentOS.Mini.Application.Dto.V2.OpenId;
using KentOS.Mini.Web.Services.V2;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// KURUMSAL KİMLİK SAĞLAYICI — sızdırmaması gerekenler.
/// </summary>
public class OpenIdTests
{
    /// <summary>
    /// AÇIK YÖNLENDİRME kapalı.
    /// </summary>
    /// <remarks>
    /// Dönüş yolu sorgu dizesinden geliyor. Süzülmeseydi
    /// <c>?donus=https://saldirgan.example</c> ile kullanıcı, giriş yaptıktan
    /// HEMEN SONRA saldırganın sayfasına gönderilebilirdi — üstelik jeton
    /// adres parçasında, yani saldırganın eline geçerek.
    /// </remarks>
    [Theory]
    [InlineData("https://saldirgan.example", "/")]
    [InlineData("http://saldirgan.example/x", "/")]
    [InlineData("//saldirgan.example", "/")]          // protokole yakın
    [InlineData("///saldirgan.example", "/")]
    [InlineData("javascript:alert(1)", "/")]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("   ", "/")]
    public void Disariya_yonlendirme_reddedilir(string? gelen, string beklenen)
        => Assert.Equal(beklenen, OpenIdServisi.GuvenliDonusYolu(gelen));

    [Theory]
    [InlineData("/ajanda")]
    [InlineData("/talepler/42")]
    [InlineData("/")]
    public void Uygulama_ici_yollar_korunur(string yol)
        => Assert.Equal(yol, OpenIdServisi.GuvenliDonusYolu(yol));

    /// <summary>
    /// İstemci sırrı DTO'da yok.
    /// </summary>
    /// <remarks>
    /// Ayar ekranı sırrı forma dolduramaz ve doldurmamalı: yanıtta taşınan
    /// bir sır, tarayıcı geçmişine ve önbelleğe düşer. Ekranın tek ihtiyacı
    /// "tanımlı mı?" sorusunun cevabı — o da <c>SirTanimli</c>.
    /// </remarks>
    [Fact]
    public void Ayar_yaniti_istemci_sirrini_tasimaz()
    {
        var sizan = typeof(OpenIdAyarDto)
            .GetProperties()
            .Where(p => p.Name.Contains("Sir", StringComparison.OrdinalIgnoreCase)
                        && p.Name != nameof(OpenIdAyarDto.SirTanimli))
            .Select(p => p.Name)
            .ToList();

        Assert.True(sizan.Count == 0, $"Ayar DTO'su sır sızdırıyor: {string.Join(", ", sizan)}");
    }

    /// <summary>
    /// ANONİM yanıt yapılandırmayı sızdırmaz.
    /// </summary>
    /// <remarks>
    /// <c>giris-durumu</c> giriş yapmamış herkese açık. Yetkili adres,
    /// istemci kimliği ya da kapsamlar oradan dönseydi, kurumun kimlik
    /// altyapısı tarayıcıya açık bir uçtan haritalanabilirdi.
    /// </remarks>
    [Fact]
    public void Anonim_giris_yaniti_yalnizca_dugmeyi_tarif_eder()
    {
        var alanlar = typeof(OpenIdGirisDto).GetProperties().Select(p => p.Name).OrderBy(a => a);

        Assert.Equal(
            new[] { nameof(OpenIdGirisDto.GorunenAd), nameof(OpenIdGirisDto.Kullanilabilir) }
                .OrderBy(a => a),
            alanlar);
    }
}
