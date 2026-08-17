using KentOS.Mini.Application.Dto.Sibeski;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.ViewModels
{
    public class IcmesuyuAnalizDetailViewModel
    {
        [JsonPropertyName("tarih")]
        public DateOnly Tarih { get; set; }
        [JsonPropertyName("analizNoktaVerileri")]
        public List<IcmesuyuAnalizDto> AnalizNoktaVerileri { get; set; }

    }
}
