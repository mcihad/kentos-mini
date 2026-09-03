using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.AuthPolicies;

namespace KentOS.Kalem.Web.Controllers.Api
{
    /// <summary>
    /// Etkinlik zaman çizelgesi — salt okunur, ayrı uç.
    /// Rota: /api/AjandaOlayApi/{ajandaId}
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthPolicyNames.Ajanda)]
    public class AjandaOlayApiController(
        IAjandaOlayService _olayService,
        ILogger<AjandaOlayApiController> _logger) : ControllerBase
    {
        [HttpGet("{ajandaId:long}")]
        public async Task<ActionResult<IEnumerable<AjandaOlayDto>>> GetAsync(long ajandaId)
        {
            try
            {
                return Ok(await _olayService.GetirAsync(ajandaId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ajanda olayları getirilemedi. AjandaId={AjandaId}", ajandaId);
                return StatusCode(500, new { message = "Zaman çizelgesi yüklenemedi." });
            }
        }
    }
}
