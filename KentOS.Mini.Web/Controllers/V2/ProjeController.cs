using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto.V2.IsTakip;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>
/// PROJE — görevlerin çatısı, kanban panosu ve gantt çizelgesi.
/// </summary>
/// <remarks>
/// <para>
/// Görev uçlarıyla aynı düzen: <c>X-Etkin-Birim</c> başlığına saygılı,
/// görünürlük kapısı birim, kapsam dışı kayıt <c>404</c>.
/// </para>
/// <para>
/// <b>Kart taşıma <c>proje.yonet</c> istiyor</b> ve bu bilinçli: sürükleme
/// görevin DURUMUNU değiştiriyor. Yalnızca görüntüleme izniyle açılsaydı,
/// panoya bakabilen herkes görev akışını yürütebilirdi.
/// </para>
/// </remarks>
[Route("api/v2/proje")]
public class ProjeController(IProjeServisi _servis) : V2ControllerBase
{
    [HttpGet]
    [Izin(Izinler.ProjeGoruntule)]
    [ProducesResponseType<SayfaliSonuc<ProjeOzetDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<ProjeOzetDto>> ListeAsync(
        [FromQuery] SayfaIstegi sayfa,
        [FromQuery] bool altBirimlerDahil,
        [FromQuery] bool yalnizAcik,
        [FromQuery] long? yoneticiId,
        [FromQuery] List<ProjeDurumu>? durumlar,
        CancellationToken iptal) =>
        _servis.ListeAsync(new ProjeSuzgecDto
        {
            Sayfa = sayfa.Sayfa,
            Boyut = sayfa.Boyut,
            Ara = sayfa.Ara,
            Sirala = sayfa.Sirala,
            Azalan = sayfa.Azalan,
            AltBirimlerDahil = altBirimlerDahil,
            YalnizAcik = yalnizAcik,
            YoneticiId = yoneticiId,
            Durumlar = durumlar,
        }, iptal);

    [HttpGet("{id:long}")]
    [Izin(Izinler.ProjeGoruntule)]
    [ProducesResponseType<ProjeDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ProjeDetayDto> GetirAsync(long id, CancellationToken iptal) =>
        _servis.GetirAsync(id, iptal);

    [HttpPost]
    [Izin(Izinler.ProjeYonet)]
    [ProducesResponseType<ProjeDetayDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjeDetayDto>> OlusturAsync(
        [FromBody] ProjeKayitDto istek, CancellationToken iptal)
    {
        var proje = await _servis.OlusturAsync(istek, iptal);

        // Adres ELLE yazılıyor — MVC eylem adından `Async` ekini düşürdüğü
        // için `nameof(GetirAsync)` hiçbir rotayla eşleşmiyor ve uç 500
        // dönüyordu. Gerekçenin tamamı `GorevTipiController`de yazılı.
        return Created($"/api/v2/proje/{proje.Id}", proje);
    }

    [HttpPut("{id:long}")]
    [Izin(Izinler.ProjeYonet)]
    [ProducesResponseType<ProjeDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ProjeDetayDto> GuncelleAsync(
        long id, [FromBody] ProjeKayitDto istek, CancellationToken iptal) =>
        _servis.GuncelleAsync(id, istek, iptal);

    /// <summary>
    /// Projeyi siler — GÖREVLERİ SİLMEZ, bağlarını boşaltır.
    /// </summary>
    /// <remarks>
    /// Proje bir çatı, işin sahibi değil. Cascade kursaydık bir projeyi
    /// silmek altındaki bütün işi ve kanıtını da götürürdü.
    /// </remarks>
    [HttpDelete("{id:long}")]
    [Izin(Izinler.ProjeYonet)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SilAsync(long id, CancellationToken iptal)
    {
        await _servis.SilAsync(id, iptal);
        return NoContent();
    }

    // ── ekip ───────────────────────────────────────────────────────────

    /// <summary>
    /// Proje ekibini yazar — TAM LİSTE.
    /// </summary>
    /// <remarks>
    /// Ayrı izin (<c>proje.uyeYonet</c>): ekibi düzenlemek ile projenin
    /// tarihini ve bütçesini değiştirmek farklı ağırlıkta işler. Proje
    /// yöneticisine ekibini kurma yetkisi verirken bütçeyi de açmak zorunda
    /// kalmamak gerekiyor.
    /// </remarks>
    [HttpPut("{id:long}/uye")]
    [Izin(Izinler.ProjeUyeYonet, Izinler.ProjeYonet)]
    [ProducesResponseType<ProjeDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ProjeDetayDto> UyeleriYazAsync(
        long id, [FromBody] ProjeEkibiIstegiDto istek, CancellationToken iptal) =>
        _servis.UyeleriYazAsync(id, istek.Uyeler, istek.YoneticiId, iptal);

    // ── kanban ─────────────────────────────────────────────────────────

    [HttpGet("{id:long}/pano")]
    [Izin(Izinler.ProjeGoruntule)]
    [ProducesResponseType<PanoDto>(StatusCodes.Status200OK)]
    public Task<PanoDto> PanoAsync(long id, CancellationToken iptal) =>
        _servis.PanoAsync(id, iptal);

    /// <summary>
    /// Kartı başka sütuna taşır — yani görevin DURUMUNU değiştirir.
    /// </summary>
    /// <remarks>
    /// Geçiş durum akışından geçiyor; panoyu akışın dışında tutsaydık kartı
    /// sürükleyerek onay kapısını atlamak mümkün olurdu.
    /// </remarks>
    [HttpPost("{id:long}/pano/tasi")]
    [Izin(Izinler.ProjeYonet)]
    [ProducesResponseType<PanoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<PanoDto> KartTasiAsync(
        long id, [FromBody] KartTasimaDto istek, CancellationToken iptal) =>
        _servis.KartTasiAsync(id, istek, iptal);

    // ── gantt ──────────────────────────────────────────────────────────

    [HttpGet("{id:long}/gantt")]
    [Izin(Izinler.ProjeGoruntule)]
    [ProducesResponseType<List<GanttSatiriDto>>(StatusCodes.Status200OK)]
    public Task<List<GanttSatiriDto>> GanttAsync(long id, CancellationToken iptal) =>
        _servis.GanttAsync(id, iptal);

    // ── kilometre taşı ─────────────────────────────────────────────────

    /// <summary>Projeye tek bir kilometre taşı ekler.</summary>
    /// <remarks>
    /// <b>Neden ayrı bir uç.</b> Ara hedef eklemenin tek yolu projenin
    /// TAMAMINI düzenleme formuna girmekti: bütçe, tarih, ekip ve pano
    /// sütunlarıyla açılan bir form, tek satırlık bir iş için fazla. Üstelik
    /// o formu kaydetmek projenin geri kalanını da yeniden yazıyor, yani
    /// yalnızca hedef eklemek isteyen kişi farkında olmadan başka alanları
    /// da kaydetmiş oluyordu.
    /// </remarks>
    [HttpPost("{id:long}/kilometre-tasi")]
    [Izin(Izinler.ProjeYonet)]
    [ProducesResponseType<KilometreTasiDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<KilometreTasiDto> KilometreTasiEkleAsync(
        long id, [FromBody] KilometreTasiDto istek, CancellationToken iptal) =>
        _servis.KilometreTasiEkleAsync(id, istek, iptal);

    /// <summary>Kilometre taşını siler; bağlı görevlerin yalnızca bağı kopar.</summary>
    [HttpDelete("{id:long}/kilometre-tasi/{tasId:long}")]
    [Izin(Izinler.ProjeYonet)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> KilometreTasiSilAsync(
        long id, long tasId, CancellationToken iptal)
    {
        await _servis.KilometreTasiSilAsync(id, tasId, iptal);
        return NoContent();
    }

    /// <summary>Kilometre taşını tamamlar ya da yeniden açar.</summary>
    /// <remarks>
    /// Tamamlanma ELLE işaretleniyor. "Bağlı görevlerin hepsi bitince
    /// kendiliğinden" denebilirdi ama hiç görev bağlanmamış bir taş açılır
    /// açılmaz tamamlanmış görünürdü.
    /// </remarks>
    [HttpPost("{id:long}/kilometre-tasi/{tasId:long}")]
    [Izin(Izinler.ProjeYonet)]
    [ProducesResponseType<KilometreTasiDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<KilometreTasiDto> KilometreTasiAsync(
        long id, long tasId, [FromQuery] bool tamamlandi, CancellationToken iptal) =>
        _servis.KilometreTasiTamamlaAsync(id, tasId, tamamlandi, iptal);
}

/// <summary>Proje ekibi yazma isteği.</summary>
public class ProjeEkibiIstegiDto
{
    [System.Text.Json.Serialization.JsonPropertyName("yoneticiId")]
    public long? YoneticiId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("uyeler")]
    public List<ProjeUyeIstegiDto> Uyeler { get; set; } = [];
}
