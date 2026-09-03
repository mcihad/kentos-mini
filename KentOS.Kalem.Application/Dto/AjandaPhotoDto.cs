using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Dto
{
    public class AjandaPhotoDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [Display(Name = "Dosya Adı")]
        [JsonPropertyName("filename")]
        public string Filename { get; set; }
        [Display(Name = "Dosya Türü")]
        [JsonPropertyName("contentType")]
        public string ContentType { get; set; }
        [JsonPropertyName("ajandaId")]
        public long AjandaId { get; set; }
    }
}
