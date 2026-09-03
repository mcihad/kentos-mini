using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using KentOS.Kalem.Application.Dto.V2.IsTakip;
using KentOS.Kalem.Web.Filters;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Web.Controllers.V2;

/// <summary>
/// VATANDAŞ PORTALI — uygulamanın TEK anonim yazma ucu.
/// </summary>
/// <remarks>
/// <para>
/// <b>"Her v2 controller'ı JWT ilan eder" kuralı burada BİLEREK kırılıyor.</b>
/// Vatandaştan bildirim almak, kimliği doğrulanmamış bir kaynağın
/// veritabanına satır yazması demek ve bunun başka bir yolu yok. Bu yüzden
/// controller <see cref="V2ControllerBase"/>'den TÜREMİYOR: o taban JWT
/// zorunlu kılıyor. Doğrulama ve hata filtreleri elle bağlanıyor.
/// </para>
///
/// <para>
/// <b>Kırılan kuralın karşılığında konan korumalar:</b>
/// </para>
/// <list type="bullet">
///   <item><b>Kurum ayarındaki bayrak kapalıysa uçlar HİÇ YOK</b> (404).
///         Portal varsayılan olarak kapalı; açmak yöneticinin ayrı bir
///         kararı.</item>
///   <item>Tek controller, üç yazma ucu — yüzey mümkün olduğunca dar.</item>
///   <item>Her uçta HIZ SINIRI, IP başına.</item>
///   <item>Telefon doğrulaması: kod karma olarak saklanıyor, deneme sınırlı,
///         süre kısa.</item>
///   <item>Numara başına ayrı sınır — hız sınırı IP'ye bakıyor, dağıtık bir
///         kaynak tek numaraya SMS yağdırabilirdi.</item>
///   <item>Fotoğraflar <c>StorageArea.Private</c>; imzalı ve kısa ömürlü bir
///         anahtar olmadan yüklenemiyor.</item>
///   <item>Yanıtta yalnızca takip numarası — birim, personel ve görev
///         kimliği dışarıya sızmıyor.</item>
/// </list>
///
/// <para>
/// <b>Okuma ucu YOK.</b> "Takip numaramla kaydımı sorgulayayım" özelliği
/// cazip ama numarayı bilen herkese o vatandaşın adını, telefonunu ve
/// adresini açardı; sorgulama eklenecekse telefon doğrulamasının ardına
/// konmalı.
/// </para>
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route("api/v2/bildir")]
[ServiceFilter(typeof(V2HataFiltresi))]
[ServiceFilter(typeof(V2DogrulamaFiltresi))]
/*
  KAPI HER ŞEYDEN ÖNCE.

  Varsayılan sırada kapalı portal, bozuk bir gövdeye 400 dönüyordu: yani
  "burada bir uç var, sadece gövden hatalı" diyordu. Kapalı portal hiç var
  olmamış bir portaldan ayırt edilememeli.

  `-2001` keyfi değil: 400'ü döndüren şey bizim doğrulama filtremiz değil,
  `[ApiController]`'ın kendi `ModelStateInvalidFilter`'ı ve o `-2000`'de
  çalışıyor. Kapının ondan da önde olması gerekiyordu. Ölçüldü.
*/
[ServiceFilter(typeof(VatandasPortaliFiltresi), Order = -2001)]
[Produces("application/json")]
[EnableRateLimiting(HizSiniri.VatandasPortali)]
public class BildirimPortalController(
    IVatandasBildirimServisi _servis) : ControllerBase
{
    /// <summary>Telefona doğrulama kodu gönderir.</summary>
    [HttpPost("kod")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> KodAsync(
        [FromBody] DogrulamaIstegiDto istek, CancellationToken iptal)
    {
        await _servis.KodGonderAsync(istek.Telefon, IstekIp(), iptal);
        return NoContent();
    }

    /// <summary>Kodu doğrular ve kısa ömürlü bilet döner.</summary>
    [HttpPost("dogrula")]
    [ProducesResponseType<DogrulamaSonucuDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<DogrulamaSonucuDto> DogrulaAsync(
        [FromBody] DogrulamaOnayDto istek, CancellationToken iptal) =>
        _servis.KodDogrulaAsync(istek.Telefon, istek.Kod, IstekIp(), iptal);

    /// <summary>Bildirimi kaydeder. Yalnızca takip numarası döner.</summary>
    [HttpPost]
    [ProducesResponseType<VatandasBildirimiSonucuDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<VatandasBildirimiSonucuDto> BildirAsync(
        [FromBody] VatandasBildirimiIstegiDto istek, CancellationToken iptal) =>
        _servis.BildirAsync(istek, IstekIp(), iptal);

    /// <summary>
    /// Bildirime fotoğraf ekler.
    /// </summary>
    /// <remarks>
    /// Bildirim kimliği DEĞİL, imzalı ve kısa ömürlü bir anahtar alıyor:
    /// kimlik doğrudan kabul edilseydi sıralı bir sayı deneyerek başkasının
    /// bildirimine fotoğraf eklemek mümkün olurdu.
    /// </remarks>
    [HttpPost("fotograf")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FotografAsync(
        [FromForm] string anahtar, IFormFile dosya, CancellationToken iptal)
    {
        if (dosya is null || dosya.Length == 0)
            throw new Exceptions.BusinessRuleException("Dosya boş.");

        using var akis = dosya.OpenReadStream();
        using var bellek = new MemoryStream();
        await akis.CopyToAsync(bellek, iptal);

        await _servis.FotografEkleAsync(
            anahtar,
            new IsYuklenenDosya(dosya.FileName, dosya.ContentType, bellek.ToArray()),
            iptal);

        return NoContent();
    }

    /// <summary>
    /// İsteğin geldiği IP.
    /// </summary>
    /// <remarks>
    /// Ters vekil arkasında <c>RemoteIpAddress</c> vekilin adresini verir;
    /// <c>ForwardedHeaders</c> ara katmanı kuruluysa gerçek adres oraya
    /// yazılır. Kayıt yalnızca kötüye kullanım araştırması için tutuluyor,
    /// bir yetki kararına girmiyor — bu yüzden başlığa güvenmek burada
    /// kabul edilebilir bir risk.
    /// </remarks>
    private string? IstekIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}
