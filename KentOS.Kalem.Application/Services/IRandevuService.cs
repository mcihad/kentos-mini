using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Dto.Analiz;
using KentOS.Kalem.Application.Dto.Randevu;
using KentOS.Kalem.Application.Dto.ViewModels;
using KentOS.Kalem.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Services
{
    public interface IRandevuService
    {
        Task<IEnumerable<RandevuDto>> GetAllAsync(bool selfInclude = false);
        Task<IEnumerable<RandevuListDto>> GetAllListAsync(bool includeDescendants = false);
        Task<IEnumerable<RandevuListDto>> GetArchiveListAsync(bool includeDescendants = false);
        Task<IEnumerable<RandevuListDto>> GetByDurumIdAsync(long durumId, bool includeDescendants = false);
        Task<long> CountAsync();
        Task<long> CountByDurumAsync(long durumId,bool includeDescendants=false);
        Task<RandevuDto> GetByIdAsync(long id);
        Task<RandevuDto> CreateAsync(RandevuDto randevuDto);
        Task<RandevuDto> UpdateAsync(RandevuDto randevuDto);
        Task<IEnumerable<RandevuDto>> SearchAsync(RandevuSearchParametersDto searchParameters);
        Task DeleteAsync(long id);
        Task<IEnumerable<RandevuNotDto>> GetAllNotAsync(long randevuId);
        Task<IEnumerable<RandevuDosyaDto>> GetAllDosyaAsync(long randevuId);
        Task<IEnumerable<RandevuHareketDto>> GetAllHareketAsync(long randevuId);
        Task<RandevuNotDto> CreateNotAsync(long id,RandevuNotDto randevuNotDto);
        Task<RandevuDto> CreateHavaleAsync(RandevuHavaleDto randevuHavaleDto);
        Task<bool> SendToParentAsync(long randevuId);
        Task<bool> RandevuToAjandaAsync(RandevuToAjandaDto randevuToAjandaDto);

        /// <summary>
        /// Talebi etkinliğe dönüştürür ve <b>oluşan etkinliğin kimliğini</b> döner.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="RandevuToAjandaAsync"/> yalnızca <c>bool</c> dönüyor ve
        /// istemci oluşan etkinliğe gidemiyordu; imzası v1 sözleşmesi olduğu
        /// için değiştirilmedi, yenisi eklendi.
        /// </para>
        /// <para>
        /// Eskisi hatayı YUTUYOR (<c>catch { return false; }</c>): kullanıcı
        /// "eklenemedi" görüyor ama sebebini kimse öğrenemiyordu. Bu metot
        /// fırlatır; v2 hata süzgeci RFC 7807 üretir ve kayıt sistem
        /// hatalarına düşer.
        /// </para>
        /// </remarks>
        Task<long> TalebiEtkinligeCevirAsync(RandevuToAjandaDto istek);
        Task<RandevuDto> ChangeDurumAsync(long randevuId, long durumId);
        Task<RandevuDto> ChangeTipAsync(long randevuId, long tipId);
        Task<bool> AddToArchiveAsync(long randevuId);
        Task<bool> RemoveFromArchiveAsync(long randevuId);

        Task<IEnumerable<RandevuCountByDurumDto>> GetCountByDurum(bool includeDescendants = false);
        Task<IEnumerable<RandevuCountByTipDto>> GetCountByTip(bool includeDescendants = false);
        Task<IEnumerable<RandevuListDto>> GetByTipIdAsync(long tipId, bool includeDescendants = false);
        Task<RandevuDto> UploadOzgecmisAsync(long randevuId, MultipartFormDataContent ozgecmisFile);



    }
}
