using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Dto.Randevu;
using KentOS.Kalem.Application.Dto.ViewModels;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.AuthPolicies;
using KentOS.Kalem.Web.Exceptions;

namespace KentOS.Kalem.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy =AuthPolicyNames.Ajanda, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

    public class RandevuApiController(IRandevuService randevuService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] bool includeDescendants=false)
        {
            var randevular = await randevuService.GetAllAsync(includeDescendants);
            return Ok(randevular);
        }

        [HttpGet("List")]
        public async Task<IActionResult> GetList([FromQuery] bool includeDescendants = false)
        {
            var randevular = await randevuService.GetAllListAsync(includeDescendants);
            return Ok(randevular);
        }

        //ArchiveList
        [HttpGet("ArchiveList")]
        public async Task<IActionResult> GetArchiveList([FromQuery] bool includeDescendants = false)
        {
            var randevular = await randevuService.GetArchiveListAsync(includeDescendants);
            return Ok(randevular);
        }

        [HttpGet("CountByDurum")]
        public async Task<IActionResult> GetCountByDurum([FromQuery] bool includeDescendants = false)
        {
            var countByDurum = await randevuService.GetCountByDurum(includeDescendants);
            return Ok(countByDurum);
        }

        [HttpGet("Count")]
        public async Task<IActionResult> GetCount()
        {
            var count = await randevuService.CountAsync();
            return Ok(count);
        }

        [HttpGet("CountByDurum/{durumId}")]
        public async Task<IActionResult> GetCountByDurum(long durumId, [FromQuery] bool includeDescendants = false)
        {
            var count = await randevuService.CountByDurumAsync(durumId, includeDescendants);
            return Ok(count);
        }

        [HttpGet("ByDurumId/{durumId}")]
        public async Task<IActionResult> GetByDurumId(long durumId, [FromQuery] bool includeDescendants = false)
        {
            var randevular = await randevuService.GetByDurumIdAsync(durumId, includeDescendants);
            return Ok(randevular);
        }

        //by tip
        [HttpGet("CountByTip")]
        public async Task<IActionResult> GetCountByTip([FromQuery] bool includeDescendants = false)
        {
            var countByTip = await randevuService.GetCountByTip(includeDescendants);
            return Ok(countByTip);
        }

        [HttpGet("ByTipId/{tipId}")]
        public async Task<IActionResult> GetByTipId(long tipId, [FromQuery] bool includeDescendants = false)
        {
            var randevular = await randevuService.GetByTipIdAsync(tipId, includeDescendants);
            return Ok(randevular);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            try
            {
                var randevu = await randevuService.GetByIdAsync(id);
                return Ok(randevu);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{id}/Notlar")]
        public async Task<IActionResult> GetNotlar(long id)
        {
            try
            {
                var notlar = await randevuService.GetAllNotAsync(id);
                return Ok(notlar);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{id}/Dosyalar")]
        public async Task<IActionResult> GetDosyalar(long id)
        {
            try
            {
                var dosyalar = await randevuService.GetAllDosyaAsync(id);
                return Ok(dosyalar);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{id}/Hareketler")]
        public async Task<IActionResult> GetHareketler(long id)
        {
            try
            {
                var hareketler = await randevuService.GetAllHareketAsync(id);
                return Ok(hareketler);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("Search")]
        public async Task<IActionResult> Search([FromBody] RandevuSearchParametersDto searchParameters)
        {
            var randevular = await randevuService.SearchAsync(searchParameters);
            return Ok(randevular);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RandevuDto randevuDto)
        {
            try
            {
                var created = await randevuService.CreateAsync(randevuDto);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] RandevuDto randevuDto)
        {
            if (id != randevuDto.Id)
                return BadRequest("ID mismatch");

            try
            {
                var updated = await randevuService.UpdateAsync(randevuDto);
                return Ok(updated);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                await randevuService.DeleteAsync(id);
                return NoContent();
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("{id}/Not")]
        public async Task<IActionResult> CreateNot(long id, [FromBody] RandevuNotDto notDto)
        {
            try
            {
                var created = await randevuService.CreateNotAsync(id,notDto);
                return Ok(created);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("{id}/Havale")]
        public async Task<IActionResult> CreateHavale(long id, [FromBody] RandevuHavaleDto havaleDto)
        {
            if (id != havaleDto.Id)
                return BadRequest("ID mismatch");

            try
            {
                var updated = await randevuService.CreateHavaleAsync(havaleDto);
                return Ok(updated);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{randevuId}/SendToParent")]
        public async Task<IActionResult> SendToParent(long randevuId)
        {
            try
            {
                var ok = await randevuService.SendToParentAsync(randevuId);
                if (ok)
                    return Ok();
                else
                    return BadRequest();
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }


        [HttpGet("{randevuId}/ChangeTipId/{tipId}")]
        public async Task<IActionResult> ChangeTip(long randevuId, long tipId)
        {
            try
            {
                var updated = await randevuService.ChangeTipAsync(randevuId, tipId);
                return Ok(updated);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{randevuId}/ChangeDurumId/{durumId}")]
        public async Task<IActionResult> ChangeDurum(long randevuId, long durumId)
        {
            try
            {
                var updated = await randevuService.ChangeDurumAsync(randevuId, durumId);
                return Ok(updated);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("AddToAjanda")]
        public async Task<IActionResult> AddToAjanda(RandevuToAjandaDto randevuToAjandaDto)
        {
            try
            {
                var result = await randevuService.RandevuToAjandaAsync(randevuToAjandaDto);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        //add to archive
        [HttpGet("{randevuId}/AddToArchive")]
        public async Task<IActionResult> AddToArchive(long randevuId)
        {
            try
            {
                var result = await randevuService.AddToArchiveAsync(randevuId);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        //remove from archive
        [HttpGet("{randevuId}/RemoveFromArchive")]
        public async Task<IActionResult> RemoveFromArchive(long randevuId)
        {
            try
            {
                var result = await randevuService.RemoveFromArchiveAsync(randevuId);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("{randevuId}/UploadOzgecmis")]
        public async Task<IActionResult> UploadOzgecmis(long randevuId)
        {
            // İstek çok parçalı (multipart) değilse `Request.Form` erişimi
            // InvalidDataException atıp 500 üretiyordu; istemci hatası 400 olmalı.
            if (!Request.HasFormContentType)
            {
                return BadRequest("Dosya çok parçalı (multipart/form-data) olarak gönderilmelidir.");
            }

            if (Request.Form.Files.Count == 0)
            {
                return BadRequest("Yüklenecek dosya bulunamadı.");
            }

            var formFile = Request.Form.Files[0];
            using var memoryStream = new MemoryStream();
            await formFile.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StreamContent(memoryStream), "ozgecmis", formFile.FileName);

            var result = await randevuService.UploadOzgecmisAsync(randevuId, multipartContent);
            return Ok(result);
        }

    }
}
