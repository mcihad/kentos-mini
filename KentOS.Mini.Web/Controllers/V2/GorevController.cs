using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto.V2.IsTakip;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>
/// GÖREV — iş takibinin ana ucu.
/// </summary>
/// <remarks>
/// <para>
/// Bütün uçlar <c>X-Etkin-Birim</c> başlığına saygılı: başkan yardımcısı
/// bağlı bir müdürlüğü seçtiğinde bu uçlar o müdürlüğün işlerini gösterir.
/// Başlık her istekte YENİDEN doğrulanıyor; istemciden gelen hiçbir şey
/// yetki yerine geçmiyor.
/// </para>
/// <para>
/// <b>Aşama tamamlama ayrı bir izin</b> (<c>gorev.asama</c>): saha personeli
/// kendi işini ilerletebilmeli ama görev açıp silememeli.
/// </para>
/// </remarks>
[Route("api/v2/gorev")]
public class GorevController(
    IGorevServisi _servis,
    IIsEkServisi _ekler,
    IIsYorumServisi _yorumlar) : V2ControllerBase
{
    // ── liste ve detay ─────────────────────────────────────────────────

    [HttpGet]
    [Izin(Izinler.GorevGoruntule)]
    [ProducesResponseType<SayfaliSonuc<GorevOzetDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<GorevOzetDto>> ListeAsync(
        [FromQuery] GorevSuzgecDto suzgec, CancellationToken iptal) =>
        _servis.ListeAsync(suzgec, iptal);

    [HttpGet("{id:long}")]
    [Izin(Izinler.GorevGoruntule)]
    [ProducesResponseType<GorevDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<GorevDetayDto> GetirAsync(long id, CancellationToken iptal) =>
        _servis.GetirAsync(id, iptal);

    /// <summary>Zaman çizelgesi — en yeni önce.</summary>
    [HttpGet("{id:long}/olaylar")]
    [Izin(Izinler.GorevGoruntule)]
    [ProducesResponseType<List<IsOlayDto>>(StatusCodes.Status200OK)]
    public Task<List<IsOlayDto>> OlaylarAsync(long id, CancellationToken iptal) =>
        _servis.OlaylarAsync(id, iptal);

    // ── yazma ──────────────────────────────────────────────────────────

    [HttpPost]
    [Izin(Izinler.GorevEkle)]
    [ProducesResponseType<GorevDetayDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GorevDetayDto>> OlusturAsync(
        [FromBody] GorevKayitDto istek, CancellationToken iptal)
    {
        var gorev = await _servis.OlusturAsync(istek, iptal);

        // Adres ELLE yazılıyor — MVC eylem adından `Async` ekini düşürdüğü
        // için `nameof(GetirAsync)` hiçbir rotayla eşleşmiyor ve uç 500
        // dönüyordu. Gerekçenin tamamı `GorevTipiController`de yazılı.
        return Created($"/api/v2/gorev/{gorev.Id}", gorev);
    }

    [HttpPut("{id:long}")]
    [Izin(Izinler.GorevDuzenle)]
    [ProducesResponseType<GorevDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<GorevDetayDto> GuncelleAsync(
        long id, [FromBody] GorevKayitDto istek, CancellationToken iptal) =>
        _servis.GuncelleAsync(id, istek, iptal);

    /// <summary>
    /// Görevi ve alt görevlerini TÜMÜYLE siler.
    /// </summary>
    /// <remarks>
    /// Dosyalar, yorumlar ve zaman çizelgesi geri gelmez. Yapılmayacak bir işi
    /// kapatmak için silmek değil <b>iptal</b> kullanılır — iptal kaydı ve
    /// gerekçesi durur.
    /// </remarks>
    [HttpDelete("{id:long}")]
    [Izin(Izinler.GorevSil)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SilAsync(long id, CancellationToken iptal)
    {
        await _servis.SilAsync(id, iptal);
        return NoContent();
    }

    // ── akış ───────────────────────────────────────────────────────────

    /// <summary>Atamaları TAM LİSTE olarak yazar; yeni atananlara bildirir.</summary>
    [HttpPut("{id:long}/atama")]
    [Izin(Izinler.GorevAtama)]
    [ProducesResponseType<GorevDetayDto>(StatusCodes.Status200OK)]
    public Task<GorevDetayDto> AtaAsync(
        long id, [FromBody] List<GorevAtamaIstegiDto> atamalar, CancellationToken iptal) =>
        _servis.AtaAsync(id, atamalar, iptal);

    /// <summary>
    /// Durum değiştirir — başlatma, beklemeye alma, iptal.
    /// </summary>
    /// <remarks>
    /// ONAY ve İADE bu uçtan GEÇMEZ: ikisi ayrı izne bağlı
    /// (<c>gorev.onayla</c>). Aynı uçtan yapılsaydı, görevi düzenleyebilen
    /// herkes kendi işini onaylayabilirdi ve onay kapısı anlamını yitirirdi.
    /// </remarks>
    [HttpPut("{id:long}/durum")]
    [Izin(Izinler.GorevDuzenle)]
    [ProducesResponseType<GorevDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<GorevDetayDto> DurumAsync(
        long id, [FromBody] GorevDurumIstegiDto istek, CancellationToken iptal)
    {
        if (istek.Durum is GorevDurumu.Tamamlandi or GorevDurumu.IadeEdildi)
        {
            throw new Exceptions.BusinessRuleException(
                "Onay ve iade bu uçtan yapılmaz; /onay ucunu kullanın.");
        }

        return _servis.DurumDegistirAsync(id, istek, iptal);
    }

    /// <summary>
    /// Tamamlanma beyanı — personelin "bitirdim" düğmesi.
    /// </summary>
    /// <remarks>
    /// Zorunlu aşamalar bitmeden çalışmaz ve görevi TAMAMLAMAZ: yalnızca
    /// onaya gönderir. Kabul yöneticinin işi.
    /// </remarks>
    [HttpPost("{id:long}/tamamla")]
    [Izin(Izinler.GorevAsama, Izinler.GorevDuzenle)]
    [ProducesResponseType<GorevDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<GorevDetayDto> TamamlanmayaGonderAsync(long id, CancellationToken iptal) =>
        _servis.DurumDegistirAsync(id,
            new GorevDurumIstegiDto { Durum = GorevDurumu.TamamlanmaBekliyor }, iptal);

    /// <summary>
    /// ONAY KAPISI — yönetici görevi kabul eder ya da gerekçeyle iade eder.
    /// </summary>
    /// <remarks>
    /// Modülün en önemli tek kuralı: personelin "bitirdim" beyanı ile kurumun
    /// kabulü aynı şey değil. Bu uç olmadan hiçbir görev tamamlanmış sayılmaz.
    /// </remarks>
    [HttpPost("{id:long}/onay")]
    [Izin(Izinler.GorevOnayla)]
    [ProducesResponseType<GorevDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<GorevDetayDto> OnayAsync(
        long id, [FromBody] GorevDurumIstegiDto istek, CancellationToken iptal)
    {
        if (istek.Durum is not (GorevDurumu.Tamamlandi or GorevDurumu.IadeEdildi))
        {
            throw new Exceptions.BusinessRuleException(
                "Onay ucundan yalnızca ONAY ya da İADE yapılır.");
        }

        return _servis.DurumDegistirAsync(id, istek, iptal);
    }

    /// <summary>Sıradaki aşamayı tamamlar ya da (zorunlu değilse) atlar.</summary>
    [HttpPost("{id:long}/asama/{asamaId:long}")]
    [Izin(Izinler.GorevAsama)]
    [ProducesResponseType<GorevDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<GorevDetayDto> AsamaAsync(
        long id, long asamaId, [FromBody] GorevAsamaIstegiDto istek, CancellationToken iptal) =>
        _servis.AsamaTamamlaAsync(id, asamaId, istek, iptal);

    // ── ekler ──────────────────────────────────────────────────────────

    [HttpGet("{id:long}/ek")]
    [Izin(Izinler.GorevGoruntule)]
    [ProducesResponseType<List<IsEkDto>>(StatusCodes.Status200OK)]
    public async Task<List<IsEkDto>> EklerAsync(long id, CancellationToken iptal)
    {
        // Görünürlük kapısı ÖNCE: ek servisi çok biçimli ve varlığın kime ait
        // olduğunu bilmiyor. Bu çağrı olmadan kapsam dışı bir görevin
        // dosyaları listelenebilirdi.
        await _servis.GetirAsync(id, iptal);
        return await _ekler.ListeAsync(IsVarligi.Gorev, id, iptal);
    }

    [HttpPost("{id:long}/ek")]
    [Izin(Izinler.GorevDuzenle, Izinler.GorevAsama)]
    [ProducesResponseType<IsEkDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IsEkDto> EkYukleAsync(
        long id, IFormFile dosya, [FromForm] string? aciklama, CancellationToken iptal) =>
        EkYazAsync(IsVarligi.Gorev, id, id, dosya, aciklama, iptal);

    /// <summary>Aşama kanıtı — fotoğraf zorunlu aşamalar bunu bekliyor.</summary>
    [HttpPost("{id:long}/asama/{asamaId:long}/ek")]
    [Izin(Izinler.GorevAsama)]
    [ProducesResponseType<IsEkDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IsEkDto> AsamaEkYukleAsync(
        long id, long asamaId, IFormFile dosya, [FromForm] string? aciklama,
        CancellationToken iptal) =>
        EkYazAsync(IsVarligi.GorevAsama, asamaId, id, dosya, aciklama, iptal);

    [HttpGet("ek/{ekId:long}")]
    [Izin(Izinler.GorevGoruntule)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EkIndirAsync(long ekId, CancellationToken iptal)
    {
        var (icerik, ad, tur) = await _ekler.IcerikAsync(ekId, iptal);
        return File(icerik, tur, ad);
    }

    [HttpDelete("ek/{ekId:long}")]
    [Izin(Izinler.GorevDuzenle, Izinler.GorevAsama)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EkSilAsync(long ekId, CancellationToken iptal)
    {
        await _ekler.SilAsync(ekId, iptal);
        return NoContent();
    }

    // ── yorumlar ───────────────────────────────────────────────────────

    [HttpGet("{id:long}/yorum")]
    [Izin(Izinler.GorevGoruntule)]
    [ProducesResponseType<List<IsYorumDto>>(StatusCodes.Status200OK)]
    public async Task<List<IsYorumDto>> YorumlarAsync(long id, CancellationToken iptal)
    {
        await _servis.GetirAsync(id, iptal);
        return await _yorumlar.AgacAsync(IsVarligi.Gorev, id, iptal);
    }

    [HttpPost("{id:long}/yorum")]
    [Izin(Izinler.GorevGoruntule)]
    [ProducesResponseType<IsYorumDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IsYorumDto> YorumEkleAsync(
        long id, [FromBody] GorevYorumIstegiDto istek, CancellationToken iptal)
    {
        await _servis.GetirAsync(id, iptal);
        return await _yorumlar.EkleAsync(IsVarligi.Gorev, id, istek.Metin, istek.UstYorumId, iptal);
    }

    [HttpPut("yorum/{yorumId:long}")]
    [Izin(Izinler.GorevGoruntule)]
    [ProducesResponseType<IsYorumDto>(StatusCodes.Status200OK)]
    public Task<IsYorumDto> YorumDuzenleAsync(
        long yorumId, [FromBody] GorevYorumIstegiDto istek, CancellationToken iptal) =>
        _yorumlar.DuzenleAsync(yorumId, istek.Metin, iptal);

    [HttpDelete("yorum/{yorumId:long}")]
    [Izin(Izinler.GorevGoruntule)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> YorumSilAsync(long yorumId, CancellationToken iptal)
    {
        await _yorumlar.SilAsync(yorumId, iptal);
        return NoContent();
    }

    // ── iç ─────────────────────────────────────────────────────────────

    /// <summary>Yüklemeyi görünürlük kapısından geçirip ek servisine devreder.</summary>
    private async Task<IsEkDto> EkYazAsync(
        IsVarligi tur, long varlikId, long gorevId, IFormFile dosya, string? aciklama,
        CancellationToken iptal)
    {
        if (dosya is null || dosya.Length == 0)
            throw new Exceptions.BusinessRuleException("Dosya boş.");

        // Aşama eki de görevin kapısından geçiyor: aşama kimliği tek başına
        // hangi göreve ait olduğunu söylemiyor ve doğrulanmadan kabul
        // edilseydi, başka birimin görevine kanıt yüklenebilirdi.
        var detay = await _servis.GetirAsync(gorevId, iptal);

        if (tur == IsVarligi.GorevAsama && detay.Asamalar.All(a => a.Id != varlikId))
            throw new Exceptions.EntityNotFoundException("Aşama bulunamadı.");

        using var akis = dosya.OpenReadStream();
        using var bellek = new MemoryStream();
        await akis.CopyToAsync(bellek, iptal);

        return await _ekler.EkleAsync(tur, varlikId,
            new IsYuklenenDosya(dosya.FileName, dosya.ContentType, bellek.ToArray()),
            aciklama, iptal);
    }
}

/// <summary>Yorum yazma/düzenleme isteği.</summary>
public class GorevYorumIstegiDto
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Yorum metni zorunlu.")]
    [System.ComponentModel.DataAnnotations.MaxLength(4000)]
    [System.Text.Json.Serialization.JsonPropertyName("metin")]
    public string Metin { get; set; } = string.Empty;

    /// <summary>Yanıtlanan yorum — iç içe yorumlar için.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("ustYorumId")]
    public long? UstYorumId { get; set; }
}
