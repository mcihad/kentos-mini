using Microsoft.IdentityModel.Tokens;
using KentOS.Mini.Web.Configuration;
using KentOS.Mini.Web.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KentOS.Mini.Web.Services
{
    /// <summary>
    /// Jeton üretimi ve doğrulaması.
    /// </summary>
    /// <remarks>
    /// Ayarlar <see cref="JwtOptions"/> üzerinden okunur; anahtar adları
    /// değişmediği için hem <c>appsettings.json</c> hem de <c>JWT__SECRET</c>
    /// ortam değişkeni aynı yere düşer. Bu ÖNEMLİ: imza anahtarı değişirse
    /// sahadaki bütün mobil oturumlar düşer.
    /// </remarks>
    public class JwtService : IJwtService
    {
        private readonly JwtOptions _ayar;

        public JwtService(JwtOptions ayar) => _ayar = ayar;

        /// <summary>
        /// Yapılandırmadan kurar.
        /// </summary>
        /// <remarks>
        /// İkinci bir KURUCU olarak yazılamaz: DI kapsayıcısı iki kurucu
        /// arasında seçim yapamayıp "ambiguous constructors" ile açılışta
        /// patlıyor. Bu yüzden fabrika metodu.
        /// </remarks>
        public static JwtService FromConfiguration(IConfiguration configuration) =>
            new(OptionsRegistration.Read<JwtOptions>(configuration, JwtOptions.SectionName));

        public JwtSecurityToken GenerateToken(List<Claim> claims)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_ayar.Secret));

            var token = new JwtSecurityToken(
                issuer: _ayar.ValidIssuer,
                audience: _ayar.ValidAudience,
                expires: DateTime.UtcNow.AddMinutes(_ayar.TokenExpiration),
                claims: claims,
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }

        public bool IsTokenExpired(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            // ValidTo her zaman UTC'dir; DateTime.Now (yerel) ile karşılaştırmak yanlış sonuç verirdi.
            return jwtToken?.ValidTo < DateTime.UtcNow;
        }

        public bool IsTokenValid(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_ayar.Secret));
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _ayar.ValidIssuer,
                ValidAudience = _ayar.ValidAudience,
                IssuerSigningKey = signingKey
            };

            try
            {
                tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<string> GetRoles(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            return jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        }
    }
}
