using KentOS.Mini.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto.V2.Etkinlik;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>Takvim görünümlerinin veri kaynağı.</summary>
[Route("api/v2/takvim")]
// OKUMA İKİ İZİNDEN BİRİYLE.
//
// Basın kullanıcısında `ajanda.goruntule` yok, `ajanda.basinGoruntule` var.
// Sınıf düzeyinde yalnızca tam görüntüleme istendiğinde arayüz ekranı açıyor
// ama HER İSTEK 403 dönüyordu: menü ve rota izne uyuyor, uç uymuyordu.
// Yazma uçları kendi izinlerini ayrıca ilan ediyor, onlar etkilenmez.
// Listeyi daraltan süzgeç `AjandaSorguUzantilari.GorunurOlanlar` içinde.
[Izin(Izinler.AjandaGoruntule, Izinler.AjandaBasinGoruntule)]
public class TakvimController(ITakvimSorguServisi _sorgu) : V2ControllerBase
{
    /// <summary>Verilen tarih aralığındaki etkinlikler (gün/ay/ajanda görünümleri).</summary>
    [HttpPost("aralik")]
    [ProducesResponseType<List<EtkinlikOzetDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> AralikAsync([FromBody] AralikIstegi istek, CancellationToken iptal)
        => Ok(await _sorgu.AralikAsync(istek, iptal));

    /// <summary>Gün başına etkinlik sayısı.</summary>
    /// <remarks>
    /// <c>ay</c> verilmezse tüm yıl döner (SPA'nın yıl görünümü); verilirse
    /// yalnızca o ay — v1'in <c>AjandaApi/CountByDay/{ay}/{yil}</c> ucuyla
    /// aynı küme.
    /// </remarks>
    [HttpGet("sayac")]
    [ProducesResponseType<List<GunSayaciDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SayacAsync(
        [FromQuery] int yil, [FromQuery] int? ay, CancellationToken iptal)
        => Ok(await _sorgu.GunSayaclariAsync(yil == 0 ? DateTime.Now.Year : yil, ay, iptal));
}
