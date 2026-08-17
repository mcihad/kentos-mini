using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto
{
    public class MetadataDto
    {
        [JsonPropertyName("olusturmaTarih")]
        public DateTime? OlusturmaTarih { get; set; }
        [JsonPropertyName("olusturan")]
        public string? Olusturan { get; set; }
        [JsonPropertyName("guncellemeTarih")]
        public DateTime? GuncellemeTarih { get; set; }
        [JsonPropertyName("guncelleyen")]
        public string? Guncelleyen { get; set; }

    }
}
