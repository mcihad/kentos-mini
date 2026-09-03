using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Enums;

namespace KentOS.Kalem.Web.Services
{
    /// <summary>
    /// Tekrarlanan etkinlik (RRULE serisi) işlemleri.
    ///
    /// TASARIM: Kural <c>ajanda_seriler</c> tablosunda durur, tekrarlar ise gerçek
    /// <c>ajandalar</c> satırı olarak üretilir. Böylece not/fotoğraf/çiçek/durum
    /// doğal olarak "eklendiği tekrara" ait olur ve listeleme, arama, arşiv,
    /// istatistik, takvim ile SAHADAKİ ESKİ MOBİL SÜRÜMLER hiç değişmeden çalışır.
    /// </summary>
    public interface IAjandaSeriService
    {
        /// <summary>
        /// Şablon etkinlikten bir tekrar serisi kurar ve tekrarları üretir.
        /// Dönen DTO serinin İLK tekrarıdır (kullanıcının kaydettiği etkinlik).
        /// </summary>
        Task<AjandaDto> OlusturAsync(AjandaDto sablon);

        /// <summary>Bir tekrarın bağlı olduğu serinin kural bilgisi (yoksa null).</summary>
        Task<AjandaSeriDto?> GetirAsync(long ajandaId);

        /// <summary>
        /// Tekrarlanan etkinliği verilen KAPSAMDA günceller.
        /// <see cref="TekrarKapsam.Yalnizca"/> için çağrılmaz — o durumda normal
        /// tek kayıt güncellemesi işler (bkz. <c>AjandaService.UpdateAsync</c>).
        /// </summary>
        Task<AjandaDto> GuncelleAsync(AjandaDto dto, TekrarKapsam kapsam);

        /// <summary>
        /// TEK SEFERLİK bir etkinliği tekrarlanan hâle getirir: etkinliğin
        /// kendisi serinin İLK tekrarı olur (kimliği, notları, fotoğrafları
        /// yerinde kalır), kalan tekrarlar yeni satır olarak üretilir.
        /// </summary>
        Task<AjandaDto> SeriyeCevirAsync(AjandaDto dto);

        /// <summary>
        /// Tekrarı KALDIRIR; etkinlik tek seferliğe döner.
        /// <see cref="TekrarKapsam.Yalnizca"/>: yalnızca bu tekrar seriden koparılır,
        /// seri diğer tekrarlarla devam eder.
        /// <see cref="TekrarKapsam.BundanSonrakiler"/>: bu tekrardan sonrası kaldırılır.
        /// <see cref="TekrarKapsam.Tumu"/>: serinin tamamı kaldırılır, bu kayıt kalır.
        /// </summary>
        Task<AjandaDto> TekrariKaldirAsync(AjandaDto dto, TekrarKapsam kapsam);

        /// <summary>Tekrarlanan etkinliği verilen kapsamda siler (soft delete).</summary>
        Task<bool> SilAsync(long ajandaId, TekrarKapsam kapsam);

        /// <summary>
        /// Sonsuz/uzun serilerin üretim ufkunu ileriye taşır. Arka plan görevi
        /// günde bir çağırır; ayrıca ufkun ötesine bakan sorgular tetikleyebilir.
        /// Üretilen yeni tekrar sayısını döndürür.
        /// </summary>
        Task<int> UfkuGenisletAsync(CancellationToken iptalJetonu = default);
    }
}
