using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Filters;

/// <summary>
/// VATANDAŞ PORTALI KAPISI — kurum ayarındaki bayrak kapalıysa uçlar yok.
/// </summary>
/// <remarks>
/// <para>
/// Portal, uygulamanın <b>tek anonim yazma yüzeyi</b>. Onu bir yapılandırma
/// dosyasına değil kurum ayarına bağlamak, "kim açtı, ne zaman açtı"
/// sorusunun cevabını sunucuya erişmeden verilebilir kılıyor; ama bunun
/// karşılığında bayrağın <b>gerçekten kapatabiliyor</b> olması şart.
/// Ekranı gizlemek yetmez — uçlar açık kaldığı sürece portal açıktır.
/// </para>
///
/// <para>
/// <b>404, 403 değil.</b> 403 "burada bir şey var ama kapalı" diyor ve
/// saldırgana geri gelmesi için sebep veriyor. Kapalı portal, hiç var
/// olmamış bir portaldan ayırt edilememeli.
/// </para>
///
/// <para>
/// <b>Kapı hız sınırının ARDINDA.</b> Bayrak bir veritabanı okuması
/// (önbellekli de olsa) ve kapalı bir portalın adresini bombalayan biri onu
/// tetiklememeli. <c>EnableRateLimiting</c> ara katmanda çalışıyor, bu filtre
/// ise eylem seçiminden sonra — sıra kendiliğinden doğru.
/// </para>
///
/// <para>
/// Filtre <b>eylem düzeyinde</b> çalışıyor, yetkilendirme düzeyinde değil:
/// yetkilendirme filtreleri <c>[AllowAnonymous]</c> ile kısa devre olan bir
/// hattın parçası ve orada durmak, kapıyı anonimliğin kendisiyle
/// karıştırmak olurdu. Bu kapının anonimlikle ilgisi yok; ucun var olup
/// olmadığını söylüyor.
/// </para>
/// </remarks>
public class VatandasPortaliFiltresi(
    IInstitutionService _kurum) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext baglam, ActionExecutionDelegate sonraki)
    {
        var kayit = await _kurum.GetAsync(baglam.HttpContext.RequestAborted);

        if (!kayit.CitizenReportEnabled)
        {
            baglam.Result = new NotFoundResult();
            return;
        }

        await sonraki();
    }
}
