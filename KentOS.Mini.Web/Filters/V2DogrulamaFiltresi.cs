using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using KentOS.Mini.Application.Dto.V2.Ortak;

namespace KentOS.Mini.Web.Filters;

/// <summary>
/// v2 istek gövdelerini FluentValidation ile doğrular.
///
/// <para>
/// NEDEN FİLTRE, NEDEN OTOMATİK DOĞRULAMA DEĞİL:
/// <c>FluentValidation.AspNetCore</c>'un otomatik doğrulaması MVC'ye GLOBAL
/// bağlanır ve v1'in davranışını da etkilerdi. Ayrıca
/// <c>ApiBehaviorOptions.InvalidModelStateResponseFactory</c>'ye de
/// dokunulmuyor — o da global ve v1'in hata gövdesini değiştirirdi.
/// </para>
///
/// <para>
/// Bu yüzden v2 DTO'ları <b>DataAnnotations taşımaz</b>: taşısaydı
/// <c>[ApiController]</c> kendi otomatik 400'ünü üretir ve buradaki tek tip
/// gövdeyi atlardı. Tüm kurallar doğrulayıcı sınıflarda.
/// </para>
/// </summary>
public class V2DogrulamaFiltresi(IServiceProvider _saglayici) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var hatalar = new Dictionary<string, List<string>>();

        foreach (var (ad, deger) in context.ActionArguments)
        {
            if (deger is null) continue;

            var dogrulayiciTipi = typeof(IValidator<>).MakeGenericType(deger.GetType());
            if (_saglayici.GetService(dogrulayiciTipi) is not IValidator dogrulayici) continue;

            var baglam = new ValidationContext<object>(deger);
            var sonuc = await dogrulayici.ValidateAsync(baglam, context.HttpContext.RequestAborted);
            if (sonuc.IsValid) continue;

            foreach (var hata in sonuc.Errors)
            {
                // Alan adı istemciye camelCase gitmeli — form alanlarıyla
                // eşleşsin diye.
                var alan = KucukIlkHarf(hata.PropertyName);
                if (string.IsNullOrEmpty(alan)) alan = ad;

                if (!hatalar.TryGetValue(alan, out var liste))
                {
                    liste = [];
                    hatalar[alan] = liste;
                }
                liste.Add(hata.ErrorMessage);
            }
        }

        if (hatalar.Count > 0)
        {
            context.Result = new ObjectResult(new HataYaniti
            {
                Tur = HataTurleri.Dogrulama,
                Baslik = "Doğrulama hatası",
                Durum = StatusCodes.Status400BadRequest,
                Ayrinti = "Gönderilen bilgilerde eksik veya hatalı alanlar var.",
                Ornek = context.HttpContext.Request.Path.Value,
                Hatalar = hatalar.ToDictionary(x => x.Key, x => x.Value.ToArray()),
            })
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
            return;
        }

        await next();
    }

    /// <summary>"Baslik" → "baslik", "Tekrar.Rrule" → "tekrar.rrule".</summary>
    private static string KucukIlkHarf(string ad)
    {
        if (string.IsNullOrEmpty(ad)) return ad;

        var parcalar = ad.Split('.');
        for (var i = 0; i < parcalar.Length; i++)
        {
            var p = parcalar[i];
            if (p.Length > 0)
            {
                parcalar[i] = char.ToLowerInvariant(p[0]) + p[1..];
            }
        }
        return string.Join('.', parcalar);
    }
}
