using System;
using System.ComponentModel.DataAnnotations.Schema;
using KentOS.Kalem.Application.Enums;

namespace KentOS.Kalem.Application.Models
{
    /// <summary>
    /// Etkinlik zaman çizelgesi kaydı — "kim, ne zaman, neyi değiştirdi".
    ///
    /// NEDEN AYRI TABLO: Değişiklikler daha önce serbest metin olarak
    /// <see cref="AjandaNot"/> içine yazılıyordu; kullanıcı notlarıyla karışıyor ve
    /// mobil tarafta düzgün gösterilebilmesi için metin ayrıştırmak gerekiyordu.
    /// Burada tip ayrı bir alan, değişiklikler ise YAPISAL JSON olarak durur.
    ///
    /// Mevcut <c>AjandaHareket</c> tablosu bu iş için uygun değildi: yalnızca birim
    /// havalesine özel (EskiBirim/YeniBirim zorunlu) ve kodda hiç kullanılmıyor.
    ///
    /// SADECE EKLENİR (append-only) — güncellenmez, silinmez.
    /// </summary>
    [Table("ajanda_olaylar")]
    public class AjandaOlay
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("ajanda_id")]
        public long AjandaId { get; set; }
        public Ajanda? Ajanda { get; set; }

        [Column("tip")]
        public AjandaOlayTip Tip { get; set; }

        /// <summary>İşlemi yapan kişinin görünen adı.</summary>
        [Column("kullanici", TypeName = "varchar(150)")]
        public string Kullanici { get; set; } = string.Empty;

        [Column("tarih")]
        public DateTime Tarih { get; set; } = DateTime.Now;

        /// <summary>Tek satırlık insan-okur özet, ör. "Etkinlik ertelendi".</summary>
        [Column("aciklama", TypeName = "varchar(500)")]
        public string Aciklama { get; set; } = string.Empty;

        /// <summary>
        /// Değişen alanlar: <c>[{"alan":"Başlık","eski":"A","yeni":"B"}]</c>.
        /// Yapısal tutulur ki istemci kendi biçimlendirmesini yapabilsin.
        /// Alan değişikliği içermeyen olaylarda null.
        /// </summary>
        [Column("degisiklikler_json", TypeName = "text")]
        public string? DegisikliklerJson { get; set; }
    }
}
