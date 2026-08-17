using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Web.Filters;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Dto.Randevu;
using KentOS.Mini.Application.Dto.V2.Etkinlik;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Web.Services;
using KentOS.Mini.Web.Services.V2;
using KentOS.Mini.Application.Dto.ViewModels;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.AuthPolicies;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>Etkinlik işlemleri.</summary>
[Route("api/v2/etkinlik")]
// OKUMA İKİ İZİNDEN BİRİYLE.
//
// Basın kullanıcısında `ajanda.goruntule` yok, `ajanda.basinGoruntule` var.
// Sınıf düzeyinde yalnızca tam görüntüleme istendiğinde arayüz ekranı açıyor
// ama HER İSTEK 403 dönüyordu: menü ve rota izne uyuyor, uç uymuyordu.
// Yazma uçları kendi izinlerini ayrıca ilan ediyor, onlar etkilenmez.
// Listeyi daraltan süzgeç `AjandaSorguUzantilari.GorunurOlanlar` içinde.
[Izin(Izinler.AjandaGoruntule, Izinler.AjandaBasinGoruntule)]
public class EtkinlikController(
    IAjandaService _ajandaService,
    IAjandaSeriService _seriService,
    IAjandaOlayService _olayService,
    IDosyaServisi _dosyalar) : V2ControllerBase
{
    /// <summary>
    /// Birimin TÜM etkinlikleri (tarih sınırı yok).
    /// </summary>
    /// <remarks>
    /// <para>
    /// v1 karşılığı <c>GET /api/AjandaApi</c>; aynı servisi çağırır ve aynı
    /// <c>AjandaDto</c> listesini döndürür. Sayfalanmaz — mobil ekranlar
    /// listenin tamamını istiyor ve v1 davranışı da bu; sayfalamak geçişi bir
    /// adres değişikliği olmaktan çıkarır ve ekranların yeniden yazılmasını
    /// gerektirirdi.
    /// </para>
    /// <para>
    /// <b>Takvim için bunu KULLANMAYIN.</b> Bugün için 226 kayıt dönüyor ve
    /// büyümeye devam edecek; pencereye göre veri isteyen ekranlar
    /// <c>POST /takvim/aralik</c> kullanır.
    /// </para>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<IEnumerable<AjandaDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListeAsync()
        => Ok(await _ajandaService.GetAllAsync());

    /// <summary>Tek günün etkinlikleri.</summary>
    /// <remarks>
    /// v1 karşılığı <c>POST /api/AjandaApi/GetByDate</c>. <c>takvim/aralik</c>
    /// ile karıştırmayın: o, takvim çizimi için <b>özet</b> model döndürür;
    /// bu uç tam <c>AjandaDto</c> verir ve mobilin gün listesi tam kaydın
    /// alanlarına (irtibat, bilgi notu, çiçek durumu…) bakıyor.
    /// </remarks>
    [HttpPost("gune-gore")]
    [ProducesResponseType<IEnumerable<AjandaDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GuneGoreAsync([FromBody] AjandaDateSearchDto istek)
        => Ok(await _ajandaService.GetByDateAsync(istek));

    /// <summary>
    /// Etkinliğin bağlı olduğu tekrar serisinin kuralı ve özeti.
    /// </summary>
    /// <remarks>
    /// <para>
    /// v1 karşılığı <c>GET /api/AjandaApi/{id}/Seri</c>. Tek seferlik
    /// etkinlikte v1 <b>404</b> döner; v2 de öyle döner — "bu etkinlik
    /// tekrarlanan değil" bir hata değil bir cevap, ama v1'in davranışını
    /// değiştirmek geçiş sırasında iki farklı dal yazmayı gerektirirdi.
    /// </para>
    /// <para>
    /// Kuralı DÜZENLEMEK için ayrı bir uç yok: seri güncellemesi
    /// <c>PUT /etkinlik/{id}</c> gövdesindeki <c>tekrar</c> + <c>kapsam</c>
    /// alanlarıyla yapılır. Kuralı iki ayrı yoldan değiştirilebilir kılmak,
    /// birinin diğerinin değişmezlerini atlamasına açık kapı bırakırdı.
    /// </para>
    /// </remarks>
    [HttpGet("{id:long}/seri")]
    [ProducesResponseType<AjandaSeriDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SeriAsync(long id)
    {
        var seri = await _seriService.GetirAsync(id);
        if (seri is null)
        {
            return NotFound(new HataYaniti
            {
                Tur = HataTurleri.Bulunamadi,
                Baslik = "Kayıt bulunamadı",
                Durum = StatusCodes.Status404NotFound,
                Ayrinti = "Bu etkinlik tekrarlanan bir etkinlik değil.",
                Ornek = HttpContext.Request.Path.Value,
            });
        }

        return Ok(seri);
    }

    /// <summary>
    /// Etkinliğin YALNIZCA zamanını değiştirir — sürükleme ve yeniden
    /// boyutlandırma bu ucu kullanır.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Neden ayrı bir uç nokta:</b> Genel güncelleme (<c>PUT</c>) tekrar
    /// kuralını da taşıyor. İstemciler kuralı formun başlangıç tarihinden
    /// türettiği için, bir tekrarı çarşambadan perşembeye sürüklemek
    /// <c>BYDAY=TH</c> göndermeye ve sunucunun bunu "kural değişti" diye
    /// okumasına yol açıyordu: seri bölünüyor, tekrarlar yeniden üretiliyor ve
    /// kullanıcının etkinliği kaybolmuş görünüyordu.
    /// </para>
    /// <para>
    /// Bu uç nokta gövdesinde kural TAŞIMAZ; mevcut kaydı yükleyip yalnızca
    /// tarihleri ve kapsamı değiştirerek aynı <c>IAjandaService.UpdateAsync</c>
    /// akışına verir. Böylece o hata yapısal olarak imkânsız hâle gelir.
    /// </para>
    /// </remarks>
    [HttpPatch("{id:long}/zaman")]
    [Izin(Izinler.AjandaDuzenle)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ZamanAsync(long id, [FromBody] ZamanIstegi istek)
    {
        // Mevcut kaydı DTO olarak al — gizlilik ve görünürlük kontrolü burada
        // yapılır (yetkisi olmayan için EntityNotFound fırlar).
        var mevcut = await _ajandaService.GetAsync(id);

        mevcut.BaslangicTarihi = istek.Baslangic;
        mevcut.BitisTarihi = istek.Bitis;
        mevcut.Kapsam = (TekrarKapsam)istek.Kapsam;

        // KURALA DOKUNULMAZ: Tekrar null, TekrarKaldir false kalır.
        mevcut.Tekrar = null;
        mevcut.TekrarKaldir = false;

        await _ajandaService.UpdateAsync(mevcut);
        return NoContent();
    }

    // ------------------------------------------------------------ okuma

    /// <summary>Etkinlik detayı.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<AjandaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DetayAsync(long id) => Ok(await _ajandaService.GetAsync(id));

    /// <summary>Etkinliğin notları.</summary>
    [HttpGet("{id:long}/notlar")]
    [ProducesResponseType<IEnumerable<AjandaNotDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> NotlarAsync(long id) => Ok(await _ajandaService.GetNotesAsync(id));

    /// <summary>Etkinliğin fotoğrafları.</summary>
    [HttpGet("{id:long}/fotograflar")]
    [ProducesResponseType<IEnumerable<AjandaPhotoDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> FotograflarAsync(long id)
        => Ok(await _ajandaService.GetAjandaPhotosAsync(id));

    /// <summary>Etkinliğin zaman çizelgesi (kim, ne zaman, neyi değiştirdi).</summary>
    [HttpGet("{id:long}/olaylar")]
    [ProducesResponseType<IEnumerable<AjandaOlayDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> OlaylarAsync(long id)
        => Ok(await _olayService.GetirAsync(id));

    /// <summary>Etkinliğe bağlı çiçek talimatı.</summary>
    [HttpGet("{id:long}/cicek")]
    [ProducesResponseType<CicekDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CicekAsync(long id) => Ok(await _ajandaService.GetCicekAsync(id));

    /// <summary>Gelişmiş arama.</summary>
    [HttpPost("ara")]
    [ProducesResponseType<IEnumerable<AjandaDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> AraAsync([FromBody] AjandaSearchParametersDto istek)
        => Ok(await _ajandaService.SearchAsync(istek));

    // ------------------------------------------------------------ yazma

    /// <summary>Yeni etkinlik oluşturur.</summary>
    /// <remarks>
    /// Tekrarlı etkinlik için gövdede <c>tekrar</c> alanı gönderilir; sunucu
    /// seriyi kurar ve 18 aylık ufka kadar tekrarları GERÇEK kayıt olarak üretir.
    /// </remarks>
    [HttpPost]
    [Izin(Izinler.AjandaEkle)]
    [ProducesResponseType<AjandaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OlusturAsync([FromBody] AjandaDto istek)
        => Ok(await _ajandaService.CreateAsync(istek));

    /// <summary>Etkinliği günceller.</summary>
    /// <remarks>
    /// Tekrarlı bir etkinlikte <c>kapsam</c> alanı belirleyicidir. Yalnızca
    /// zaman değiştirilecekse <c>PATCH /zaman</c> kullanılmalı — bu uç nokta
    /// gövdedeki kuralı da değerlendirir.
    /// </remarks>
    [HttpPut("{id:long}")]
    [Izin(Izinler.AjandaDuzenle)]
    [ProducesResponseType<AjandaDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GuncelleAsync(long id, [FromBody] AjandaDto istek)
    {
        istek.Id = id;
        return Ok(await _ajandaService.UpdateAsync(istek));
    }

    /// <summary>Etkinliği siler.</summary>
    /// <param name="kapsam">0 = yalnızca bu, 1 = bu ve sonrakiler, 2 = tüm seri.</param>
    [HttpDelete("{id:long}")]
    [Izin(Izinler.AjandaSil)]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SilAsync(long id, [FromQuery] int kapsam = 0)
        => Ok(await _ajandaService.DeleteAsync(id, (TekrarKapsam)kapsam));

    /// <summary>Etkinliğe not ekler.</summary>
    [HttpPost("{id:long}/not")]
    [Izin(Izinler.AjandaNotEkle)]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<IActionResult> NotEkleAsync(long id, [FromBody] AjandaNotDto istek)
    {
        istek.AjandaId = id;
        return Ok(await _ajandaService.CreateNoteAsync(istek));
    }

    /// <summary>Etkinliği erteler.</summary>
    [HttpPost("ertele")]
    [Izin(Izinler.AjandaDuzenle)]
    [ProducesResponseType<AjandaDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ErteleAsync([FromBody] AjandaErteleDto istek)
        => Ok(await _ajandaService.PostponeAsync(istek));

    /// <summary>Etkinliği başka birime havale eder.</summary>
    /// <remarks>Gizli etkinlikler havale EDİLEMEZ; sunucu reddeder.</remarks>
    [HttpPost("havale")]
    [Izin(Izinler.AjandaHavale)]
    [ProducesResponseType<AjandaDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> HavaleAsync([FromBody] AjandaHavaleDto istek)
        => Ok(await _ajandaService.HavaleAsync(istek));

    /// <summary>Üst birime gönderir.</summary>
    [HttpPost("{id:long}/ust-birime-gonder")]
    [Izin(Izinler.AjandaHavale)]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UstBirimeAsync(long id) => Ok(await _ajandaService.SendToParent(id));

    /// <summary>Statü değiştirir (beklemede / tamamlandı / iptal).</summary>
    [HttpPost("statu")]
    [Izin(Izinler.AjandaStatuDegistir)]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<IActionResult> StatuAsync([FromBody] AjandaChangeStateDto istek)
        => Ok(await _ajandaService.ChangeStateAsync(istek));

    /// <summary>Etkinlik tipini değiştirir.</summary>
    [HttpPost("{id:long}/tip/{tipId:long}")]
    [Izin(Izinler.AjandaDuzenle)]
    [ProducesResponseType<AjandaDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TipAsync(long id, long tipId)
        => Ok(await _ajandaService.ChangeTipId(id, tipId));

    /// <summary>Etkinlik durumunu değiştirir.</summary>
    [HttpPost("{id:long}/durum/{durumId:long}")]
    [Izin(Izinler.AjandaDuzenle)]
    [ProducesResponseType<AjandaDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DurumAsync(long id, long durumId)
        => Ok(await _ajandaService.ChangeDurumId(id, durumId));

    /// <summary>Çiçek talimatı oluşturur.</summary>
    /// <remarks>Gizli etkinlikte çiçek talimatı ÇIKMAZ (dış çiçekçiye SMS giderdi).</remarks>
    [HttpPost("cicek")]
    [Izin(Izinler.CicekYonet)]
    [ProducesResponseType<AjandaDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CicekGonderAsync([FromBody] AjandaCicekGonderDto istek)
        => Ok(await _ajandaService.CicekGonderAsync(istek));

    /// <summary>Çiçek talimatını iptal eder.</summary>
    [HttpDelete("{id:long}/cicek")]
    [Izin(Izinler.CicekYonet)]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CicekIptalAsync(long id)
        => Ok(await _ajandaService.DeleteCicekAsync(id));

    /// <summary>Birime SMS gönderir.</summary>
    /// <remarks>
    /// <para>Gizli etkinlikte birim SMS'i GÖNDERİLMEZ.</para>
    /// <para>
    /// Yanıt SAYILARLA döner (v1 <c>true</c> döndürüyordu): kaç kişiye
    /// yazıldı, kimin telefonu eksik, hangi birim boş. "Gönderdim ama
    /// gitmedi" şikâyetinin sebebi çoğu zaman bu üçünden biri ve hiçbiri
    /// ekranda görünmüyordu.
    /// </para>
    /// </remarks>
    [HttpPost("sms")]
    [Izin(Izinler.AjandaSmsGonder)]
    [ProducesResponseType<SmsGonderimSonucuDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SmsAsync([FromBody] SendSmsToBirimDto istek)
        => Ok(await _ajandaService.SendSmsToBirimDetayliAsync(istek));

    // ------------------------------------------------------- fotoğraf

    /// <summary>Etkinliğe fotoğraf yükler (çoklu).</summary>
    /// <remarks>
    /// En fazla 5 MB, yalnızca JPEG/PNG/WEBP. Hiçbiri kabul edilmezse
    /// <b>400 döner</b> — eski uç sessizce başarı bildiriyordu.
    /// Gizli etkinlikte birim bildirimi GÖNDERİLMEZ.
    /// </remarks>
    [HttpPost("{id:long}/fotograflar")]
    [Izin(Izinler.AjandaFotografEkle)]
    [ProducesResponseType<List<AjandaPhotoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataYaniti>(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> FotografYukleAsync(long id)
        => Ok(await _dosyalar.EtkinlikFotografiYukleAsync(id, Request.Form.Files));

    /// <summary>Fotoğrafı siler.</summary>
    [HttpDelete("fotograf/{fotografId:long}")]
    [Izin(Izinler.AjandaFotografEkle)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> FotografSilAsync(long fotografId)
    {
        await _dosyalar.EtkinlikFotografiSilAsync(fotografId);
        return NoContent();
    }

    // -------------------------------------------------------- silinmiş

    /// <summary>Silinmiş etkinlikler (geri alma listesi).</summary>
    /// <remarks>
    /// <para>
    /// Gizlilik ve birim süzgeci servis içinde uygulanır — silinmiş kayıtlar
    /// global soft-delete süzgecini atladığı için burada ekstra dikkat gerekir.
    /// </para>
    /// <para>
    /// <b><paramref name="gun"/> ile SİLİNME tarihine göre sınırlanır</b>
    /// (varsayılan 30). Uç bir dönem tüm silme geçmişini döndürüyordu ve
    /// ekranda yıllar öncesinin kayıtları bugünkülerle karışıyordu: liste
    /// "geri alma" işine yaramıyor, alakasız görünüyordu. Sınır SİLİNME
    /// tarihine göre çünkü bu bir çöp kutusu; merak edilen "ne zaman
    /// yapılacaktı" değil, "ne zaman silindi".
    /// </para>
    /// <para>
    /// <c>gun=0</c> sınırı kaldırır — eski bir kaydı bilerek arayan için.
    /// </para>
    /// </remarks>
    [HttpGet("silinmis")]
    [ProducesResponseType<SayfaliSonuc<EtkinlikOzetDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<EtkinlikOzetDto>> SilinmislerAsync(
        [FromQuery] SayfaIstegi sayfa,
        [FromQuery] int gun = 30)
    {
        var sinir = gun > 0 ? DateTime.Now.Date.AddDays(-gun) : (DateTime?)null;

        // Silinme anı, soft-delete `GuncellemeTarihi`ne yazıldığı için ondan
        // okunuyor; ayrı bir sütun yok ve eklemek canlıdaki kayıtları geriye
        // dönük dolduramazdı.
        var liste = (await _ajandaService.GetDeletedAsync())
            .Select(a => (Kayit: a, Silinme: a.GuncellemeTarihi ?? a.OlusturmaTarihi))
            .Where(x => sinir == null || x.Silinme >= sinir.Value)
            .OrderByDescending(x => x.Silinme)
            .Select(x =>
            {
                var ozet = Ozete(x.Kayit);
                ozet.SilinmeTarihi = x.Silinme;
                return ozet;
            })
            .ToList();

        if (sayfa.TemizArama is { } ara)
        {
            liste = liste
                .Where(a => a.Baslik.Contains(ara, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return SayfaliSonuc<EtkinlikOzetDto>.Bellekten(liste, sayfa);
    }

    /// <summary>Bugünden itibaren tüm etkinlikler.</summary>
    [HttpGet("bugunden")]
    [ProducesResponseType<SayfaliSonuc<EtkinlikOzetDto>>(StatusCodes.Status200OK)]
    public async Task<SayfaliSonuc<EtkinlikOzetDto>> BugundenAsync([FromQuery] SayfaIstegi sayfa)
        => SayfaliSonuc<EtkinlikOzetDto>.Bellekten(
            (await _ajandaService.GetAllFromTodayAsync()).Select(Ozete).ToList(), sayfa);

    /// <summary>
    /// Varlığı liste özetine çevirir.
    /// </summary>
    /// <remarks>
    /// Mapster KULLANILMIYOR: <c>Ajanda ⇄ AjandaNot / Cicek</c> döngüsü bir
    /// kez tüm API sürecini <c>StackOverflowException</c> ile düşürmüştü.
    /// Elle yazılan bu eşleme yalnızca ihtiyaç duyulan alanları alır ve
    /// gezinme özelliklerine dokunmaz.
    /// </remarks>
    private static EtkinlikOzetDto Ozete(Application.Models.Ajanda a) => new()
    {
        Id = a.Id,
        Baslik = a.Baslik,
        Baslangic = a.BaslangicTarihi,
        Bitis = a.BitisTarihi,
        TumGun = a.TumGun,
        Konum = a.Konum,
        TipId = a.RandevuTipId,
        TipAd = a.RandevuTip?.Ad,
        TipRenk = a.RandevuTip?.Renk,
        DurumId = a.DurumId,
        DurumAd = a.Durum?.Ad,
        DurumRenk = a.Durum?.Renk,
        Statu = (int)a.Status,
        Gizli = a.Gizli,
        SeriId = a.SeriId,
        SeriAyrik = a.SeriAyrik,
        ResimVar = a.ResimVar,
        BasinKatilsin = a.BasinKatilsin,
        BirimId = a.BirimId,
        BirimAd = a.Birim?.Ad,
    };
}
