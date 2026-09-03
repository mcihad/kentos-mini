using KentOS.Kalem.Application.Dto.Analiz;

namespace KentOS.Kalem.Application.Services;

/// <summary>
/// Talep (randevu) istatistik panosu — mahalle, meslek, tip, durum, zaman.
/// </summary>
/// <remarks>
/// Etkinlik panosundan (<see cref="IAjandaIstatistikService"/>) ayrı: ikisi
/// farklı soruları cevaplıyor ve tek arayüze sıkıştırmak, iki ekranın da
/// birbirinin alanlarını taşıması demekti.
/// </remarks>
public interface ITalepIstatistikServisi
{
    /// <param name="baslangic">Dahil. Boşsa son 12 ay.</param>
    /// <param name="bitis">Dahil. Boşsa bugün.</param>
    Task<TalepIstatistikDto> PanoAsync(DateTime? baslangic = null, DateTime? bitis = null);
}
