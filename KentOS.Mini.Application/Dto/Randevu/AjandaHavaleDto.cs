using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Dto.Randevu
{
    public class AjandaHavaleDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("birimId")]
        [Required(ErrorMessage = "Havale edilecek birim zorunludur.")]
        public long BirimId { get; set; }
        [JsonPropertyName("not")]
        public string? Not { get; set; }
    }
}
