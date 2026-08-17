using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Services;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>
/// Referans verilerin OKUMA uçları (durum, tip, birim, mahalle, meslek, çiçekçi).
///
/// <para>
/// Politika YOK: menüyü ve formları kurabilmek için her oturum sahibinin
/// bunlara erişmesi gerekiyor ve içlerinde kişisel veri bulunmuyor.
/// Yazma işlemleri <c>/api/v2/tanim/*</c> altında ve <c>Admin</c> ister.
/// </para>
///
/// <para>
/// <b>Sayfalama:</b> Küçük listeler (durum, tip, birim, çiçekçi) tek sayfada
/// gelir ama yine de sayfalı zarf içinde döner — istemcinin iki farklı yanıt
/// şekliyle uğraşmaması için. Mahalle ve meslek gerçekten büyük olabildiği
/// için <c>ara</c> ile süzülmesi beklenir.
/// </para>
/// </summary>
[Route("api/v2/ayar")]
public class AyarController(ISettingsService _ayarlar) : V2ControllerBase
{
    /// <summary>Etkinlik durumları (renkleriyle birlikte).</summary>
    /// <remarks>
    /// <c>Renk</c> alanı arayüzün RENK KAYNAĞIDIR: takvim etkinlikleri,
    /// listelerdeki kenar şeritleri ve rozetler bu değerden boyanır.
    /// </remarks>
    [HttpGet("etkinlik-durumlari")]
    [ProducesResponseType<SayfaliSonuc<AjandaDurumDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<AjandaDurumDto>> EtkinlikDurumlariAsync([FromQuery] SayfaIstegi istek)
        => Suz(await _ayarlar.GetAjandaDurumlarAsync(), istek, d => d.Ad);

    /// <summary>Etkinlik / talep tipleri.</summary>
    [HttpGet("tipler")]
    [ProducesResponseType<SayfaliSonuc<RandevuTipDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<RandevuTipDto>> TiplerAsync([FromQuery] SayfaIstegi istek)
        => Suz(await _ayarlar.GetRandevuTiplerAsync(), istek, t => t.Ad);

    /// <summary>Talep durumları.</summary>
    [HttpGet("talep-durumlari")]
    [ProducesResponseType<SayfaliSonuc<RandevuDurumDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<RandevuDurumDto>> TalepDurumlariAsync([FromQuery] SayfaIstegi istek)
        => Suz(await _ayarlar.GetRandevuDurumlarAsync(), istek, d => d.DurumAd);

    [HttpGet("mahalleler")]
    [ProducesResponseType<SayfaliSonuc<MahalleDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<MahalleDto>> MahallelerAsync([FromQuery] SayfaIstegi istek)
        => Suz(await _ayarlar.GetMahallelerAsync(), istek, m => m.Ad);

    [HttpGet("meslekler")]
    [ProducesResponseType<SayfaliSonuc<MeslekDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<MeslekDto>> MesleklerAsync([FromQuery] SayfaIstegi istek)
        => Suz(await _ayarlar.GetMesleklerAsync(), istek, m => m.Ad);

    [HttpGet("cicekciler")]
    [ProducesResponseType<SayfaliSonuc<CicekciDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<CicekciDto>> CicekcilerAsync([FromQuery] SayfaIstegi istek)
        => Suz(await _ayarlar.GetCicekcilerAsync(), istek, c => c.AdSoyad);

    [HttpGet("birimler")]
    [ProducesResponseType<SayfaliSonuc<BirimDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<BirimDto>> BirimlerAsync([FromQuery] SayfaIstegi istek)
        => Suz(await _ayarlar.GetBirimlerAsync(), istek, b => b.Ad);

    [HttpGet("alt-birimler")]
    [ProducesResponseType<SayfaliSonuc<BirimDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<BirimDto>> AltBirimlerAsync([FromQuery] SayfaIstegi istek)
        => Suz(await _ayarlar.GetAltBirimlerAsync(), istek, b => b.Ad);

    /// <summary>Alt birimler, ağaç yapısında.</summary>
    /// <remarks>
    /// Sayfalama UYGULANMAZ: ağaç, kökleriyle birlikte anlam taşır; ortasından
    /// kesilmiş bir ağaç istemcide yeniden kurulamaz. Yalnızca oturum
    /// sahibinin birimi ve altı döner, dolayısıyla sınırlı bir kümedir.
    /// </remarks>
    [HttpGet("alt-birimler/agac")]
    [ProducesResponseType<IEnumerable<BirimDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> AltBirimAgaciAsync()
        => Ok(await _ayarlar.GetAltBirimlerTreeAsync());

    /// <summary>Etkinliğe katılımcı olarak eklenebilecek birimler.</summary>
    /// <remarks>
    /// Kullanıcının kendi seviyesindekiler ve altındakiler. Üsttekiler
    /// listelenmez: bir müdürlük başkan yardımcısını kendi toplantısına
    /// çağıramaz, o davet yukarıdan gelir.
    /// </remarks>
    [HttpGet("katilimci-birimler")]
    [ProducesResponseType<SayfaliSonuc<BirimDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<BirimDto>> KatilimciBirimlerAsync([FromQuery] SayfaIstegi istek)
        => Suz(await _ayarlar.GetKatilimciBirimlerAsync(), istek, b => b.Ad);

    [HttpGet("ust-birim")]
    [ProducesResponseType<BirimDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UstBirimAsync()
        => Ok(await _ayarlar.GetUstBirimAsync());

    /// <summary>
    /// Oturum sahibinin birimindeki kullanıcılar — gizli etkinlik katılımcı
    /// seçicisi bunu kullanır.
    /// </summary>
    /// <remarks>
    /// İletişim bilgisi (e-posta, telefon, jeton) TAŞIMAZ: bu liste birimdeki
    /// herkese görünebiliyor, yalnızca ad/unvan/birim döner.
    /// </remarks>
    [HttpGet("birim-kullanicilari")]
    [ProducesResponseType<SayfaliSonuc<KatilimciDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<KatilimciDto>> BirimKullanicilariAsync([FromQuery] SayfaIstegi istek)
        => Suz(await _ayarlar.GetBirimKullanicilariAsync(), istek, k => k.TamAd);

    /// <summary>
    /// Bellekteki listeyi arar, sıralar ve sayfalar.
    /// </summary>
    /// <remarks>
    /// <see cref="ISettingsService"/> bu listeleri <c>IMemoryCache</c>'ten
    /// <c>IEnumerable</c> olarak döndürüyor; veritabanına inen bir
    /// <c>IQueryable</c> yok. Sayfalamayı bellekte yapmak burada DOĞRU seçim:
    /// veri zaten önbellekte, ikinci bir sorgu atmak onu ısrarla ısıtmak olur.
    /// Yazma tarafı (<c>/api/v2/tanim</c>) veritabanı düzeyinde sayfalar.
    /// </remarks>
    private static SayfaliSonuc<T> Suz<T>(
        IEnumerable<T> kaynak, SayfaIstegi istek, Func<T, string?> adSeciciAlani)
    {
        var liste = kaynak as IList<T> ?? kaynak.ToList();
        var ara = istek.TemizArama;

        IEnumerable<T> akis = liste;
        if (ara is not null)
        {
            akis = akis.Where(x =>
                adSeciciAlani(x)?.Contains(ara, StringComparison.OrdinalIgnoreCase) == true);
        }

        akis = istek.Azalan
            ? akis.OrderByDescending(adSeciciAlani, StringComparer.CurrentCulture)
            : akis.OrderBy(adSeciciAlani, StringComparer.CurrentCulture);

        return SayfaliSonuc<T>.Bellekten(akis.ToList(), istek);
    }

    /// <summary>SMS metninde kullanılabilen yer tutucular.</summary>
    /// <remarks>
    /// Katalog SUNUCUDAN gelir: web ve mobilde ayrı liste tutmak, birine yeni
    /// bir alan eklendiğinde ötekinin sessizce eksik kalması demekti.
    /// </remarks>
    [HttpGet("sms-yer-tutucular")]
    [ProducesResponseType<IEnumerable<SmsYerTutucu.Kayit>>(StatusCodes.Status200OK)]
    public IActionResult SmsYerTutucularAsync() => Ok(SmsYerTutucu.Katalog);
}
