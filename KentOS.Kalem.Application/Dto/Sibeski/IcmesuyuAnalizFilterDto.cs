using KentOS.Kalem.Application.Enums.Sibeski;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto.Sibeski
{
    public class IcmesuyuAnalizFilterDto
    {
        [JsonPropertyName("baslangicTarih")]
        public DateOnly? BaslangicTarih { get; set; }
        [JsonPropertyName("bitisTarih")]
        public DateOnly? BitisTarih { get; set; }
        [JsonPropertyName("analizNo")]
        public Guid? AnalizNo { get; set; }
        [JsonPropertyName("analizNokta")]
        public AnalizNokta? AnalizNokta { get; set; }
        [JsonPropertyName("parametreler")]
        public string[] Parametreler { get; set; } = [];
    }
}
