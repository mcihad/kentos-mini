using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Web.Filters;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Dto.Randevu;
using KentOS.Mini.Application.Dto.ViewModels;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Dto.V2.Talep;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>
/// Vatandaş talepleri (randevu).
///
/// <para>
/// Tüm iş mantığı mevcut <see cref="IRandevuService"/>'te; v2 yalnızca daha
/// düzenli bir yüzey sunar. İkinci bir yazma yolu AÇILMAZ.
/// </para>
/// </summary>
[Route("api/v2/talep")]
[Izin(Izinler.TalepGoruntule)]
public class TalepController(
    IRandevuService _talepler,
    ITalepSorguServisi _sorgu,
    IDosyaServisi _dosyalar,
    IHaritaServisi _harita) : V2ControllerBase
{
    /// <summary>
    /// Talep listesi — sayfalı, süzgeçli, aranabilir.
    /// </summary>
    /// <remarks>
    /// Eski <c>?altBirimlerDahil=</c>, <c>/arsiv</c> ve <c>/durum/{id}</c>
    /// uçlarının hepsi buraya toplandı: aynı listeye üç ayrı yol açmak,
    /// birinde süzgeç eklenip diğerinde unutulmasına yol açıyordu.
    /// <c>arsiv=true</c>, <c>durumId=</c>, <c>tipId=</c> ile aynı sonuç alınır.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<SayfaliSonuc<TalepOzetDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<TalepOzetDto>> ListeAsync([FromQuery] TalepSuzgeci suzgec)
        => _sorgu.ListeAsync(suzgec);

    /// <summary>
    /// Durum başına talep sayıları — filtre çipleri ve ana sayfa kartları.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sıfır kayıtlı durumlar da döner; çipin listeden kaybolması "böyle bir
    /// durum yok" izlenimi verirdi. v1 yalnızca dolu grupları döndürür.
    /// </para>
    /// <para>
    /// <b><c>arsiv</c> parametresi kritiktir.</b> v1'in
    /// <c>RandevuApi/CountByDurum</c> ucu YALNIZCA ARŞİVLENMİŞ talepleri
    /// sayar (<c>RandevuService.GetCountByDurum</c> → <c>r.Arsivlendi</c>) —
    /// mobil bu sayaçları Arşiv sekmesinde kullandığı için bu kasıtlı.
    /// Varsayılan <c>false</c> ise AKTİF talepleri sayar; SPA'nın filtre
    /// çipleri bunu ister. <c>arsiv=true</c> v1 ile birebir aynı kümedir.
    /// </para>
    /// </remarks>
    [HttpGet("durum-sayaclari")]
    [ProducesResponseType<List<DurumSayaciDto>>(StatusCodes.Status200OK)]
    public Task<List<DurumSayaciDto>> DurumSayaclariAsync(
        [FromQuery] bool altBirimlerDahil = false,
        [FromQuery] bool arsiv = false,
        [FromQuery] bool? ajandayaEklendi = null)
        => _sorgu.DurumSayaclariAsync(altBirimlerDahil, arsiv, ajandayaEklendi);

    /// <summary>Tip başına talep sayıları.</summary>
    /// <remarks>
    /// v1 karşılığı <c>GET /api/RandevuApi/CountByTip</c> — o da yalnızca
    /// arşivlenmişleri sayar, dolayısıyla birebir eşleşme için
    /// <c>arsiv=true</c> geçilmelidir. Ayrıntı için durum sayaçlarına bakın.
    /// </remarks>
    [HttpGet("tip-sayaclari")]
    [ProducesResponseType<List<DurumSayaciDto>>(StatusCodes.Status200OK)]
    public Task<List<DurumSayaciDto>> TipSayaclariAsync(
        [FromQuery] bool altBirimlerDahil = false, [FromQuery] bool arsiv = false)
        => _sorgu.TipSayaclariAsync(altBirimlerDahil, arsiv);

    /// <summary>Toplam talep sayısı.</summary>
    [HttpGet("sayi")]
    [ProducesResponseType<long>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SayiAsync() => Ok(await _talepler.CountAsync());

    /// <summary>Talep detayı.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<RandevuDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DetayAsync(long id) => Ok(await _talepler.GetByIdAsync(id));

    [HttpGet("{id:long}/notlar")]
    [ProducesResponseType<SayfaliSonuc<RandevuNotDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<RandevuNotDto>> NotlarAsync(long id, [FromQuery] SayfaIstegi sayfa)
        => SayfaliSonuc<RandevuNotDto>.Bellekten(await _talepler.GetAllNotAsync(id), sayfa);

    [HttpGet("{id:long}/dosyalar")]
    [ProducesResponseType<SayfaliSonuc<RandevuDosyaDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<RandevuDosyaDto>> DosyalarAsync(long id, [FromQuery] SayfaIstegi sayfa)
        => SayfaliSonuc<RandevuDosyaDto>.Bellekten(await _talepler.GetAllDosyaAsync(id), sayfa);

    /// <summary>Talebin hareket geçmişi (kim, ne zaman, ne yaptı).</summary>
    [HttpGet("{id:long}/hareketler")]
    [ProducesResponseType<SayfaliSonuc<RandevuHareketDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<RandevuHareketDto>> HareketlerAsync(long id, [FromQuery] SayfaIstegi sayfa)
        => SayfaliSonuc<RandevuHareketDto>.Bellekten(await _talepler.GetAllHareketAsync(id), sayfa);

    /// <summary>Gelişmiş arama (mobilin kullandığı parametre kümesiyle).</summary>
    /// <remarks>
    /// Gündelik arama için <c>GET /talep?ara=</c> yeterli ve daha hızlıdır;
    /// bu uç, tarih/birim/tip kombinasyonu gerektiren raporlama içindir.
    /// Servis belleğe çektiği için sonuç burada sayfalanır.
    /// </remarks>
    [HttpPost("ara")]
    [ProducesResponseType<SayfaliSonuc<RandevuDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<RandevuDto>> AraAsync(
        [FromBody] RandevuSearchParametersDto istek,
        [FromQuery] SayfaIstegi sayfa)
        => SayfaliSonuc<RandevuDto>.Bellekten(await _talepler.SearchAsync(istek), sayfa);

    [HttpPost]
    [Izin(Izinler.TalepEkle)]
    [ProducesResponseType<RandevuDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> OlusturAsync([FromBody] RandevuDto istek)
        => Ok(await _talepler.CreateAsync(istek));

    [HttpPut("{id:long}")]
    [Izin(Izinler.TalepDuzenle)]
    [ProducesResponseType<RandevuDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GuncelleAsync(long id, [FromBody] RandevuDto istek)
    {
        istek.Id = id;
        return Ok(await _talepler.UpdateAsync(istek));
    }

    [HttpPost("{id:long}/not")]
    [Izin(Izinler.TalepNotEkle)]
    [ProducesResponseType<RandevuNotDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> NotEkleAsync(long id, [FromBody] RandevuNotDto istek)
        => Ok(await _talepler.CreateNotAsync(id, istek));

    [HttpPost("havale")]
    [Izin(Izinler.TalepHavale)]
    [ProducesResponseType<RandevuDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> HavaleAsync([FromBody] RandevuHavaleDto istek)
        => Ok(await _talepler.CreateHavaleAsync(istek));

    [HttpPost("{id:long}/ust-birime-gonder")]
    [Izin(Izinler.TalepHavale)]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UstBirimeAsync(long id) => Ok(await _talepler.SendToParentAsync(id));

    [HttpPost("{id:long}/durum/{durumId:long}")]
    [Izin(Izinler.TalepDurumDegistir)]
    [ProducesResponseType<RandevuDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DurumDegistirAsync(long id, long durumId)
        => Ok(await _talepler.ChangeDurumAsync(id, durumId));

    [HttpPost("{id:long}/tip/{tipId:long}")]
    [Izin(Izinler.TalepDuzenle)]
    [ProducesResponseType<RandevuDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TipDegistirAsync(long id, long tipId)
        => Ok(await _talepler.ChangeTipAsync(id, tipId));

    /// <summary>Talebi ajandaya (etkinliğe) dönüştürür.</summary>
    /// <remarks>
    /// <para>
    /// Oluşan <b>etkinliğin kimliğini</b> döner ki istemci doğrudan oraya
    /// gidebilsin. v1 karşılığı yalnızca <c>true/false</c> döndürüyor,
    /// kullanıcı da "eklendi" mesajından sonra etkinliği elle arıyordu.
    /// </para>
    /// <para>
    /// <c>baslangicTarih</c> serbesttir — talep ileri bir tarihe/saate
    /// eklenebilir.
    /// </para>
    /// </remarks>
    [HttpPost("ajandaya-ekle")]
    [Izin(Izinler.TalepAjandayaEkle)]
    [ProducesResponseType<AjandayaEklendiDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> AjandayaEkleAsync([FromBody] RandevuToAjandaDto istek)
        => Ok(new AjandayaEklendiDto { EtkinlikId = await _talepler.TalebiEtkinligeCevirAsync(istek) });

    [HttpPost("{id:long}/arsivle")]
    [Izin(Izinler.TalepArsivle)]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ArsivleAsync(long id) => Ok(await _talepler.AddToArchiveAsync(id));

    // ---------------------------------------------------------- dosya

    /// <summary>Talebe dosya yükler.</summary>
    /// <remarks>
    /// En fazla 20 MB. Çalıştırılabilir uzantılar reddedilir — dosyalar
    /// <c>wwwroot</c> altında statik sunuluyor ve indirilip çalıştırılabilir.
    /// </remarks>
    [HttpPost("{id:long}/dosya")]
    [Izin(Izinler.TalepDosyaYukle)]
    [ProducesResponseType<RandevuDosyaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> DosyaYukleAsync(long id, [FromForm] string? aciklama)
    {
        var dosya = Request.Form.Files.FirstOrDefault();
        return Ok(await _dosyalar.TalepDosyasiYukleAsync(id, dosya!, aciklama));
    }

    /// <summary>Başvuranın özgeçmişini yükler.</summary>
    /// <remarks>
    /// <para>
    /// Genel talep dosyalarından AYRI: özgeçmiş <c>Randevu</c> üzerinde kendi
    /// alanına yazılır (<c>OzgecmisDurum</c>/<c>OzgecmisDosya</c>) ve iş
    /// talebi ekranında ayrı gösterilir. Aynı uca yığmak, "bu talebin
    /// özgeçmişi var mı" sorusunu dosya listesini tarayarak cevaplamayı
    /// gerektirirdi.
    /// </para>
    /// <para>
    /// v1 karşılığı <c>POST /api/RandevuApi/{id}/UploadOzgecmis</c>; aynı
    /// servisi çağırır, gövde şekli de aynı (<c>multipart/form-data</c>,
    /// alan adı <c>ozgecmis</c>).
    /// </para>
    /// </remarks>
    [HttpPost("{id:long}/ozgecmis")]
    [Izin(Izinler.TalepDosyaYukle)]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [ProducesResponseType<RandevuDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OzgecmisYukleAsync(long id)
    {
        // Çok parçalı değilse `Request.Form` erişimi InvalidDataException atıp
        // 500 üretiyor; bu bir istemci hatası, 400 olmalı.
        if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
        {
            return BadRequest(new HataYaniti
            {
                Tur = HataTurleri.Dogrulama,
                Baslik = "Doğrulama hatası",
                Durum = StatusCodes.Status400BadRequest,
                Ayrinti = "Özgeçmiş çok parçalı (multipart/form-data) gönderilmelidir.",
                Ornek = HttpContext.Request.Path.Value,
            });
        }

        var dosya = Request.Form.Files[0];
        using var bellek = new MemoryStream();
        await dosya.CopyToAsync(bellek);
        bellek.Position = 0;

        var icerik = new MultipartFormDataContent
        {
            { new StreamContent(bellek), "ozgecmis", dosya.FileName },
        };

        return Ok(await _talepler.UploadOzgecmisAsync(id, icerik));
    }

    /// <summary>Talep dosyasını indirir.</summary>
    /// <remarks>
    /// Statik <c>/uploads/...</c> yolu v1 ve eski MVC için duruyor; yeni
    /// istemciler bu ucu kullanır ve dosya <b>birim süzgecinden</b> geçer.
    /// Mobilde adresi elle kurup tarayıcıya açmak, jeton taşımadığı için
    /// yalnızca statik yolun açık olması sayesinde çalışıyordu.
    /// </remarks>
    [HttpGet("dosya/{dosyaId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DosyaIndirAsync(long dosyaId)
    {
        var (akis, ad, tur) = await _dosyalar.TalepDosyasiAsync(dosyaId);
        return File(akis, tur, ad);
    }

    /// <summary>Talep dosyasını siler.</summary>
    [HttpDelete("dosya/{dosyaId:long}")]
    [Izin(Izinler.TalepDosyaYukle)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DosyaSilAsync(long dosyaId)
    {
        await _dosyalar.TalepDosyasiSilAsync(dosyaId);
        return NoContent();
    }

    /// <summary>Arşivden çıkarır.</summary>
    [HttpPost("{id:long}/arsivden-cikar")]
    [Izin(Izinler.TalepArsivle)]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ArsivdenCikarAsync(long id)
        => Ok(await _talepler.RemoveFromArchiveAsync(id));

    /// <summary>Talebi siler.</summary>
    [HttpDelete("{id:long}")]
    [Izin(Izinler.TalepSil)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SilAsync(long id)
    {
        await _talepler.DeleteAsync(id);
        return NoContent();
    }

    // ---------------------------------------------------------- harita

    /// <summary>Koordinatı olan talepler — harita ekranı.</summary>
    /// <remarks>
    /// Sayfalama YOK: harita tüm işaretçileri bir arada ister, sayfalanmış
    /// bir harita yanıltıcı olur. Bunun yerine tarih aralığı ve arşiv
    /// süzgeciyle sınırlanır ve bozuk koordinatlar sunucuda elenir.
    /// </remarks>
    [HttpGet("harita")]
    [ProducesResponseType<List<HaritaNoktasiDto>>(StatusCodes.Status200OK)]
    public Task<List<HaritaNoktasiDto>> HaritaAsync(
        [FromQuery] bool arsivDahil = false,
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => _harita.NoktalarAsync(arsivDahil, baslangic, bitis);

    /// <summary>Halk günü kayıtları.</summary>
    /// <remarks>Aralık verilmezse son 3 ay.</remarks>
    [HttpGet("halk-gunleri")]
    [ProducesResponseType<SayfaliSonuc<HalkGunuDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<HalkGunuDto>> HalkGunleriAsync(
        [FromQuery] SayfaIstegi sayfa,
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null)
        => _harita.HalkGunleriAsync(baslangic, bitis, sayfa);
}
