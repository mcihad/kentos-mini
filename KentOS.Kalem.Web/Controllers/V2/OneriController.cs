using KentOS.Kalem.Web.AuthPolicies;
using KentOS.Kalem.Application.Identity;
using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Application.Dto.V2.Ortak;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Services;

namespace KentOS.Kalem.Web.Controllers.V2;

/// <summary>Öneri ve şikâyetler.</summary>
[Route("api/v2/oneri")]
[Izin(Izinler.OneriGoruntule)]
public class OneriController(
    IOneriService _oneriService,
    ICurrentUserService _mevcutKullanici) : V2ControllerBase
{
    /// <summary>Tüm öneriler.</summary>
    [HttpGet]
    [ProducesResponseType<SayfaliSonuc<OneriDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<OneriDto>> ListeAsync([FromQuery] SayfaIstegi sayfa)
        => Sayfala(await _oneriService.GetAllAsync(), sayfa);

    /// <summary>Cevap bekleyenler.</summary>
    [HttpGet("bekleyen")]
    [ProducesResponseType<SayfaliSonuc<OneriDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<OneriDto>> BekleyenAsync([FromQuery] SayfaIstegi sayfa)
        => Sayfala(await _oneriService.GetWaitingOnerilerAsync(), sayfa);

    /// <summary>
    /// Oturum açan kullanıcının kendi önerileri.
    /// </summary>
    /// <remarks>
    /// v1'de kullanıcı kimliği <b>rotadan</b> geliyordu (<c>/User/{userId}</c>),
    /// yani herkes başkasının önerilerini okuyabiliyordu. v2'de kimlik yalnızca
    /// oturumdan alınır; istemci kendi kimliğini gönderemez.
    /// </para>
    /// <para>
    /// Ayrıca boş sonuç <b>200 + boş dizi</b> döner. v1'in aynı yolu, kaydı
    /// olmayan kullanıcıya 404 veriyordu — istemci tarafında "hata" gibi
    /// görünüyordu.
    /// </remarks>
    /// <param name="tip">Boşsa hepsi.</param>
    [HttpGet("benim")]
    [ProducesResponseType<SayfaliSonuc<OneriDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> BenimAsync(
        [FromQuery] SayfaIstegi sayfa, [FromQuery] OneriTip? tip = null)
    {
        var kullaniciId = await _mevcutKullanici.GetUserIdAsync();
        if (kullaniciId is null) return Forbid();

        var liste = tip is null
            ? await _oneriService.KullaniciOnerileriAsync(kullaniciId.Value)
            : await _oneriService.GetOnerilerByUserIdAsync(kullaniciId.Value, tip.Value);

        return Ok(Sayfala(liste, sayfa));
    }

    /// <summary>Oturum açan kullanıcının belirli tarih aralığındaki önerileri.</summary>
    [HttpGet("benim/aralik")]
    [ProducesResponseType<SayfaliSonuc<OneriDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> BenimAralikAsync(
        [FromQuery] DateTime baslangic,
        [FromQuery] DateTime bitis,
        [FromQuery] SayfaIstegi sayfa)
    {
        var kullaniciId = await _mevcutKullanici.GetUserIdAsync();
        if (kullaniciId is null) return Forbid();

        return Ok(Sayfala(
            await _oneriService.GetOnerilerByUserIdAsync(kullaniciId.Value, baslangic, bitis),
            sayfa));
    }

    /// <summary>Öneri detayı.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<OneriDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DetayAsync(long id)
    {
        var oneri = await _oneriService.GetAsync(id);
        return oneri is null ? NotFound() : Ok(oneri);
    }

    /// <summary>Yeni öneri.</summary>
    [Izin(Izinler.OneriGoruntule)]
    [HttpPost]
    [ProducesResponseType<OneriDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> OlusturAsync([FromBody] OneriDto istek)
        => Ok(await _oneriService.CreateAsync(istek));

    /// <summary>Öneriyi günceller.</summary>
    [Izin(Izinler.OneriGoruntule)]
    [HttpPut("{id:long}")]
    [ProducesResponseType<OneriDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GuncelleAsync(long id, [FromBody] OneriDto istek)
    {
        istek.Id = id;
        return Ok(await _oneriService.UpdateAsync(istek));
    }

    /// <summary>Öneriyi cevaplar.</summary>
    [Izin(Izinler.OneriYanitla)]
    [HttpPost("{id:long}/cevap")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CevaplaAsync(long id, [FromBody] OneriCevapDto istek)
        => Ok(await _oneriService.AnswerAsync(id, istek));

    /// <summary>Öneriyi siler.</summary>
    [Izin(Izinler.OneriGoruntule)]
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SilAsync(long id)
    {
        await _oneriService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Öneri listelerini arar ve sayfalar.
    /// </summary>
    /// <remarks>
    /// <c>IOneriService</c> <c>IEnumerable</c> döndürüyor; sayfalama bellekte
    /// yapılır. Öneri sayısı birkaç yüzü geçmiyor, veritabanı düzeyine
    /// taşımak için servis imzasını değiştirmek gerekirdi.
    /// </remarks>
    private static SayfaliSonuc<OneriDto> Sayfala(IEnumerable<OneriDto> kaynak, SayfaIstegi sayfa)
    {
        var ara = sayfa.TemizArama;
        var liste = kaynak.ToList();

        if (ara is not null)
        {
            liste = liste.Where(o =>
                (o.Baslik?.Contains(ara, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (o.Aciklama?.Contains(ara, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (o.KullaniciAdi?.Contains(ara, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        // En yeni önce.
        liste = liste.OrderByDescending(o => o.Tarih).ToList();
        return SayfaliSonuc<OneriDto>.Bellekten(liste, sayfa);
    }
}
