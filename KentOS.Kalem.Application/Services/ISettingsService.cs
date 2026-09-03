using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Services
{
    public interface ISettingsService
    {
        Task<IEnumerable<MahalleDto>> GetMahallelerAsync();
        Task<IEnumerable<RandevuDurumDto>> GetRandevuDurumlarAsync();
        Task<IEnumerable<RandevuTipDto>> GetRandevuTiplerAsync();
        Task<IEnumerable<AjandaDurumDto>> GetAjandaDurumlarAsync();
        Task<IEnumerable<CicekciDto>> GetCicekcilerAsync();
        Task<IEnumerable<MeslekDto>> GetMesleklerAsync();
        Task<IEnumerable<BirimDto>> GetBirimlerAsync();
        Task<IEnumerable<BirimDto>> GetAltBirimlerAsync();
        Task<IEnumerable<BirimDto>> GetAltBirimlerTreeAsync();

        /// <summary>
        /// Etkinliğe KATILIMCI olarak eklenebilecek birimler.
        /// </summary>
        /// <remarks>
        /// Kullanıcının kendi seviyesindekiler ve altındakiler. Üsttekiler
        /// listelenmez: bir müdürlük, başkan yardımcısını toplantısına
        /// "çağıramaz" — o davet yukarıdan gelir.
        /// </remarks>
        Task<IEnumerable<BirimDto>> GetKatilimciBirimlerAsync();

        /// <summary>
        /// Oturum açan kullanıcının birimindeki kullanıcılar (kendisi hariç) —
        /// gizli etkinlik katılımcı seçicisi için sade liste.
        /// </summary>
        Task<IEnumerable<KatilimciDto>> GetBirimKullanicilariAsync();
        Task<BirimDto> GetUstBirimAsync();
        Task UpdateFcmTokenAsync(string fcmToken);
        Task LoadAllAsync();
    }
}
