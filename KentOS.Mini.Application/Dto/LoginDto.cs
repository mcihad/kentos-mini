using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto
{
    public class LoginDto
    {
        [Required(ErrorMessage ="Kullanıcı Adı Girin")]
        [Display(Name = "Kullanıcı Adı")]
        [JsonPropertyName("username")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Şifre Girin")]
        [Display(Name = "Şifre")]
        [JsonPropertyName("password")]
        public string Password { get; set; }
        //Remember me
        [Display(Name = "Beni Hatırla")]
        [JsonPropertyName("rememberMe")]

        public bool RememberMe { get; set; } = false;
    }
}
