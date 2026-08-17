using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace KentOS.Mini.Web.Filters;

/// <summary>
/// HIZ SINIRI — yalnızca ANONİM uçlar için.
/// </summary>
/// <remarks>
/// <para>
/// Uygulamanın geri kalanında hız sınırı YOK ve bu bilinçli: kimliği
/// doğrulanmış bir personelin isteklerini kısmak, yoğun bir günde işini
/// yavaşlatmaktan başka bir işe yaramaz. Sınır yalnızca kimliği
/// doğrulanmamış kaynağa uygulanıyor.
/// </para>
/// <para>
/// .NET'in yerleşik <c>AddRateLimiter</c>'ı kullanılıyor; yeni paket
/// gerekmiyor.
/// </para>
/// <para>
/// <b>Bölümleme IP başına.</b> Aynı kurumsal ağın arkasındaki iki vatandaş
/// aynı bölüme düşer; bu yüzden pencere cömert tutuldu — asıl daraltma
/// numara başına, servis katmanında.
/// </para>
/// </remarks>
public static class HizSiniri
{
    /// <summary>Vatandaş portalı politikasının adı.</summary>
    public const string VatandasPortali = "vatandas-portali";

    /// <summary>Bir IP'nin bir dakikada atabileceği istek.</summary>
    private const int DakikalikIstek = 12;

    public static IServiceCollection AddVatandasHizSiniri(this IServiceCollection servisler)
    {
        servisler.AddRateLimiter(secenekler =>
        {
            // 429 varsayılan olarak GÖVDESİZ dönüyor ve istemci "sunucu
            // bozuldu" ile "çok hızlısınız" ayrımını yapamıyordu.
            secenekler.OnRejected = async (baglam, iptal) =>
            {
                baglam.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                baglam.HttpContext.Response.ContentType = "application/problem+json";

                await baglam.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = HataTurleri.IsKurali,
                    title = "Çok fazla istek",
                    status = 429,
                    detail = "Kısa sürede çok fazla istek gönderildi. Lütfen biraz bekleyin.",
                }, iptal);
            };

            secenekler.AddPolicy(VatandasPortali, baglam =>
                RateLimitPartition.GetFixedWindowLimiter(
                    // Adres çözülemezse hepsi TEK bölüme düşüyor: bilinmeyen
                    // kaynağa ayrı ayrı hak vermek, sınırı anlamsız kılardı.
                    baglam.Connection.RemoteIpAddress?.ToString() ?? "bilinmeyen",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = DakikalikIstek,
                        Window = TimeSpan.FromMinutes(1),

                        // Kuyruk YOK: sınıra takılan istek beklemek yerine
                        // hemen 429 almalı. Kuyrukta bekleyen istek, portalı
                        // yavaş sanan kullanıcıya formu yeniden gönderttirir.
                        QueueLimit = 0,
                    }));
        });

        return servisler;
    }
}
