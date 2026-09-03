namespace KentOS.Kalem.Web.Middleware;

/// <summary>
/// Gönderilen dosyaların HTTP'den doğrudan indirilmesini engeller.
/// </summary>
/// <remarks>
/// <para>
/// Dosyalar <c>wwwroot/uploads/gonderim</c> altında duruyor. Sebebi izin:
/// uygulama havuzu kimliğine <c>wwwroot/uploads</c> için yazma hakkı zaten
/// verilmiş durumda (etkinlik fotoğrafları ve talep dosyaları iki yıldır oraya
/// yazılıyor). Ayrı bir klasör, her kurulumda elle izin vermeyi gerektirirdi.
/// </para>
/// <para>
/// Ama <c>wwwroot</c> altındaki her şey statik dosya ara katmanıyla
/// <b>kimlik doğrulanmadan</b> servis edilir. Bu ara katman, adresi bilen
/// birinin belgeyi indirmesini keser: gönderim dosyalarına tek giriş
/// <c>GET /api/v2/gonderim/{id}/dosya</c>, o da gönderen/alıcı süzgecinden
/// geçer.
/// </para>
/// <para>
/// <b>Sıra önemlidir:</b> <c>UseStaticFiles</c>'tan ÖNCE eklenmelidir.
/// <c>GonderimDosyaKorumasiTests</c> hem kuralı hem de sıralamayı bekçiler.
/// </para>
/// </remarks>
public static class GonderimDosyaKorumasi
{
    /// <summary>Korunan klasörün web yolu.</summary>
    public const string Yol = "/uploads/gonderim";

    /// <summary>
    /// Bu istek gönderim klasörüne mi gidiyor?
    /// </summary>
    /// <remarks>
    /// <c>StartsWithSegments</c> kullanılır, düz <c>StartsWith</c> değil:
    /// <c>/uploads/gonderimler-arsivi</c> gibi başka bir klasör yanlışlıkla
    /// kapatılmamalı. Karşılaştırma büyük/küçük harf duyarsız — Windows dosya
    /// sistemi de öyle ve <c>/UPLOADS/GONDERIM/...</c> ile atlatılabilirdi.
    /// </remarks>
    public static bool Kapali(PathString istekYolu) =>
        istekYolu.StartsWithSegments(Yol, StringComparison.OrdinalIgnoreCase);

    /// <summary>Korumayı ardışık düzene ekler. <c>UseStaticFiles</c>'tan ÖNCE çağrılmalı.</summary>
    public static IApplicationBuilder UseGonderimDosyaKorumasi(this IApplicationBuilder app) =>
        app.Use(async (baglam, sonraki) =>
        {
            if (Kapali(baglam.Request.Path))
            {
                // 403 DEĞİL 404: "burada bir dosya var ama giremezsin" bilgisi
                // bile sızıntıdır. Klasörün varlığı görünmemeli.
                baglam.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await sonraki();
        });
}
