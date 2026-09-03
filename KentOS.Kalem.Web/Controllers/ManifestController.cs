using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Web.Controllers;

/// <summary>
/// PWA manifesti — kurum bilgisine göre ÜRETİLİR.
/// </summary>
/// <remarks>
/// <para>
/// Manifest daha önce ön yüzün <c>public/</c> klasöründe duran statik bir
/// dosyaydı ve içinde kurum adı yazıyordu. Uygulama başka belediyelere
/// verileceği için bu kabul edilemez: kurum değişince manifest de değişmeli
/// ve bunun için ön yüzü yeniden derlemek gerekmemeli.
/// </para>
/// <para>
/// <b>Rota kökte</b> (<c>/manifest.webmanifest</c>), <c>/api/v2</c> altında
/// değil: <c>index.html</c> içindeki <c>&lt;link rel="manifest"&gt;</c> bu
/// adresi gösteriyor ve tarayıcının kurulabilirlik denetimi kapsam (scope)
/// ile aynı köke bakıyor.
/// </para>
/// <para>
/// Kimlik doğrulaması YOK: manifest kurulum sırasında, oturum açılmadan
/// okunuyor. İçinde gizli bilgi de yok.
/// </para>
/// </remarks>
[ApiController]
[AllowAnonymous]
public class ManifestController(IInstitutionService _kurum) : ControllerBase
{
    /// <summary>Simge tanımları — kuruma göre değişmez, dosya adları sabit.</summary>
    private static readonly object[] Simgeler =
    [
        new { src = "/ikon/ikon-48.png", sizes = "48x48", type = "image/png", purpose = "any" },
        new { src = "/ikon/ikon-72.png", sizes = "72x72", type = "image/png", purpose = "any" },
        new { src = "/ikon/ikon-96.png", sizes = "96x96", type = "image/png", purpose = "any" },
        new { src = "/ikon/ikon-128.png", sizes = "128x128", type = "image/png", purpose = "any" },
        new { src = "/ikon/ikon-144.png", sizes = "144x144", type = "image/png", purpose = "any" },
        new { src = "/ikon/ikon-152.png", sizes = "152x152", type = "image/png", purpose = "any" },
        new { src = "/ikon/ikon-192.png", sizes = "192x192", type = "image/png", purpose = "any" },
        new { src = "/ikon/ikon-256.png", sizes = "256x256", type = "image/png", purpose = "any" },
        new { src = "/ikon/ikon-384.png", sizes = "384x384", type = "image/png", purpose = "any" },
        new { src = "/ikon/ikon-512.png", sizes = "512x512", type = "image/png", purpose = "any" },
        new { src = "/ikon/maskable-192.png", sizes = "192x192", type = "image/png", purpose = "maskable" },
        new { src = "/ikon/maskable-512.png", sizes = "512x512", type = "image/png", purpose = "maskable" },
    ];

    /// <summary>Ana ekran kısayolları.</summary>
    private static readonly object[] Kisayollar =
    [
        new { name = "Takvim", short_name = "Takvim", description = "Aylık takvim görünümü",
              url = "/takvim", icons = new[] { new { src = "/ikon/ikon-192.png", sizes = "192x192" } } },
        new { name = "Ajanda", short_name = "Ajanda", description = "Günlük program akışı",
              url = "/ajanda", icons = new[] { new { src = "/ikon/ikon-192.png", sizes = "192x192" } } },
        new { name = "Talepler", short_name = "Talepler", description = "Vatandaş talepleri",
              url = "/talepler", icons = new[] { new { src = "/ikon/ikon-192.png", sizes = "192x192" } } },
    ];

    [HttpGet("/manifest.webmanifest")]
    [Produces("application/manifest+json")]
    public async Task<IActionResult> ManifestAsync(CancellationToken iptal)
    {
        var k = await _kurum.GetAsync(iptal);

        var uygulamaAdi = string.IsNullOrWhiteSpace(k.ApplicationName) ? "KentOS.Kalem" : k.ApplicationName;
        var kurumAdi = k.ResolvedDisplayName;
        var marka = string.IsNullOrWhiteSpace(k.BrandPrimary) ? "#002E6D" : k.BrandPrimary;

        var manifest = new
        {
            id = "/",
            name = string.IsNullOrWhiteSpace(kurumAdi) ? uygulamaAdi : $"{kurumAdi} · {uygulamaAdi}",
            short_name = string.IsNullOrWhiteSpace(k.ApplicationShortName) ? uygulamaAdi : k.ApplicationShortName,
            description = k.ApplicationDescription ?? string.Empty,
            lang = "tr",
            dir = "ltr",
            start_url = "/",
            // Kapsam KÖK olmalı: `start_url` kapsam dışında kalırsa tarayıcı
            // uygulamayı "kurulabilir" saymıyor.
            scope = "/",
            display = "standalone",
            display_override = new[] { "window-controls-overlay", "standalone", "minimal-ui" },
            orientation = "any",
            theme_color = marka,
            background_color = marka,
            categories = new[] { "productivity", "business", "government" },
            prefer_related_applications = false,
            // Kendini ilişkili uygulama olarak bildirir; `getInstalledRelatedApps`
            // kurulu olup olmadığını böyle anlayabiliyor (bkz. pwa/install.ts).
            related_applications = new[] { new { platform = "webapp", url = "/manifest.webmanifest" } },
            icons = Simgeler,
            shortcuts = Kisayollar,
        };

        // Manifest kurum kaydı değişince güncellenmeli; uzun önbellek, kurum
        // adını değiştiren yöneticiye "hiçbir şey olmadı" hissi verirdi.
        Response.Headers.CacheControl = "public, max-age=300";
        return new JsonResult(manifest) { ContentType = "application/manifest+json" };
    }
}
