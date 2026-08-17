using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto
{
    public class CicekciDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [Display(Name = "Ad Soyad")]
        [Required(ErrorMessage = "Ad Soyad alanı gereklidir.")]
        [MaxLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
        [JsonPropertyName("adSoyad")]
        public string AdSoyad { get; set; }
        [Display(Name = "Telefon")]
        [Required(ErrorMessage = "Telefon alanı gereklidir.")]
        [JsonPropertyName("telefon")]
        public string Telefon { get; set; }
        [Display(Name = "Adres")]
        [Required(ErrorMessage = "Adres alanı gereklidir.")]
        [JsonPropertyName("adres")]
        public string Adres { get; set; }
        [JsonPropertyName("aktif")]
        public bool Aktif { get; set; }
    }
}
