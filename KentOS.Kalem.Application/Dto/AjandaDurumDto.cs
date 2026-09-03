using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;



namespace KentOS.Kalem.Application.Dto
{
    public class AjandaDurumDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [Required(ErrorMessage = "Durum Adı Zorunludur")]
        [Display(Name = "Durum Adı")]
        [JsonPropertyName("ad")]
        [MaxLength(100, ErrorMessage = "Durum adı en fazla 100 karakter olabilir.")]
        public string Ad { get; set; }
        [Display(Name = "Renk")]
        [Required(ErrorMessage = "Renk Zorunludur")]
        [JsonPropertyName("renk")]
        [MaxLength(20, ErrorMessage = "Renk en fazla 20 karakter olabilir.")]
        public string Renk { get; set; }

        [Display(Name = "Açıklama")]
        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }
        [Display(Name = "Simge")]
        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

    }
}
