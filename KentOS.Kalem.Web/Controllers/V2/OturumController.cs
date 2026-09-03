using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using KentOS.Kalem.Web.Filters;
using KentOS.Kalem.Application.Dto.V2.Ortak;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Application.Dto.V2.Oturum;
using KentOS.Kalem.Web.AuthPolicies;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Services;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Web.Controllers.V2;

/// <summary>Oturum açma ve oturum sahibinin bilgileri.</summary>
[Route("api/v2/oturum")]
public class OturumController(
    IOturumServisi _oturumServisi,
    UserManager<AppUser> _userManager,
    IBirimService _birimService,
    IUserService _userService,
    IOturumKaydiServisi _oturumKaydi,
    IIzinServisi _izinServisi,
    ICurrentUserService _mevcutKullanici) : V2ControllerBase
{
    /// <summary>Kullanıcı adı ve parola ile giriş yapar, JWT döndürür.</summary>
    /// <response code="200">Giriş başarılı.</response>
    /// <response code="401">Kimlik hatalı ya da hesap kilitli.</response>
    [AllowAnonymous]
    [EnableRateLimiting(Filters.HizSiniri.Giris)]
    [HttpPost("giris")]
    [ProducesResponseType<GirisYaniti>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GirisAsync([FromBody] GirisIstegi istek)
    {
        var sonuc = await _oturumServisi.GirisYapAsync(istek.KullaniciAdi, istek.Parola);

        if (sonuc.Tur != GirisSonucTuru.Basarili)
        {
            return Unauthorized(new HataYaniti
            {
                Tur = HataTurleri.Kimlik,
                Baslik = "Giriş yapılamadı",
                Durum = StatusCodes.Status401Unauthorized,
                Ayrinti = sonuc.Mesaj,
                Ornek = HttpContext.Request.Path.Value,
            });
        }

        return Ok(new GirisYaniti
        {
            Jeton = sonuc.Jeton!,
            GecerlilikSonu = sonuc.GecerlilikSonu!.Value,
        });
    }

    /// <summary>Oturum açan kullanıcının kimliği, birimi ve yetkileri.</summary>
    [HttpGet("ben")]
    [ProducesResponseType<BenYaniti>(StatusCodes.Status200OK)]
    public async Task<IActionResult> BenAsync()
    {
        var kullanici = await _mevcutKullanici.GetCurrentAsync();
        var roller = await _userManager.GetRolesAsync(kullanici);

        string? birimAd = null;
        if (kullanici.BirimId is > 0)
        {
            var birim = await _birimService.GetAsync(kullanici.BirimId.Value);
            birimAd = birim?.Ad;
        }

        var tamAd = $"{kullanici.Ad} {kullanici.Soyad}".Trim();
        var izinler = await _izinServisi.IzinleriAsync(kullanici.Id);

        return Ok(new BenYaniti
        {
            Id = kullanici.Id,
            KullaniciAdi = kullanici.UserName ?? string.Empty,
            Ad = kullanici.Ad,
            Soyad = kullanici.Soyad,
            TamAd = string.IsNullOrWhiteSpace(tamAd) ? (kullanici.UserName ?? string.Empty) : tamAd,
            Unvan = kullanici.Unvan,
            Eposta = kullanici.Email,
            BirimId = kullanici.BirimId,
            BirimAd = birimAd,
            Roller = [.. roller],
            // ESKİ ALANLAR, ARTIK İZİNDEN TÜRETİLİYOR.
            //
            // `AppUser` sütunları veritabanında duruyor ama okunmuyor: yetki
            // tek yerden, rolün izinlerinden geliyor. Alanlar yanıttan
            // KALDIRILMADI çünkü sahadaki eski uygulama sürümleri onlara
            // bakıyor — kaldırmak, mağazadan güncelleme almamış telefonlarda
            // gizli etkinlik anahtarını ve gönder düğmesini sessizce kapatırdı.
            GizliEtkinlikEkleyebilir = izinler.Contains(Application.Identity.Izinler.AjandaGizliEtkinlik),
            DosyaGonderebilir = izinler.Contains(Application.Identity.Izinler.GonderimGonder),
            // Bu alan SÜTUNDAN okunuyor, izinden değil: yukarıdaki ikisi birer
            // yetki ve yetkinin kaynağı rol; bu ise kullanıcıya özel bir tercih
            // ve rolle ifade edilemez. Aynı rolün iki üyesinden biri sahada
            // olabilir, öteki masada.
            SahaPersoneli = kullanici.SahaPersoneli,
            // Arayüzün menüleri şekillendirmesi için; yetkinin kaynağı yine sunucu.
            //
            // İKİ LİSTE BİR ARADA: `yetkiler` eski beş politika (sahadaki eski
            // uygulama sürümleri ona bakıyor), `izinler` ise yeni ince taneli
            // liste. Politikalar emekliye ayrılana kadar ikisi de gönderilir.
            Yetkiler = YetkiCozucu.Coz(roller),
            Izinler = [.. izinler],
        });
    }

    /// <summary>
    /// Oturum sahibinin KENDİ parolasını değiştirir.
    /// </summary>
    /// <remarks>
    /// Yöneticinin başkasının parolasını sıfırlaması ayrı uçtadır
    /// (<c>POST /api/v2/yonetim/kullanicilar/{id}/parola</c>). Burada mevcut
    /// parola ZORUNLU: oturumu ele geçirilmiş bir tarayıcı, parolayı
    /// değiştirip kalıcı erişim sağlayamasın.
    /// </remarks>
    /// <summary>Oturum sahibinin bildirim tercihleri.</summary>
    /// <remarks>
    /// v1 karşılığı <c>GET /api/AccountApi/Settings</c>. Alan adları
    /// <see cref="UserSettingDto"/>'dan geldiği gibi (İngilizce) bırakıldı:
    /// mobil bu modeli bugün kullanıyor ve geçiş sırasında iki farklı
    /// isimlendirmeyle uğraşmak, tek tek her anahtarın elle eşlenmesi
    /// demekti — sessizce kapanan bir bildirim tercihi en zor fark edilen
    /// hatalardan biri.
    /// </remarks>
    [HttpGet("tercihler")]
    [ProducesResponseType<UserSettingDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TercihlerAsync()
        => Ok(await _userService.GetSetting());

    /// <summary>Bildirim tercihlerini kaydeder.</summary>
    /// <remarks>
    /// v1 <c>POST</c> kullanıyor; v2 <c>PUT</c>, çünkü işlem tam değiştirme
    /// (idempotent). Gövde şeması aynı.
    /// </remarks>
    [HttpPut("tercihler")]
    [ProducesResponseType<UserSettingDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TercihKaydetAsync([FromBody] UserSettingDto istek)
        => Ok(await _userService.UpdateSetting(istek));

    [HttpPost("parola")]
    [ProducesResponseType<PasswordChangeResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ParolaDegistirAsync([FromBody] PasswordChangeDto istek)
    {
        if (istek.NewPassword != istek.NewPasswordConfirm)
        {
            return BadRequest(new HataYaniti
            {
                Tur = HataTurleri.Dogrulama,
                Baslik = "Doğrulama hatası",
                Durum = StatusCodes.Status400BadRequest,
                Ayrinti = "Yeni parola ile tekrarı aynı değil.",
                Ornek = HttpContext.Request.Path.Value,
            });
        }

        var sonuc = await _userService.PasswordChange(istek);

        if (!sonuc.Success)
        {
            return BadRequest(new HataYaniti
            {
                Tur = HataTurleri.IsKurali,
                Baslik = "Parola değiştirilemedi",
                Durum = StatusCodes.Status400BadRequest,
                Ayrinti = sonuc.Message,
                Ornek = HttpContext.Request.Path.Value,
            });
        }

        return Ok(sonuc);
    }

    /// <summary>
    /// Oturumu kapatır.
    /// </summary>
    /// <remarks>
    /// JWT'nin sunucu tarafında iptal listesi YOK; bu uç jetonu geçersiz
    /// kılmaz, yalnızca <b>denetim kaydı</b> yazar. Gerçek çıkış istemcide
    /// jetonun silinmesiyle olur. Kaydı yazmadan geçmek, "kim ne zaman
    /// çıktı" sorusunu cevapsız bırakırdı.
    /// </remarks>
    [HttpPost("cikis")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CikisAsync()
    {
        var kullanici = await _mevcutKullanici.GetCurrentAsync();
        await _oturumKaydi.KaydetAsync(
            kullanici.Id, kullanici.UserName ?? string.Empty, OturumOlayi.Cikis, true);
        return NoContent();
    }

    /// <summary>Oturum açma / kapama denetim kayıtları.</summary>
    /// <remarks>
    /// Başarısız denemeler de listelenir — arka arkaya gelen başarısızlık,
    /// hesap kilitlenmeden önce görülmesi gereken tek sinyaldir.
    /// </remarks>
    [HttpGet("kayitlar")]
    [Izin(Izinler.YonetimOturumKaydi)]
    [ProducesResponseType<SayfaliSonuc<OturumKaydiDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<OturumKaydiDto>> KayitlarAsync([FromQuery] OturumKaydiSuzgeci suzgec)
        => _oturumKaydi.ListeAsync(suzgec);
}
