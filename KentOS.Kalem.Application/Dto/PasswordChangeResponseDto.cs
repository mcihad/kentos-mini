using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto
{
    public class PasswordChangeResponseDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
