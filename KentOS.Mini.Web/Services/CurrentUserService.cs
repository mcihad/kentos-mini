using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using System.Diagnostics;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace KentOS.Mini.Web.Services
{
    public class CurrentUserService(
        IHttpContextAccessor httpContext,
        AppDbContext context,
        AuthPolicies.IIzinServisi izinler) : ICurrentUserService
    {
        /// <inheritdoc />
        public async Task<bool> YalnizcaBasinMiAsync()
        {
            var kullaniciId = await GetUserIdAsync();
            if (kullaniciId is null) return false;

            var sahip = await izinler.IzinleriAsync(kullaniciId.Value);

            // Tam görüntüleme izni daraltmayı ezer: iki izin bir arada
            // verildiğinde kullanıcının ajandası boşalmasın.
            if (sahip.Contains(Application.Identity.Izinler.AjandaGoruntule)) return false;

            return sahip.Contains(Application.Identity.Izinler.AjandaBasinGoruntule);
        }

        /// <inheritdoc />
        public async Task<string?> GetCurrentBirimAdiAsync()
        {
            var birimId = GetCurrentBirimId();
            if (birimId <= 0) return null;

            return await context.Birimler
                .Where(b => b.Id == birimId)
                .Select(b => b.Ad)
                .FirstOrDefaultAsync();
        }

        public  string GetUsername()
        {
            if (httpContext.HttpContext == null)
            {
                return null;
            }
            return httpContext.HttpContext.User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;    
        }

        public long GetCurrentBirimId()
        {
            if (httpContext.HttpContext == null)
            {
                return 0;
            }
            var kurumId = httpContext.HttpContext.User.FindFirstValue(WorkClaimTypes.BirimIdIdentifier);
            return long.TryParse(kurumId, out var id) ? id : 0;
        }

        /// <summary>
        /// Sayısal kullanıcı kimliği. Claim varsa DB'ye gidilmez; yoksa kullanıcı
        /// adından tek sorguyla bulunur (eski çerezler için emniyet).
        /// </summary>
        public async Task<long?> GetUserIdAsync()
        {
            if (httpContext.HttpContext == null)
            {
                return null;
            }

            var claim = httpContext.HttpContext.User.FindFirstValue(WorkClaimTypes.UserIdIdentifier);
            if (long.TryParse(claim, out var id) && id > 0)
            {
                return id;
            }

            var username = GetUsername();
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            return await context.Users
                .Where(x => x.UserName == username)
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<AppUser> GetCurrentAsync()
        {
            var username = GetUsername();
            return await context.Users.FirstOrDefaultAsync(x => x.UserName == username) ?? throw new EntityNotFoundException("Kullanıcı bulunamadı");
        }


        public async Task<string> GetFullNameAsync()
        {
            var user = await GetCurrentAsync();
            return $"{user.Ad} {user.Soyad}";
        }

        public async Task<string> GetFullNameAndUnvan()
        {
            var user = await GetCurrentAsync();
            return $"{user.Ad} {user.Soyad} - {user.Unvan}";

        }
    }
}
