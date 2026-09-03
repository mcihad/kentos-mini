using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Application.Services;

namespace KentOS.Kalem.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AnalizApiController(IAnalizService _analizService) : ControllerBase
    {
        [HttpGet("GetRandevuDurumCount")]
        public async Task<IActionResult> GetRandevuDurumCount()
        {
            var result = await _analizService.GetRandevuDurumCountAsync();
            return Ok(result);
        }

        [HttpGet("GetRandevuBirimCount")]
        public async Task<IActionResult> GetRandevuBirimCount()
        {
            var result = await _analizService.GetRandevuBirimCountAsync();
            return Ok(result);
        }

        [HttpGet("GetRandevuMonthCount")]
        public async Task<IActionResult> GetRandevuMonthCount()
        {
            var result = await _analizService.GetRandevuMonthCountAsync();
            return Ok(result);
        }

        [HttpGet("GetRandevuTipCount")]
        public async Task<IActionResult> GetRandevuTipCount()
        {
            var result = await _analizService.GetRandevuTipCountAsync();
            return Ok(result);
        }

        //GetArchivedCountByBirim
        [HttpGet("GetArchivedCountByBirim")]
        public async Task<IActionResult> GetArchivedCountByBirim()
        {
            var result = await _analizService.GetArchivedCountByBirimAsync();
            return Ok(result);
        }

    }
}
