using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Controllers.Api
{
    /// <summary>
    /// Ayarlar servisini kullanarak ayarlarla ilgili işlemleri yapar
    /// </summary>
    /// <param name="_settingsService"></param>
    /// <param name="_birimService"></param>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SettingsApiController(ISettingsService _settingsService, IBirimService _birimService) : ControllerBase
    {
        /// <summary>
        /// Randevu tiplerini getirir 
        /// </summary>
        /// <returns></returns>
        [HttpGet("RandevuTipler")]
        public async Task<ActionResult<IEnumerable<RandevuTipDto>>> GetRandevuTiplerAsync() =>
            Ok(await _settingsService.GetRandevuTiplerAsync());

        /// <summary>
        /// Randevu durumlarını getirir
        /// </summary>
        /// <returns></returns>
        [HttpGet("RandevuDurumlar")]
        public async Task<ActionResult<IEnumerable<RandevuDurumDto>>> GetRandevuDurumlarAsync() =>
            Ok(await _settingsService.GetRandevuDurumlarAsync());

        /// <summary>
        /// Mahalleleri getirir
        /// </summary>
        /// <returns></returns>
        [HttpGet("Mahalleler")]
        public async Task<ActionResult<IEnumerable<MahalleDto>>> GetMahallelerAsync() =>
            Ok(await _settingsService.GetMahallelerAsync());

        /// <summary>
        /// Ajanda durumlarını getirir
        /// </summary>
        /// <returns></returns>
        [HttpGet("AjandaDurumlar")]
        public async Task<ActionResult<IEnumerable<AjandaDurumDto>>> GetAjandaDurumlarAsync() =>
            Ok(await _settingsService.GetAjandaDurumlarAsync());

        /// <summary>
        /// Meslekleri getirir
        /// </summary>
        /// <returns></returns>
        [HttpGet("Meslekler")]
        public async Task<ActionResult<IEnumerable<MeslekDto>>> GetMesleklerAsync() =>
            Ok(await _settingsService.GetMesleklerAsync());
        [HttpGet("Cicekciler")]
        public async Task<ActionResult<IEnumerable<CicekciDto>>> GetCicekcilerAsync() =>
            Ok(await _settingsService.GetCicekcilerAsync());

        [HttpGet("AltBirimler")]
        public async Task<ActionResult<IEnumerable<BirimDto>>> GetAltBirimlerAsync() =>
            Ok(await _settingsService.GetAltBirimlerAsync());

        [HttpGet("Birimler")]
        public async Task<ActionResult<IEnumerable<BirimDto>>> GetBirimlerAsync() =>
            Ok(await _settingsService.GetBirimlerAsync());

        /// <summary>
        /// Gizli etkinlikte katılımcı olarak seçilebilecek kişiler: oturum açan
        /// kullanıcının birimindeki kullanıcılar (kendisi hariç).
        /// </summary>
        [HttpGet("BirimKullanicilari")]
        public async Task<ActionResult<IEnumerable<KatilimciDto>>> GetBirimKullanicilariAsync() =>
            Ok(await _settingsService.GetBirimKullanicilariAsync());

        [HttpGet("AltBirimlerTree")]
        public async Task<ActionResult<IEnumerable<BirimDto>>> GetAltBirimlerTreeAsync() =>
            Ok(await _settingsService.GetAltBirimlerTreeAsync());

        [HttpGet("UstBirim")]
        public async Task<ActionResult<BirimDto>> GetUstBirimAsync()
        {
            
            try { 
                return Ok(await _settingsService.GetUstBirimAsync()); 
            }
            catch (Exception ex) when (ex is EntityNotFoundException)
            { 
                return NotFound(new ErrorResponseDto()
                {
                    Code = ErrorCodes.NotFound,
                    Message = ex.Message
                }); 
            }
        }

        //update fcm token
        [HttpGet("UpdateFcmToken")]
        public async Task<ActionResult> UpdateFcmTokenAsync([FromQuery] string fcmToken)
        {
            try
            {
                await _settingsService.UpdateFcmTokenAsync(fcmToken);
                return Ok();
            }
            catch (Exception ex) when (ex is EntityNotFoundException)
            {
                return NotFound(new ErrorResponseDto()
                {
                    Code = ErrorCodes.NotFound,
                    Message = ex.Message
                });
            }
        }


    }
}
