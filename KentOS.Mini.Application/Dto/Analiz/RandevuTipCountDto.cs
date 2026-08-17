using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.Analiz
{
    public class RandevuTipCountDto
    {
        [JsonPropertyName("tip")]
        public string Tip { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }
        [JsonPropertyName("renk")]
        public string Renk { get; set; }
    }
}
