using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Services;

namespace KentOS.Mini.Web.AuthPolicies;

/// <summary>
/// Ucu bir izne bağlar: <c>[Izin(Izinler.AjandaSil)]</c>.
/// </summary>
/// <remarks>
/// <para>
/// Politika yerine süzgeç kullanılmasının sebebi: politika adları derleme
/// anında metin, izin sabitleri ise <c>const</c>. Yanlış yazılan bir politika
/// adı çalışma anında "politika bulunamadı" ile patlar; yanlış yazılan bir
/// izin sabiti <b>derlenmez</b>.
/// </para>
/// <para>
/// <b>Rol adı DEĞİL izin denetlenir.</b> Eski <c>[Authorize(Roles=...)]</c>
/// kullanımları, yetki dağılımını koda gömüyordu ve her değişiklik bir sürüm
/// demekti.
/// </para>
/// <para>
/// Birden fazla izin verilirse <b>herhangi biri</b> yeterlidir (VEYA).
/// Hepsinin birden gerekmesi gereken bir uç çıkmadı; çıkarsa öznitelik
/// üst üste yazılır, o zaman VE olur.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class IzinAttribute(params string[] izinler) : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _izinler = izinler;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        /*
          [AllowAnonymous] BU FİLTREYİ DE GEÇERSİZ KILAR.

          `[Authorize]` için bunu çerçeve kendisi yapıyor ama `IzinAttribute`
          elle yazılmış bir yetkilendirme filtresi ve kendi kapısını kuruyor:
          sınıf düzeyinde `[Izin(...)]` taşıyan bir controller'da, metoda
          `[AllowAnonymous]` yazmak yetmiyordu — istek yine 401 alıyordu.

          Ölçülen sonuç: çiçekçiye SMS ile giden teslim bağlantısı, uçlar
          anonim ilan edilmiş olmasına rağmen açılmıyordu. Anonim uç istisna
          değil kural olduğu için denetim burada, tek yerde.
        */
        var anonimMi = context.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>()
            .Any();
        if (anonimMi) return;

        // Kimlik doğrulaması ayrı bir katman; buraya gelmişse jeton geçerli.
        // Yine de savunma amaçlı: kimliksiz istek 401 almalı, 403 değil.
        var kullanici = context.HttpContext.RequestServices
            .GetRequiredService<ICurrentUserService>();

        var kullaniciId = await kullanici.GetUserIdAsync();
        if (kullaniciId is null or 0)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var servis = context.HttpContext.RequestServices.GetRequiredService<IIzinServisi>();
        var sahip = await servis.IzinleriAsync(kullaniciId.Value);

        if (_izinler.Any(sahip.Contains)) return;

        // Hangi iznin eksik olduğunu SÖYLER. "Yetkiniz yok" mesajı, kullanıcıyı
        // da yöneticiyi de kör bırakıyordu: hangi yetkinin verilmesi gerektiği
        // ancak koda bakarak anlaşılıyordu.
        var eksik = _izinler
            .Select(i => Izinler.Katalog.FirstOrDefault(k => k.Ad == i)?.Baslik ?? i)
            .ToList();

        context.Result = new ObjectResult(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            title = "Yetkiniz yok",
            status = StatusCodes.Status403Forbidden,
            detail = eksik.Count == 1
                ? $"Bu işlem için \"{eksik[0]}\" yetkisi gerekiyor."
                : $"Bu işlem için şu yetkilerden biri gerekiyor: {string.Join(", ", eksik)}.",
            gerekenIzinler = _izinler,
        })
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }
}
