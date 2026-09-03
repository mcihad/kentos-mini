using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Web.Services
{
    public interface IJwtService
    {
        JwtSecurityToken GenerateToken(List<Claim> claims);
        bool IsTokenExpired(string token);
        bool IsTokenValid(string token);
        //get roles
        List<string> GetRoles(string token);
    }
}
