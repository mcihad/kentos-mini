using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using KentOS.Mini.Web.Services;
using Xunit;

namespace KentOS.Mini.Tests;

public class JwtServiceTests
{
    // HmacSha256 imzalama için secret en az 32 bayt olmalıdır.
    private const string Secret = "unit-test-super-secret-key-0123456789!!";
    private const string Issuer = "wc-issuer";
    private const string Audience = "wc-audience";

    private static JwtService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT:Secret"] = Secret,
                ["JWT:ValidIssuer"] = Issuer,
                ["JWT:ValidAudience"] = Audience,
                ["JWT:TokenExpiration"] = "60"
            })
            .Build();

        // Fabrika üzerinden: aynı zamanda `JWT:*` anahtarlarının JwtOptions'a
        // doğru bağlandığını da doğruluyor.
        return JwtService.FromConfiguration(config);
    }

    [Fact]
    public void Secret_En_Az_32_Bayt_Olmali()
    {
        Assert.True(Encoding.UTF8.GetBytes(Secret).Length >= 32);
    }

    [Fact]
    public void GenerateToken_ValidTo_UtcNow_Arti_60_Dakikaya_Yakin_Olmali()
    {
        var service = CreateService();
        var before = DateTime.UtcNow;

        var token = service.GenerateToken(new List<Claim>
        {
            new(ClaimTypes.Name, "tester")
        });

        var after = DateTime.UtcNow;

        // ValidTo yaklaşık olarak (now + 60 dk) olmalı, saniye kırpması nedeniyle tolerans veriyoruz.
        var beklenenAlt = before.AddMinutes(60).AddSeconds(-5);
        var beklenenUst = after.AddMinutes(60).AddSeconds(5);

        Assert.InRange(token.ValidTo, beklenenAlt, beklenenUst);
    }

    [Fact]
    public void GenerateToken_Issuer_Audience_Ve_Claimleri_Icermeli()
    {
        var service = CreateService();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "cihad"),
            new(ClaimTypes.Role, "Admin")
        };

        var token = service.GenerateToken(claims);

        Assert.Equal(Issuer, token.Issuer);
        Assert.Contains(Audience, token.Audiences);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Name && c.Value == "cihad");
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void Uretilen_Token_Gecerli_Ve_Suresi_Dolmamis_Olmali()
    {
        var service = CreateService();
        var token = service.GenerateToken(new List<Claim> { new(ClaimTypes.Name, "tester") });
        var raw = new JwtSecurityTokenHandler().WriteToken(token);

        Assert.True(service.IsTokenValid(raw));
        Assert.False(service.IsTokenExpired(raw));
    }

    [Fact]
    public void GetRoles_Token_Icindeki_Rolleri_Dondurmeli()
    {
        var service = CreateService();
        var token = service.GenerateToken(new List<Claim>
        {
            new(ClaimTypes.Role, "Admin"),
            new(ClaimTypes.Role, "Editor")
        });
        var raw = new JwtSecurityTokenHandler().WriteToken(token);

        var roles = service.GetRoles(raw);

        Assert.Contains("Admin", roles);
        Assert.Contains("Editor", roles);
    }
}
