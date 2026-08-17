using Microsoft.AspNetCore.StaticFiles;
using KentOS.Mini.Web.Options;
using KentOS.Mini.Web.Storage;

namespace KentOS.Mini.Web.Middleware;

/// <summary>
/// Nesne deposu kullanılırken eski <c>/uploads/...</c> adreslerini ayakta
/// tutar.
/// </summary>
/// <remarks>
/// <para>
/// <b>Bu ara katman olmadan mobil uygulama kırılır.</b> Sahadaki v1
/// istemcileri etkinlik fotoğraflarını ve talep eklerini doğrudan
/// <c>/uploads/ajanda/….jpg</c> adresinden indiriyor; bu, dokunulmayacağına
/// söz verilen sözleşmenin parçası. Depolama S3'e alındığında o dosyalar
/// diskte kalmadığı için statik dosya ara katmanı 404 dönerdi.
/// </para>
/// <para>
/// Köprü, isteği nesne deposundan karşılar. Yerel sağlayıcıda hiç devreye
/// girmez — <see cref="IFileStorage.IsRemote"/> yanlışsa istek doğrudan
/// geçirilir, yani bugünkü kurulumda ek maliyet yok.
/// </para>
/// <para>
/// <b>Sıra:</b> <see cref="GonderimDosyaKorumasi"/>'ndan SONRA, statik
/// dosyalardan ÖNCE. Böylece gönderim klasörü buradan da sızmaz.
/// </para>
/// </remarks>
public static class UzakDepoKopruSu
{
    /// <summary>Köprünün karşıladığı yol öneki.</summary>
    public const string Yol = "/uploads";

    /// <summary>Ara katmanı ardışık düzene ekler.</summary>
    public static IApplicationBuilder UseUzakDepoKopruSu(this IApplicationBuilder app)
    {
        var depo = app.ApplicationServices.GetRequiredService<IFileStorage>();

        // Yerel depoda köprüye gerek yok: dosyalar zaten diskte ve statik
        // dosya ara katmanı onları çok daha ucuza servis ediyor.
        if (!depo.IsRemote) return app;

        var turSaptayici = new FileExtensionContentTypeProvider();

        return app.Use(async (baglam, sonraki) =>
        {
            if (!baglam.Request.Path.StartsWithSegments(Yol, StringComparison.OrdinalIgnoreCase) ||
                !HttpMethods.IsGet(baglam.Request.Method))
            {
                await sonraki();
                return;
            }

            // Gönderim klasörü BURADAN DA açılmaz.
            if (GonderimDosyaKorumasi.Kapali(baglam.Request.Path))
            {
                baglam.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var anahtar = baglam.Request.Path.Value!.TrimStart('/');

            byte[]? icerik;
            try
            {
                icerik = await depo.ReadAllBytesAsync(
                    StorageArea.Public, anahtar, baglam.RequestAborted);
            }
            catch (ArgumentException)
            {
                // Bozuk/dizin dışına çıkan anahtar — var olmayan dosyayla aynı cevap.
                icerik = null;
            }

            if (icerik is null)
            {
                await sonraki();
                return;
            }

            baglam.Response.ContentType =
                turSaptayici.TryGetContentType(anahtar, out var tur) ? tur : "application/octet-stream";
            baglam.Response.ContentLength = icerik.Length;
            await baglam.Response.Body.WriteAsync(icerik, baglam.RequestAborted);
        });
    }
}
