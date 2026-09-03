using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Dto
{
    public class MahalleDto
    {
        [JsonPropertyName("id")]
        [Display(Name = "Id")]
        public long Id { get; set; }
        [JsonPropertyName("ad")]
        [Display(Name = "Ad")]
        [Required(ErrorMessage = "Mahalle adı zorunludur.")]
        [MaxLength(200, ErrorMessage = "Mahalle adı en fazla 200 karakter olabilir.")]
        public string Ad { get; set; }
    }
}
