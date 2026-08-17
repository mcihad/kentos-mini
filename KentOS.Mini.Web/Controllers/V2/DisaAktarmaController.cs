using KentOS.Mini.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>
/// Liste çıktıları (Excel / PDF).
/// </summary>
/// <remarks>
/// <b>Sayfalama YOK ve bilinçli:</b> dışa aktarmanın amacı tam listeyi tek
/// dosyada vermek. Bunun yerine <b>süzgeç</b> zorunlu tutulur — tarih aralığı
/// verilmezse tüm geçmiş döner ve iki yıllık veride bu çok büyük bir dosyadır.
/// Süzgeçler, sayfalı liste ucundakilerle aynı adları taşır.
/// </remarks>
[Route("api/v2/disa-aktar")]
// Basın kullanıcısı KENDİ (daraltılmış) listesinin çıktısını alır: çıktı
// servisi de aynı sorgu kapısından geçiyor, yani PDF/Excel yalnızca basına
// açık etkinlikleri içerir.
[Izin(Izinler.AjandaCiktiAl)]
public class DisaAktarmaController(IDisaAktarmaServisi _disaAktar) : V2ControllerBase
{
    /// <summary>Etkinlik listesi — Excel.</summary>
    [HttpGet("etkinlik/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> EtkinlikExcelAsync([FromQuery] DisaAktarmaSuzgeci suzgec)
        => Gonder(await _disaAktar.EtkinlikExcelAsync(suzgec));

    /// <summary>Etkinlik listesi — PDF.</summary>
    [HttpGet("etkinlik/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> EtkinlikPdfAsync([FromQuery] DisaAktarmaSuzgeci suzgec)
        => Gonder(await _disaAktar.EtkinlikPdfAsync(suzgec));

    /// <summary>Talep listesi — Excel.</summary>
    [HttpGet("talep/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> TalepExcelAsync([FromQuery] DisaAktarmaSuzgeci suzgec)
        => Gonder(await _disaAktar.TalepExcelAsync(suzgec));

    /// <summary>Talep listesi — PDF.</summary>
    [HttpGet("talep/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> TalepPdfAsync([FromQuery] DisaAktarmaSuzgeci suzgec)
        => Gonder(await _disaAktar.TalepPdfAsync(suzgec));

    /// <summary>
    /// Günlük program çıktısı (PDF).
    /// </summary>
    /// <param name="tarih">Boşsa bugün.</param>
    /// <param name="tasarim">1 standart · 2 kompakt · 3 detaylı · 4 boş not sayfası.</param>
    /// <remarks>
    /// Eski arayüzdeki dört ayrı yazdırma sayfasının karşılığı. Basılıp masaya
    /// konan bir belge olduğu için gizli etkinlikler sorguda süzülür.
    /// </remarks>
    [HttpGet("gunluk-program")]
    [Produces("application/pdf")]
    public async Task<IActionResult> GunlukProgramAsync(
        [FromQuery] DateTime? tarih = null,
        [FromQuery] ProgramTasarimi tasarim = ProgramTasarimi.Standart)
        => Gonder(await _disaAktar.GunlukProgramAsync(tarih ?? DateTime.Today, tasarim));

    /// <summary>
    /// Günlük programın yazdırılabilir HTML çıktısı.
    /// </summary>
    /// <remarks>
    /// PDF ile aynı veriyi kullanır ama tarayıcıda önizlenip yazıcı
    /// ayarlarıyla basılır. Tek dosya: dış CSS/JS/font yok — yazıcı
    /// bilgisayarında internet olmayabilir.
    /// </remarks>
    [HttpGet("gunluk-program/html")]
    [Produces("text/html")]
    public async Task<IActionResult> GunlukProgramHtmlAsync(
        [FromQuery] DateTime? tarih = null,
        [FromQuery] ProgramTasarimi tasarim = ProgramTasarimi.Standart)
        => Content(
            await _disaAktar.GunlukProgramHtmlAsync(tarih ?? DateTime.Today, tasarim),
            "text/html; charset=utf-8");

    private FileContentResult Gonder(DisaAktarmaDosyasi d)
        => File(d.Icerik, d.IcerikTuru, d.DosyaAdi);
}
