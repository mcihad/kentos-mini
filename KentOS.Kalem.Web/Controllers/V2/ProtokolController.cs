using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Application.Dto.V2.Ortak;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Web.AuthPolicies;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Web.Controllers.V2;

/// <summary>Sıralama güncelleme isteği.</summary>
public class SiralamaIstegi
{
    public List<SiralamaOgesi> Ogeler { get; set; } = [];
}

public class SiralamaOgesi
{
    public long Id { get; set; }
    public int SiraNo { get; set; }
}

/// <summary>
/// İl protokol listesi.
/// </summary>
/// <remarks>
/// <b>Okuma</b> ajanda politikasına açık: etkinlik planlayan herkesin
/// protokol sırasını ve iletişim bilgilerini görmesi gerekiyor.
/// <b>Yazma</b> yalnızca <c>Admin</c>: liste resmî bir kaynak, herkesin
/// düzenlemesi doğruluğunu bozar.
/// </remarks>
[Route("api/v2/protokol")]
[Izin(Izinler.ProtokolGoruntule)]
public class ProtokolController(IProtokolServisi _protokol) : V2ControllerBase
{
    /// <summary>Protokol listesi — sıra numarasına göre.</summary>
    [HttpGet]
    [ProducesResponseType<SayfaliSonuc<ProtokolDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<ProtokolDto>> ListeAsync([FromQuery] ProtokolSuzgeci suzgec)
        => _protokol.ListeAsync(suzgec);

    /// <summary>Kategoriler ve kayıt sayıları — süzgeç çipleri ve form seçicisi.</summary>
    [HttpGet("kategoriler")]
    [ProducesResponseType<List<ProtokolKategoriDto>>(StatusCodes.Status200OK)]
    public Task<List<ProtokolKategoriDto>> KategorilerAsync() => _protokol.KategorilerAsync();

    /// <summary>
    /// Kategori ekler.
    /// </summary>
    /// <remarks>
    /// Protokol formundaki <b>+</b> düğmesi buraya gider: kullanıcı kaydı
    /// bırakıp ayrı bir tanım ekranına gitmek zorunda kalmasın diye kategori
    /// yerinde açılabiliyor.
    /// </remarks>
    [HttpPost("kategoriler")]
    [Izin(Izinler.ProtokolYonet)]
    [ProducesResponseType<ProtokolKategoriDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status400BadRequest)]
    public Task<ProtokolKategoriDto> KategoriOlusturAsync([FromBody] ProtokolKategoriIstegi istek)
        => _protokol.KategoriOlusturAsync(istek);

    [HttpPut("kategoriler/{id:long}")]
    [Izin(Izinler.ProtokolYonet)]
    [ProducesResponseType<ProtokolKategoriDto>(StatusCodes.Status200OK)]
    public Task<ProtokolKategoriDto> KategoriGuncelleAsync(long id, [FromBody] ProtokolKategoriIstegi istek)
        => _protokol.KategoriGuncelleAsync(id, istek);

    [HttpDelete("kategoriler/{id:long}")]
    [Izin(Izinler.ProtokolYonet)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> KategoriSilAsync(long id)
    {
        await _protokol.KategoriSilAsync(id);
        return NoContent();
    }

    /// <summary>Kurumlar ve kayıt sayıları.</summary>
    [HttpGet("kurumlar")]
    [ProducesResponseType<List<ProtokolGrubuDto>>(StatusCodes.Status200OK)]
    public Task<List<ProtokolGrubuDto>> KurumlarAsync() => _protokol.KurumlarAsync();

    [HttpGet("{id:long}")]
    [ProducesResponseType<ProtokolDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public Task<ProtokolDto> DetayAsync(long id) => _protokol.GetirAsync(id);

    [Izin(Izinler.ProtokolYonet)]
    [HttpPost]
    [ProducesResponseType<ProtokolDto>(StatusCodes.Status200OK)]
    public Task<ProtokolDto> OlusturAsync([FromBody] ProtokolIstegi istek)
        => _protokol.OlusturAsync(istek);

    [HttpPut("{id:long}")]
    [Izin(Izinler.ProtokolYonet)]
    [ProducesResponseType<ProtokolDto>(StatusCodes.Status200OK)]
    public Task<ProtokolDto> GuncelleAsync(long id, [FromBody] ProtokolIstegi istek)
        => _protokol.GuncelleAsync(id, istek);

    [HttpDelete("{id:long}")]
    [Izin(Izinler.ProtokolYonet)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SilAsync(long id)
    {
        await _protokol.SilAsync(id);
        return NoContent();
    }

    /// <summary>Sıra numaralarını topluca yazar.</summary>
    /// <remarks>
    /// Sürükle-bırak sonrası tek istekte gönderilir; satır satır güncelleme
    /// yarım kalırsa liste rastgele bir düzende kalırdı.
    /// </remarks>
    [HttpPost("siralama")]
    [Izin(Izinler.ProtokolYonet)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SiralamaAsync([FromBody] SiralamaIstegi istek)
    {
        await _protokol.SiralamaGuncelleAsync(
            istek.Ogeler.Select(o => (o.Id, o.SiraNo)).ToList());
        return NoContent();
    }

    /// <summary>Kişinin davet geçmişi — hangi törene çağrıldı, ne cevap verdi.</summary>
    /// <remarks>
    /// Telefonu elinde tutan kişi aramadan önce geçen sefer ne olduğunu bilmek
    /// istiyor: "geçen tören için de aramıştık, gelemedi" bilgisi konuşmanın
    /// tonunu belirliyor. Liste birim süzgecinden geçer.
    /// </remarks>
    [HttpGet("{id:long}/davetler")]
    [ProducesResponseType<List<ProtokolDavetGecmisiDto>>(StatusCodes.Status200OK)]
    public Task<List<ProtokolDavetGecmisiDto>> DavetGecmisiAsync(long id)
        => _protokol.DavetGecmisiAsync(id);

    /// <summary>
    /// Liste Excel çıktısı — ekrandaki süzgeçlerle.
    /// </summary>
    /// <remarks>
    /// Süzgeç nesnesi listedekiyle AYNI: kullanıcı ekranda ne görüyorsa
    /// dosyada onu bulmalı. Ayrı bir süzgeç sınıfı olsaydı ikisi zamanla
    /// ayrışır ve "Excel eksik geliyor" diye bir şikâyet doğardı. Dışa
    /// aktarma görüntülemekten AYRI bir izin ister: dosya kurum dışına
    /// taşınabiliyor.
    /// </remarks>
    [HttpGet("excel")]
    [Izin(Izinler.ProtokolCiktiAl)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ProtokolExcelAsync(
        [FromQuery] ProtokolSuzgeci suzgec)
        => Gonder(await _protokol.ExcelAsync(suzgec));

    private FileContentResult Gonder(DisaAktarmaDosyasi d)
        => File(d.Icerik, d.IcerikTuru, d.DosyaAdi);
}
