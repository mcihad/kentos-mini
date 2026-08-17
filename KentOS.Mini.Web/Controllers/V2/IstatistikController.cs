using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Application.Dto.Analiz;
using KentOS.Mini.Application.Services;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>Ajanda istatistikleri.</summary>
/// <remarks>
/// Hesaplama tamamen <see cref="IAjandaIstatistikService"/> içinde; burada
/// yalnızca aralık normalize edilir. İstatistik sorguları gizlilik süzgecini
/// servis düzeyinde geçer — bu katmanda ek bir filtre YOKTUR, eklenmemelidir
/// (iki yerde süzmek, birinin unutulduğunda sessizce sızmasına yol açar).
/// </remarks>
// İstatistikler etkinlik ve talep verisini özetliyor; eski sistemde de
// ajanda modülünün parçasıydı. Politika olmadan, ajandaya erişemeyen roller
// (Basin, Medya, Cicek…) birim sayılarını okuyabiliyordu.
[Izin(Izinler.IstatistikGoruntule)]
[Route("api/v2/istatistik")]
public class IstatistikController(
    IAjandaIstatistikService _istatistikService,
    ITalepIstatistikServisi _talepIstatistik) : V2ControllerBase
{
    /// <summary>Tüm istatistik panosu tek çağrıda.</summary>
    /// <param name="baslangic">Dahil. Boşsa servisin varsayılanı (içinde bulunulan yıl).</param>
    /// <param name="bitis">Dahil.</param>
    [HttpGet]
    [ProducesResponseType<AjandaIstatistikDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> PanoAsync(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => Ok(await _istatistikService.GetIstatistiklerAsync(baslangic, bitis));

    /// <summary>Talep panosu — mahalle, meslek, tip, durum ve zaman dağılımları.</summary>
    /// <remarks>
    /// Etkinlik panosundan AYRI bir uç: ikisi farklı soruları cevaplıyor
    /// (biri "makamın günü nasıl geçiyor", öteki "vatandaş neyi, nereden, kim
    /// aracılığıyla istiyor") ve tek yanıtta birleştirmek, her iki ekranın da
    /// kullanmadığı alanları indirmesi demekti.
    /// </remarks>
    /// <param name="baslangic">Dahil. Boşsa son 12 ay.</param>
    /// <param name="bitis">Dahil. Boşsa bugün.</param>
    [HttpGet("talep")]
    [ProducesResponseType<TalepIstatistikDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TalepPanoAsync(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => Ok(await _talepIstatistik.PanoAsync(baslangic, bitis));
}
