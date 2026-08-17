using KentOS.Mini.Application.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto
{
    public class RandevuNotDto
    {
        [Display(Name = "Not Tipi")]
        [JsonPropertyName("tip")]
        public string? Tip { get; set; }
        [Display(Name = "Not")]
        [Required(ErrorMessage = "Not metni zorunludur.")]
        [MaxLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
        [JsonPropertyName("not")]
        public string Not { get; set; }
        [Display(Name = "Randevu")]
        [JsonPropertyName("tarih")]
        public DateTime? Tarih { get; set; } = DateTime.Now;
        [Display(Name = "Oluşturan")]
        [JsonPropertyName("olusturan")]
        public string? Olusturan { get; set; }

    }
}
