using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Dto
{
    public class OneriCevapDto
    {
        [JsonPropertyName("cevap")]
        public string? Cevap { get; set; }
    }
}
