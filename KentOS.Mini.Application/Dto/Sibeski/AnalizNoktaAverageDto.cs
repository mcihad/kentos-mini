using KentOS.Mini.Application.Enums.Sibeski;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.Sibeski
{
    public class AnalizNoktaAverageDto
    {
        [JsonPropertyName("analizNokta")]
        public AnalizNokta AnalizNokta { get; set; }
        [JsonPropertyName("sicaklikAvg")]
        public float SicaklikAvg { get; set; }
        [JsonPropertyName("phAvg")]
        public float PhAvg { get; set; }
        [JsonPropertyName("phsAvg")]
        public float PhsAvg { get; set; }
        [JsonPropertyName("bulaniklikAvg")]
        public float BulaniklikAvg { get; set; }
        [JsonPropertyName("renkAvg")]
        public float RenkAvg { get; set; }
        [JsonPropertyName("serbestKlorAvg")]
        public float SerbestKlorAvg { get; set; }
        [JsonPropertyName("alkaliniteAvg")]
        public float AlkaliniteAvg { get; set; }
        [JsonPropertyName("tsertlikAvg")]
        public float TsertlikAvg { get; set; }
        [JsonPropertyName("casertlikAvg")]
        public float CasertlikAvg { get; set; }
        [JsonPropertyName("mgsertlikAvg")]
        public float MgsertlikAvg { get; set; }
        [JsonPropertyName("tdsAvg")]
        public float TdsAvg { get; set; }
        [JsonPropertyName("iletkenlikAvg")]
        public float IletkenlikAvg { get; set; }
        [JsonPropertyName("demirAvg")]
        public float DemirAvg { get; set; }
        [JsonPropertyName("manganAvg")]
        public float ManganAvg { get; set; }
        [JsonPropertyName("nitratAvg")]
        public float NitratAvg { get; set; }
        [JsonPropertyName("nitritAvg")]
        public float NitritAvg { get; set; }
        [JsonPropertyName("amonyakAvg")]
        public float AmonyakAvg { get; set; }
        [JsonPropertyName("permanganatAvg")]
        public float PermanganatAvg { get; set; }
        [JsonPropertyName("oksijenAvg")]
        public float OksijenAvg { get; set; }
        [JsonPropertyName("bakteriyolojikAvg")]
        public float BakteriyolojikAvg { get; set; }
    }
}
