using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Dto.Randevu;
using KentOS.Kalem.Application.Dto.ViewModels;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.AuthPolicies;
using KentOS.Kalem.Web.Exceptions;
using KentOS.Kalem.Web.Services;

namespace KentOS.Kalem.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthPolicyNames.Ajanda)]
    public class AjandaApiController(
        IAjandaService _ajandaService,
        IAjandaSeriService _seriService
    ) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AjandaDto>>> GetAjandaAsync() =>
            Ok(await _ajandaService.GetAllAsync());


        [HttpGet("{id}")]
        public async Task<ActionResult<AjandaDto>> GetAjandaAsync(long id)
        {
            try
            {
                return Ok(await _ajandaService.GetAsync(id));
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new ErrorResponseDto
                {
                    Code = ErrorCodes.NotFound,
                    Message = ex.Message
                });
            }
        }

        [HttpPost("Search")]
        public async Task<ActionResult<IEnumerable<AjandaDto>>> SearchAjandaAsync([FromBody] AjandaSearchParametersDto searchParameters) =>
            Ok(await _ajandaService.SearchAsync(searchParameters));


        [HttpGet("{ajandaId}/Photos")]
        public async Task<ActionResult<AjandaPhotoDto>> GetPhotosAsync(long ajandaId)
        {
            var ajanda = await _ajandaService.GetAsync(ajandaId);
            if (ajanda == null)
            {
                return NotFound(new ErrorResponseDto
                {
                    Code = ErrorCodes.NotFound,
                    Message = "Ajanda bulunamadı"
                });
            }

            return Ok(await _ajandaService.GetAjandaPhotosAsync(ajandaId));
        }

        [HttpGet("Date/{date}")]
        public async Task<ActionResult<IEnumerable<AjandaDto>>> GetByDateAsync(DateOnly date) =>
            Ok(await _ajandaService.GetByDateAsync(date));


        [HttpPost]
        public async Task<ActionResult<AjandaDto>> CreateAjandaAsync([FromBody] AjandaDto ajandaDto)
        {
            var ajanda = await _ajandaService.CreateAsync(ajandaDto);
            return CreatedAtAction(nameof(GetAjandaAsync), new { id = ajanda.Id }, ajanda);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<AjandaDto>> UpdateAjandaAsync(long id, [FromBody] AjandaDto ajandaDto)
        {
            if (id != ajandaDto.Id)
            {
                return BadRequest("ID mismatch.");
            }

            var ajanda = await _ajandaService.UpdateAsync(ajandaDto);
            return CreatedAtAction(nameof(GetAjandaAsync), new { id = ajanda.Id }, ajanda);
        }

        [HttpPost("Postpone")]
        public async Task<ActionResult<AjandaDto>> PostponeAjandaAsync([FromBody] AjandaErteleDto postponeAjandaDto)
        {
            var ajanda = await _ajandaService.PostponeAsync(postponeAjandaDto);
            return CreatedAtAction(nameof(GetAjandaAsync), new { id = ajanda.Id }, ajanda);
        }

        [HttpPost("Havale")]
        public async Task<ActionResult<AjandaDto>> HavaleAjandaAsync([FromBody] AjandaHavaleDto havaleAjandaDto)
        {
            var ajanda = await _ajandaService.HavaleAsync(havaleAjandaDto);
            return CreatedAtAction(nameof(GetAjandaAsync), new { id = ajanda.Id }, ajanda);
        }

        [HttpGet("{ajandaId}/SendToParent")]
        public async Task<ActionResult> SendToParentAsync(long ajandaId)
        {
            var ok = await _ajandaService.SendToParent(ajandaId);
            if (ok)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost("CicekGonder")]
        public async Task<ActionResult<AjandaDto>> CicekGonderAjandaAsync([FromBody] AjandaCicekGonderDto cicekGonderAjandaDto)
        {
            var ajanda = await _ajandaService.CicekGonderAsync(cicekGonderAjandaDto);
            return CreatedAtAction(nameof(GetAjandaAsync), new { id = ajanda.Id }, ajanda);
        }

        [HttpPost("CreateNote")]
        public async Task<ActionResult<AjandaDto>> CreateNoteAjandaAsync([FromBody] AjandaNotDto ajandaNotDto)
        {
            var ok = await _ajandaService.CreateNoteAsync(ajandaNotDto);

            if (ok)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// Etkinliğin bağlı olduğu tekrar serisinin kuralı ve özeti.
        /// Tek seferlik etkinlikte 404 döner.
        /// </summary>
        [HttpGet("{ajandaId}/Seri")]
        public async Task<ActionResult<AjandaSeriDto>> GetSeriAsync(long ajandaId)
        {
            var seri = await _seriService.GetirAsync(ajandaId);
            if (seri == null)
            {
                return NotFound(new ErrorResponseDto
                {
                    Code = ErrorCodes.NotFound,
                    Message = "Bu etkinlik tekrarlanan bir etkinlik değil."
                });
            }

            return Ok(seri);
        }

        [HttpGet("{id}/Notes")]
        public async Task<ActionResult<IEnumerable<AjandaNotDto>>> GetNotesAsync(long id)
        {
            var notes = await _ajandaService.GetAllNoteAsync(id);
            return Ok(notes);
        }

        [HttpGet("{ajandaId}/Cicek")]
        public async Task<ActionResult<CicekDto>> GetCicekAsync(long ajandaId)
        {
            var cicek = await _ajandaService.GetCicekAsync(ajandaId);
            if (cicek == null)
            {
                return NotFound(new ErrorResponseDto
                {
                    Code = ErrorCodes.NotFound,
                    Message = "Çiçek bulunamadı"
                });
            }
            return Ok(cicek);
        }

        /// <summary>
        /// Etkinliği siler. Tekrarlanan etkinlikte <paramref name="kapsam"/> ile
        /// "yalnızca bu" (varsayılan), "bundan sonrakiler" veya "tümü" seçilebilir.
        /// Kapsam göndermeyen eski istemcilerde davranış bugünküyle aynıdır.
        /// </summary>
        [HttpDelete("{ajandaId}")]
        public async Task<ActionResult> DeleteAjandaAsync(long ajandaId, [FromQuery] TekrarKapsam kapsam = TekrarKapsam.Yalnizca)
        {
            var ok = await _ajandaService.DeleteAsync(ajandaId, kapsam);
            if (ok)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpDelete("{ajandaId}/Cicek")]
        public async Task<ActionResult> DeleteCicekAsync(long ajandaId)
        {
            var ok = await _ajandaService.DeleteCicekAsync(ajandaId);
            if (ok)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost("ChangeState")]
        public async Task<ActionResult> ChangeStateAsync([FromBody] AjandaChangeStateDto ajandaChangeStateDto)
        {
            var ok = await _ajandaService.ChangeStateAsync(ajandaChangeStateDto);
            if (ok)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet("{ajandaId}/ChangeTipId/{tipId}")]
        public async Task<ActionResult> ChangeTipIdAsync(long ajandaId, long tipId)
        {
            var ajanda = await _ajandaService.ChangeTipId(ajandaId, tipId);
            return Ok(ajanda);
        }

        [HttpGet("{ajandaId}/ChangeDurumId/{durumId}")]
        public async Task<ActionResult> ChangeDurumIdAsync(long ajandaId, long durumId)
        {
            var ajanda = await _ajandaService.ChangeDurumId(ajandaId, durumId);
            return Ok(ajanda);

        }

        [HttpPost("{ajandaId}/UploadPhoto")]
        public async Task<IActionResult> UploadPhoto(long ajandaId)
        {
            // İstek çok parçalı (multipart) değilse `Request.Form` erişimi
            // InvalidDataException atıp 500 üretiyordu; istemci hatası 400 olmalı.
            if (!Request.HasFormContentType)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Code = ErrorCodes.BadRequest,
                    Message = "Dosya çok parçalı (multipart/form-data) olarak gönderilmelidir."
                });
            }

            if (Request.Form.Files.Count == 0)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Code = ErrorCodes.BadRequest,
                    Message = "Yüklenecek dosya bulunamadı."
                });
            }

            var formFile = Request.Form.Files[0];
            using var memoryStream = new MemoryStream();
            await formFile.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StreamContent(memoryStream), "photo", formFile.FileName);

            var result = await _ajandaService.UploadPhotoAsync(ajandaId, multipartContent);
            return Ok(result);
        }

        //implement SendSmsToBirimAsync service method
        [HttpPost("SendSmsToBirim")]
        public async Task<ActionResult> SendSmsToBirimAsync([FromBody] SendSmsToBirimDto sendSmsToBirim)
        {
            var ok = await _ajandaService.SendSmsToBirimAsync(sendSmsToBirim);
            if (ok)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost("GetByDate")]
        public async Task<ActionResult<IEnumerable<AjandaDto>>> GetByDateAsync([FromBody] AjandaDateSearchDto dateSearch) =>
            Ok(await _ajandaService.GetByDateAsync(dateSearch));


        [HttpGet("CountByDay/{month}/{year}")]
        public async Task<ActionResult<IEnumerable<AjandaCountDto>>> GetCountByDayAsync(int month, int year) =>
            Ok(await _ajandaService.GetCountByDayAsync(month, year));
    }
}
