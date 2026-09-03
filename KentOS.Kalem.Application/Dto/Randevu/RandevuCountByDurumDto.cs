using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Dto.Randevu
{
    public class RandevuCountByDurumDto
    {
        [JsonPropertyName("durumId")]
        public long DurumId { get; set; }
        [JsonPropertyName("durumAd")]
        public string DurumAd { get; set; }
        [JsonPropertyName("count")]
        public long Count { get; set; }
    }
}
