using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Application.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>Gönderime not ekleme isteği.</summary>
public class GonderimNotIstegi
{
    public string Metin { get; set; } = string.Empty;
}

/// <summary>
/// Kullanıcıdan kullanıcıya dosya gönderimi.
/// </summary>
/// <remarks>
/// <para>
/// <b>Görünürlük servis katmanında</b>: bir gönderimi yalnızca gönderen ve
/// alıcı görebilir, rol bypass'ı yok. Controller yalnızca "gönderim
/// başlatma" yetkisini denetler.
/// </para>
/// <para>
/// Göndermek <c>gonderim.gonder</c> iznini ister; <b>almak yetki istemez</b>,
/// aksi hâlde gönderilen dosya kimseye ulaşmazdı. Yetki eskiden kullanıcı
/// kaydındaki <c>DosyaGonderebilir</c> bayrağındaydı; sütun duruyor ama artık
/// karar vermiyor — aynı yetkinin iki kaynağı olması, rol ekranından kısılan
/// bir iznin kullanıcı kaydından açık kalması demekti.
/// </para>
/// </remarks>
[Route("api/v2/gonderim")]
[Izin(Izinler.GonderimGoruntule)]
public class GonderimController(
    IDosyaGonderimiServisi _gonderim,
    UserManager<AppUser> _kullaniciYoneticisi,
    ICurrentUserService _mevcutKullanici,
    AuthPolicies.IIzinServisi _izinler) : V2ControllerBase
{
    private async Task<long> KullaniciIdAsync() =>
        await _mevcutKullanici.GetUserIdAsync()
        ?? throw new UnauthorizedAccessException("Oturum kullanıcısı çözülemedi.");

    /// <summary>
    /// Gönderim başlatma yetkisini doğrular.
    /// </summary>
    /// <remarks>
    /// Yetki her istekte VERİTABANINDAN okunur, JWT'den değil: yetki geri
    /// alındığında kullanıcının jetonu 15 saat daha geçerli olurdu. (İzin
    /// servisi 5 dakika önbelleklese de rol değişiminde önbellek düşürülür.)
    /// </remarks>
    private async Task<AppUser> GonderebilenKullaniciAsync()
    {
        var kullanici = await _mevcutKullanici.GetCurrentAsync();

        if (!await _izinler.VarMiAsync(kullanici.Id, Izinler.GonderimGonder))
        {
            throw new UnauthorizedAccessException(
                "Dosya gönderme yetkiniz yok. Birim yöneticinizle görüşün.");
        }

        return kullanici;
    }

    /// <summary>Gelen ve giden gönderimler.</summary>
    /// <remarks>
    /// Yetki İSTEMEZ: kendisine gönderilen dosyayı herkes görebilmeli, yoksa
    /// gönderilen dosya kimseye ulaşmazdı.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<SayfaliSonuc<GonderimOzetDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<GonderimOzetDto>> ListeAsync([FromQuery] GonderimSuzgeci suzgec)
        => await _gonderim.ListeAsync(await KullaniciIdAsync(), suzgec);

    /// <summary>Okunmamış gelen gönderim sayısı — menü rozeti.</summary>
    [HttpGet("okunmamis-sayi")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<int> OkunmamisAsync()
        => await _gonderim.OkunmamisSayisiAsync(await KullaniciIdAsync());

    /// <summary>Gönderim detayı ve yazışma.</summary>
    /// <remarks>Alıcı ilk açtığında kayıt okundu olarak işaretlenir.</remarks>
    [HttpGet("{id:long}")]
    [ProducesResponseType<GonderimDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public async Task<GonderimDetayDto> DetayAsync(long id)
        => await _gonderim.DetayAsync(await KullaniciIdAsync(), id);

    /// <summary>Gönderilen dosyayı indirir.</summary>
    /// <remarks>
    /// Dosyalar <c>wwwroot</c> DIŞINDA duruyor ve yalnızca buradan iniyor:
    /// statik dosya ara katmanı kimlik doğrulamadığı için, tahmin edilmesi zor
    /// bir adres bile "yalnızca iki taraf görebilir" kuralını sağlamazdı.
    /// </remarks>
    [HttpGet("{id:long}/dosya")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DosyaAsync(long id)
    {
        var (akis, ad, tur) = await _gonderim.DosyaAsync(await KullaniciIdAsync(), id);
        return File(akis, tur, ad);
    }

    /// <summary>Dosya gönderir.</summary>
    /// <remarks>
    /// `multipart/form-data`: <c>aliciId</c>, <c>konu</c>, <c>not</c> ve
    /// <c>dosya</c>. En fazla 25 MB; çalıştırılabilir uzantılar reddedilir.
    /// </remarks>
    [Izin(Izinler.GonderimGonder)]
    [HttpPost]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [ProducesResponseType<GonderimDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status403Forbidden)]
    public async Task<GonderimDetayDto> GonderAsync(
        [FromForm] long aliciId,
        [FromForm] string konu,
        [FromForm] string? not)
    {
        var kullanici = await GonderebilenKullaniciAsync();
        var dosya = Request.Form.Files.FirstOrDefault();

        return await _gonderim.GonderAsync(kullanici.Id, aliciId, konu, not, dosya!);
    }

    /// <summary>Gönderime not ekler (iki taraf da yazabilir).</summary>
    [Izin(Izinler.GonderimGoruntule)]
    [HttpPost("{id:long}/not")]
    [ProducesResponseType<GonderimNotuDto>(StatusCodes.Status200OK)]
    public async Task<GonderimNotuDto> NotEkleAsync(long id, [FromBody] GonderimNotIstegi istek)
        => await _gonderim.NotEkleAsync(await KullaniciIdAsync(), id, istek.Metin);

    /// <summary>Gönderimi siler.</summary>
    /// <remarks>
    /// Yalnızca GÖNDEREN silebilir: alıcının kendisine gelen bir belgeyi
    /// gönderenin bilgisi olmadan yok etmesi çözülemez bir anlaşmazlık yaratır.
    /// </remarks>
    [Izin(Izinler.GonderimGonder)]
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SilAsync(long id)
    {
        await _gonderim.SilAsync(await KullaniciIdAsync(), id);
        return NoContent();
    }

    /// <summary>
    /// Dosya gönderilebilecek kullanıcılar.
    /// </summary>
    /// <remarks>
    /// Yalnızca ad, unvan ve birim döner — iletişim bilgisi TAŞIMAZ. Bu
    /// liste gönderim yetkisi olan herkese açık; e-posta/telefon sızdırmak
    /// için bir kanal olmamalı.
    /// </remarks>
    [HttpGet("alicilar")]
    [ProducesResponseType<List<AliciDto>>(StatusCodes.Status200OK)]
    public async Task<List<AliciDto>> AlicilarAsync([FromQuery] string? ara)
    {
        var ben = await GonderebilenKullaniciAsync();

        var sorgu = _kullaniciYoneticisi.Users
            .AsNoTracking()
            .Where(u => u.Id != ben.Id);

        if (!string.IsNullOrWhiteSpace(ara))
        {
            var k = $"%{ara.Trim()}%";
            sorgu = sorgu.Where(u =>
                EF.Functions.ILike(u.UserName!, k) ||
                (u.Ad != null && EF.Functions.ILike(u.Ad, k)) ||
                (u.Soyad != null && EF.Functions.ILike(u.Soyad, k)));
        }

        return await sorgu
            .OrderBy(u => u.Ad).ThenBy(u => u.Soyad)
            .Take(50)
            .Select(u => new AliciDto
            {
                Id = u.Id,
                AdSoyad = (u.Ad + " " + u.Soyad).Trim(),
                KullaniciAdi = u.UserName!,
                Unvan = u.Unvan,
                BirimAd = u.Birim == null ? null : u.Birim.Ad,
            })
            .ToListAsync();
    }
}

/// <summary>Alıcı seçicisindeki kullanıcı — iletişim bilgisi TAŞIMAZ.</summary>
public class AliciDto
{
    public long Id { get; set; }
    public string AdSoyad { get; set; } = string.Empty;
    public string KullaniciAdi { get; set; } = string.Empty;
    public string? Unvan { get; set; }
    public string? BirimAd { get; set; }
}
