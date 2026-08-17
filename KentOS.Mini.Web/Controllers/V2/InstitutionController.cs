using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>
/// Kurum bilgileri — kurum adı, iletişim, uygulama adı ve kurumsal kimlik.
/// </summary>
/// <remarks>
/// <para>
/// <b>Okuma anonimdir.</b> Giriş ekranı da amblemi, kurum adını ve marka
/// rengini göstermek zorunda; bunlar oturum açılmadan önce okunabilmeli.
/// Yanıtta gizli bilgi yok — hepsi zaten sayfanın görünen yüzü.
/// </para>
/// <para>
/// <b>Yazma <c>sistem.kurum</c> ister.</b> Kurum adını ve amblemini
/// değiştirmek, sistemin bütün kullanıcılarının gördüğü yüzü değiştirmek
/// demek.
/// </para>
/// <para>
/// Bu uç SPA ve mobilin ÇALIŞMA ANINDA okuduğu tek kurum kaynağıdır; hiçbir
/// istemci derlemesine kurum bilgisi gömülmez. Böylece uygulamayı başka bir
/// belediyeye vermek, veritabanındaki tek satırı düzenlemekten ibaret olur.
/// </para>
/// </remarks>
[Route("api/v2/institution")]
public class InstitutionController(IInstitutionService _kurum) : V2ControllerBase
{
    /// <summary>Kurum bilgisi ve kurumsal kimlik.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<KurumBilgisiDto>(StatusCodes.Status200OK)]
    public Task<KurumBilgisiDto> KurumAsync(CancellationToken iptal)
        => _kurum.GetPublicAsync(iptal);

    /// <summary>Kurum bilgisini günceller.</summary>
    [HttpPut]
    [Izin(Izinler.SistemKurum)]
    [ProducesResponseType<KurumBilgisiDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status400BadRequest)]
    public Task<KurumBilgisiDto> GuncelleAsync(
        [FromBody] KurumGuncellemeIstegi istek, CancellationToken iptal)
        => _kurum.UpdateAsync(istek, iptal);
}
