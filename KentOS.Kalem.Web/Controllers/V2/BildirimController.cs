using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Application.Dto.V2.Ortak;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Web.Controllers.V2;

/// <summary>Tarayıcı push jetonunun kaydı ve silinmesi.</summary>
[Route("api/v2/bildirim")]
public class BildirimController(
    IWebBildirimServisi _bildirimServisi,
    IBildirimMerkeziServisi _merkez,
    ICurrentUserService _mevcutKullanici) : V2ControllerBase
{
    /// <summary>Oturum sahibinin sayısal kimliği; yoksa 403.</summary>
    private async Task<long> KullaniciIdAsync() =>
        await _mevcutKullanici.GetUserIdAsync()
        ?? throw new UnauthorizedAccessException("Oturum kullanıcısı çözülemedi.");

    // ─────────────────────────────────────────── bildirim merkezi

    /// <summary>Kullanıcının bildirimleri (en yeni önce).</summary>
    /// <remarks>
    /// Aynı bildirim kullanıcının her cihazı için ayrı satır üretiyor;
    /// liste bunları TEKİLLEŞTİRİR, yoksa iki cihazlı kullanıcı her şeyi
    /// çift görürdü.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<SayfaliSonuc<BildirimDto>>(StatusCodes.Status200OK)]
    /// <param name="tumGecmis">
    /// <c>true</c> ise ZAMAN PENCERESİ uygulanmaz ve tüm geçmiş döner —
    /// "Tüm bildirimler" sayfası bunu kullanır. Varsayılan <c>false</c>:
    /// zil ve rozet yalnızca son 30 güne bakar, aksi hâlde iki yıllık giden
    /// kutusu (on binden fazla kayıt) ilk açılışta kullanıcının üstüne
    /// yığılıyordu.
    /// </param>
    public async Task<SayfaliSonuc<BildirimDto>> ListeAsync(
        [FromQuery] SayfaIstegi istek,
        [FromQuery] bool yalnizcaOkunmamis = false,
        [FromQuery] bool tumGecmis = false)
        => await _merkez.ListeAsync(await KullaniciIdAsync(), istek, yalnizcaOkunmamis, tumGecmis);

    /// <summary>Okunmamış bildirim sayısı — appbar rozetinin kaynağı.</summary>
    [HttpGet("okunmamis-sayi")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<int> OkunmamisSayiAsync()
        => await _merkez.OkunmamisSayisiAsync(await KullaniciIdAsync());

    /// <summary>Tek bildirimi okundu işaretler.</summary>
    [HttpPost("{id:long}/okundu")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> OkunduAsync(long id)
    {
        await _merkez.OkunduIsaretleAsync(await KullaniciIdAsync(), id);
        return NoContent();
    }

    /// <summary>Tümünü okundu işaretler.</summary>
    [HttpPost("tumu-okundu")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<int> TumuOkunduAsync()
        => await _merkez.TumunuOkunduIsaretleAsync(await KullaniciIdAsync());

    /// <summary>Bildirimi siler.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SilAsync(long id)
    {
        await _merkez.SilAsync(await KullaniciIdAsync(), id);
        return NoContent();
    }

    /// <summary>Okunmuş bildirimleri toplu siler.</summary>
    /// <remarks>Kuyrukta bekleyen (henüz gönderilmemiş) satırlara dokunulmaz.</remarks>
    [HttpDelete("okunanlar")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<int> OkunanlariSilAsync()
        => await _merkez.OkunanlariSilAsync(await KullaniciIdAsync());

    // ─────────────────────────────────────────────── push jetonu

    /// <summary>
    /// Tarayıcı push jetonunu kaydeder.
    /// </summary>
    /// <remarks>
    /// v1'deki <c>GET /api/SettingsApi/UpdateFcmToken?fcmToken=</c> aksine POST
    /// ve gövdeyle: jeton sorgu dizesinde giderse erişim günlüklerine, vekil
    /// sunucu günlüklerine ve tarayıcı geçmişine düşer.
    /// </remarks>
    [HttpPost("web-jeton")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> JetonKaydetAsync([FromBody] JetonIstegi istek)
    {
        var kullaniciId = await _mevcutKullanici.GetUserIdAsync();
        if (kullaniciId is null) return Forbid();

        await _bildirimServisi.JetonKaydetAsync(kullaniciId.Value, istek.Jeton);
        return NoContent();
    }

    /// <summary>
    /// Çıkışta tarayıcı push jetonunu siler.
    /// </summary>
    /// <remarks>
    /// Ortak bilgisayarda bir sonraki kullanıcının, önceki kullanıcının
    /// bildirimlerini almasını engeller.
    /// </remarks>
    [HttpDelete("web-jeton")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> JetonSilAsync([FromBody] JetonIstegi istek)
    {
        var kullaniciId = await _mevcutKullanici.GetUserIdAsync();
        if (kullaniciId is null) return Forbid();

        await _bildirimServisi.JetonSilAsync(kullaniciId.Value, istek.Jeton);
        return NoContent();
    }

    /// <summary>
    /// Mobil uygulama push jetonunu kaydeder.
    /// </summary>
    /// <remarks>
    /// v1 karşılığı <c>GET /api/SettingsApi/UpdateFcmToken?fcmToken=</c> —
    /// <b>durum değiştiren bir GET</b> ve jetonu sorgu dizesinde taşıyor,
    /// yani erişim günlüklerine düşüyor. v2 POST + gövde kullanır ve jetonu
    /// başka kullanıcılardan geri çalar (ortak telefonda bildirim sızıntısı).
    /// v1 ucu aynı sütuna yazmaya devam eder; ikisi bir arada çalışır.
    /// </remarks>
    [HttpPost("mobil-jeton")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MobilJetonKaydetAsync([FromBody] JetonIstegi istek)
    {
        var kullaniciId = await _mevcutKullanici.GetUserIdAsync();
        if (kullaniciId is null) return Forbid();

        await _bildirimServisi.MobilJetonKaydetAsync(kullaniciId.Value, istek.Jeton);
        return NoContent();
    }

    /// <summary>Çıkışta mobil push jetonunu siler.</summary>
    [HttpDelete("mobil-jeton")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MobilJetonSilAsync([FromBody] JetonIstegi istek)
    {
        var kullaniciId = await _mevcutKullanici.GetUserIdAsync();
        if (kullaniciId is null) return Forbid();

        await _bildirimServisi.MobilJetonSilAsync(kullaniciId.Value, istek.Jeton);
        return NoContent();
    }
}

/// <summary>Jeton taşıyan istek gövdesi.</summary>
public class JetonIstegi
{
    [JsonPropertyName("jeton")]
    public string Jeton { get; set; } = string.Empty;
}
