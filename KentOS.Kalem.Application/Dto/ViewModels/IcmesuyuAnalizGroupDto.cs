using KentOS.Kalem.Application.Dto.Sibeski;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto.ViewModels
{
    public class IcmesuyuAnalizGroupDto
    {
        [JsonPropertyName("tarih")]
        public DateOnly Tarih { get; set; }
        [JsonPropertyName("analizler")]
        public List<IcmesuyuAnalizDto> Analizler { get; set; }
    }
}
