using KentOS.Mini.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>
/// Davet listeleri — protokolden seçilen kişilerin takibi.
/// </summary>
/// <remarks>
/// Görünürlük servis katmanında: davetler oluşturan kullanıcının birimine
/// ait ve yalnızca o birim görür.
/// </remarks>
[Route("api/v2/davet")]
[Izin(Izinler.DavetGoruntule)]
public class DavetController(
    IDavetServisi _davet,
    IDavetCiktiServisi _cikti,
    IIsimKartiServisi _kart) : V2ControllerBase
{
    [HttpGet]
    [ProducesResponseType<SayfaliSonuc<DavetOzetDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<DavetOzetDto>> ListeAsync([FromQuery] DavetSuzgeci suzgec)
        => _davet.ListeAsync(suzgec);

    [HttpGet("{id:long}")]
    [ProducesResponseType<DavetDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public Task<DavetDetayDto> DetayAsync(long id) => _davet.DetayAsync(id);

    [Izin(Izinler.DavetYonet)]
    [HttpPost]
    [ProducesResponseType<DavetDetayDto>(StatusCodes.Status200OK)]
    public Task<DavetDetayDto> OlusturAsync([FromBody] DavetIstegi istek)
        => _davet.OlusturAsync(istek);

    [HttpPut("{id:long}")]
    [Izin(Izinler.DavetYonet)]
    [ProducesResponseType<DavetDetayDto>(StatusCodes.Status200OK)]
    public Task<DavetDetayDto> GuncelleAsync(long id, [FromBody] DavetIstegi istek)
        => _davet.GuncelleAsync(id, istek);

    [HttpDelete("{id:long}")]
    [Izin(Izinler.DavetYonet)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SilAsync(long id)
    {
        await _davet.SilAsync(id);
        return NoContent();
    }

    /// <summary>Davete kişi ekler — tek tek ya da kategorinin tamamı.</summary>
    [Izin(Izinler.DavetYonet)]
    [HttpPost("{id:long}/kisi")]
    [ProducesResponseType<DavetDetayDto>(StatusCodes.Status200OK)]
    public Task<DavetDetayDto> KisiEkleAsync(long id, [FromBody] DavetKisiEkleIstegi istek)
        => _davet.KisiEkleAsync(id, istek);

    /// <summary>Takip bilgisini günceller (arandı / mesaj / durum / not).</summary>
    [Izin(Izinler.DavetYonet)]
    [HttpPut("{id:long}/kisi/{kisiId:long}")]
    [ProducesResponseType<DavetKisiDto>(StatusCodes.Status200OK)]
    public Task<DavetKisiDto> KisiGuncelleAsync(
        long id, long kisiId, [FromBody] DavetKisiGuncelleIstegi istek)
        => _davet.KisiGuncelleAsync(id, kisiId, istek);

    [Izin(Izinler.DavetYonet)]
    [HttpDelete("{id:long}/kisi/{kisiId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> KisiCikarAsync(long id, long kisiId)
    {
        await _davet.KisiCikarAsync(id, kisiId);
        return NoContent();
    }

    /// <summary>
    /// Davet listesini PDF olarak indirir.
    /// </summary>
    /// <param name="tur">
    /// <c>Durumlu</c> takip çıktısı · <c>Telefonlu</c> arama listesi ·
    /// <c>BosKatilim</c> törende elle işaretlenecek boş liste ·
    /// <c>BosProtokol</c> yalnızca ad/unvan/kurum.
    /// </param>
    /// <param name="kategoriId">Verilirse yalnızca o kategori yazdırılır.</param>
    [HttpGet("{id:long}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PdfAsync(
        long id,
        [FromQuery] DavetCiktiTuru tur = DavetCiktiTuru.Durumlu,
        [FromQuery] long? kategoriId = null)
    {
        var (icerik, ad) = await _cikti.PdfAsync(id, tur, kategoriId);
        return File(icerik, "application/pdf", ad);
    }

    /// <summary>Kart tasarım katalogu — istemci seçenekleri buradan çizer.</summary>
    [HttpGet("kart-tasarimlari")]
    public IActionResult KartTasarimlari() => Ok(new
    {
        kesme = Web.Services.V2.KartTasarimlari.Kesme.Select(t => new { t.Anahtar, t.Ad }),
        masa = Web.Services.V2.KartTasarimlari.Masa.Select(t => new { t.Anahtar, t.Ad }),
    });

    /// <summary>Bu davete ait KESME kartları — sandalyeye yapıştırılan etiketler.</summary>
    /// <remarks>
    /// Kaynak PROTOKOL DEFTERİ değil, BU DAVET: basılacak olan kurumun bütün
    /// protokol listesi değil, bu törene çağrılanlar. <paramref name="durum"/>
    /// ile "katılacak" diyenlere daraltılır — varsayılan davranış budur, çünkü
    /// bütün davetliyi basmak boş sandalyelere isimlik koymak demek.
    /// </remarks>
    [HttpGet("{id:long}/kesme-kartlari/pdf")]
    public async Task<IActionResult> KesmeKartPdfAsync(
        long id,
        [FromQuery] DavetDurumu? durum = DavetDurumu.Katilacak,
        [FromQuery] int sutun = 2,
        [FromQuery] int satir = 10,
        [FromQuery] string? tasarim = null,
        [FromQuery] bool unvan = true,
        [FromQuery] bool kurum = false,
        [FromQuery] bool kesmeCizgisi = true,
        [FromQuery] bool logo = false,
        [FromQuery] int logoYeri = 1,
        [FromQuery] int logoBoyu = 1,
        [FromQuery] bool antet = false)
    {
        var dosya = await _kart.KesmeKartPdfAsync(id, durum,
            new KesmeKartAyari(sutun, satir, tasarim, unvan, kurum, kesmeCizgisi,
                               logo, logoYeri, logoBoyu, antet));
        return File(dosya.Icerik, dosya.IcerikTuru, dosya.DosyaAdi);
    }

    /// <summary>Bu davete ait MASA kartları — ortadan katlanan çadır kartlar.</summary>
    [HttpGet("{id:long}/masa-kartlari/pdf")]
    public async Task<IActionResult> MasaKartPdfAsync(
        long id,
        [FromQuery] DavetDurumu? durum = DavetDurumu.Katilacak,
        [FromQuery] int sayfaBasi = 2,
        [FromQuery] string? tasarim = null,
        [FromQuery] bool unvan = true,
        [FromQuery] bool kurum = false,
        [FromQuery] bool ciftYuz = true,
        [FromQuery] bool logo = false,
        [FromQuery] int logoYeri = 1,
        [FromQuery] int logoBoyu = 1,
        [FromQuery] bool antet = false)
    {
        var dosya = await _kart.MasaKartPdfAsync(id, durum,
            new MasaKartAyari(sayfaBasi, tasarim, unvan, kurum, ciftYuz,
                              logo, logoYeri, logoBoyu, antet));
        return File(dosya.Icerik, dosya.IcerikTuru, dosya.DosyaAdi);
    }
}
