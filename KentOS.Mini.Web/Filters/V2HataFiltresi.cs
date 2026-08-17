using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Filters;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Filters;

/// <summary>
/// v2 uç noktalarının istisnalarını RFC 7807 gövdesine çevirir.
///
/// <para>
/// YALNIZCA v2'ye takılır (<see cref="Controllers.V2.V2ControllerBase"/> üzerinden);
/// <c>MvcOptions.Filters</c>'a eklenmez. Böylece v1'in davranışı kanıtlanabilir
/// şekilde değişmez.
/// </para>
///
/// <para>
/// <c>ExceptionHandled = true</c> işaretlemek kritik: global
/// <see cref="EntityNotFoundExceptionFilter"/> her istisnayı yakalıyor ve bu
/// bayrak olmadan buradaki yanıtı ezerdi.
/// </para>
/// </summary>
public class V2HataFiltresi(ILogger<V2HataFiltresi> _logger) : IAsyncActionFilter
{
    /// <summary>
    /// Postgres <c>SQLSTATE</c> kodu — iç istisnadan okunur.
    /// </summary>
    /// <remarks>
    /// Npgsql'e derleme zamanı bağımlılık kurmamak için tip ADIYLA
    /// eşleştiriliyor: filtre, veri sağlayıcısını bilmek zorunda değil ve
    /// sağlayıcı değişirse burası derlemeyi kırmıyor.
    /// </remarks>
    private static string? PostgresKodu(Exception istisna)
    {
        for (var i = istisna.InnerException; i is not null; i = i.InnerException)
        {
            if (i.GetType().FullName != "Npgsql.PostgresException") continue;
            return i.GetType().GetProperty("SqlState")?.GetValue(i) as string;
        }
        return null;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var sonuc = await next();

        if (sonuc.Exception is null || sonuc.ExceptionHandled)
        {
            return;
        }

        var yol = context.HttpContext.Request.Path.Value;

        var yanit = sonuc.Exception switch
        {
            EntityNotFoundException e => new HataYaniti
            {
                Tur = HataTurleri.Bulunamadi,
                Baslik = "Kayıt bulunamadı",
                Durum = StatusCodes.Status404NotFound,
                Ayrinti = e.Message,
                Ornek = yol,
            },
            BusinessRuleException e => new HataYaniti
            {
                Tur = HataTurleri.IsKurali,
                Baslik = "İşlem yapılamadı",
                Durum = StatusCodes.Status400BadRequest,
                // İş kuralı mesajları zaten kullanıcıya gösterilecek nitelikte
                // Türkçe metinler; olduğu gibi geçirilir.
                Ayrinti = e.Message,
                Ornek = yol,
            },
            UnauthorizedAccessException => new HataYaniti
            {
                Tur = HataTurleri.Yetkisiz,
                Baslik = "Bu işlem için yetkiniz yok",
                Durum = StatusCodes.Status403Forbidden,
                Ornek = yol,
            },

            /*
              VERİTABANI KISIT İHLALLERİ 400'E ÇEVRİLİR.

              Gövdede var olmayan bir kimlik geldiğinde (silinmiş bir etkinlik
              tipi, başka birimin mahallesi) Postgres yabancı anahtar hatası
              atıyor ve bu 500 olarak dönüyordu: kullanıcı "beklenmeyen bir
              hata" görüyor, sistem hataları tablosuna kayıt düşüyor ve
              gerçekte yapılması gereken tek şey "geçersiz seçim" demekti.
              Sahada yakalanan hata buydu (`fk_ajandalar_randevu_tipleri_...`).

              Mesaj kullanıcıya HAM verilmez: kısıt adı tablo/kolon yapısını
              ele veriyor. Hangi kısıt olduğu günlüğe ve sistem hataları
              kaydına düşmeye devam ediyor.
            */
            DbUpdateException d when PostgresKodu(d) is "23503" => new HataYaniti
            {
                Tur = HataTurleri.IsKurali,
                Baslik = "İşlem yapılamadı",
                Durum = StatusCodes.Status400BadRequest,
                Ayrinti = "Seçilen kayıtlardan biri bulunamadı ya da silinmiş. "
                        + "Listeleri tazeleyip yeniden deneyin.",
                Ornek = yol,
            },
            DbUpdateException d when PostgresKodu(d) is "23502" => new HataYaniti
            {
                Tur = HataTurleri.IsKurali,
                Baslik = "Eksik bilgi",
                Durum = StatusCodes.Status400BadRequest,
                Ayrinti = "Zorunlu alanlardan biri boş bırakılmış.",
                Ornek = yol,
            },
            DbUpdateException d when PostgresKodu(d) is "23505" => new HataYaniti
            {
                Tur = HataTurleri.IsKurali,
                Baslik = "Kayıt zaten var",
                Durum = StatusCodes.Status400BadRequest,
                Ayrinti = "Aynı kayıt daha önce eklenmiş.",
                Ornek = yol,
            },

            _ => null,
        };

        if (yanit is null)
        {
            // Beklenmeyen hata: ayrıntı DIŞARI VERİLMEZ. v1'in global filtresi
            // ham `exception.Message` döndürüyor (bilgi sızıntısı); v2 bunu
            // tekrarlamaz. Günlükle eşleştirmek için bir iz kimliği verilir.
            var iz = context.HttpContext.TraceIdentifier;
            _logger.LogError(sonuc.Exception,
                "v2 beklenmeyen hata. İz: {Iz}, Yol: {Yol}", iz, yol);

            // Konsol günlüğü sunucu yeniden başlayınca kayboluyor; kullanıcı
            // "hata aldım" dediğinde geriye bakacak bir şey kalmıyordu.
            // Kayıt isteği BOZMAZ: servis kendi hatalarını yutuyor.
            var kayit = context.HttpContext.RequestServices
                .GetService<Services.V2.IHataKaydiServisi>();
            if (kayit is not null && sonuc.Exception is not null)
            {
                await kayit.KaydetAsync(
                    sonuc.Exception, context.HttpContext, iz,
                    StatusCodes.Status500InternalServerError);
            }

            yanit = new HataYaniti
            {
                Tur = HataTurleri.Sunucu,
                Baslik = "Beklenmeyen bir hata oluştu",
                Durum = StatusCodes.Status500InternalServerError,
                Ayrinti = "İşlem tamamlanamadı. Sorun sürerse iz kimliğiyle bildirin.",
                Ornek = yol,
                IzKimligi = iz,
            };
        }

        sonuc.Result = new ObjectResult(yanit) { StatusCode = yanit.Durum };
        sonuc.ExceptionHandled = true;
    }
}
