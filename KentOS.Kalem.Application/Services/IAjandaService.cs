using KentOS.Kalem.Application.Dto;
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
    public interface IAjandaService
    {
        Task<List<Ajanda>> GetAllAsync(AjandaSearchParametersDto searchParameters);
        Task<List<Ajanda>> GetDeletedAsync();
        Task<List<Ajanda>> GetAllFromTodayAsync();
        Task<Ajanda> GetByIdAsync(long? id);
        Task<AjandaDto> GetByIdWithoutUserRestrictionAsync(long? id);

        Task<IEnumerable<AjandaDto>> GetAllAsync();
        Task<IEnumerable<AjandaCountDto>> GetCountByDayAsync(int month, int year);
        Task<IEnumerable<AjandaDto>> GetByDateAsync(AjandaDateSearchDto dateSearch);
        Task<IEnumerable<AjandaDto>> SearchAsync(AjandaSearchParametersDto searchParameters);
        Task<IEnumerable<AjandaDto>> GetByDateAsync(DateOnly date);
        Task<IEnumerable<AjandaDto>> GetAllMediaJoinsAsync();
        Task<AjandaDto> GetAsync(long id);
        Task<IEnumerable<AjandaPhotoDto>> GetAjandaPhotosAsync(long ajandaId);
        //get notes
        Task<IEnumerable<AjandaNotDto>> GetNotesAsync(long ajandaId);
        //create
        Task<AjandaDto> CreateAsync(AjandaDto ajandaDto);
        Task<AjandaDto> UpdateAsync(AjandaDto ajandaDto);
        Task<AjandaDto> PostponeAsync(AjandaErteleDto ajandaErteleDto);
        Task<AjandaDto> HavaleAsync(AjandaHavaleDto ajandaHaveleDto);
        Task<bool> SendToParent(long ajandaId);
        Task<AjandaDto> CicekGonderAsync(AjandaCicekGonderDto ajandaCicekGonderDto);
        Task<bool> CreateNoteAsync(AjandaNotDto ajandaNotDto);
        Task<IEnumerable<AjandaNotDto>> GetAllNoteAsync(long ajandaId);
        Task<CicekDto> GetCicekAsync(long ajandaId);
        Task<bool> DeleteCicekAsync(long ajandaId);
        Task<bool> DeleteAsync(long ajandaId);

        /// <summary>
        /// Tekrarlanan etkinliği verilen kapsamda siler (yalnızca bu / bundan
        /// sonrakiler / tümü). Kapsam belirtmeyen çağrılar için yukarıdaki tek
        /// parametreli sürüm bugünkü davranışı sürdürür.
        /// </summary>
        Task<bool> DeleteAsync(long ajandaId, Enums.TekrarKapsam kapsam);

        /// <summary>
        /// Etkinliğin ALT KAYNAKLARINA (not, fotoğraf, çiçek, zaman çizelgesi)
        /// erişim kapısı: gizli etkinlikte yalnızca ekleyen ve katılımcılar true alır.
        /// </summary>
        Task<bool> GorebilirMiAsync(long ajandaId);
        Task<bool> ChangeStateAsync(AjandaChangeStateDto ajandaChangeStateDto);
        Task<AjandaDto> ChangeTipId(long ajandaId, long tipId);
        Task<AjandaDto> ChangeDurumId(long ajandaId, long durumId);
        Task<AjandaDto> UploadPhotoAsync(long ajandaId,MultipartFormDataContent photo);
        Task<bool> SendSmsToBirimAsync(SendSmsToBirimDto sendSmsToBirim);

        /// <summary>
        /// Aynı gönderim, ama SONUCU sayılarla döner.
        /// </summary>
        /// <remarks>
        /// v1'in <see cref="SendSmsToBirimAsync"/> imzası değişmiyor (canlı
        /// sözleşme) ama <c>true</c> dönmesi bir şey söylemiyordu: telefon
        /// numarası olmayan kullanıcı sessizce atlanıyor ve gönderen kişi
        /// "gönderdim ama gitmedi" diyordu. Yeni istemciler bu metodu çağırır
        /// ve kaç kişiye yazıldığını, kimin telefonunun eksik olduğunu,
        /// hangi birimin boş olduğunu gösterir.
        /// </remarks>
        Task<SmsGonderimSonucuDto> SendSmsToBirimDetayliAsync(SendSmsToBirimDto sendSmsToBirim);
    }
}
