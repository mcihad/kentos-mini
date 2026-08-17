using KentOS.Mini.Web.AuthPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Dto.V2.Yonetim;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>Birim, kullanıcı ve rol yönetimi.</summary>
/// <remarks>
/// Tümü <c>Admin</c> rolüne kapalıdır; <c>Sistem</c> ve <c>BaskanOzel</c>
/// rollerini atamak ayrıca <c>Sistem</c> yetkisi ister (servis katmanında
/// zorlanır, yalnızca arayüzde gizlenmez).
/// </remarks>
[Route("api/v2/yonetim")]
[Izin(Izinler.YonetimKullanici)]
public class YonetimController(
    IYonetimServisi _yonetim,
    ICurrentUserService _mevcutKullanici) : V2ControllerBase
{
    private bool SistemYetkisi => User.IsInRole(UserRoles.Sistem);

    // ------------------------------------------------------------ kullanıcı

    /// <summary>Tüm kullanıcılar (rolleriyle birlikte).</summary>
    [HttpGet("kullanicilar")]
    [ProducesResponseType<SayfaliSonuc<KullaniciOzetDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<KullaniciOzetDto>> KullanicilarAsync([FromQuery] SayfaIstegi istek)
        => _yonetim.KullanicilarAsync(istek);

    /// <summary>Kullanıcı detayı.</summary>
    [HttpGet("kullanicilar/{id:long}")]
    [ProducesResponseType<KullaniciOzetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> KullaniciAsync(long id) => Ok(await _yonetim.KullaniciAsync(id));

    /// <summary>Yeni kullanıcı oluşturur.</summary>
    [Izin(Izinler.YonetimKullanici)]
    [HttpPost("kullanicilar")]
    [ProducesResponseType<KullaniciOzetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> KullaniciOlusturAsync([FromBody] KullaniciOlusturIstegi istek)
        => Ok(await _yonetim.KullaniciOlusturAsync(istek, SistemYetkisi));

    /// <summary>Kullanıcıyı günceller (parola hariç).</summary>
    [Izin(Izinler.YonetimKullanici)]
    [HttpPut("kullanicilar/{id:long}")]
    [ProducesResponseType<KullaniciOzetDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> KullaniciGuncelleAsync(
        long id, [FromBody] KullaniciGuncelleIstegi istek)
        => Ok(await _yonetim.KullaniciGuncelleAsync(id, istek, SistemYetkisi));

    /// <summary>Kullanıcının parolasını sıfırlar.</summary>
    [Izin(Izinler.YonetimKullanici)]
    [HttpPost("kullanicilar/{id:long}/parola")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ParolaSifirlaAsync(long id, [FromBody] ParolaSifirlaIstegi istek)
    {
        await _yonetim.ParolaSifirlaAsync(id, istek);
        return NoContent();
    }

    /// <summary>Kullanıcıyı siler.</summary>
    [Izin(Izinler.YonetimKullanici)]
    [HttpDelete("kullanicilar/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> KullaniciSilAsync(long id)
    {
        var isteyen = await _mevcutKullanici.GetUserIdAsync();
        await _yonetim.KullaniciSilAsync(id, isteyen ?? 0);
        return NoContent();
    }

    // --------------------------------------------------------------- birim

    /// <summary>Birim ağacı (kökten yapraklara, kullanıcı sayılarıyla).</summary>
    [HttpGet("birimler")]
    [Izin(Izinler.YonetimBirim)]
    [ProducesResponseType<List<BirimDugumDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> BirimlerAsync() => Ok(await _yonetim.BirimAgaciAsync());

    /// <summary>Yeni birim.</summary>
    [HttpPost("birimler")]
    [Izin(Izinler.YonetimBirim)]
    [ProducesResponseType<BirimDugumDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> BirimOlusturAsync([FromBody] BirimIstegi istek)
        => Ok(await _yonetim.BirimOlusturAsync(istek));

    /// <summary>Birimi günceller.</summary>
    [HttpPut("birimler/{id:long}")]
    [Izin(Izinler.YonetimBirim)]
    [ProducesResponseType<BirimDugumDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> BirimGuncelleAsync(long id, [FromBody] BirimIstegi istek)
        => Ok(await _yonetim.BirimGuncelleAsync(id, istek));

    /// <summary>Birimi siler.</summary>
    [HttpDelete("birimler/{id:long}")]
    [Izin(Izinler.YonetimBirim)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BirimSilAsync(long id)
    {
        await _yonetim.BirimSilAsync(id);
        return NoContent();
    }

    // ----------------------------------------------------------------- rol

    /// <summary>Roller ve kullanıcı sayıları.</summary>
    /// <summary>İşlemi yapan kullanıcının kimliği — korumalı rol denetimi için.</summary>
    private async Task<long> IsteyenIdAsync() =>
        await _mevcutKullanici.GetUserIdAsync()
        ?? throw new UnauthorizedAccessException("Oturum kullanıcısı çözülemedi.");

    /// <summary>Birim detayı — istatistikler ve birimdeki kullanıcılar.</summary>
    [HttpGet("birimler/{id:long}")]
    [Izin(Izinler.YonetimBirim)]
    [ProducesResponseType<BirimDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BirimDetayAsync(long id)
        => Ok(await _yonetim.BirimDetayAsync(id));

    /// <summary>Roldeki kullanıcılar.</summary>
    [HttpGet("roller/{ad}/kullanicilar")]
    [Izin(Izinler.YonetimRol)]
    [ProducesResponseType<List<KullaniciOzetDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RolKullanicilariAsync(string ad)
        => Ok(await _yonetim.RolKullanicilariAsync(ad));

    /// <summary>Kullanıcıyı role ekler.</summary>
    /// <remarks>
    /// <c>Sistem</c> ve <c>BaskanOzel</c> rollerini yalnızca <c>Sistem</c>
    /// yetkisi olanlar atayabilir — kısıt SUNUCUDA zorlanır.
    /// </remarks>
    [HttpPost("roller/{ad}/kullanicilar/{kullaniciId:long}")]
    [Izin(Izinler.YonetimRol)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RoleKullaniciEkleAsync(string ad, long kullaniciId)
    {
        await _yonetim.RoleKullaniciEkleAsync(ad, kullaniciId, await IsteyenIdAsync());
        return NoContent();
    }

    /// <summary>Kullanıcıyı rolden çıkarır.</summary>
    [HttpDelete("roller/{ad}/kullanicilar/{kullaniciId:long}")]
    [Izin(Izinler.YonetimRol)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RoldenKullaniciCikarAsync(string ad, long kullaniciId)
    {
        await _yonetim.RoldenKullaniciCikarAsync(ad, kullaniciId, await IsteyenIdAsync());
        return NoContent();
    }

    [HttpGet("roller")]
    [Izin(Izinler.YonetimRol)]
    [ProducesResponseType<List<RolDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RollerAsync() => Ok(await _yonetim.RollerAsync());

    // ═══════════════════════════════════════════════ rol ve izin yönetimi

    /// <summary>Yeni rol oluşturur.</summary>
    /// <remarks>
    /// Rol yalnızca bir kap; ne yapabildiğini <c>roller/{id}/izinler</c>
    /// belirler. Yeni rol izinsiz doğar — dolu doğsaydı, yönetici hangi
    /// yetkileri verdiğini bilmeden bir rol dağıtmış olurdu.
    /// </remarks>
    [HttpPost("roller")]
    [Izin(Izinler.YonetimRol)]
    [ProducesResponseType<RolDto>(StatusCodes.Status200OK)]
    public Task<RolDto> RolOlusturAsync([FromBody] RolIstegi istek)
        => _yonetim.RolOlusturAsync(istek);

    /// <summary>Rolün AÇIKLAMASINI günceller — ad değiştirilemez.</summary>
    [HttpPut("roller/{id:long}")]
    [Izin(Izinler.YonetimRol)]
    [ProducesResponseType<RolDto>(StatusCodes.Status200OK)]
    public Task<RolDto> RolGuncelleAsync(long id, [FromBody] RolIstegi istek)
        => _yonetim.RolGuncelleAsync(id, istek);

    [HttpDelete("roller/{id:long}")]
    [Izin(Izinler.YonetimRol)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RolSilAsync(long id)
    {
        await _yonetim.RolSilAsync(id);
        return NoContent();
    }

    /// <summary>İzin kataloğu — koddan tohumlanır, yönetimde seçim listesi.</summary>
    [HttpGet("izinler")]
    [Izin(Izinler.YonetimRol)]
    [ProducesResponseType<List<IzinDto>>(StatusCodes.Status200OK)]
    public Task<List<IzinDto>> IzinKatalogAsync() => _yonetim.IzinKatalogAsync();

    [HttpGet("roller/{id:long}/izinler")]
    [Izin(Izinler.YonetimRol)]
    [ProducesResponseType<List<string>>(StatusCodes.Status200OK)]
    public Task<List<string>> RolIzinleriAsync(long id) => _yonetim.RolIzinleriAsync(id);

    /// <summary>Rolün izinlerini TOPLUCA yazar (listede olmayan kaldırılır).</summary>
    [HttpPut("roller/{id:long}/izinler")]
    [Izin(Izinler.YonetimRol)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RolIzinleriYazAsync(long id, [FromBody] RolIzinIstegi istek)
    {
        await _yonetim.RolIzinleriniYazAsync(id, istek.Izinler);
        return NoContent();
    }
}
