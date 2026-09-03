using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Application.Models;

namespace KentOS.Kalem.Web.Data
{
    public class DataSeeder
    {
        private static async Task EnsureRoles(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
            foreach (var role in UserRoles.GetRoles())
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new AppRole { Name = role });
                }
            }
        }

        private static async Task<long> EnsureBirim(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            if (!context.Birimler.Any())
            {
                var birim = new Birim();
                birim.Ad = "Belediye Başkanlığı";
                birim.Yetkili = "Dr. Adem UZUN";
                birim.Unvan = "Belediye Başkanı";
                birim.Telefon = "0 346 221 16 42";

                context.Birimler.Add(birim);

                await context.SaveChangesAsync();
                return birim.Id;
            }

            return await context.Birimler.Select(b => b.Id).FirstAsync();
        }

        private static async Task EnsureAdmin(IServiceProvider serviceProvider, long birimId)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var admin = await userManager.FindByNameAsync("admin");
            if (admin == null)
            {
                var user = new AppUser
                {
                    UserName = "admin",
                    Email = "cihadgundogdu@gmail.com",
                    Ad = "Cihad",
                    Soyad = "GÜNDOĞDU",
                    PhoneNumber = "0541 298 34 50",
                    SecurityStamp = Guid.NewGuid().ToString(),
                    BirimId = birimId
                };

                await userManager.CreateAsync(user, "Admin123.");
                await userManager.AddToRoleAsync(user, UserRoles.Admin);
                await userManager.AddToRoleAsync(user, UserRoles.Sistem);
            }
        }

        public static async Task EnsureRandevuDurumData(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            if (!context.RandevuDurumlar.Any())
            {
                var durumlar = new List<RandevuDurum>
                {
                    new() { DurumAd = "Beklemede", Renk = "#ebcc34" },
                    new() { DurumAd = "Onaylandı", Renk = "#29a35e" },
                    new() { DurumAd = "Devam Ediyor", Renk = "#34abeb" },
                    new() { DurumAd = "Tamamlandı", Renk = "#763feb" },
                    new() { DurumAd = "Reddedildi", Renk = "#ed5a55" },
                    new() { DurumAd = "İptal Edildi", Renk = "#ba58e8" }
                };
                context.RandevuDurumlar.AddRange(durumlar);
                await context.SaveChangesAsync();
            }
        }

        public static async Task EnsureRandevuTipData(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            if (!context.RandevuTipleri.Any())
            {
                var tipler = new List<RandevuTip>
                {
                    new() {Ad = "Halk Günü", Renk = "#FFD700"},
                    new() {Ad = "İş Talep", Renk = "#FFD700"},
                    new() {Ad = "Görüşme Talebi", Renk = "#008000"},
                    new() {Ad = "Açılış Daveti", Renk = "#FF0000"},
                    new() {Ad = "Resmi Tören", Renk = "#000000"},
                    new() {Ad = "Eğitim", Renk = "#0000FF"},
                    new() {Ad = "Toplantı", Renk = "#FFD700"},
                    new() {Ad = "Diğer", Renk = "#FFD700"}
                };
                context.RandevuTipleri.AddRange(tipler);
                await context.SaveChangesAsync();
            }
        }

        public static async Task EnsureAjandaDurumData(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            if (!context.AjandaDurumlar.Any())
            {
                var durumlar = new List<AjandaDurum>
                {
                    new() { Ad = "Beklemede", Renk = "#ebcc34" },
                    new() { Ad = "Onaylandı", Renk = "#29a35e" },
                    new() { Ad = "Devam Ediyor", Renk = "#34abeb" },
                    new() { Ad = "Tamamlandı", Renk = "#763feb" },
                    new() { Ad = "Reddedildi", Renk = "#ed5a55" },
                    new() { Ad = "İptal Edildi", Renk = "#ba58e8" }
                };
                context.AjandaDurumlar.AddRange(durumlar);
                await context.SaveChangesAsync();
            }
        }

        public static async Task EnsureInitialData(IServiceProvider serviceProvider)
        {
            await EnsureRoles(serviceProvider);
            // İzin kataloğu koddan gelir ve her açılışta eksikler eklenir;
            // ilk kurulumda roller bugünkü politikaların karşılığını alır.
            await IzinTohumu.UygulaAsync(
                serviceProvider.GetRequiredService<AppDbContext>());
            var birimId = await EnsureBirim(serviceProvider);
            await EnsureAdmin(serviceProvider, birimId);
            await EnsureRandevuDurumData(serviceProvider);
            await EnsureRandevuTipData(serviceProvider);
            await EnsureAjandaDurumData(serviceProvider);
        }
    }
}
