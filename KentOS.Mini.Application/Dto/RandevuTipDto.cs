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
    public class RandevuTipDto
    {
        [Display(Name = "Id")]
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [Display(Name = "Randevu Tipi")]
        [Required(ErrorMessage = "Randevu tipi adı boş bırakılamaz")]
        [JsonPropertyName("ad")]
        [MaxLength(50, ErrorMessage = "Tip adı en fazla 50 karakter olabilir.")]
        public string Ad { get; set; }
        [Display(Name = "Renk")]
        [JsonPropertyName("renk")]
        public string? Renk { get; set; }
        [Display(Name = "Açıklama")]
        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }
    }
}
