using System.Collections.Generic;
using System.Threading.Tasks;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Enums;

namespace KentOS.Mini.Application.Services
{
    /// <summary>
    /// Etkinlik zaman çizelgesi. Yazma işlemi HİÇBİR ZAMAN hata fırlatmaz —
    /// günlükleme sorunu, kullanıcının asıl işlemini (kaydetme, erteleme vb.)
    /// düşürmemelidir.
    /// </summary>
    public interface IAjandaOlayService
    {
        Task KaydetAsync(
            long ajandaId,
            AjandaOlayTip tip,
            string aciklama,
            IEnumerable<AjandaAlanDegisikligiDto>? degisiklikler = null);

        /// <summary>En yeni olay en üstte.</summary>
        Task<IEnumerable<AjandaOlayDto>> GetirAsync(long ajandaId);
    }
}
