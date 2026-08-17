using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto
{
    public class BirimDto
    {
        [Display(Name = "Id")]
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [Display(Name = "Ad")]
        [JsonPropertyName("ad")]
        [Required(ErrorMessage = "Birim adı zorunludur.")]
        [MaxLength(100, ErrorMessage = "Birim adı en fazla 100 karakter olabilir.")]
        public string Ad { get; set; }
        [Display(Name = "Yetkili")]
        [JsonPropertyName("yetkili")]
        [Required(ErrorMessage = "Yetkili zorunludur.")]
        [MaxLength(100, ErrorMessage = "Yetkili en fazla 100 karakter olabilir.")]
        public string Yetkili { get; set; }
        [Display(Name = "Telefon")]
        [JsonPropertyName("telefon")]
        public string? Telefon { get; set; }
        [Display(Name = "Email")]
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        [Display(Name = "Adres")]
        [JsonPropertyName("adres")]
        public string? Adres { get; set; }
        [Display(Name = "Açıklama")]
        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }
        [Display(Name = "Üst Birim")]
        [JsonPropertyName("ustBirimId")]
        public long? UstBirimId { get; set; }
        [Display(Name = "Seviye")]
        [JsonPropertyName("level")]
        public int Level { get; set; }
        
        [JsonPropertyName("children")]
        public List<BirimDto> Children { get; set; } = new();

        [NotMapped]
        [JsonPropertyName("fullAd")]
        public string FullAd => $"{Ad} ({Yetkili})";
    }
}
