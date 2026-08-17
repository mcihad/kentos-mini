using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Web.Filters;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>
/// v2 controller'larının ortak tabanı.
///
/// <para>
/// <b>Rota:</b> her controller kendi rotasını <b>birebir yazar</b>
/// (<c>[Route("api/v2/etkinlik")]</c>). <c>[controller]</c> belirteci
/// KULLANILMAZ — v1'de bu, <c>AjandaApiController</c>'ı <c>/api/AjandaApi</c>
/// yapan tuzağı doğurmuştu ve ayrıca MVC'deki <c>AjandaController</c> ile ad
/// çakışması riski var.
/// </para>
///
/// <para>
/// <b>Kimlik:</b> her zaman JWT. Uygulamanın varsayılan challenge şeması
/// Cookie olduğu için bu açıkça yazılmazsa yetkisiz istek 401 yerine giriş
/// sayfasına 302 döner ve SPA bunu anlayamaz.
/// </para>
///
/// <para>
/// <b>Filtreler:</b> doğrulama ve hata dönüşümü yalnızca bu tabandan gelir;
/// <c>MvcOptions.Filters</c>'a hiçbir şey eklenmez. v1 bu yüzden etkilenmez.
/// </para>
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ServiceFilter(typeof(V2HataFiltresi))]
[ServiceFilter(typeof(V2DogrulamaFiltresi))]
[Produces("application/json")]
public abstract class V2ControllerBase : ControllerBase
{
}
