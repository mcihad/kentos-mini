using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto.V2.IsTakip;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>
/// VATANDAŞ BİLDİRİMİ KARŞILAMA — personel tarafı.
/// </summary>
/// <remarks>
/// <para>
/// Portal (<see cref="BildirimPortalController"/>) anonim ve yalnızca yazıyor;
/// burası kimlik doğrulamalı ve yalnızca personelin gördüğü taraf. İkisi ayrı
/// controller: aynı sınıfta olsalardı bir <c>[AllowAnonymous]</c>'un yanlış
/// yere kayması bütün kayıtları dışarıya açardı.
/// </para>
/// <para>
/// <b>Görünürlük kapısı BİRİM DEĞİL.</b> Gelen bildirim henüz hiçbir birime
/// ait değil — karşılama personelinin işi zaten onu bir birime yönlendirmek.
/// Kapı bu yüzden izin: <c>bildirim.karsila</c>.
/// </para>
/// </remarks>
/*
  ROTA `api/v2/vatandas-bildirimi`, sade `api/v2/bildirim` DEĞİL.

  O yol BİLDİRİM MERKEZİNE ait (`BildirimController` — kullanıcıya düşen
  uygulama içi bildirimler). Aynı yolu ilan etmek çalışma anında
  `AmbiguousMatchException` üretiyor ve İKİ ucu birden 500'e düşürüyordu;
  hata derlemede değil yalnızca istek atıldığında görünüyor.
*/
[Route("api/v2/vatandas-bildirimi")]
public class BildirimKarsilamaController(
    IVatandasBildirimServisi _servis,
    IIsEkServisi _ekler) : V2ControllerBase
{
    [HttpGet]
    [Izin(Izinler.BildirimKarsila)]
    [ProducesResponseType<SayfaliSonuc<VatandasBildirimiDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<VatandasBildirimiDto>> ListeAsync(
        [FromQuery] SayfaIstegi istek,
        [FromQuery] VatandasBildirimDurumu? durum,
        CancellationToken iptal) =>
        _servis.ListeAsync(istek, durum, iptal);

    [HttpGet("{id:long}")]
    [Izin(Izinler.BildirimKarsila)]
    [ProducesResponseType<VatandasBildirimiDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<VatandasBildirimiDto> GetirAsync(long id, CancellationToken iptal) =>
        _servis.GetirAsync(id, iptal);

    /// <summary>Vatandaşın yüklediği fotoğraflar.</summary>
    [HttpGet("{id:long}/ek")]
    [Izin(Izinler.BildirimKarsila)]
    [ProducesResponseType<List<IsEkDto>>(StatusCodes.Status200OK)]
    public async Task<List<IsEkDto>> EklerAsync(long id, CancellationToken iptal)
    {
        // Görünürlük kapısı ÖNCE: ek servisi çok biçimli ve varlığın kime ait
        // olduğunu bilmiyor.
        await _servis.GetirAsync(id, iptal);
        return await _ekler.ListeAsync(IsVarligi.VatandasBildirimi, id, iptal);
    }

    /// <summary>Vatandaşın yüklediği bir dosyanın içeriği.</summary>
    /// <remarks>
    /// <para>
    /// <b>Neden ayrı bir uç:</b> ek indirme yalnızca
    /// <c>api/v2/gorev/ek/{ekId}</c> üzerinden yapılabiliyordu ve o uç
    /// <c>gorev.goruntule</c> istiyor. Karşılama personelinin görev izni
    /// olmak zorunda değil — sonuç: fotoğrafları LİSTELEYEBİLİYOR ama
    /// açamıyordu. Şikayeti değerlendirmenin en hızlı yolu resme bakmak
    /// olduğu için bu, ekranın asıl işini yapamaması demekti.
    /// </para>
    /// <para>
    /// <b>Ek, bildirime ait mi diye DENETLENİYOR.</b> Kimlik doğrudan
    /// içeriğe çevrilseydi, herhangi bir ek kimliğini deneyen bir kullanıcı
    /// görev ve özgeçmiş eklerini de bu uçtan okurdu — sıra numarası tahmin
    /// etmek zor bir şey değil.
    /// </para>
    /// </remarks>
    [HttpGet("{id:long}/ek/{ekId:long}")]
    [Izin(Izinler.BildirimKarsila)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EkIcerikAsync(long id, long ekId, CancellationToken iptal)
    {
        await _servis.GetirAsync(id, iptal);

        var bildirimEkleri = await _ekler.ListeAsync(IsVarligi.VatandasBildirimi, id, iptal);
        if (bildirimEkleri.All(e => e.Id != ekId)) return NotFound();

        var (icerik, ad, tur) = await _ekler.IcerikAsync(ekId, iptal);
        return File(icerik, tur, ad);
    }

    /// <summary>
    /// Bildirimi bir birime yönlendirir ve GÖREV AÇAR.
    /// </summary>
    /// <remarks>
    /// Bir bildirim bir KEZ yönlendirilir; ikinci çağrı reddediliyor. Aksi
    /// hâlde aynı şikayet için birden çok görev açılırdı.
    /// </remarks>
    [HttpPost("{id:long}/yonlendir")]
    [Izin(Izinler.BildirimYonlendir)]
    [ProducesResponseType<VatandasBildirimiDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<VatandasBildirimiDto> YonlendirAsync(
        long id, [FromBody] BildirimYonlendirmeDto istek, CancellationToken iptal) =>
        _servis.YonlendirAsync(id, istek, iptal);

    /// <summary>Bildirimi işleme almaz — gerekçe ZORUNLU.</summary>
    [HttpPost("{id:long}/reddet")]
    [Izin(Izinler.BildirimYonlendir)]
    [ProducesResponseType<VatandasBildirimiDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<VatandasBildirimiDto> ReddetAsync(
        long id, [FromBody] BildirimRetDto istek, CancellationToken iptal) =>
        _servis.ReddetAsync(id, istek.Not, iptal);
}

/// <summary>
/// SAHA — tespit, harita ve "benim işlerim".
/// </summary>
/// <remarks>
/// Saha ekranları kabuksuz ve tek elle kullanılıyor; uçları da öyle dar
/// tutuldu. Tespit doğrudan görev açıyor — karşılama adımı yok, çünkü tespiti
/// yapan zaten kurumun personeli.
/// </remarks>
[Route("api/v2/saha")]
public class SahaController(ISahaServisi _servis) : V2ControllerBase
{
    /// <summary>Sahada görülen sorunu doğrudan görev olarak açar.</summary>
    [HttpPost("tespit")]
    [Izin(Izinler.SahaTespit, Izinler.GorevEkle)]
    [ProducesResponseType<GorevDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<GorevDetayDto> TespitAsync(
        [FromBody] SahaTespitiDto istek, CancellationToken iptal) =>
        _servis.TespitAsync(istek, iptal);

    /// <summary>Kullanıcının üzerindeki açık görevler — ekip ataması dahil.</summary>
    [HttpGet("islerim")]
    [Izin(Izinler.GorevGoruntule)]
    [ProducesResponseType<List<GorevOzetDto>>(StatusCodes.Status200OK)]
    public Task<List<GorevOzetDto>> IslerimAsync(CancellationToken iptal) =>
        _servis.BenimIslerimAsync(iptal);

    /// <summary>Harita noktaları — görevler ve (istenirse) bekleyen bildirimler.</summary>
    [HttpGet("harita")]
    [Izin(Izinler.GorevGoruntule)]
    [ProducesResponseType<List<IsHaritaNoktasiDto>>(StatusCodes.Status200OK)]
    public Task<List<IsHaritaNoktasiDto>> HaritaAsync(
        [FromQuery] bool altBirimlerDahil,
        [FromQuery] bool bildirimlerDahil,
        [FromQuery] bool yalnizAcik,
        CancellationToken iptal) =>
        _servis.NoktalarAsync(altBirimlerDahil, bildirimlerDahil, yalnizAcik, iptal);
}
