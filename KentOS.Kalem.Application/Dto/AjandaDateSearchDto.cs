using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Dto
{
    public class AjandaDateSearchDto
    {
        [JsonPropertyName("date")]
        [Required(ErrorMessage = "Tarih zorunludur.")]
        public DateTime Date { get; set; }
    }
}
