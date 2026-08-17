using KentOS.Mini.Application.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Dto.Randevu
{
    public class AjandaChangeStateDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("newStatus")]
        public AjandaStatus NewStatus { get; set; }
    }
}
