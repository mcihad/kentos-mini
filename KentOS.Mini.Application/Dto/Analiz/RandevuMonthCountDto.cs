using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.Analiz
{
    public class RandevuMonthCountDto
    {
        [JsonPropertyName("month")]
        public string Month { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
