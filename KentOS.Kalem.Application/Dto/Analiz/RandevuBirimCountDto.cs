using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto.Analiz
{
    public class RandevuBirimCountDto
    {
        [JsonPropertyName("birim")]
        public string Birim { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
