using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto
{
    public class CicekKartDto
    {
        [JsonPropertyName("ajanda")]
        public AjandaDto Ajanda { get; set; }
        [JsonPropertyName("cicek")]
        public CicekDto Cicek { get; set; }
    }
}
