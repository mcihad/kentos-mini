using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Services
{
    public interface IOneriService
    {
        Task<OneriDto> CreateAsync(OneriDto oneriDto);
        Task<OneriDto> GetAsync(long id);
        Task<IEnumerable<OneriDto>> GetAllAsync();
        Task<OneriDto> UpdateAsync(OneriDto oneriDto);
        Task DeleteAsync(long id);
        Task<OneriDto> AnswerAsync(long id, OneriCevapDto cevap);
        Task<IEnumerable<OneriDto>> GetWaitingOnerilerAsync();
        Task<IEnumerable<OneriDto>> GetOnerilerByUserIdAsync(long userId);

        /// <summary>
        /// Bir kullanıcının TÜM önerileri; kayıt yoksa BOŞ liste döner.
        /// </summary>
        /// <remarks>
        /// <see cref="GetOnerilerByUserIdAsync(long)"/> ile aynı işi yapmaz:
        /// o metot <c>FirstOrDefault</c> kullanıyor (yalnızca ilk kaydı
        /// döndürüyor) ve kayıt yoksa <c>EntityNotFoundException</c> fırlatıyor,
        /// yani boş liste 404 oluyor. Bu davranış canlı mobil sözleşmesinin
        /// parçası olduğu için DEĞİŞTİRİLMEDİ; v2 bu doğru sürümü kullanır.
        /// </remarks>
        Task<IEnumerable<OneriDto>> KullaniciOnerileriAsync(long kullaniciId);
        Task<IEnumerable<OneriDto>> GetOnerilerByUserIdAsync(long userId, OneriTip tip);
        Task<IEnumerable<OneriDto>> GetOnerilerByUserIdAsync(long userId, DateTime startDate, DateTime endDate);
    }
}
