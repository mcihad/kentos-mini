using KentOS.Mini.Web.AuthPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>
/// Sunucu hata kayıtları.
/// </summary>
/// <remarks>
/// <para>
/// <b>Yalnızca <c>Sistem</c> rolü.</b> Admin bile göremez: kayıtlarda istek
/// gövdeleri, IP adresleri ve yığın izleri var — yani hem kişisel veri hem de
/// saldırı yüzeyini tarif eden bilgi. Bu, sistemi ayakta tutan kişinin
/// ekranı; yönetim ekranı değil.
/// </para>
/// </remarks>
[Route("api/v2/hata")]
[Izin(Izinler.SistemHata)]
public class HataController(IHataKaydiServisi _hatalar) : V2ControllerBase
{
    /// <summary>Hata listesi — en son görülen üstte.</summary>
    [HttpGet]
    [ProducesResponseType<SayfaliSonuc<HataOzetDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<HataOzetDto>> ListeAsync([FromQuery] HataSuzgeci suzgec)
        => _hatalar.ListeAsync(suzgec);

    /// <summary>Hata detayı — yığın izi, istek gövdesi ve AI raporu dahil.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<HataDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DetayAsync(long id)
        => Ok(await _hatalar.DetayAsync(id));

    /// <summary>Çözüm notu ve durumu kaydeder.</summary>
    [Izin(Izinler.SistemHata)]
    [HttpPut("{id:long}")]
    [ProducesResponseType<HataDetayDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> NotKaydetAsync(long id, [FromBody] HataNotIstegi istek)
        => Ok(await _hatalar.NotKaydetAsync(
            id, istek.Notlar, istek.Cozuldu, User.Identity?.Name ?? "?"));

    /// <summary>Tek kaydı siler.</summary>
    [Izin(Izinler.SistemHata)]
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SilAsync(long id)
    {
        await _hatalar.SilAsync(id);
        return NoContent();
    }

    /// <summary>Çözülmüş kayıtların tamamını siler.</summary>
    /// <remarks>
    /// Yalnızca ÇÖZÜLENLER: çözülmemiş bir kaydı toplu silmek, üzerinde
    /// çalışılan bir sorunun izini kaybetmek demek.
    /// </remarks>
    [Izin(Izinler.SistemHata)]
    [HttpDelete("cozulenler")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CozulenleriSilAsync()
        => Ok(await _hatalar.CozulenleriSilAsync());
}
