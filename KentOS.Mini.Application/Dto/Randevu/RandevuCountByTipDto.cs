using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Dto.Randevu
{
    public class RandevuCountByTipDto
    {
        [JsonPropertyName("tipId")]
        public long TipId { get; set; }
        [JsonPropertyName("tipAd")]
        public string TipAd { get; set; }
        [JsonPropertyName("count")]
        public long Count { get; set; }
    }
}
