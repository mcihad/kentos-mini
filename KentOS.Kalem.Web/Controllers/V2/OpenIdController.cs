using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using KentOS.Kalem.Application.Dto.V2.OpenId;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.AuthPolicies;

namespace KentOS.Kalem.Web.Controllers.V2;

/// <summary>
/// Kurumsal kimlik sağlayıcı (OpenID Connect).
/// </summary>
/// <remarks>
/// İki ayrı kitle: <b>yetkili</b> ayarı görür ve değiştirir
/// (<c>sistem.openid</c>), <b>giriş ekranı</b> ise henüz kimliği olmayan
/// bir ziyaretçi — o yüzden giriş yolundaki üç uç anonim.
/// </remarks>
[Route("api/v2/openid")]
[Izin(Izinler.SistemOpenid)]
public class OpenIdController(IOpenIdService _servis) : V2ControllerBase
{
    /// <summary>Ayarı okur; istemci sırrı DÖNMEZ, yalnızca tanımlı olup olmadığı.</summary>
    [HttpGet]
    [ProducesResponseType<OpenIdAyarDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> AyarAsync() => Ok(await _servis.AyarAsync());

    /// <summary>Ayarı kaydeder. Boş istemci sırrı "değiştirme" demektir.</summary>
    /// <remarks>
    /// İzin sınıf düzeyinde de var; burada AÇIKÇA tekrarlanıyor. Okuma ve
    /// yazma aynı izinle korunuyor — bu ayarda "görebilen ama
    /// değiştiremeyen" diye bir kullanıcı yok: istemci kimliğini görebilen
    /// kişi zaten sağlayıcı yapılandırmasının tamamını biliyor.
    /// </remarks>
    [Izin(Izinler.SistemOpenid)]
    [HttpPut]
    [ProducesResponseType<OpenIdAyarDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> KaydetAsync([FromBody] OpenIdAyarIstegi istek)
        => Ok(await _servis.KaydetAsync(istek));

    /// <summary>Sağlayıcıya gerçekten ulaşılıyor mu?</summary>
    /// <remarks>
    /// Ayar ekranındaki "bağlantıyı sına" düğmesi. Kaydetmeden önce
    /// denenebilmesi önemli: yanlış adresle kaydedip giriş ekranına
    /// çalışmayan bir düğme koymak, kullanıcıyı geri dönemeyeceği bir
    /// sayfada bırakıyor.
    /// </remarks>
    [Izin(Izinler.SistemOpenid)]
    [HttpPost("sina")]
    [ProducesResponseType<OpenIdSinamaDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SinaAsync() => Ok(await _servis.SinaAsync());

    // ─────────────────────────────────────────────────── giriş yolu (anonim)

    /// <summary>
    /// Giriş ekranı: sağlayıcı düğmesi çizilsin mi?
    /// </summary>
    /// <remarks>
    /// <b>Anonim</b>, çünkü çağıran henüz giriş yapmamış. Yanıt yalnızca
    /// "kullanılabilir mi" ve düğme metni; yetkili adres, istemci kimliği
    /// ya da kapsamlar buradan sızmaz.
    /// </remarks>
    [AllowAnonymous]
    [HttpGet("giris-durumu")]
    [ProducesResponseType<OpenIdGirisDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GirisDurumuAsync() => Ok(await _servis.GirisDurumuAsync());

    /// <summary>Kullanıcıyı sağlayıcıya yönlendirir.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(Filters.HizSiniri.Giris)]
    [HttpGet("baslat")]
    public async Task<IActionResult> BaslatAsync([FromQuery] string? donus)
        => Redirect(await _servis.YetkilendirmeAdresiAsync(donus));

    /// <summary>
    /// Sağlayıcıdan dönüş: kodu jetona çevirir ve uygulamaya yollar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Jeton ADRES PARÇASINDA (<c>#</c>) taşınıyor, sorgu dizesinde
    /// değil.</b> Sorgu dizesi sunucu günlüklerine, ters vekil kayıtlarına ve
    /// <c>Referer</c> başlığına düşüyor; adres parçası tarayıcıdan hiç
    /// çıkmıyor. SPA onu okuyup hemen adres çubuğundan siliyor.
    /// </para>
    /// <para>
    /// Hata da aynı yolla dönüyor: kullanıcı bir API gövdesi değil, giriş
    /// ekranında anlaşılır bir mesaj görmeli.
    /// </para>
    /// </remarks>
    [AllowAnonymous]
    [EnableRateLimiting(Filters.HizSiniri.Giris)]
    [HttpGet("geri-donus")]
    public async Task<IActionResult> GeriDonusAsync(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            // Sağlayıcıda "izin verme" diyen kullanıcı da buraya düşüyor;
            // bu bir hata değil, bir karar. Giriş ekranına sessizce dönülür.
            return Redirect($"/giris#saglayiciHata={Uri.EscapeDataString("Giriş tamamlanmadı.")}");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return Redirect($"/giris#saglayiciHata={Uri.EscapeDataString("Giriş yanıtı eksik.")}");
        }

        try
        {
            var (jeton, gecerlilikSonu, donusYolu) = await _servis.GeriDonusAsync(code, state);

            // Geçerlilik sonu da taşınıyor: istemci jetonu `{jeton,
            // gecerlilikSonu}` olarak saklıyor ve eksik yazılan bir kayıt,
            // oturumun ne zaman biteceğini bilemez hâle getirirdi.
            return Redirect($"/giris#jeton={Uri.EscapeDataString(jeton)}"
                + $"&biti{"s"}={Uri.EscapeDataString(gecerlilikSonu?.ToString("o") ?? string.Empty)}"
                + $"&donus={Uri.EscapeDataString(donusYolu)}");
        }
        catch (Exceptions.BusinessRuleException h)
        {
            return Redirect($"/giris#saglayiciHata={Uri.EscapeDataString(h.Message)}");
        }
    }
}
