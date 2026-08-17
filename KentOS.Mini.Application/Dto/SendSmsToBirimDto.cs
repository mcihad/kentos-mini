using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Dto
{
    public class SendSmsToBirimDto
    {
        [JsonPropertyName("ajandaId")]
        public long AjandaId { get; set; }
        //birim ids long array
        [JsonPropertyName("birimIds")]
        [Required(ErrorMessage = "En az bir birim seçilmelidir.")]
        public long[] BirimIds { get; set; }
        [JsonPropertyName("message")]
        [Required(ErrorMessage = "Mesaj metni zorunludur.")]
        [MaxLength(500, ErrorMessage = "Mesaj en fazla 500 karakter olabilir.")]
        public string Message { get; set; }

    }
}
