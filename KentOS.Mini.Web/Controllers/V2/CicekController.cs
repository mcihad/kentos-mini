using KentOS.Mini.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Services.V2;
using KentOS.Mini.Application.Dto.V2.Cicek;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>Çiçekçiler ve çiçek talimatları.</summary>
/// <remarks>
/// Bir etkinliğe çiçek talimatı bağlamak <c>POST /api/v2/etkinlik/cicek</c>
/// üzerinden yapılır — kural (gizli etkinlikte çiçek çıkmaz) orada, ajanda
/// servisinde uygulanır. Burası çiçekçi kayıtlarının yönetimidir.
/// </remarks>
[Route("api/v2/cicek")]
[Izin(Izinler.CicekGoruntule)]
public class CicekController(
    ICicekciService _cicekciService,
    ICicekciDetayServisi _detay) : V2ControllerBase
{
    /// <summary>Çiçekçi dosyası — talimatlar, bağlı programlar ve sayılar.</summary>
    /// <remarks>
    /// Süzgeç talimatın OLUŞTURULMA tarihine göre; gönderilmemiş talimatlar da
    /// dönemin içinde sayılsın diye ("bu ay kaç iş verdik" sorusu).
    /// </remarks>
    [HttpGet("cicekciler/{id:long}/detay")]
    [ProducesResponseType<CicekciDetayDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DetayAsync(
        long id, [FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis)
        => Ok(await _detay.DetayAsync(id, baslangic, bitis));

    /// <summary>Çiçekçi dökümü — Excel.</summary>
    [HttpGet("cicekciler/{id:long}/excel")]
    [Izin(Izinler.CicekYonet)]
    public async Task<IActionResult> ExcelAsync(
        long id, [FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis)
    {
        var dosya = await _detay.ExcelAsync(id, baslangic, bitis);
        return File(dosya.Icerik, dosya.IcerikTuru, dosya.DosyaAdi);
    }

    /// <summary>Çiçekçi dökümü — PDF.</summary>
    [HttpGet("cicekciler/{id:long}/pdf")]
    [Izin(Izinler.CicekYonet)]
    public async Task<IActionResult> PdfAsync(
        long id, [FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis)
    {
        var dosya = await _detay.PdfAsync(id, baslangic, bitis);
        return File(dosya.Icerik, dosya.IcerikTuru, dosya.DosyaAdi);
    }

    /// <summary>Çiçekçiler.</summary>
    [HttpGet("cicekciler")]
    [ProducesResponseType<IEnumerable<CicekciDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CicekcilerAsync() => Ok(await _cicekciService.GetAllAsync());

    /// <summary>Çiçekçi sayısı.</summary>
    [HttpGet("cicekciler/sayi")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SayiAsync() => Ok(await _cicekciService.GetCountAsync());

    /// <summary>Çiçekçi detayı.</summary>
    [HttpGet("cicekciler/{id:long}")]
    [ProducesResponseType<CicekciDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CicekciAsync(long id) => Ok(await _cicekciService.GetByIdAsync(id));

    /// <summary>Yeni çiçekçi.</summary>
    [Izin(Izinler.CicekYonet)]
    [HttpPost("cicekciler")]
    [ProducesResponseType<CicekciDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CicekciEkleAsync([FromBody] CicekciDto istek)
        => Ok(await _cicekciService.CreateAsync(istek));

    /// <summary>Çiçekçiyi günceller.</summary>
    [Izin(Izinler.CicekYonet)]
    [HttpPut("cicekciler/{id:long}")]
    [ProducesResponseType<CicekciDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CicekciGuncelleAsync(long id, [FromBody] CicekciDto istek)
    {
        istek.Id = id;
        return Ok(await _cicekciService.UpdateAsync(istek));
    }

    /// <summary>Çiçekçiyi siler.</summary>
    [Izin(Izinler.CicekYonet)]
    [HttpDelete("cicekciler/{id:long}")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CicekciSilAsync(long id)
        => Ok(await _cicekciService.DeleteAsync(id));

    /// <summary>Bir çiçekçiye düşen talimatlar.</summary>
    [HttpGet("cicekciler/{id:long}/talimatlar")]
    [ProducesResponseType<IEnumerable<CicekDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TalimatlarAsync(long id)
        => Ok(await _cicekciService.GetCiceklerAsync(id));

    /// <summary>Çiçekçiye talimat ekler.</summary>
    [Izin(Izinler.CicekYonet)]
    [HttpPost("cicekciler/{id:long}/talimatlar")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TalimatEkleAsync(long id, [FromBody] CicekDto istek)
        => Ok(await _cicekciService.AddCicekAsync(id, istek));

    /// <summary>Çiçek kartı (teslim fişi) — kurum içi görünüm.</summary>
    [HttpGet("kart/{guid}")]
    [ProducesResponseType<CicekKartDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> KartAsync(string guid)
        => Ok(await _cicekciService.GetCicekKartAsync(guid));

    /// <summary>
    /// ÇİÇEKÇİNİN GÖRDÜĞÜ KART — giriş gerektirmez.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Neden anonim:</b> çiçekçi kurumun kullanıcısı değil. Hesabı, rolü,
    /// jetonu yok; kart bağlantısı ona SMS ile gidiyor. Uç önce sınıf
    /// düzeyindeki <c>[Izin(CicekGoruntule)]</c> ve JWT kapısının arkasındaydı,
    /// yani SMS'teki bağlantı çiçekçide <b>hiç açılmıyordu</b> — akış baştan
    /// sona kırıktı.
    /// </para>
    /// <para>
    /// <b>Yetki belirteci GUID'in kendisi:</b> tahmin edilemez ve yalnızca
    /// talimatın SMS'inde geçiyor. Yanıt da buna göre daraltıldı — doğrulama
    /// kodu ve etkinliğin geri kalanı dönmez (bkz. <c>CicekTeslimKartiDto</c>).
    /// </para>
    /// <para>
    /// Gizli etkinlikler çiçek talimatı üretmiyor, dolayısıyla bu uçtan gizli
    /// bir kaydın bilgisi sızamaz.
    /// </para>
    /// </remarks>
    [AllowAnonymous]
    [HttpGet("teslim-karti/{guid}")]
    [ProducesResponseType<CicekTeslimKartiDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TeslimKartiAsync(string guid)
        => Ok(await _cicekciService.TeslimKartiAsync(guid));

    /// <summary>
    /// Kartın teslim edildiğini doğrulama koduyla işaretler — giriş gerektirmez.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uç <c>[Izin(CicekYonet)]</c> istiyordu; çiçeği teslim eden kişide o izin
    /// hiç olmadığı için <b>teslim işaretlemesi yapılamıyordu</b>. Kapı artık
    /// yetki değil <b>doğrulama kodu</b>: kod yalnızca talimat SMS'inde geçiyor
    /// ve kartla birlikte gösterilmiyor.
    /// </para>
    /// <para>
    /// Kaba kuvvete karşı beş deneme sınırı var (servis katmanında, sayaç
    /// veritabanında).
    /// </para>
    /// </remarks>
    /// <remarks>
    /// <para>
    /// <b>Çok parçalı istek</b>: doğrulama kodu ve isteğe bağlı teslim
    /// fotoğrafı TEK çağrıda gidiyor. İki ayrı uç olsaydı çiçekçi kodu iki
    /// kez girecek ya da fotoğraf kodsuz bir uçtan yüklenecekti — ikincisi,
    /// bağlantıyı bilen herkesin fotoğrafı değiştirebilmesi demek.
    /// </para>
    /// <para>
    /// Hız sınırı giriş politikasıyla aynı: uç anonim ve beş denemelik
    /// kod kapısını dakikada binlerce istekle zorlamak mümkün olmamalı.
    /// </para>
    /// </remarks>
    [AllowAnonymous]
    [EnableRateLimiting(Filters.HizSiniri.Giris)]
    [HttpPost("teslim-karti/{guid}/teslim")]
    [ProducesResponseType<CicekTeslimKartiDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TeslimEtAsync(
        string guid,
        [FromForm] int dogrulamaKodu,
        IFormFile? fotograf)
    {
        await using var akis = fotograf?.OpenReadStream();

        return Ok(await _cicekciService.TeslimEtAsync(
            guid, dogrulamaKodu, akis, fotograf?.FileName, fotograf?.ContentType));
    }

    /// <summary>Kurum içinden teslim işaretleme (yetkiyle).</summary>
    [Izin(Izinler.CicekYonet)]
    [HttpPost("kart/{guid}/teslim")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TeslimAsync(string guid, [FromBody] TeslimIstegi istek)
        => Ok(await _cicekciService.CicekKartGonderildiAsync(guid, istek.DogrulamaKodu));
}

/// <summary>Çiçek kartı teslim isteği.</summary>
public class TeslimIstegi
{
    /// <summary>Alıcıya SMS ile giden doğrulama kodu.</summary>
    public int DogrulamaKodu { get; set; }
}
