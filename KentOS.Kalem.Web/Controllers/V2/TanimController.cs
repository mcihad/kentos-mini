using KentOS.Kalem.Web.AuthPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Application.Dto.V2.Ortak;
using KentOS.Kalem.Application.Dto.V2.Referans;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Web.Controllers.V2;

/// <summary>
/// Referans (tanım) verilerinin yönetimi: etkinlik tipleri, etkinlik
/// durumları, talep durumları, mahalleler, meslekler.
/// </summary>
/// <remarks>
/// Okuma uçları <c>/api/v2/ayar/*</c> altında herkese açıktır (açılır listeler
/// için). Buradaki uçlar <b>yazma</b> içindir ve <c>Admin</c> ister — eski
/// MVC controller'larındaki yetkiyle aynı.
/// </remarks>
[Route("api/v2/tanim")]
[Izin(Izinler.TanimYonet)]
public class TanimController(IReferansServisi _referans) : V2ControllerBase
{
    // ───────────────────────────────────────────── etkinlik tipleri

    /// <summary>Etkinlik / talep tipleri.</summary>
    /// <remarks>Aynı tablo (<c>RandevuTip</c>) hem etkinlikte hem talepte kullanılıyor.</remarks>
    [HttpGet("etkinlik-tipleri")]
    [ProducesResponseType<SayfaliSonuc<TanimDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<TanimDto>> EtkinlikTipleriAsync([FromQuery] SayfaIstegi istek)
        => _referans.TanimlarAsync(TanimTuru.EtkinlikTipi, istek);

    [HttpGet("etkinlik-tipleri/{id:long}")]
    [ProducesResponseType<TanimDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public Task<TanimDto> EtkinlikTipiAsync(long id)
        => _referans.TanimAsync(TanimTuru.EtkinlikTipi, id);

    [HttpPost("etkinlik-tipleri")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType<TanimDto>(StatusCodes.Status200OK)]
    public Task<TanimDto> EtkinlikTipiOlusturAsync([FromBody] TanimIstegi istek)
        => _referans.TanimOlusturAsync(TanimTuru.EtkinlikTipi, istek);

    [HttpPut("etkinlik-tipleri/{id:long}")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType<TanimDto>(StatusCodes.Status200OK)]
    public Task<TanimDto> EtkinlikTipiGuncelleAsync(long id, [FromBody] TanimIstegi istek)
        => _referans.TanimGuncelleAsync(TanimTuru.EtkinlikTipi, id, istek);

    [HttpDelete("etkinlik-tipleri/{id:long}")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EtkinlikTipiSilAsync(long id)
    {
        await _referans.TanimSilAsync(TanimTuru.EtkinlikTipi, id);
        return NoContent();
    }

    // ──────────────────────────────────────────── etkinlik durumları

    /// <summary>Etkinlik durumları — takvimin renk kaynağı.</summary>
    [HttpGet("etkinlik-durumlari")]
    [ProducesResponseType<SayfaliSonuc<TanimDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<TanimDto>> EtkinlikDurumlariAsync([FromQuery] SayfaIstegi istek)
        => _referans.TanimlarAsync(TanimTuru.EtkinlikDurumu, istek);

    [HttpGet("etkinlik-durumlari/{id:long}")]
    [ProducesResponseType<TanimDto>(StatusCodes.Status200OK)]
    public Task<TanimDto> EtkinlikDurumuAsync(long id)
        => _referans.TanimAsync(TanimTuru.EtkinlikDurumu, id);

    [HttpPost("etkinlik-durumlari")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType<TanimDto>(StatusCodes.Status200OK)]
    public Task<TanimDto> EtkinlikDurumuOlusturAsync([FromBody] TanimIstegi istek)
        => _referans.TanimOlusturAsync(TanimTuru.EtkinlikDurumu, istek);

    [HttpPut("etkinlik-durumlari/{id:long}")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType<TanimDto>(StatusCodes.Status200OK)]
    public Task<TanimDto> EtkinlikDurumuGuncelleAsync(long id, [FromBody] TanimIstegi istek)
        => _referans.TanimGuncelleAsync(TanimTuru.EtkinlikDurumu, id, istek);

    [HttpDelete("etkinlik-durumlari/{id:long}")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EtkinlikDurumuSilAsync(long id)
    {
        await _referans.TanimSilAsync(TanimTuru.EtkinlikDurumu, id);
        return NoContent();
    }

    // ─────────────────────────────────────────────── talep durumları

    /// <summary>Talep durumları — talep listesinin renk kaynağı.</summary>
    [HttpGet("talep-durumlari")]
    [ProducesResponseType<SayfaliSonuc<TanimDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<TanimDto>> TalepDurumlariAsync([FromQuery] SayfaIstegi istek)
        => _referans.TanimlarAsync(TanimTuru.TalepDurumu, istek);

    [HttpGet("talep-durumlari/{id:long}")]
    [ProducesResponseType<TanimDto>(StatusCodes.Status200OK)]
    public Task<TanimDto> TalepDurumuAsync(long id)
        => _referans.TanimAsync(TanimTuru.TalepDurumu, id);

    [HttpPost("talep-durumlari")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType<TanimDto>(StatusCodes.Status200OK)]
    public Task<TanimDto> TalepDurumuOlusturAsync([FromBody] TanimIstegi istek)
        => _referans.TanimOlusturAsync(TanimTuru.TalepDurumu, istek);

    [HttpPut("talep-durumlari/{id:long}")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType<TanimDto>(StatusCodes.Status200OK)]
    public Task<TanimDto> TalepDurumuGuncelleAsync(long id, [FromBody] TanimIstegi istek)
        => _referans.TanimGuncelleAsync(TanimTuru.TalepDurumu, id, istek);

    [HttpDelete("talep-durumlari/{id:long}")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TalepDurumuSilAsync(long id)
    {
        await _referans.TanimSilAsync(TanimTuru.TalepDurumu, id);
        return NoContent();
    }

    // ───────────────────────────────────────────────────── mahalleler

    /// <summary>Mahalleler.</summary>
    /// <remarks>
    /// Binlerce kayıt olabilir; açılır listede <c>ara</c> parametresiyle
    /// süzülmesi beklenir, tamamının çekilmesi değil.
    /// </remarks>
    [HttpGet("mahalleler")]
    [ProducesResponseType<SayfaliSonuc<AdKaydiDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<AdKaydiDto>> MahallelerAsync([FromQuery] SayfaIstegi istek)
        => _referans.MahallelerAsync(istek);

    [HttpPost("mahalleler")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType<AdKaydiDto>(StatusCodes.Status200OK)]
    public Task<AdKaydiDto> MahalleOlusturAsync([FromBody] AdKaydiIstegi istek)
        => _referans.MahalleOlusturAsync(istek);

    [HttpPut("mahalleler/{id:long}")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType<AdKaydiDto>(StatusCodes.Status200OK)]
    public Task<AdKaydiDto> MahalleGuncelleAsync(long id, [FromBody] AdKaydiIstegi istek)
        => _referans.MahalleGuncelleAsync(id, istek);

    [HttpDelete("mahalleler/{id:long}")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MahalleSilAsync(long id)
    {
        await _referans.MahalleSilAsync(id);
        return NoContent();
    }

    /// <summary>Mahalleleri satır listesinden toplu ekler.</summary>
    [HttpPost("mahalleler/ice-aktar")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType<TopluIceAktarmaSonucu>(StatusCodes.Status200OK)]
    public Task<TopluIceAktarmaSonucu> MahalleIceAktarAsync([FromBody] TopluIceAktarmaIstegi istek)
        => _referans.MahalleIceAktarAsync(istek);

    /// <summary>TÜM mahalleleri siler.</summary>
    /// <remarks>Taleplerde kullanılan mahalle varsa reddedilir.</remarks>
    [HttpDelete("mahalleler")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status400BadRequest)]
    public Task<int> MahalleTumunuSilAsync() => _referans.MahalleTumunuSilAsync();

    // ────────────────────────────────────────────────────── meslekler

    /// <summary>Meslekler.</summary>
    [HttpGet("meslekler")]
    [ProducesResponseType<SayfaliSonuc<AdKaydiDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<AdKaydiDto>> MesleklerAsync([FromQuery] SayfaIstegi istek)
        => _referans.MesleklerAsync(istek);

    [HttpPost("meslekler")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType<AdKaydiDto>(StatusCodes.Status200OK)]
    public Task<AdKaydiDto> MeslekOlusturAsync([FromBody] AdKaydiIstegi istek)
        => _referans.MeslekOlusturAsync(istek);

    [HttpPut("meslekler/{id:long}")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType<AdKaydiDto>(StatusCodes.Status200OK)]
    public Task<AdKaydiDto> MeslekGuncelleAsync(long id, [FromBody] AdKaydiIstegi istek)
        => _referans.MeslekGuncelleAsync(id, istek);

    [HttpDelete("meslekler/{id:long}")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MeslekSilAsync(long id)
    {
        await _referans.MeslekSilAsync(id);
        return NoContent();
    }

    /// <summary>Meslekleri satır listesinden toplu ekler.</summary>
    [HttpPost("meslekler/ice-aktar")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType<TopluIceAktarmaSonucu>(StatusCodes.Status200OK)]
    public Task<TopluIceAktarmaSonucu> MeslekIceAktarAsync([FromBody] TopluIceAktarmaIstegi istek)
        => _referans.MeslekIceAktarAsync(istek);

    /// <summary>TÜM meslekleri siler.</summary>
    /// <remarks>
    /// <c>Randevu.Meslek</c> metin alanı olduğu için eski talepler bozulmaz;
    /// yalnızca açılır liste boşalır.
    /// </remarks>
    [HttpDelete("meslekler")]
    [Izin(Izinler.TanimYonet)]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public Task<int> MeslekTumunuSilAsync() => _referans.MeslekTumunuSilAsync();
}
