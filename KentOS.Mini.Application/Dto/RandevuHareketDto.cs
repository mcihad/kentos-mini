using KentOS.Mini.Application.Models;
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
    public class RandevuHareketDto
    {
        [JsonPropertyName("kullanici")]
        public string Kullanici { get; set; }
        [Display(Name = "Gelen Birim")]
        [JsonPropertyName("eskiBirim")]
        public string EskiBirim { get; set; }
        [Display(Name = "Giden Birim")]
        [JsonPropertyName("yeniBirim")]
        public string YeniBirim { get; set; }
        [Display(Name = "Aşağı Hareket")]
        [JsonPropertyName("asagiHareket")]
        public bool AsagiHareket { get; set; } = true;
        [Display(Name = "İşlem Tarihi")]
        [JsonPropertyName("tarih")]
        public DateTime Tarih { get; set; }
    }
}
