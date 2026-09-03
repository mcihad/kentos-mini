using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Dto
{
    public class PasswordChangeDto
    {
        [JsonPropertyName("password")]
        public string Password { get; set; }
        [JsonPropertyName("newPassword")]
        public string NewPassword { get; set; }
        [JsonPropertyName("newPasswordConfirm")]
        public string NewPasswordConfirm { get; set; }
    }
}
