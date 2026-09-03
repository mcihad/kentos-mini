using System.ComponentModel.DataAnnotations;
using KentOS.Kalem.Application.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Dto
{
    public class OneriDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("baslik")]
        [Required(ErrorMessage = "Başlık zorunludur.")]
        [MaxLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir.")]
        public string Baslik { get; set; }
        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }
        [JsonPropertyName("tip")]
        public OneriTip Tip { get; set; }
        [JsonPropertyName("tarih")]
        public DateTime? Tarih { get; set; }
        [JsonPropertyName("kullaniciId")]
        public long? KullaniciId { get; set; }
        [JsonPropertyName("kullaniciAdi")]
        public string? KullaniciAdi { get; set; }
        [JsonPropertyName("cevap")]
        public string? Cevap { get; set; }
        [JsonPropertyName("cevapTarih")]
        public DateTime? CevapTarih { get; set; }
    }
}
