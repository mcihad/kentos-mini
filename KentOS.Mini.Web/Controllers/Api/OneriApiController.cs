using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.AuthPolicies;

namespace KentOS.Mini.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthPolicyNames.Ajanda)]
    public class OneriApiController(IOneriService _oneriService) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<ActionResult<OneriDto>> GetAsync(long id)
        {
            var oneri = await _oneriService.GetAsync(id);
            if (oneri == null)
            {
                return NotFound();
            }
            return Ok(oneri);
        }

        [HttpGet("User/{userId}")]
        public async Task<ActionResult<IEnumerable<OneriDto>>> GetByUserIdAsync(long userId)
        {
            var oneriler = await _oneriService.GetOnerilerByUserIdAsync(userId);
            return Ok(oneriler);
        }

        [HttpGet("User/{userId}/Tip/{tip}")]
        public async Task<ActionResult<IEnumerable<OneriDto>>> GetByUserIdAndTipAsync(long userId, OneriTip tip)
        {
            var oneriler = await _oneriService.GetOnerilerByUserIdAsync(userId, tip);
            return Ok(oneriler);
        }

        [HttpGet("User/{userId}/date")]
        public async Task<ActionResult<IEnumerable<OneriDto>>> GetByUserIdAndDateAsync(long userId, DateTime startDate, DateTime endDate)
        {
            var oneriler = await _oneriService.GetOnerilerByUserIdAsync(userId, startDate, endDate);
            return Ok(oneriler);
        }

        [HttpGet("Waiting")]
        public async Task<ActionResult<IEnumerable<OneriDto>>> GetWaitingOnerilerAsync()
        {
            var oneriler = await _oneriService.GetWaitingOnerilerAsync();
            return Ok(oneriler);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OneriDto>>> GetAllAsync()
        {
            var oneriler = await _oneriService.GetAllAsync();
            return Ok(oneriler);
        }

        [HttpPost]
        public async Task<ActionResult<OneriDto>> CreateAsync([FromBody] OneriDto oneriDto)
        {
            var createdOneri = await _oneriService.CreateAsync(oneriDto);
            return CreatedAtAction(nameof(GetAsync), new { id = createdOneri.Id }, createdOneri);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<OneriDto>> UpdateAsync(long id, [FromBody] OneriDto oneriDto)
        {
            if (id != oneriDto.Id)
            {
                return BadRequest();
            }

            var updatedOneri = await _oneriService.UpdateAsync(oneriDto);
            return Ok(updatedOneri);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(long id)
        {
            await _oneriService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/Answer")]
        public async Task<ActionResult<OneriDto>> AnswerAsync(long id, [FromBody] OneriCevapDto oneriCevap)
        {
            var answeredOneri = await _oneriService.AnswerAsync(id, oneriCevap);
            return Ok(answeredOneri);
        }
    }
}
