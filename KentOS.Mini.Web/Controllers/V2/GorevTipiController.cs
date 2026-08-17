using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto.V2.IsTakip;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>
/// GÖREV TİPİ — hizmet standardının tanımlandığı yer.
/// </summary>
/// <remarks>
/// Okuma <c>gorev.goruntule</c> ile de açık: görev açacak personelin hangi
/// tipleri seçebileceğini görmesi gerekiyor ve bunun için tanım yönetme
/// yetkisi istemek, her personeli standart değiştirebilir yapardı.
/// </remarks>
[Route("api/v2/gorev-tipi")]
public class GorevTipiController(IGorevTipiServisi _servis) : V2ControllerBase
{
    [HttpGet]
    [Izin(Izinler.GorevTipYonet, Izinler.GorevGoruntule)]
    [ProducesResponseType<SayfaliSonuc<GorevTipiDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<GorevTipiDto>> ListeAsync(
        [FromQuery] SayfaIstegi istek,
        [FromQuery] bool yalnizKullanimda,
        CancellationToken iptal) =>
        _servis.ListeAsync(istek, yalnizKullanimda, iptal);

    /// <summary>Etkin birimin kullanabileceği tipler — görev açma ekranı için.</summary>
    [HttpGet("kullanilabilir")]
    [Izin(Izinler.GorevGoruntule, Izinler.GorevEkle)]
    [ProducesResponseType<List<GorevTipiDto>>(StatusCodes.Status200OK)]
    public Task<List<GorevTipiDto>> KullanilabilirlerAsync(CancellationToken iptal) =>
        _servis.KullanilabilirlerAsync(iptal);

    [HttpGet("{id:long}")]
    [Izin(Izinler.GorevTipYonet, Izinler.GorevGoruntule)]
    [ProducesResponseType<GorevTipiDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<GorevTipiDto> GetirAsync(long id, CancellationToken iptal) =>
        _servis.GetirAsync(id, iptal);

    [HttpPost]
    [Izin(Izinler.GorevTipYonet)]
    [ProducesResponseType<GorevTipiDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GorevTipiDto>> OlusturAsync(
        [FromBody] GorevTipiKayitDto istek, CancellationToken iptal)
    {
        var tip = await _servis.OlusturAsync(istek, iptal);
        return CreatedAtAction(nameof(GetirAsync), new { id = tip.Id }, tip);
    }

    [HttpPut("{id:long}")]
    [Izin(Izinler.GorevTipYonet)]
    [ProducesResponseType<GorevTipiDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<GorevTipiDto> GuncelleAsync(
        long id, [FromBody] GorevTipiKayitDto istek, CancellationToken iptal) =>
        _servis.GuncelleAsync(id, istek, iptal);

    [HttpDelete("{id:long}")]
    [Izin(Izinler.GorevTipYonet)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SilAsync(long id, CancellationToken iptal)
    {
        await _servis.SilAsync(id, iptal);
        return NoContent();
    }
}
