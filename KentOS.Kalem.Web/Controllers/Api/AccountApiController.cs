using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using KentOS.Kalem.Application.Dto;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using KentOS.Kalem.Web.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Application.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountApiController(
        UserManager<AppUser> userManager,
        IJwtService jwtService,
        IBirimService birimService,
        IUserService userService,
        IOturumServisi oturumServisi
        ) : ControllerBase
    {

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Code = ErrorCodes.BadRequest,
                    Message = "Kullanıcı adı ve şifre alanları boş bırakılamaz"
                });
            }

            // Akışın kendisi `OturumServisi`'ne taşındı; v2 de aynı servisi
            // çağırıyor. Kimlik doğrulamanın iki kopyası olsaydı, birinde
            // yapılan bir düzeltme diğerinde unutulurdu.
            //
            // Bu uç noktanın JSON sözleşmesi (ErrorResponseDto / LoginResponseDto)
            // mobil uygulamanın kullandığı sözleşmedir ve DEĞİŞMEDİ.
            var sonuc = await oturumServisi.GirisYapAsync(model.Username, model.Password);

            if (sonuc.Tur != GirisSonucTuru.Basarili)
            {
                return Unauthorized(new ErrorResponseDto
                {
                    Code = ErrorCodes.Unauthorized,
                    Message = sonuc.Mesaj
                });
            }

            return Ok(new LoginResponseDto
            {
                Token = sonuc.Jeton!,
                Expiration = sonuc.GecerlilikSonu!.Value,
            });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("Me")]
        public async Task<IActionResult> Me()
        {
            var user = await userService.Get();
            return Ok(user);
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("Settings")]
        public async Task<IActionResult> Settings()
        {
            var settings = await userService.GetSetting();
            return Ok(settings);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("Settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UserSettingDto settings)
        {
            var updatedSettings = await userService.UpdateSetting(settings);
            return Ok(updatedSettings);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("PasswordChange")]
        public async Task<IActionResult> PasswordChange([FromBody] PasswordChangeDto model)
        {
            var response = await userService.PasswordChange(model);
            if (response.Success)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }
    }
}
