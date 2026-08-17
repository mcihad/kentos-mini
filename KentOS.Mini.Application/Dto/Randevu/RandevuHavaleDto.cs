using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.Randevu
{
    /// <summary>Talebin başka bir birime havale edilmesi.</summary>
    /// <remarks>
    /// GERİYE DÖNÜK UYUM: Alanlar uzun süre yanıltıcı adlarla taşındı —
    /// birim kimliği <c>ajandaId</c>, not ise <c>kullaniciId</c> anahtarıyla
    /// gidiyordu. Doğru adlar (<c>birimId</c>, <c>not</c>) eklendi; eski adlar
    /// sahadaki mobil sürümler için KABUL EDİLMEYE ve YANITTA DÖNMEYE devam
    /// ediyor. Eski sürümler kalktığında alias'lar kaldırılabilir.
    /// </remarks>
    public class RandevuHavaleDto
    {
        [JsonPropertyName("id")]
        [Required(ErrorMessage = "Talep kimliği zorunludur.")]
        public long Id { get; set; }

        /// <summary>Havale edilecek birim.</summary>
        [JsonPropertyName("birimId")]
        [Required(ErrorMessage = "Havale edilecek birim zorunludur.")]
        public long BirimId { get; set; }

        /// <summary>Havale notu.</summary>
        [JsonPropertyName("not")]
        [MaxLength(1000, ErrorMessage = "Not en fazla 1000 karakter olabilir.")]
        public string? Not { get; set; }

        /// <summary>Eski ad — birim kimliği (bkz. sınıf açıklaması).</summary>
        [JsonPropertyName("ajandaId")]
        public long? BirimIdEskiAd
        {
            get => BirimId;
            set
            {
                if (value.HasValue && BirimId == 0)
                {
                    BirimId = value.Value;
                }
            }
        }

        /// <summary>Eski ad — havale notu (bkz. sınıf açıklaması).</summary>
        [JsonPropertyName("kullaniciId")]
        public string? NotEskiAd
        {
            get => Not;
            set => Not ??= value;
        }
    }
}
