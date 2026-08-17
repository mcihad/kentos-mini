using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.ViewModels
{
    public class CicekciViewModel
    {
        [JsonPropertyName("cicekler")]
        public IEnumerable<CicekDto> Cicekler { get; set; }
        [JsonPropertyName("cicekci")]
        public CicekciDto Cicekci { get; set; }
    }
}
