using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.Analiz
{
    public class RandevuArchivedCountDtoByBirim
    {
        [JsonPropertyName("birimAdi")]
        public string BirimAdi { get; set; }
        [JsonPropertyName("archivedCount")]
        public int ArchivedCount { get; set; }
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }
}
