using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Dto.ViewModels
{
    public class RandevuSearchParametersDto
    {
        [JsonPropertyName("konu")]
        public string? Konu { get; set; }

        [JsonPropertyName("ad")]
        public string? Ad { get; set; }

        [JsonPropertyName("soyad")]
        public string? Soyad { get; set; }

        [JsonPropertyName("meslek")]
        public string? Meslek { get; set; }

        [JsonPropertyName("telefon")]
        public string? Telefon { get; set; }

        [JsonPropertyName("baslangicTarih")]
        public DateTime? BaslangicTarih { get; set; }

        [JsonPropertyName("bitisTarih")]
        public DateTime? BitisTarih { get; set; }

        [JsonPropertyName("ozgecmisDurum")]
        public bool? OzgecmisDurum { get; set; }

        [JsonPropertyName("birimId")]
        public long? BirimId { get; set; }

        [JsonPropertyName("mahalleId")]
        public long? MahalleId { get; set; }

        [JsonPropertyName("randevuDurumId")]
        public long? RandevuDurumId { get; set; }

        [JsonPropertyName("randevuTipId")]
        public long? RandevuTipId { get; set; }
    }
}
