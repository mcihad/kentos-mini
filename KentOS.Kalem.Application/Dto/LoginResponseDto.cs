using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto
{
    public class LoginResponseDto
    {


        [JsonPropertyName("token")]
        public string Token { get; set; }
        [JsonPropertyName("validTo")]
        public DateTime Expiration { get; set; }
    }
}
