using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.Randevu
{
    /// <summary>Etkinlik için çiçek siparişi.</summary>
    /// <remarks>
    /// GERİYE DÖNÜK UYUM: Etkinlik kimliği uzun süre <c>id</c> anahtarıyla
    /// taşındı. Doğru ad <c>ajandaId</c> eklendi; <c>id</c> hem kabul edilmeye
    /// hem yanıtta dönmeye devam ediyor.
    /// </remarks>
    public class AjandaCicekGonderDto
    {
        /// <summary>Çiçek gönderilecek etkinlik.</summary>
        [JsonPropertyName("ajandaId")]
        [Display(Name = "Etkinlik")]
        [Required(ErrorMessage = "Etkinlik kimliği zorunludur.")]
        public long AjandaId { get; set; }

        [JsonPropertyName("cicekciId")]
        [Display(Name = "Çiçekçi")]
        [Required(ErrorMessage = "Çiçekçi seçimi zorunludur.")]
        public long CicekciId { get; set; }

        [JsonPropertyName("not")]
        [MaxLength(1000, ErrorMessage = "Not en fazla 1000 karakter olabilir.")]
        public string? Not { get; set; }

        /// <summary>Eski ad — etkinlik kimliği (bkz. sınıf açıklaması).</summary>
        [JsonPropertyName("id")]
        public long? Id
        {
            get => AjandaId;
            set
            {
                if (value.HasValue && AjandaId == 0)
                {
                    AjandaId = value.Value;
                }
            }
        }
    }
}
