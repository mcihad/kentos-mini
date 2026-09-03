using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Application.Dto.Analiz;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.AuthPolicies;

namespace KentOS.Kalem.Web.Controllers.Api
{
    /// <summary>
    /// Birimin etkinlik istatistikleri — mobil arşiv ekranındaki grafikleri besler.
    ///
    /// AYRI UÇ: mevcut hiçbir controller/servis değiştirilmedi. Bu uç yalnızca
    /// okuma yapar (AsNoTracking), yazma ve bildirim üretmez; dolayısıyla
    /// çalışan sistemin davranışını değiştiremez. Uç kapansa bile mevcut
    /// işlevler etkilenmez.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthPolicyNames.Ajanda)]
    public class AjandaIstatistikApiController(
        IAjandaIstatistikService _istatistikService,
        ILogger<AjandaIstatistikApiController> _logger) : ControllerBase
    {
        /// <summary>
        /// GET /api/AjandaIstatistik?baslangic=2025-01-01&amp;bitis=2026-08-11
        /// Tarihler verilmezse son 12 ay kullanılır.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<AjandaIstatistikDto>> GetAsync(
            [FromQuery] DateTime? baslangic = null,
            [FromQuery] DateTime? bitis = null)
        {
            try
            {
                return Ok(await _istatistikService.GetIstatistiklerAsync(baslangic, bitis));
            }
            catch (Exception ex)
            {
                // İstatistik ekranı çökse bile uygulamanın geri kalanı etkilenmesin:
                // hata yut, boş/anlamlı bir yanıt yerine 500 dön ve logla.
                _logger.LogError(ex, "Ajanda istatistikleri hesaplanamadı.");
                return StatusCode(500, new { message = "İstatistikler hesaplanamadı." });
            }
        }
    }
}
