using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto.V2.Form;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>
/// DİNAMİK FORM VE ANKET — yetkili yüzeyi.
/// </summary>
/// <remarks>
/// Vatandaşın gördüğü yüzey ayrı bir controller'da
/// (<see cref="FormPortalController"/>) ve anonim. İkisini aynı sınıfta
/// tutmak, bir <c>[AllowAnonymous]</c>'un yanlış uca düşmesi hâlinde
/// yönetim uçlarını da açardı.
/// </remarks>
[Route("api/v2/form")]
[Izin(Izinler.FormGoruntule)]
public class FormController(
    IFormServisi _servis,
    IFormYanitServisi _yanitServisi,
    IFormCiktiServisi _cikti) : V2ControllerBase
{
    // ── form ───────────────────────────────────────────────────────────

    /// <summary>Birimin formları — sayfalı.</summary>
    [HttpGet]
    [ProducesResponseType<SayfaliSonuc<FormOzetDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<FormOzetDto>> ListeAsync(
        [FromQuery] FormSuzgecDto suzgec, CancellationToken iptal) =>
        _servis.ListeAsync(suzgec, iptal);

    /// <summary>Form detayı — çalışılan tanımıyla.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<FormDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<FormDetayDto> GetirAsync(long id, CancellationToken iptal) =>
        _servis.GetirAsync(id, iptal);

    [Izin(Izinler.FormYonet)]
    [HttpPost]
    [ProducesResponseType<FormDetayDto>(StatusCodes.Status200OK)]
    public Task<FormDetayDto> OlusturAsync(
        [FromBody] FormKayitDto istek, CancellationToken iptal) =>
        _servis.OlusturAsync(istek, iptal);

    [Izin(Izinler.FormYonet)]
    [HttpPut("{id:long}")]
    [ProducesResponseType<FormDetayDto>(StatusCodes.Status200OK)]
    public Task<FormDetayDto> GuncelleAsync(
        long id, [FromBody] FormKayitDto istek, CancellationToken iptal) =>
        _servis.GuncelleAsync(id, istek, iptal);

    /// <summary>
    /// Çalışılan tanımı dondurup vatandaşa açar.
    /// </summary>
    /// <remarks>
    /// <b>Tasarlamak ile yayınlamak ayrı izin.</b> Yayınlanan bağlantı kurum
    /// dışına çıkıyor ve geri alınması zor; formu kuran herkesin onu
    /// yayınlayabilmesi gerekmiyor.
    /// </remarks>
    [Izin(Izinler.FormYayinla)]
    [HttpPost("{id:long}/yayinla")]
    [ProducesResponseType<FormDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<FormDetayDto> YayinlaAsync(long id, CancellationToken iptal) =>
        _servis.YayinlaAsync(id, iptal);

    /// <summary>Yanıt kabulünü açar/kapatır.</summary>
    [Izin(Izinler.FormYayinla)]
    [HttpPost("{id:long}/durum")]
    [ProducesResponseType<FormDetayDto>(StatusCodes.Status200OK)]
    public Task<FormDetayDto> DurumAsync(
        long id, [FromQuery] FormDurumu durum, CancellationToken iptal) =>
        _servis.DurumDegistirAsync(id, durum, iptal);

    [Izin(Izinler.FormYonet)]
    [HttpPost("{id:long}/kopyala")]
    [ProducesResponseType<FormDetayDto>(StatusCodes.Status200OK)]
    public Task<FormDetayDto> KopyalaAsync(long id, CancellationToken iptal) =>
        _servis.KopyalaAsync(id, iptal);

    [Izin(Izinler.FormYonet)]
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SilAsync(long id, CancellationToken iptal)
    {
        await _servis.SilAsync(id, iptal);
        return NoContent();
    }

    // ── yanıtlar ───────────────────────────────────────────────────────

    /// <summary>Gelen yanıtlar — sayfalı, JSONB süzgeciyle.</summary>
    [Izin(Izinler.FormYanitGoruntule)]
    [HttpGet("{id:long}/yanit")]
    [ProducesResponseType<SayfaliSonuc<FormYanitOzetDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<FormYanitOzetDto>> YanitlarAsync(
        long id, [FromQuery] FormYanitSuzgecDto suzgec, CancellationToken iptal) =>
        _yanitServisi.ListeAsync(id, suzgec, iptal);

    /// <summary>Tek yanıtın detayı — verildiği SÜRÜMÜN tanımıyla.</summary>
    [Izin(Izinler.FormYanitGoruntule)]
    [HttpGet("{id:long}/yanit/{yanitId:long}")]
    [ProducesResponseType<FormYanitDetayDto>(StatusCodes.Status200OK)]
    public Task<FormYanitDetayDto> YanitAsync(
        long id, long yanitId, CancellationToken iptal) =>
        _yanitServisi.GetirAsync(id, yanitId, iptal);

    /// <summary>
    /// Yanıta eklenen dosyayı indirir — KİMLİK DENETİMLİ.
    /// </summary>
    /// <remarks>
    /// Dosyalar gizli alanda; statik bir yol yok. Vatandaşın yüklediği belge
    /// kimlik fotokopisi olabiliyor ve <c>wwwroot/uploads</c> kimlik
    /// doğrulanmadan servis ediliyor.
    /// </remarks>
    [Izin(Izinler.FormYanitGoruntule)]
    [HttpGet("{id:long}/yanit/{yanitId:long}/dosya/{dosyaId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DosyaAsync(
        long id, long yanitId, long dosyaId, CancellationToken iptal)
    {
        var (akis, ad, tip) = await _yanitServisi.DosyaIndirAsync(id, yanitId, dosyaId, iptal);
        return File(akis, tip, ad);
    }

    /// <summary>Yanıt dağılımları — grafikler bu uçtan besleniyor.</summary>
    [Izin(Izinler.FormYanitGoruntule)]
    [HttpGet("{id:long}/ozet")]
    [ProducesResponseType<FormOzetRaporuDto>(StatusCodes.Status200OK)]
    public Task<FormOzetRaporuDto> OzetAsync(long id, CancellationToken iptal) =>
        _yanitServisi.OzetAsync(id, iptal);

    /// <summary>Yanıtı geçersiz sayar (kayıt silinmez).</summary>
    [Izin(Izinler.FormYanitSil)]
    [HttpDelete("{id:long}/yanit/{yanitId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> YanitSilAsync(long id, long yanitId, CancellationToken iptal)
    {
        await _yanitServisi.SilAsync(id, yanitId, iptal);
        return NoContent();
    }

    /// <summary>
    /// Yanıtları Excel olarak indirir — sütunlar TANIMDAN türetilir.
    /// </summary>
    [Izin(Izinler.FormCiktiAl)]
    [HttpGet("{id:long}/excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExcelAsync(long id, CancellationToken iptal)
    {
        var (icerik, ad) = await _cikti.ExcelAsync(id, iptal);

        return File(icerik,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ad);
    }
}
