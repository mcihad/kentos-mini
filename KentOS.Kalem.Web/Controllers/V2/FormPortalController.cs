using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using KentOS.Kalem.Application.Dto.V2.Form;
using KentOS.Kalem.Web.Filters;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Web.Controllers.V2;

/// <summary>
/// VATANDAŞIN GÖRDÜĞÜ FORM — giriş gerektirmez.
/// </summary>
/// <remarks>
/// <para>
/// <b>Uygulamanın ikinci anonim yazma yüzeyi.</b> Vatandaş bildirim
/// portalının kalıbını izliyor ve gerekçeleri aynı:
/// </para>
/// <list type="bullet">
///   <item><b>Ayrı controller ve ayrı rota kökü</b> (<c>/api/v2/form-portal</c>).
///     Yetkili uçlarla aynı ağaçta olsaydı, kısıtsız bir segment yetkili
///     bir ucu gölgeleyebilirdi; ayrı önek bu sınıfı tümüyle kaldırıyor ve
///     ters vekil/WAF için tek bir süzülebilir ad alanı bırakıyor.</item>
///   <item><b><see cref="V2ControllerBase"/>'den TÜREMİYOR</b>: o taban JWT
///     zorunlu kılıyor. Doğrulama ve hata filtreleri elle bağlanıyor.</item>
///   <item><b>Kurum ayarındaki bayrak kapalıysa uçlar YOK</b> (404), kapı
///     <c>Order = -2001</c> ile model doğrulamasından bile önde.</item>
///   <item><b>Hız sınırı okuma/yazma ayrı</b>, bölüm anahtarı
///     <c>ip|erisimAnahtari</c>.</item>
/// </list>
/// <para>
/// <b>Yetki belirteci adresteki anahtar.</b> Formun kimliği gövdeden
/// alınsaydı, bir formun adresinden başka bir forma yanıt yazmak mümkün
/// olurdu.
/// </para>
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route("api/v2/form-portal")]
[ServiceFilter(typeof(V2HataFiltresi))]
[ServiceFilter(typeof(V2DogrulamaFiltresi))]
[ServiceFilter(typeof(FormPortaliFiltresi), Order = -2001)]
[Produces("application/json")]
public class FormPortalController(IFormYanitServisi _servis) : ControllerBase
{
    /// <summary>Yayındaki formu getirir.</summary>
    [HttpGet("{anahtar}")]
    [EnableRateLimiting(HizSiniri.FormPortaliOkuma)]
    [ProducesResponseType<FormPortalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<FormPortalDto> FormAsync(string anahtar, CancellationToken iptal) =>
        _servis.PortalFormuAsync(anahtar, iptal);

    /// <summary>Yanıtı gönderir.</summary>
    /// <remarks>
    /// Yanıt olarak yalnızca <b>takip numarası</b> ve teşekkür içeriği
    /// dönüyor: iç kimlikler, birim ve form kimliği vatandaşa verilmiyor.
    /// </remarks>
    [HttpPost("{anahtar}")]
    [EnableRateLimiting(HizSiniri.FormPortaliYazma)]
    [ProducesResponseType<FormYanitSonucuDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public Task<FormYanitSonucuDto> GonderAsync(
        string anahtar, [FromBody] FormYanitIstegiDto istek, CancellationToken iptal) =>
        _servis.GonderAsync(anahtar, istek, IstekIp(), Tarayici(), iptal);

    /// <summary>Yarım kalan yanıtı sunucuda saklar.</summary>
    /// <remarks>
    /// Yalnızca formun <c>kaydetDevamEt</c> ayarı açıkken anlamlı; kapalıyken
    /// istemci zaten çağırmıyor. Taslak satırı <b>yanıt sayacını
    /// ARTIRMAZ</b> — yoksa yüz taslak açan biri formu kotasından kapatırdı.
    /// </remarks>
    [HttpPost("{anahtar}/taslak")]
    [EnableRateLimiting(HizSiniri.FormPortaliYazma)]
    [ProducesResponseType<FormTaslakSonucuDto>(StatusCodes.Status200OK)]
    public Task<FormTaslakSonucuDto> TaslakAsync(
        string anahtar, [FromBody] FormYanitIstegiDto istek, CancellationToken iptal) =>
        _servis.TaslakKaydetAsync(anahtar, istek, iptal);

    /// <summary>Yarım kalan yanıtı sürdürme anahtarıyla geri getirir.</summary>
    [HttpGet("{anahtar}/taslak/{surdurme}")]
    [EnableRateLimiting(HizSiniri.FormPortaliOkuma)]
    [ProducesResponseType<FormYanitIstegiDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TaslakGetirAsync(
        string anahtar, string surdurme, CancellationToken iptal)
    {
        var taslak = await _servis.TaslakGetirAsync(anahtar, surdurme, iptal);
        return taslak is null ? NotFound() : Ok(taslak);
    }

    /// <summary>Form alanına dosya yükler.</summary>
    /// <remarks>
    /// <b>Ayrı uç, gönderimle birlikte değil:</b> zorunlu bir dosya alanı
    /// doğrulamaya giriyor ve 12 MB'lık gövde doğrulamada düşerse her şey
    /// yeniden yüklenirdi. Dönen <c>surdurmeAnahtari</c> gönderimde geri
    /// gelmeli — gelmezse dosya sahipsiz kalır.
    /// </remarks>
    [HttpPost("{anahtar}/dosya")]
    [EnableRateLimiting(HizSiniri.FormPortaliYazma)]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [ProducesResponseType<FormDosyaSonucuDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<FormDosyaSonucuDto> DosyaAsync(
        string anahtar,
        [FromForm] string alanKimligi,
        [FromForm] string? surdurmeAnahtari,
        IFormFile dosya,
        CancellationToken iptal)
    {
        await using var akis = dosya.OpenReadStream();

        return await _servis.DosyaYukleAsync(
            anahtar, alanKimligi, surdurmeAnahtari,
            akis, dosya.FileName, dosya.ContentType, iptal);
    }

    /// <summary>
    /// İstek IP'si — ters vekil arkasında gerçek adres.
    /// </summary>
    /// <remarks>
    /// <c>UseForwardedHeaders</c> <c>X-Forwarded-For</c>'u zaten işliyor,
    /// yani buradaki değer IIS/nginx arkasında da doğru. Adres saklanmıyor;
    /// servis tuzlanmış özetini yazıyor.
    /// </remarks>
    private string? IstekIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? Tarayici() =>
        Request.Headers.UserAgent.ToString() is { Length: > 0 } u ? u : null;
}
