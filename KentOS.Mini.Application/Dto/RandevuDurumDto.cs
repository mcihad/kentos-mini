using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto
{
    public class RandevuDurumDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [Display(Name = "Durum Adı")]
        [Required(ErrorMessage = "Durum adı boş bırakılamaz")]
        [JsonPropertyName("durumAd")]
        [MaxLength(50, ErrorMessage = "Durum adı en fazla 50 karakter olabilir.")]
        public string DurumAd { get; set; }

        [Display(Name = "Renk")]
        [Required(ErrorMessage = "Renk alanı boş bırakılamaz")]
        [JsonPropertyName("renk")]
        [MaxLength(50, ErrorMessage = "Renk en fazla 50 karakter olabilir.")]
        public string Renk { get; set; }
        [Display(Name = "Simge")]
        [JsonPropertyName("simge")]
        public string? Simge { get; set; }
        [Display(Name = "Açıklama")]
        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }
    }
}
