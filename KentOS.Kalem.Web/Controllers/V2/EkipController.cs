using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Application.Dto.V2.IsTakip;
using KentOS.Kalem.Application.Dto.V2.Ortak;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Web.AuthPolicies;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Web.Controllers.V2;

/// <summary>
/// EKİP — birime bağlı kalıcı çalışma grubu.
/// </summary>
/// <remarks>
/// Okuma <c>gorev.atama</c> ile de açık: göreve ekip atayacak kişinin ekip
/// listesini görmesi gerekiyor, ama ekibi düzenleyebilmesi gerekmiyor.
/// </remarks>
[Route("api/v2/ekip")]
public class EkipController(IEkipServisi _servis) : V2ControllerBase
{
    [HttpGet]
    [Izin(Izinler.EkipYonet, Izinler.GorevAtama, Izinler.GorevGoruntule)]
    [ProducesResponseType<SayfaliSonuc<EkipDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<EkipDto>> ListeAsync(
        [FromQuery] SayfaIstegi istek,
        [FromQuery] bool altBirimlerDahil,
        [FromQuery] bool yalnizKullanimda,
        CancellationToken iptal) =>
        _servis.ListeAsync(istek, altBirimlerDahil, yalnizKullanimda, iptal);

    [HttpGet("{id:long}")]
    [Izin(Izinler.EkipYonet, Izinler.GorevAtama, Izinler.GorevGoruntule)]
    [ProducesResponseType<EkipDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EkipDto> GetirAsync(long id, CancellationToken iptal) =>
        _servis.GetirAsync(id, iptal);

    [HttpPost]
    [Izin(Izinler.EkipYonet)]
    [ProducesResponseType<EkipDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EkipDto>> OlusturAsync(
        [FromBody] EkipKayitDto istek, CancellationToken iptal)
    {
        var ekip = await _servis.OlusturAsync(istek, iptal);

        // Adres ELLE yazılıyor — MVC eylem adından `Async` ekini düşürdüğü
        // için `nameof(GetirAsync)` hiçbir rotayla eşleşmiyor ve uç 500
        // dönüyordu. Gerekçenin tamamı `GorevTipiController`de yazılı.
        return Created($"/api/v2/ekip/{ekip.Id}", ekip);
    }

    [HttpPut("{id:long}")]
    [Izin(Izinler.EkipYonet)]
    [ProducesResponseType<EkipDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EkipDto> GuncelleAsync(
        long id, [FromBody] EkipKayitDto istek, CancellationToken iptal) =>
        _servis.GuncelleAsync(id, istek, iptal);

    [HttpDelete("{id:long}")]
    [Izin(Izinler.EkipYonet)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SilAsync(long id, CancellationToken iptal)
    {
        await _servis.SilAsync(id, iptal);
        return NoContent();
    }
}
