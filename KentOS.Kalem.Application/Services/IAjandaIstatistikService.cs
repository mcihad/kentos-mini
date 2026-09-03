using System;
using System.Threading.Tasks;
using KentOS.Kalem.Application.Dto.Analiz;

namespace KentOS.Kalem.Application.Services
{
    /// <summary>
    /// Birim bazlı etkinlik (Ajanda) istatistikleri. TAMAMEN SALT OKUNUR:
    /// hiçbir kayıt eklemez/değiştirmez/silmez, bildirim üretmez ve mevcut
    /// iş akışlarına dokunmaz. Ayrı bir uçtan servis edilir.
    /// </summary>
    public interface IAjandaIstatistikService
    {
        /// <param name="baslangic">Dahil. Verilmezse bugünden 12 ay geri.</param>
        /// <param name="bitis">Dahil. Verilmezse bugün.</param>
        Task<AjandaIstatistikDto> GetIstatistiklerAsync(DateTime? baslangic = null, DateTime? bitis = null);
    }
}
