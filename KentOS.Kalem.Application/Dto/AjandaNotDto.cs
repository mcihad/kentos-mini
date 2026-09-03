using System.ComponentModel.DataAnnotations;
using KentOS.Kalem.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Dto
{
    public class AjandaNotDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("not")]
        [Required(ErrorMessage = "Not metni zorunludur.")]
        [MaxLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
        public string Not { get; set; }

        [JsonPropertyName("ajandaId")]
        public long? AjandaId { get; set; }

        [JsonPropertyName("olusturan")]
        public string? Olusturan { get; set; }

        [JsonPropertyName("olusturulmaTarihi")]
        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;
    }
}
