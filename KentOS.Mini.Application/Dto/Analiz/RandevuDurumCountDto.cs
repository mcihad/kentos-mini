using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.Analiz
{
    public class RandevuDurumCountDto
    {
        [JsonPropertyName("durum")]
        public string Durum { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }
        [JsonPropertyName("renk")]
        public string Renk { get; set; }
    }
}
