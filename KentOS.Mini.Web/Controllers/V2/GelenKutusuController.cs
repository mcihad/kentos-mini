using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto.V2.IsTakip;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>
/// BİRİM GELEN KUTUSU — birimden birime iş devri.
/// </summary>
/// <remarks>
/// Kayıtlar görev tamamlandığında tipteki devir kuralıyla kendiliğinden
/// düşüyor; buradan yalnızca KARAR veriliyor. Kabul görev açar, ret kaynak
/// birime gerekçeli bildirim gönderir.
/// </remarks>
[Route("api/v2/gelen-kutusu")]
public class GelenKutusuController(IGelenKutusuServisi _servis) : V2ControllerBase
{
    [HttpGet]
    [Izin(Izinler.GelenKutusuGoruntule)]
    [ProducesResponseType<SayfaliSonuc<GelenKutusuDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<GelenKutusuDto>> ListeAsync(
        [FromQuery] SayfaIstegi istek,
        [FromQuery] GelenKutusuDurumu? durum,
        [FromQuery] bool altBirimlerDahil,
        CancellationToken iptal) =>
        _servis.ListeAsync(istek, durum, altBirimlerDahil, iptal);

    /// <summary>Bekleyen kayıt sayısı — menüdeki rozet.</summary>
    [HttpGet("bekleyen")]
    [Izin(Izinler.GelenKutusuGoruntule)]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public Task<int> BekleyenAsync(CancellationToken iptal) =>
        _servis.BekleyenSayisiAsync(iptal);

    /// <summary>Kaydı kabul eder ve HEDEF BİRİMDE görev açar.</summary>
    [HttpPost("{id:long}/kabul")]
    [Izin(Izinler.GelenKutusuKarar)]
    [ProducesResponseType<GelenKutusuDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<GelenKutusuDto> KabulAsync(
        long id, [FromBody] GelenKutusuKabulDto istek, CancellationToken iptal) =>
        _servis.KabulAsync(id, istek, iptal);

    /// <summary>Kaydı reddeder — kaynak birime gerekçeli bildirim gider.</summary>
    [HttpPost("{id:long}/reddet")]
    [Izin(Izinler.GelenKutusuKarar)]
    [ProducesResponseType<GelenKutusuDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<GelenKutusuDto> ReddetAsync(
        long id, [FromBody] GelenKutusuRetDto istek, CancellationToken iptal) =>
        _servis.ReddetAsync(id, istek.Gerekce, iptal);

    /// <summary>Bilgilendirme kaydını okundu işaretler — karar gerektirmez.</summary>
    [HttpPost("{id:long}/okundu")]
    [Izin(Izinler.GelenKutusuKarar)]
    [ProducesResponseType<GelenKutusuDto>(StatusCodes.Status200OK)]
    public Task<GelenKutusuDto> OkunduAsync(long id, CancellationToken iptal) =>
        _servis.OkunduAsync(id, iptal);
}

/// <summary>
/// GECİKME PANOSU — birim karnesi ve süre aşımları.
/// </summary>
/// <remarks>
/// Mevcut <c>IstatistikController</c>'a eklenmedi: o ekran makamın
/// etkinlik ve talep sayılarını gösteriyor, burası birimlerin iş yükünü.
/// Aynı controller'da toplamak, iki ekranın da ötekinin izniyle açılması
/// demekti.
/// </remarks>
[Route("api/v2/is-istatistik")]
public class IsIstatistikController(IIsIstatistikServisi _servis) : V2ControllerBase
{
    [HttpGet]
    [Izin(Izinler.IsIstatistik)]
    [ProducesResponseType<IsIstatistikDto>(StatusCodes.Status200OK)]
    public Task<IsIstatistikDto> PanoAsync(
        [FromQuery] bool altBirimlerDahil, CancellationToken iptal) =>
        _servis.PanoAsync(altBirimlerDahil, iptal);
}
