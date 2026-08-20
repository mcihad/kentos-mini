using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Filters;

/// <summary>
/// FORM PORTALI KAPISI — kurum ayarındaki bayrak kapalıysa uçlar yok.
/// </summary>
/// <remarks>
/// <para>
/// Uygulamanın <b>ikinci anonim yazma yüzeyi</b>. Kararları vatandaş
/// bildirim portalından devralıyor ve gerekçeleri aynı:
/// </para>
/// <para>
/// <b>404, 403 değil.</b> 403 "burada bir şey var ama kapalı" diyor ve
/// saldırgana geri gelmesi için sebep veriyor. Kapalı portal, hiç var
/// olmamış bir portaldan ayırt edilememeli.
/// </para>
/// <para>
/// <b>KAPI HER ŞEYDEN ÖNCE — <c>Order = -2001</c>.</b> Bu sayı ölçülmüş:
/// 400'ü döndüren şey bizim doğrulama filtremiz değil,
/// <c>[ApiController]</c>'ın kendi <c>ModelStateInvalidFilter</c>'ı ve o
/// <c>-2000</c>'de çalışıyor. Kapı ondan önde olmazsa kapalı portal, bozuk
/// bir gövdeye 400 dönüp "burada bir uç var" der.
/// </para>
/// <para>
/// <b>Bayrak bildirim portalınınkinden AYRI.</b> Şikâyet portalını açmanın
/// form portalını da açması, tek kararla iki ayrı maruziyet demekti.
/// </para>
/// </remarks>
public class FormPortaliFiltresi(
    IInstitutionService _kurum) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext baglam, ActionExecutionDelegate sonraki)
    {
        var kayit = await _kurum.GetAsync(baglam.HttpContext.RequestAborted);

        if (!kayit.FormPortalEnabled)
        {
            baglam.Result = new NotFoundResult();
            return;
        }

        await sonraki();
    }
}
