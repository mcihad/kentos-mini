using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Web.AuthPolicies;
using KentOS.Kalem.Application.Dto.Analiz;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Web.Controllers.V2;

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
    ITalepIstatistikServisi _talepIstatistik,
    IIstatistikMerkeziServisi _merkez,
    IIstatistikCiktiServisi _cikti) : V2ControllerBase
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

    /*
      ─────────────── KONU PANOLARI ───────────────

      Hepsi aynı şekli döndürüyor (`KonuIstatistigiDto`), yani istemcide tek
      bir çizici var.

      İZİN İKİ KATLI ve bilinçli: sınıf düzeyindeki `istatistik.goruntule`
      MERKEZİ açıyor, metot düzeyindeki modül izni o kartı açıyor. Yalnızca
      modül iznine bakılsaydı, istatistik yetkisi olmayan bir kullanıcı
      merkezdeki kartlardan bazılarını görürdü; yalnızca merkez iznine
      bakılsaydı halk gününü hiç görmeyen biri halk günü sayılarını okurdu —
      sayı da bir bilgidir.
    */

    /// <summary>Halk günü panosu.</summary>
    [HttpGet("halk-gunu")]
    [Izin(Izinler.HalkgunuGoruntule)]
    [ProducesResponseType<KonuIstatistigiDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> HalkGunuAsync(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => Ok(await _merkez.HalkGunuAsync(baslangic, bitis));

    /// <summary>Form ve anket panosu.</summary>
    [HttpGet("form")]
    [Izin(Izinler.FormGoruntule)]
    [ProducesResponseType<KonuIstatistigiDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> FormAsync(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => Ok(await _merkez.FormAsync(baslangic, bitis));

    /// <summary>Protokol ve davet panosu.</summary>
    [HttpGet("protokol")]
    [Izin(Izinler.ProtokolGoruntule)]
    [ProducesResponseType<KonuIstatistigiDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ProtokolAsync(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => Ok(await _merkez.ProtokolAsync(baslangic, bitis));

    /// <summary>Çiçek gönderi panosu.</summary>
    [HttpGet("cicek")]
    [Izin(Izinler.CicekGoruntule)]
    [ProducesResponseType<KonuIstatistigiDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CicekAsync(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => Ok(await _merkez.CicekAsync(baslangic, bitis));

    /// <summary>Özgeçmiş havuzu panosu.</summary>
    [HttpGet("ozgecmis")]
    [Izin(Izinler.OzgecmisGoruntule)]
    [ProducesResponseType<KonuIstatistigiDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> OzgecmisAsync(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => Ok(await _merkez.OzgecmisAsync(baslangic, bitis));

    /// <summary>
    /// Sistem sağlığı panosu — YALNIZCA <c>Sistem</c>.
    /// </summary>
    /// <remarks>
    /// Hata ekranının kapısıyla aynı (<c>sistem.hata</c>): Admin bile
    /// göremez. Pano yığın izi ya da istek gövdesi döndürmüyor ama "hangi uç
    /// patlıyor" bilgisi de saldırı yüzeyini tarif ediyor.
    /// </remarks>
    [HttpGet("sistem")]
    [Izin(Izinler.SistemHata)]
    [ProducesResponseType<KonuIstatistigiDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SistemAsync(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => Ok(await _merkez.SistemAsync(baslangic, bitis));

    /*
      PANO ÇIKTISI — kayıt listesi değil SAYILAR.

      Ay sonu raporuna yapıştırılacak olan bu. Kayıtların kendisini isteyen
      modülün kendi çıktı ucunu kullanır (`disa-aktar/*`, çiçekçi dosyası,
      halk günü çizelgesi). İzin kapısı panonun kendisiyle AYNI: sayıyı
      okuyamayan onu indiremez de.
    */

    /// <summary>halk-gunu panosunun Excel çıktısı.</summary>
    [HttpGet("halk-gunu/excel")]
    [Izin(Izinler.HalkgunuGoruntule)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> HalkGunuExcelAsync(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => Gonder(_cikti.Excel(
            await _merkez.HalkGunuAsync(baslangic, bitis), baslangic, bitis));

    /// <summary>form panosunun Excel çıktısı.</summary>
    [HttpGet("form/excel")]
    [Izin(Izinler.FormGoruntule)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> FormExcelAsync(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => Gonder(_cikti.Excel(
            await _merkez.FormAsync(baslangic, bitis), baslangic, bitis));

    /// <summary>protokol panosunun Excel çıktısı.</summary>
    [HttpGet("protokol/excel")]
    [Izin(Izinler.ProtokolGoruntule)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ProtokolExcelAsync(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => Gonder(_cikti.Excel(
            await _merkez.ProtokolAsync(baslangic, bitis), baslangic, bitis));

    /// <summary>cicek panosunun Excel çıktısı.</summary>
    [HttpGet("cicek/excel")]
    [Izin(Izinler.CicekGoruntule)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> CicekExcelAsync(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => Gonder(_cikti.Excel(
            await _merkez.CicekAsync(baslangic, bitis), baslangic, bitis));

    /// <summary>ozgecmis panosunun Excel çıktısı.</summary>
    [HttpGet("ozgecmis/excel")]
    [Izin(Izinler.OzgecmisGoruntule)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> OzgecmisExcelAsync(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => Gonder(_cikti.Excel(
            await _merkez.OzgecmisAsync(baslangic, bitis), baslangic, bitis));

    /// <summary>sistem panosunun Excel çıktısı.</summary>
    [HttpGet("sistem/excel")]
    [Izin(Izinler.SistemHata)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> SistemExcelAsync(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => Gonder(_cikti.Excel(
            await _merkez.SistemAsync(baslangic, bitis), baslangic, bitis));

    private FileContentResult Gonder(DisaAktarmaDosyasi d)
        => File(d.Icerik, d.IcerikTuru, d.DosyaAdi);
}
