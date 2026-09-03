using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Application.Dto.V2.Ortak;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Web.AuthPolicies;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Web.Controllers.V2;

/// <summary>
/// HALK GÜNÜ — gün, zaman dilimleri, bekleyenler havuzu, görüşme kayıtları.
/// </summary>
/// <remarks>
/// <para>
/// Üç ayrı kullanıcıya hizmet ediyor: <b>sekreter</b> günü kurar ve listeyi
/// doldurur, <b>salondaki personel</b> tabletten sırayla ilerleyip not tutar,
/// <b>başkan</b> özeti okur. İzinler de bu üçlüye göre bölünmüş durumda —
/// salon operatörüne yalnızca <c>halkgunu.gorusme</c> verilebiliyor.
/// </para>
/// <para>
/// Sınıf düzeyindeki izin OKUMA kapısı; her yazma ucu kendi iznini ayrıca
/// ilan ediyor.
/// </para>
/// </remarks>
[Route("api/v2/halk-gunu")]
[Izin(Izinler.HalkgunuGoruntule)]
public class HalkGunuController(
    IHalkGunuServisi _halkGunu,
    IHalkGunuIslemServisi _islem,
    IHalkGunuCiktiServisi _cikti) : V2ControllerBase
{
    // ── gün ────────────────────────────────────────────────────────────

    /// <summary>Halk günleri — tarih aralığı ve duruma göre.</summary>
    [HttpGet]
    [ProducesResponseType<SayfaliSonuc<HalkGunuOzetDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<HalkGunuOzetDto>> ListeAsync([FromQuery] HalkGunuSuzgeci suzgec)
        => _halkGunu.ListeAsync(suzgec);

    /// <summary>Günün tamamı: dilimler, sıralı kişiler ve atanmamışlar.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<HalkGunuDetayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public Task<HalkGunuDetayDto> DetayAsync(long id) => _halkGunu.DetayAsync(id);

    /// <summary>Başkan görünümü — sayılar, mahalle dağılımı, takip listesi.</summary>
    [HttpGet("{id:long}/ozet")]
    [ProducesResponseType<HalkGunuOzetiDto>(StatusCodes.Status200OK)]
    public Task<HalkGunuOzetiDto> OzetAsync(long id) => _islem.OzetAsync(id);

    [HttpPost]
    [Izin(Izinler.HalkgunuYonet)]
    [ProducesResponseType<HalkGunuDetayDto>(StatusCodes.Status200OK)]
    public Task<HalkGunuDetayDto> OlusturAsync([FromBody] HalkGunuIstegi istek)
        => _halkGunu.OlusturAsync(istek);

    [HttpPut("{id:long}")]
    [Izin(Izinler.HalkgunuYonet)]
    [ProducesResponseType<HalkGunuDetayDto>(StatusCodes.Status200OK)]
    public Task<HalkGunuDetayDto> GuncelleAsync(long id, [FromBody] HalkGunuIstegi istek)
        => _halkGunu.GuncelleAsync(id, istek);

    [HttpDelete("{id:long}")]
    [Izin(Izinler.HalkgunuYonet)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SilAsync(long id)
    {
        await _halkGunu.SilAsync(id);
        return NoContent();
    }

    // ── zaman dilimleri ────────────────────────────────────────────────

    /// <summary>
    /// Dilim ekler — tek dilim ya da toplu üretim.
    /// </summary>
    /// <remarks>
    /// Gövdede <c>dilimDakika</c> varsa aralık o uzunlukta dilimlere bölünür
    /// (14:00–15:00 / 10 dk → altı dilim). Sekreterin on dilimi tek tek
    /// girmesi işin en sıkıcı kısmıydı.
    /// </remarks>
    [HttpPost("{id:long}/dilim")]
    [Izin(Izinler.HalkgunuYonet)]
    [ProducesResponseType<HalkGunuDetayDto>(StatusCodes.Status200OK)]
    public Task<HalkGunuDetayDto> DilimEkleAsync(long id, [FromBody] DilimIstegi istek)
        => _halkGunu.DilimEkleAsync(id, istek);

    [HttpPut("dilim/{dilimId:long}")]
    [Izin(Izinler.HalkgunuYonet)]
    [ProducesResponseType<HalkGunuDetayDto>(StatusCodes.Status200OK)]
    public Task<HalkGunuDetayDto> DilimGuncelleAsync(long dilimId, [FromBody] DilimIstegi istek)
        => _halkGunu.DilimGuncelleAsync(dilimId, istek);

    [HttpDelete("dilim/{dilimId:long}")]
    [Izin(Izinler.HalkgunuYonet)]
    [ProducesResponseType<HalkGunuDetayDto>(StatusCodes.Status200OK)]
    public Task<HalkGunuDetayDto> DilimSilAsync(long dilimId)
        => _halkGunu.DilimSilAsync(dilimId);

    // ── bekleyenler havuzu ─────────────────────────────────────────────

    /// <summary>Bekleyenler havuzu — halk gününde görüşmek isteyenler.</summary>
    [HttpGet("basvuru")]
    [ProducesResponseType<SayfaliSonuc<BasvuruDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<BasvuruDto>> BasvuruListeAsync([FromQuery] BasvuruSuzgeci suzgec)
        => _halkGunu.BasvuruListeAsync(suzgec);

    [HttpPost("basvuru")]
    [Izin(Izinler.HalkgunuBasvuru)]
    [ProducesResponseType<BasvuruDto>(StatusCodes.Status200OK)]
    public Task<BasvuruDto> BasvuruOlusturAsync([FromBody] BasvuruIstegi istek)
        => _halkGunu.BasvuruOlusturAsync(istek);

    [HttpPut("basvuru/{id:long}")]
    [Izin(Izinler.HalkgunuBasvuru)]
    [ProducesResponseType<BasvuruDto>(StatusCodes.Status200OK)]
    public Task<BasvuruDto> BasvuruGuncelleAsync(long id, [FromBody] BasvuruIstegi istek)
        => _halkGunu.BasvuruGuncelleAsync(id, istek);

    /// <summary>
    /// Görüşmeyi UYGUN GÖRMEZ — kayıt havuzda kalır, gerekçesi yazılır.
    /// </summary>
    /// <remarks>
    /// Kişi bir güne atanmışsa o atama kaldırılır (görüşülmüş kayda
    /// dokunulmaz): ret kararı listeyi kuran kişiden sonra geldiğinde
    /// vatandaş salonda çağrılmaya devam ediyordu.
    /// </remarks>
    [HttpPost("basvuru/{id:long}/reddet")]
    [Izin(Izinler.HalkgunuBasvuru)]
    [ProducesResponseType<BasvuruDto>(StatusCodes.Status200OK)]
    public Task<BasvuruDto> BasvuruReddetAsync(long id, [FromBody] RedIstegi istek)
        => _halkGunu.BasvuruReddetAsync(id, istek.Neden);

    /// <summary>Reddedilen ya da iptal edilen kaydı havuza geri alır.</summary>
    [HttpPost("basvuru/{id:long}/geri-al")]
    [Izin(Izinler.HalkgunuBasvuru)]
    [ProducesResponseType<BasvuruDto>(StatusCodes.Status200OK)]
    public Task<BasvuruDto> BasvuruGeriAlAsync(long id)
        => _halkGunu.BasvuruGeriAlAsync(id);

    [HttpDelete("basvuru/{id:long}")]
    [Izin(Izinler.HalkgunuBasvuru)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BasvuruSilAsync(long id)
    {
        await _halkGunu.BasvuruSilAsync(id);
        return NoContent();
    }

    /// <summary>
    /// "Bu vatandaşı daha önce gördük mü?" — telefon (ve/veya ad) ile geçmiş.
    /// </summary>
    /// <remarks>
    /// Kişi eklerken ÖNCE telefon sorulur; numara girilir girilmez bu özet
    /// çıkar. Dört kaynağa birden bakar: talepler, etkinliklerdeki irtibat
    /// kişisi, önceki halk günleri ve kurum protokolü. Telefon
    /// karşılaştırması normalleştirilmiş olarak yapılır — kayıtlardaki
    /// numaralar boşluklu/bitişik/ülke kodlu karışık.
    /// </remarks>
    [HttpGet("kisi-gecmisi")]
    [ProducesResponseType<KisiGecmisiDto>(StatusCodes.Status200OK)]
    /// <param name="haricKatilim">
    /// EKRANDA AÇIK olan görüşmenin katılım kimliği; geçmişe sayılmaz.
    /// Verilmezse bütün kayıtlar sayılır (yeni vatandaş formunda böyle).
    /// </param>
    public Task<KisiGecmisiDto> KisiGecmisiAsync(
        [FromQuery] string? telefon,
        [FromQuery] string? ad,
        [FromQuery] long? haricKatilim = null)
        => _islem.KisiGecmisiAsync(telefon, ad, haricKatilim);

    /// <summary>
    /// VATANDAŞ DOSYASI — kişinin kurumla bütün geçmişi, ayrıntısıyla.
    /// </summary>
    /// <remarks>
    /// Özet ucundan ayrı: özet başvuru formunda her tuş vuruşunda çağrılıyor
    /// ve hafif kalmalı; bu uç yalnızca dosya açıldığında çağrılır ve
    /// taleplerin konusunu, durumunu, halk günü görüşme notlarını ve
    /// etkinlikleri tam listeyle döndürür.
    ///
    /// Salonda vatandaşla otururken "geçen mart kaldırım için gelmiştiniz, o
    /// iş Fen İşleri'ne havale edildi" diyebilmek için var.
    /// </remarks>
    [HttpGet("kisi-gecmisi/dosya")]
    [ProducesResponseType<KisiDosyasiDto>(StatusCodes.Status200OK)]
    public Task<KisiDosyasiDto> KisiDosyasiAsync(
        [FromQuery] string? telefon,
        [FromQuery] string? ad,
        [FromQuery] long? haricKatilim = null)
        => _islem.KisiDosyasiAsync(telefon, ad, haricKatilim);

    // ── atama ve sıralama ──────────────────────────────────────────────

    /// <summary>Havuzdaki kişileri güne (ve varsa bir dilime) atar.</summary>
    [HttpPost("{id:long}/katilim")]
    [Izin(Izinler.HalkgunuAtama)]
    [ProducesResponseType<HalkGunuDetayDto>(StatusCodes.Status200OK)]
    public Task<HalkGunuDetayDto> KatilimEkleAsync(long id, [FromBody] KatilimEkleIstegi istek)
        => _halkGunu.KatilimEkleAsync(id, istek.BasvuruIdler ?? [], istek.DilimId);

    [HttpDelete("katilim/{katilimId:long}")]
    [Izin(Izinler.HalkgunuAtama)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> KatilimCikarAsync(long katilimId)
    {
        await _halkGunu.KatilimCikarAsync(katilimId);
        return NoContent();
    }

    /// <summary>
    /// Dilim ve sıra numaralarını topluca yazar.
    /// </summary>
    /// <remarks>
    /// İstemci yeni diziyi kurup TAMAMINI gönderir; tek tek "yukarı taşı"
    /// ucu, iki kullanıcı aynı anda oynadığında sırayı bozardı.
    /// </remarks>
    [HttpPost("{id:long}/siralama")]
    [Izin(Izinler.HalkgunuAtama)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SiralamaAsync(long id, [FromBody] SiralamaIstegiDto istek)
    {
        await _halkGunu.SiralamaGuncelleAsync(id, istek.Ogeler ?? []);
        return NoContent();
    }

    // ── görüşme ────────────────────────────────────────────────────────

    /// <summary>Salondaki kayıt: geldi/görüşüldü, not, "ilgilenilecek" işareti.</summary>
    [HttpPost("katilim/{katilimId:long}/gorusme")]
    [Izin(Izinler.HalkgunuGorusme)]
    [ProducesResponseType<HalkGunuKatilimDto>(StatusCodes.Status200OK)]
    public Task<HalkGunuKatilimDto> GorusmeAsync(long katilimId, [FromBody] GorusmeIstegi istek)
        => _halkGunu.GorusmeKaydetAsync(katilimId, istek);

    // ── SMS, Excel, talep ──────────────────────────────────────────────

    /// <summary>Listedeki herkese randevu saatini içeren SMS gönderir.</summary>
    [HttpPost("{id:long}/sms")]
    [Izin(Izinler.HalkgunuSms)]
    [ProducesResponseType<HalkGunuSmsSonucu>(StatusCodes.Status200OK)]
    public Task<HalkGunuSmsSonucu> SmsAsync(long id, [FromBody] HalkGunuSmsIstegi istek)
        => _islem.SmsGonderAsync(id, istek);

    /// <summary>
    /// Günün listesi ya da tek bir grup — Excel.
    /// </summary>
    /// <remarks>
    /// <paramref name="dilimId"/> verilirse YALNIZCA o grup basılır: salonda
    /// kapıdaki görevlinin elindeki kâğıt bütün günü değil, o saatteki grubu
    /// gösteriyor.
    /// </remarks>
    [HttpGet("{id:long}/excel")]
    [Izin(Izinler.HalkgunuCiktiAl)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<FileContentResult> ExcelAsync(
        long id,
        [FromQuery] long? dilimId = null,
        [FromQuery] HalkGunuCiktiTuru tur = HalkGunuCiktiTuru.Program,
        [FromQuery] KatilimDurumu? durum = null,
        [FromQuery] bool yatay = false)
    {
        var dosya = await _cikti.ExcelAsync(id, dilimId, tur, durum, yatay);
        return File(dosya.Icerik, dosya.IcerikTuru, dosya.DosyaAdi);
    }

    /// <summary>Aynı listenin yazdırılabilir PDF hâli.</summary>
    [HttpGet("{id:long}/pdf")]
    [Izin(Izinler.HalkgunuCiktiAl)]
    [Produces("application/pdf")]
    public async Task<FileContentResult> PdfAsync(
        long id,
        [FromQuery] long? dilimId = null,
        [FromQuery] HalkGunuCiktiTuru tur = HalkGunuCiktiTuru.Program,
        [FromQuery] KatilimDurumu? durum = null,
        [FromQuery] bool yatay = false)
    {
        var dosya = await _cikti.PdfAsync(id, dilimId, tur, durum, yatay);
        return File(dosya.Icerik, dosya.IcerikTuru, dosya.DosyaAdi);
    }

    /// <summary>"İlgilenilecek" işaretli görüşmeyi talebe çevirir.</summary>
    [HttpPost("katilim/{katilimId:long}/talep")]
    [Izin(Izinler.HalkgunuTalepOlustur)]
    [ProducesResponseType<TalepSonucuDto>(StatusCodes.Status200OK)]
    public async Task<TalepSonucuDto> TalebeDonusturAsync(
        long katilimId, [FromBody] TalepOlusturIstegi istek)
        => new() { TalepId = await _islem.TalebeDonusturAsync(katilimId, istek) };

    /// <summary>
    /// Vatandaş havuzunun Excel çıktısı — ekrandaki süzgeçlerle.
    /// </summary>
    /// <remarks>
    /// Gün çıktıları (program · katılım çizelgesi · sonuç raporu) bir GÜNE
    /// ait; bu ise HAVUZUN kendisi: "bu ay kaç kişi başvurdu, kaçı geri
    /// çevrildi, hangi mahalleden geliyorlar" sorusunun cevabı gün
    /// çizelgelerinde yok.
    /// </remarks>
    [HttpGet("basvuru/excel")]
    [Izin(Izinler.HalkgunuCiktiAl)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> BasvuruExcelAsync([FromQuery] BasvuruSuzgeci suzgec)
    {
        var d = await _halkGunu.BasvuruExcelAsync(suzgec);
        return File(d.Icerik, d.IcerikTuru, d.DosyaAdi);
    }
}

// ── controller'a özel küçük gövdeler ───────────────────────────────────
//
// `ProtokolController` de sıralama isteğini böyle tanımlıyor: tek uçta
// kullanılan bir gövde için servis dosyasına gitmeye değmez.

public class KatilimEkleIstegi
{
    public long[]? BasvuruIdler { get; set; }
    public long? DilimId { get; set; }
}

public class SiralamaIstegiDto
{
    public List<SiralamaOgesiDto>? Ogeler { get; set; }
}

public class TalepSonucuDto
{
    /// <summary>Oluşan talebin kimliği — istemci oraya gidebilsin diye.</summary>
    public long TalepId { get; set; }
}
