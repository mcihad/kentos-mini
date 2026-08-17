using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto
{
    public class LoginResponseDto
    {


        [JsonPropertyName("token")]
        public string Token { get; set; }
        [JsonPropertyName("validTo")]
        public DateTime Expiration { get; set; }
    }
}
