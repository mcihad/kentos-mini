using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Web.Services.V2;

// ══════════════════════════════════════════════════════════════════ DTO'lar
//
// DTO'lar servis dosyasının içinde: `DavetServisi` ve `ProtokolServisi` de
// böyle. Modül dosyaları arasında gezinmek yerine sözleşme ile uygulama yan
// yana duruyor.

public class HalkGunuOzetDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("tarih")] public DateTime Tarih { get; set; }
    [JsonPropertyName("baslik")] public string? Baslik { get; set; }
    [JsonPropertyName("konum")] public string? Konum { get; set; }
    [JsonPropertyName("durum")] public HalkGunuDurumu Durum { get; set; }
    [JsonPropertyName("durumAd")] public string DurumAd { get; set; } = string.Empty;

    [JsonPropertyName("dilimSayisi")] public int DilimSayisi { get; set; }
    [JsonPropertyName("kisiSayisi")] public int KisiSayisi { get; set; }
    [JsonPropertyName("gorusulenSayisi")] public int GorusulenSayisi { get; set; }
    [JsonPropertyName("gelmeyenSayisi")] public int GelmeyenSayisi { get; set; }

    /// <summary>"İlgilenilecek" diye işaretlenmiş görüşme sayısı.</summary>
    [JsonPropertyName("takipSayisi")] public int TakipSayisi { get; set; }
}

public class HalkGunuDilimDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("baslangic")] public DateTime Baslangic { get; set; }
    [JsonPropertyName("bitis")] public DateTime Bitis { get; set; }
    [JsonPropertyName("baslik")] public string? Baslik { get; set; }
    [JsonPropertyName("kapasite")] public int? Kapasite { get; set; }
    [JsonPropertyName("siraNo")] public int SiraNo { get; set; }
    [JsonPropertyName("kisiler")] public List<HalkGunuKatilimDto> Kisiler { get; set; } = [];
}

public class HalkGunuKatilimDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("basvuruId")] public long BasvuruId { get; set; }
    [JsonPropertyName("dilimId")] public long? DilimId { get; set; }
    [JsonPropertyName("siraNo")] public int SiraNo { get; set; }

    [JsonPropertyName("adSoyad")] public string AdSoyad { get; set; } = string.Empty;
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }
    [JsonPropertyName("mahalleAd")] public string? MahalleAd { get; set; }
    [JsonPropertyName("konu")] public string? Konu { get; set; }

    [JsonPropertyName("durum")] public KatilimDurumu Durum { get; set; }
    [JsonPropertyName("durumAd")] public string DurumAd { get; set; } = string.Empty;
    [JsonPropertyName("gorusmeNotu")] public string? GorusmeNotu { get; set; }
    [JsonPropertyName("gorusmeTarihi")] public DateTime? GorusmeTarihi { get; set; }
    [JsonPropertyName("degerlendirmeyeEsas")] public bool DegerlendirmeyeEsas { get; set; }
    [JsonPropertyName("degerlendirmeNotu")] public string? DegerlendirmeNotu { get; set; }
    [JsonPropertyName("olusanRandevuId")] public long? OlusanRandevuId { get; set; }
    [JsonPropertyName("smsTarihi")] public DateTime? SmsTarihi { get; set; }
}

public class HalkGunuDetayDto : HalkGunuOzetDto
{
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("dilimler")] public List<HalkGunuDilimDto> Dilimler { get; set; } = [];

    /// <summary>Güne alınmış ama henüz bir dilime atanmamış kişiler.</summary>
    [JsonPropertyName("atanmamislar")] public List<HalkGunuKatilimDto> Atanmamislar { get; set; } = [];
}

public class HalkGunuIstegi
{
    [JsonPropertyName("tarih")] public DateTime Tarih { get; set; }
    [JsonPropertyName("baslik")] public string? Baslik { get; set; }
    [JsonPropertyName("konum")] public string? Konum { get; set; }
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("durum")] public HalkGunuDurumu? Durum { get; set; }
}

/// <summary>
/// Dilim isteği — tek dilim ya da <b>toplu üretim</b>.
/// </summary>
/// <remarks>
/// <see cref="DilimDakika"/> doluysa <see cref="Baslangic"/>–<see cref="Bitis"/>
/// arası o uzunlukta dilimlere bölünür (14:00–15:00 / 10 dk → altı dilim).
/// Boşsa tek dilim üretilir. Sekreterin on dilimi tek tek girmesi işin en
/// sıkıcı kısmıydı.
/// </remarks>
public class DilimIstegi
{
    [JsonPropertyName("baslangic")] public DateTime Baslangic { get; set; }
    [JsonPropertyName("bitis")] public DateTime Bitis { get; set; }
    [JsonPropertyName("baslik")] public string? Baslik { get; set; }
    [JsonPropertyName("kapasite")] public int? Kapasite { get; set; }
    [JsonPropertyName("dilimDakika")] public int? DilimDakika { get; set; }
}

public class BasvuruDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("soyad")] public string? Soyad { get; set; }
    [JsonPropertyName("adSoyad")] public string AdSoyad { get; set; } = string.Empty;
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }
    [JsonPropertyName("mahalleId")] public long? MahalleId { get; set; }
    [JsonPropertyName("mahalleAd")] public string? MahalleAd { get; set; }
    [JsonPropertyName("meslek")] public string? Meslek { get; set; }
    [JsonPropertyName("konu")] public string? Konu { get; set; }
    [JsonPropertyName("not")] public string? Not { get; set; }
    [JsonPropertyName("durum")] public BasvuruDurumu Durum { get; set; }
    [JsonPropertyName("durumAd")] public string DurumAd { get; set; } = string.Empty;
    [JsonPropertyName("randevuId")] public long? RandevuId { get; set; }
    [JsonPropertyName("redNedeni")] public string? RedNedeni { get; set; }
    [JsonPropertyName("redTarihi")] public DateTime? RedTarihi { get; set; }
    [JsonPropertyName("olusturmaTarihi")] public DateTime OlusturmaTarihi { get; set; }

    /// <summary>Hangi halk günlerine atandı (tarih listesi).</summary>
    [JsonPropertyName("halkGunleri")] public List<DateTime> HalkGunleri { get; set; } = [];
}

public class BasvuruIstegi
{
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("soyad")] public string? Soyad { get; set; }
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }
    [JsonPropertyName("mahalleId")] public long? MahalleId { get; set; }
    [JsonPropertyName("meslek")] public string? Meslek { get; set; }
    [JsonPropertyName("konu")] public string? Konu { get; set; }
    [JsonPropertyName("not")] public string? Not { get; set; }
    [JsonPropertyName("durum")] public BasvuruDurumu? Durum { get; set; }
    [JsonPropertyName("randevuId")] public long? RandevuId { get; set; }
}

public class GorusmeIstegi
{
    [JsonPropertyName("durum")] public KatilimDurumu? Durum { get; set; }
    [JsonPropertyName("gorusmeNotu")] public string? GorusmeNotu { get; set; }
    [JsonPropertyName("degerlendirmeyeEsas")] public bool? DegerlendirmeyeEsas { get; set; }
    [JsonPropertyName("degerlendirmeNotu")] public string? DegerlendirmeNotu { get; set; }
}

public class SiralamaOgesiDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("dilimId")] public long? DilimId { get; set; }
    [JsonPropertyName("siraNo")] public int SiraNo { get; set; }
}

public class HalkGunuSuzgeci : SayfaIstegi
{
    [JsonPropertyName("baslangic")] public DateTime? Baslangic { get; set; }
    [JsonPropertyName("bitis")] public DateTime? Bitis { get; set; }
    [JsonPropertyName("durum")] public HalkGunuDurumu? Durum { get; set; }
}

public class BasvuruSuzgeci : SayfaIstegi
{
    [JsonPropertyName("durum")] public BasvuruDurumu? Durum { get; set; }

    /// <summary>
    /// Yalnızca hiçbir güne atanmamışlar — atama ekranının varsayılanı.
    /// </summary>
    /// <remarks>
    /// REDDEDİLENLER bu kümeye girmez: "bekleyenler" listesi atanacak kişileri
    /// gösteriyor, geri çevrilmiş bir kayıt orada durursa yanlışlıkla yeniden
    /// atanır.
    /// </remarks>
    [JsonPropertyName("atanmamis")] public bool Atanmamis { get; set; }
}

// ═══════════════════════════════════════════════════════════════ arayüz

public interface IHalkGunuServisi
{
    Task<SayfaliSonuc<HalkGunuOzetDto>> ListeAsync(HalkGunuSuzgeci suzgec);
    Task<HalkGunuDetayDto> DetayAsync(long id);
    Task<HalkGunuDetayDto> OlusturAsync(HalkGunuIstegi istek);
    Task<HalkGunuDetayDto> GuncelleAsync(long id, HalkGunuIstegi istek);
    Task SilAsync(long id);

    Task<HalkGunuDetayDto> DilimEkleAsync(long halkGunuId, DilimIstegi istek);
    Task<HalkGunuDetayDto> DilimGuncelleAsync(long dilimId, DilimIstegi istek);
    Task<HalkGunuDetayDto> DilimSilAsync(long dilimId);

    Task<SayfaliSonuc<BasvuruDto>> BasvuruListeAsync(BasvuruSuzgeci suzgec);

    /// <summary>Vatandaş havuzunun Excel çıktısı — ekrandaki süzgeçlerle.</summary>
    Task<DisaAktarmaDosyasi> BasvuruExcelAsync(BasvuruSuzgeci suzgec);
    Task<BasvuruDto> BasvuruOlusturAsync(BasvuruIstegi istek);
    Task<BasvuruDto> BasvuruGuncelleAsync(long id, BasvuruIstegi istek);
    Task<BasvuruDto> BasvuruReddetAsync(long id, string? neden);
    Task<BasvuruDto> BasvuruGeriAlAsync(long id);
    Task BasvuruSilAsync(long id);

    Task<HalkGunuDetayDto> KatilimEkleAsync(long halkGunuId, long[] basvuruIdler, long? dilimId);
    Task KatilimCikarAsync(long katilimId);
    Task SiralamaGuncelleAsync(long halkGunuId, IReadOnlyList<SiralamaOgesiDto> ogeler);
    Task<HalkGunuKatilimDto> GorusmeKaydetAsync(long katilimId, GorusmeIstegi istek);
}

// ══════════════════════════════════════════════════════════════ uygulama

/// <summary>
/// HALK GÜNÜ — gün, zaman dilimleri, bekleyenler havuzu ve görüşme kayıtları.
/// </summary>
/// <remarks>
/// <para>
/// Üç ayrı kullanıcıya hizmet ediyor ve tasarım bu üçlüden çıktı:
/// <b>sekreter</b> günü kurar ve listeyi doldurur, <b>salondaki personel</b>
/// tabletten sırayla ilerleyip not tutar, <b>başkan</b> özeti okur.
/// </para>
/// <para>
/// <b>Birim izolasyonu:</b> halk günleri oluşturan kullanıcının birimine ait —
/// etkinlik ve davetteki kuralla aynı.
/// </para>
/// <para>
/// Kişi bilgisi katılım kaydına KOPYALANMAZ, başvurudan okunur: vatandaş
/// telefonunu düzelttirdiğinde eski katılım kayıtları da güncel kalır.
/// </para>
/// </remarks>
public class HalkGunuServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici) : IHalkGunuServisi
{
    // ── görünürlük kapıları ────────────────────────────────────────────

    /// <summary>GÜN kapısı — her sorgu buradan başlamalı.</summary>
    private IQueryable<HalkGunu> GorunurGunler() =>
        _context.HalkGunleri
            .AsNoTracking()
            .Where(h => h.BirimId == _kullanici.GetCurrentBirimId());

    /// <summary>BAŞVURU kapısı — havuz da birime ait.</summary>
    private IQueryable<HalkGunuBasvuru> GorunurBasvurular() =>
        _context.HalkGunuBasvurulari
            .AsNoTracking()
            .Where(b => b.BirimId == _kullanici.GetCurrentBirimId());

    /// <summary>Yazma yolu — izleme AÇIK, bulunamazsa 404.</summary>
    private async Task<HalkGunu> GunBulAsync(long id) =>
        await _context.HalkGunleri
            .FirstOrDefaultAsync(h => h.Id == id
                                   && h.BirimId == _kullanici.GetCurrentBirimId())
        ?? throw new EntityNotFoundException("Halk günü bulunamadı.");

    // ── gün ────────────────────────────────────────────────────────────

    public Task<SayfaliSonuc<HalkGunuOzetDto>> ListeAsync(HalkGunuSuzgeci suzgec)
    {
        var sorgu = GorunurGunler();

        if (suzgec.Baslangic is { } bas) sorgu = sorgu.Where(h => h.Tarih >= bas.Date);
        if (suzgec.Bitis is { } bit) sorgu = sorgu.Where(h => h.Tarih <= bit.Date);
        if (suzgec.Durum is { } d) sorgu = sorgu.Where(h => h.Durum == d);

        if (suzgec.TemizArama is { } ara)
        {
            sorgu = sorgu.Where(h =>
                (h.Baslik != null && EF.Functions.ILike(h.Baslik, $"%{ara}%")) ||
                (h.Konum != null && EF.Functions.ILike(h.Konum, $"%{ara}%")));
        }

        // Sayaçlar SQL'de hesaplanır; listede on gün varsa on ayrı sorgu
        // atmak yerine tek sorgu.
        return sorgu
            .OrderByDescending(h => h.Tarih)
            .ThenByDescending(h => h.Id)
            .Select(h => new HalkGunuOzetDto
            {
                Id = h.Id,
                Tarih = h.Tarih,
                Baslik = h.Baslik,
                Konum = h.Konum,
                Durum = h.Durum,
                DurumAd = string.Empty,
                DilimSayisi = h.Dilimler.Count,
                KisiSayisi = h.Katilimlar.Count,
                GorusulenSayisi = h.Katilimlar.Count(k => k.Durum == KatilimDurumu.Gorusuldu),
                GelmeyenSayisi = h.Katilimlar.Count(k => k.Durum == KatilimDurumu.Gelmedi),
                TakipSayisi = h.Katilimlar.Count(k => k.DegerlendirmeyeEsas),
            })
            .SayfalaAsync(suzgec, x =>
            {
                // Durum etiketi SUNUCUDA üretilir: iki istemcide ayrı kurmak,
                // enum'a yeni değer eklendiğinde birinin "bilinmeyen"
                // göstermesi demekti (protokol davet geçmişinde aynı karar).
                x.DurumAd = GunDurumAdi(x.Durum);
                return x;
            });
    }

    public async Task<HalkGunuDetayDto> DetayAsync(long id)
    {
        var gun = await GorunurGunler()
            .Where(h => h.Id == id)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException("Halk günü bulunamadı.");

        var dilimler = await _context.HalkGunuDilimleri
            .AsNoTracking()
            .Where(d => d.HalkGunuId == id)
            .OrderBy(d => d.SiraNo).ThenBy(d => d.Baslangic)
            .ToListAsync();

        var katilimlar = await KatilimlariGetirAsync(id);

        var dto = new HalkGunuDetayDto
        {
            Id = gun.Id,
            Tarih = gun.Tarih,
            Baslik = gun.Baslik,
            Konum = gun.Konum,
            Aciklama = gun.Aciklama,
            Durum = gun.Durum,
            DurumAd = GunDurumAdi(gun.Durum),
            DilimSayisi = dilimler.Count,
            KisiSayisi = katilimlar.Count,
            GorusulenSayisi = katilimlar.Count(k => k.Durum == KatilimDurumu.Gorusuldu),
            GelmeyenSayisi = katilimlar.Count(k => k.Durum == KatilimDurumu.Gelmedi),
            TakipSayisi = katilimlar.Count(k => k.DegerlendirmeyeEsas),
            Dilimler = [.. dilimler.Select(d => new HalkGunuDilimDto
            {
                Id = d.Id,
                Baslangic = d.Baslangic,
                Bitis = d.Bitis,
                Baslik = d.Baslik,
                Kapasite = d.Kapasite,
                SiraNo = d.SiraNo,
                Kisiler = [.. katilimlar.Where(k => k.DilimId == d.Id).OrderBy(k => k.SiraNo)],
            })],
            // Güne alınmış ama dilimi olmayanlar ayrı gösterilir: "kimi
            // yerleştirmeyi unuttum" sorusunun cevabı.
            Atanmamislar = [.. katilimlar.Where(k => k.DilimId == null).OrderBy(k => k.SiraNo)],
        };

        return dto;
    }

    public async Task<HalkGunuDetayDto> OlusturAsync(HalkGunuIstegi istek)
    {
        var gun = new HalkGunu
        {
            Tarih = istek.Tarih.Date,
            Baslik = Bosalt(istek.Baslik),
            Konum = Bosalt(istek.Konum),
            Aciklama = Bosalt(istek.Aciklama),
            Durum = istek.Durum ?? HalkGunuDurumu.Planlaniyor,
            BirimId = _kullanici.GetCurrentBirimId(),
            KullaniciId = _kullanici.GetUsername(),
        };

        _context.HalkGunleri.Add(gun);
        await _context.SaveChangesAsync();
        return await DetayAsync(gun.Id);
    }

    public async Task<HalkGunuDetayDto> GuncelleAsync(long id, HalkGunuIstegi istek)
    {
        var gun = await GunBulAsync(id);

        gun.Tarih = istek.Tarih.Date;
        gun.Baslik = Bosalt(istek.Baslik);
        gun.Konum = Bosalt(istek.Konum);
        gun.Aciklama = Bosalt(istek.Aciklama);
        if (istek.Durum is { } d) gun.Durum = d;
        gun.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();
        return await DetayAsync(id);
    }

    public async Task SilAsync(long id)
    {
        var gun = await GunBulAsync(id);

        // Görüşme kaydı girilmiş bir gün silinemez: salonda tutulan notlar
        // tek kopya ve geri getirilemez.
        var gorusmeVar = await _context.HalkGunuKatilimlari
            .AnyAsync(k => k.HalkGunuId == id && k.Durum != KatilimDurumu.Bekliyor);

        if (gorusmeVar)
        {
            throw new BusinessRuleException(
                "Görüşme kaydı girilmiş halk günü silinemez. Bunun yerine durumunu İptal yapabilirsiniz.");
        }

        _context.HalkGunleri.Remove(gun);
        await _context.SaveChangesAsync();
    }

    // ── dilimler ───────────────────────────────────────────────────────

    public async Task<HalkGunuDetayDto> DilimEkleAsync(long halkGunuId, DilimIstegi istek)
    {
        var gun = await GunBulAsync(halkGunuId);

        if (istek.Bitis <= istek.Baslangic)
        {
            throw new BusinessRuleException("Bitiş saati başlangıçtan sonra olmalı.");
        }

        var sonSira = await _context.HalkGunuDilimleri
            .Where(d => d.HalkGunuId == halkGunuId)
            .MaxAsync(d => (int?)d.SiraNo) ?? 0;

        var eklenecek = new List<HalkGunuDilim>();

        if (istek.DilimDakika is { } dakika && dakika > 0)
        {
            // TOPLU ÜRETİM: 14:00–15:00 / 10 dk → altı dilim.
            // Sekreterin on dilimi tek tek girmesi işin en sıkıcı kısmıydı.
            var imlec = istek.Baslangic;
            while (imlec < istek.Bitis)
            {
                var bitis = imlec.AddMinutes(dakika);
                if (bitis > istek.Bitis) bitis = istek.Bitis;

                eklenecek.Add(new HalkGunuDilim
                {
                    HalkGunuId = halkGunuId,
                    Baslangic = imlec,
                    Bitis = bitis,
                    Kapasite = istek.Kapasite ?? 1,
                    SiraNo = ++sonSira,
                });
                imlec = bitis;
            }
        }
        else
        {
            eklenecek.Add(new HalkGunuDilim
            {
                HalkGunuId = halkGunuId,
                Baslangic = istek.Baslangic,
                Bitis = istek.Bitis,
                Baslik = Bosalt(istek.Baslik),
                Kapasite = istek.Kapasite,
                SiraNo = ++sonSira,
            });
        }

        _context.HalkGunuDilimleri.AddRange(eklenecek);
        gun.GuncellemeTarihi = DateTime.Now;
        await _context.SaveChangesAsync();

        return await DetayAsync(halkGunuId);
    }

    public async Task<HalkGunuDetayDto> DilimGuncelleAsync(long dilimId, DilimIstegi istek)
    {
        var dilim = await DilimBulAsync(dilimId);

        if (istek.Bitis <= istek.Baslangic)
        {
            throw new BusinessRuleException("Bitiş saati başlangıçtan sonra olmalı.");
        }

        dilim.Baslangic = istek.Baslangic;
        dilim.Bitis = istek.Bitis;
        dilim.Baslik = Bosalt(istek.Baslik);
        dilim.Kapasite = istek.Kapasite;

        await _context.SaveChangesAsync();
        return await DetayAsync(dilim.HalkGunuId);
    }

    public async Task<HalkGunuDetayDto> DilimSilAsync(long dilimId)
    {
        var dilim = await DilimBulAsync(dilimId);
        var gunId = dilim.HalkGunuId;

        // Dilime atanmış kişiler GÜNDE KALIR, yalnızca dilimleri boşalır
        // (FK `SetNull`). Yanlışlıkla silinen bir saat aralığı yüzünden on
        // kişinin kaydı yok olmamalı; "atanmamışlar" bölümünde görünürler.
        _context.HalkGunuDilimleri.Remove(dilim);
        await _context.SaveChangesAsync();

        return await DetayAsync(gunId);
    }

    private async Task<HalkGunuDilim> DilimBulAsync(long dilimId)
    {
        var dilim = await _context.HalkGunuDilimleri
            .FirstOrDefaultAsync(d => d.Id == dilimId)
            ?? throw new EntityNotFoundException("Zaman dilimi bulunamadı.");

        // Dilim kendi başına görünürlük taşımıyor; kapı GÜNÜN birimidir.
        await GunBulAsync(dilim.HalkGunuId);
        return dilim;
    }

    // ── başvuru havuzu ─────────────────────────────────────────────────

    /// <summary>
    /// Görünürlük kapısı + süzgeçler — liste ve çıktı AYNI yerden okur.
    /// </summary>
    /// <remarks>
    /// Gerekçesi <c>GorevServisi.SorguKurAsync</c> ile aynı: blok
    /// kopyalansaydı bir süzgeç eklendiğinde biri unutulur ve Excel
    /// ekrandakinden farklı bir küme döndürürdü.
    /// </remarks>
    private IQueryable<HalkGunuBasvuru> BasvuruSorguKur(BasvuruSuzgeci suzgec)
    {
        var sorgu = GorunurBasvurular();

        if (suzgec.Durum is { } d) sorgu = sorgu.Where(b => b.Durum == d);
        if (suzgec.Atanmamis)
        {
            sorgu = sorgu.Where(b =>
                !b.Katilimlar.Any() && b.Durum != BasvuruDurumu.Reddedildi);
        }

        if (suzgec.TemizArama is { } ara)
        {
            // Telefon aramasında SADE kolon kullanılır: kullanıcı numarayı
            // bitişik de yazsa boşluklu da yazsa bulsun.
            var sade = Telefon.Duzelt(ara);

            sorgu = sorgu.Where(b =>
                EF.Functions.ILike(b.Ad, $"%{ara}%") ||
                (b.Soyad != null && EF.Functions.ILike(b.Soyad, $"%{ara}%")) ||
                (b.Konu != null && EF.Functions.ILike(b.Konu, $"%{ara}%")) ||
                (b.TelefonSade != null && sade != null && b.TelefonSade.Contains(sade)));
        }

        return sorgu;
    }

    public Task<SayfaliSonuc<BasvuruDto>> BasvuruListeAsync(BasvuruSuzgeci suzgec)
    {
        var sorgu = BasvuruSorguKur(suzgec);

        return sorgu
            .OrderByDescending(b => b.OlusturmaTarihi)
            .Select(b => new
            {
                Kayit = b,
                MahalleAd = b.Mahalle != null ? b.Mahalle.Ad : null,
                Gunler = b.Katilimlar.Select(k => k.HalkGunu!.Tarih).ToList(),
            })
            .SayfalaAsync(suzgec, x => Cevir(x.Kayit, x.MahalleAd, x.Gunler));
    }

    public async Task<BasvuruDto> BasvuruOlusturAsync(BasvuruIstegi istek)
    {
        if (string.IsNullOrWhiteSpace(istek.Ad))
        {
            throw new BusinessRuleException("Ad zorunludur.");
        }

        var kayit = new HalkGunuBasvuru
        {
            BirimId = _kullanici.GetCurrentBirimId(),
            Olusturan = _kullanici.GetUsername(),
        };

        Uygula(kayit, istek);
        _context.HalkGunuBasvurulari.Add(kayit);
        await _context.SaveChangesAsync();

        return await BasvuruDtoAsync(kayit.Id);
    }

    public async Task<BasvuruDto> BasvuruGuncelleAsync(long id, BasvuruIstegi istek)
    {
        var kayit = await _context.HalkGunuBasvurulari
            .FirstOrDefaultAsync(b => b.Id == id
                                   && b.BirimId == _kullanici.GetCurrentBirimId())
            ?? throw new EntityNotFoundException("Başvuru bulunamadı.");

        Uygula(kayit, istek);
        kayit.GuncellemeTarihi = DateTime.Now;
        await _context.SaveChangesAsync();

        return await BasvuruDtoAsync(id);
    }

    /// <summary>
    /// Halk günü görüşmesini UYGUN GÖRMEZ.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Vatandaş havuzda kalır — kaydı silmek "kaç kişi geri çevrildi" ve
    /// "bunu neden çevirmiştik?" sorularını cevapsız bırakırdı. Gerekçe
    /// zorunlu değil ama bekleniyor; aynı kişi bir sonraki ay yeniden
    /// başvurduğunda okunacak tek şey o.
    /// </para>
    /// <para>
    /// Kişi bir güne ATANMIŞSA o atama <b>kaldırılır</b> — yalnızca henüz
    /// görüşülmemiş olanlar. Ret kararı listeyi kuran kişiden sonra geldiğinde
    /// vatandaş salonda çağrılmaya devam ediyordu. Görüşülmüş kayda
    /// dokunulmaz: olan olmuş, geçmiş silinmez.
    /// </para>
    /// </remarks>
    public async Task<BasvuruDto> BasvuruReddetAsync(long id, string? neden)
    {
        var basvuru = await BasvuruBulAsync(id);

        var bekleyenAtamalar = await _context.HalkGunuKatilimlari
            .Where(k => k.BasvuruId == id && k.Durum != KatilimDurumu.Gorusuldu)
            .ToListAsync();

        _context.HalkGunuKatilimlari.RemoveRange(bekleyenAtamalar);

        basvuru.Durum = BasvuruDurumu.Reddedildi;
        basvuru.RedNedeni = Bosalt(neden);
        basvuru.RedTarihi = DateTime.Now;
        basvuru.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();
        return await BasvuruDtoAsync(id);
    }

    /// <summary>Reddedilen ya da iptal edilen kaydı havuza geri alır.</summary>
    public async Task<BasvuruDto> BasvuruGeriAlAsync(long id)
    {
        var basvuru = await BasvuruBulAsync(id);

        // Bir güne atanmış hâldeyken geri alınırsa durum "atandı" kalmalı;
        // aksi hâlde kişi hem listede hem havuzda bekliyor görünürdü.
        var atamaVar = await _context.HalkGunuKatilimlari.AnyAsync(k => k.BasvuruId == id);

        basvuru.Durum = atamaVar ? BasvuruDurumu.Atandi : BasvuruDurumu.Bekliyor;
        basvuru.RedNedeni = null;
        basvuru.RedTarihi = null;
        basvuru.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();
        return await BasvuruDtoAsync(id);
    }

    /// <summary>Başvuruyu birim kapısından geçerek bulur.</summary>
    private async Task<HalkGunuBasvuru> BasvuruBulAsync(long id) =>
        await _context.HalkGunuBasvurulari
            .FirstOrDefaultAsync(b => b.Id == id
                                   && b.BirimId == _kullanici.GetCurrentBirimId())
        ?? throw new EntityNotFoundException("Başvuru bulunamadı.");

    public async Task BasvuruSilAsync(long id)
    {
        var kayit = await _context.HalkGunuBasvurulari
            .FirstOrDefaultAsync(b => b.Id == id
                                   && b.BirimId == _kullanici.GetCurrentBirimId())
            ?? throw new EntityNotFoundException("Başvuru bulunamadı.");

        // Bir güne atanmış başvuru silinemez (FK `Restrict`): görüşme geçmişi
        // ona bağlı. Önce katılım çıkarılır.
        var atanmis = await _context.HalkGunuKatilimlari.AnyAsync(k => k.BasvuruId == id);
        if (atanmis)
        {
            throw new BusinessRuleException(
                "Bu kişi bir halk gününe atanmış. Önce listeden çıkarın ya da durumunu İptal yapın.");
        }

        _context.HalkGunuBasvurulari.Remove(kayit);
        await _context.SaveChangesAsync();
    }

    // ── atama, sıralama, görüşme ───────────────────────────────────────

    public async Task<HalkGunuDetayDto> KatilimEkleAsync(
        long halkGunuId, long[] basvuruIdler, long? dilimId)
    {
        await GunBulAsync(halkGunuId);

        if (dilimId is { } d)
        {
            var dilim = await DilimBulAsync(d);
            if (dilim.HalkGunuId != halkGunuId)
            {
                throw new BusinessRuleException("Zaman dilimi bu halk gününe ait değil.");
            }
        }

        var istenen = basvuruIdler.Distinct().ToList();

        // Başka birimin başvurusu atanamaz.
        var gecerli = await GorunurBasvurular()
            .Where(b => istenen.Contains(b.Id))
            .Select(b => b.Id)
            .ToListAsync();

        // Zaten bu güne atanmış olanlar ayıklanır: "tümünü ekle" ikinci kez
        // basılınca benzersiz indeks hatası vermesin.
        var mevcut = await _context.HalkGunuKatilimlari
            .Where(k => k.HalkGunuId == halkGunuId)
            .Select(k => k.BasvuruId)
            .ToListAsync();

        var eklenecek = gecerli.Except(mevcut).ToList();
        if (eklenecek.Count == 0) return await DetayAsync(halkGunuId);

        // KAPASİTE ZORLANIR.
        //
        // Kapasite bilgilendirme alanı değil: 10 dakikalık dilime üç kişi
        // atanınca ekranda "3 / 1 kişi" yazıyor ve liste bozuk görünüyordu.
        // Aşmak isteyen kullanıcı dilimin kapasitesini yükseltebilir ya da
        // kapasiteyi boşaltıp sınırsız yapabilir.
        if (dilimId is { } kapasiteDilimi)
        {
            var dilim = await _context.HalkGunuDilimleri
                .AsNoTracking()
                .FirstAsync(d => d.Id == kapasiteDilimi);

            if (dilim.Kapasite is { } kapasite)
            {
                var mevcutSayi = await _context.HalkGunuKatilimlari
                    .CountAsync(k => k.DilimId == kapasiteDilimi);

                if (mevcutSayi + eklenecek.Count > kapasite)
                {
                    throw new BusinessRuleException(
                        $"Bu dilimin kapasitesi {kapasite} kişi; şu an {mevcutSayi} kişi atanmış. "
                        + "Kapasiteyi artırın ya da başka bir dilim seçin.");
                }
            }
        }

        var sonSira = await _context.HalkGunuKatilimlari
            .Where(k => k.HalkGunuId == halkGunuId && k.DilimId == dilimId)
            .MaxAsync(k => (int?)k.SiraNo) ?? 0;

        foreach (var id in eklenecek)
        {
            _context.HalkGunuKatilimlari.Add(new HalkGunuKatilim
            {
                HalkGunuId = halkGunuId,
                DilimId = dilimId,
                BasvuruId = id,
                SiraNo = ++sonSira,
            });
        }

        // Havuzdaki kayıt "atandı" olur; bekleyenler listesi böylece küçülür.
        await _context.HalkGunuBasvurulari
            .Where(b => eklenecek.Contains(b.Id) && b.Durum == BasvuruDurumu.Bekliyor)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Durum, BasvuruDurumu.Atandi));

        await _context.SaveChangesAsync();
        return await DetayAsync(halkGunuId);
    }

    public async Task KatilimCikarAsync(long katilimId)
    {
        var katilim = await KatilimBulAsync(katilimId);

        _context.HalkGunuKatilimlari.Remove(katilim);
        await _context.SaveChangesAsync();

        // Başka güne atanmadıysa havuza geri döner.
        var baskaGunVar = await _context.HalkGunuKatilimlari
            .AnyAsync(k => k.BasvuruId == katilim.BasvuruId);

        if (!baskaGunVar)
        {
            await _context.HalkGunuBasvurulari
                .Where(b => b.Id == katilim.BasvuruId && b.Durum == BasvuruDurumu.Atandi)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Durum, BasvuruDurumu.Bekliyor));
        }
    }

    /// <summary>
    /// Dilim ve sıra numaralarını topluca yazar.
    /// </summary>
    /// <remarks>
    /// İstemci yukarı/aşağı düğmeleriyle yeni diziyi kurar ve TAMAMINI
    /// gönderir; tek tek "yukarı taşı" ucu, iki kullanıcı aynı anda
    /// oynadığında sırayı bozardı. Tek `SaveChanges` — protokol sıralaması da
    /// böyle.
    /// </remarks>
    public async Task SiralamaGuncelleAsync(long halkGunuId, IReadOnlyList<SiralamaOgesiDto> ogeler)
    {
        await GunBulAsync(halkGunuId);

        var idler = ogeler.Select(o => o.Id).ToList();
        var kayitlar = await _context.HalkGunuKatilimlari
            .Where(k => k.HalkGunuId == halkGunuId && idler.Contains(k.Id))
            .ToListAsync();

        // ── Kapasite BURADA da denetlenir ──────────────────────────────
        //
        // Bu uç yalnızca "sırayı değiştir" sanılıyordu ama `dilimId` de
        // yazıyor: kişiyi başka bir saate TAŞIMANIN yolu bu. Denetim yalnızca
        // atama ucunda dururken, taşıma kapasiteyi sessizce aşıyordu —
        // 10 dakikalık dilimde dört kişi.
        var hedefler = ogeler
            .Where(o => o.DilimId is not null)
            .GroupBy(o => o.DilimId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        if (hedefler.Count > 0)
        {
            var dilimler = await _context.HalkGunuDilimleri
                .Where(d => d.HalkGunuId == halkGunuId && hedefler.Keys.Contains(d.Id))
                .Select(d => new { d.Id, d.Kapasite, d.Baslangic, d.Bitis })
                .ToListAsync();

            foreach (var dilim in dilimler)
            {
                if (dilim.Kapasite is not { } kapasite) continue;

                // Bu istekte o dilime yazılanlar + istekte adı geçmeyip zaten
                // orada duranlar.
                var dokunulmayan = await _context.HalkGunuKatilimlari
                    .CountAsync(k => k.DilimId == dilim.Id && !idler.Contains(k.Id));

                var toplam = hedefler[dilim.Id] + dokunulmayan;
                if (toplam > kapasite)
                {
                    throw new BusinessRuleException(
                        $"{dilim.Baslangic:HH:mm}–{dilim.Bitis:HH:mm} diliminin kapasitesi " +
                        $"{kapasite} kişi; {toplam} kişi yerleştirilmek isteniyor.");
                }
            }
        }

        foreach (var kayit in kayitlar)
        {
            var oge = ogeler.First(o => o.Id == kayit.Id);
            kayit.DilimId = oge.DilimId;
            kayit.SiraNo = oge.SiraNo;
            kayit.GuncellemeTarihi = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<HalkGunuKatilimDto> GorusmeKaydetAsync(long katilimId, GorusmeIstegi istek)
    {
        var katilim = await KatilimBulAsync(katilimId);

        if (istek.Durum is { } durum)
        {
            katilim.Durum = durum;

            // Görüşme tarihi İLK kez "görüşüldü" olduğunda damgalanır ve
            // sonraki düzeltmelerde korunur (davet listesindeki "arandı"
            // damgasıyla aynı kural).
            if (durum == KatilimDurumu.Gorusuldu)
            {
                katilim.GorusmeTarihi ??= DateTime.Now;
            }
            else if (durum == KatilimDurumu.Bekliyor)
            {
                katilim.GorusmeTarihi = null;
            }
        }

        if (istek.GorusmeNotu is not null) katilim.GorusmeNotu = Bosalt(istek.GorusmeNotu);
        if (istek.DegerlendirmeyeEsas is { } esas) katilim.DegerlendirmeyeEsas = esas;
        if (istek.DegerlendirmeNotu is not null) katilim.DegerlendirmeNotu = Bosalt(istek.DegerlendirmeNotu);

        katilim.GuncellemeTarihi = DateTime.Now;

        // Havuzdaki kayıt da ilerler: aynı kişi başka bir güne yeniden
        // eklenmek istendiğinde durumu doğru görünsün.
        if (katilim.Durum == KatilimDurumu.Gorusuldu)
        {
            await _context.HalkGunuBasvurulari
                .Where(b => b.Id == katilim.BasvuruId)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Durum, BasvuruDurumu.Gorusuldu));
        }

        await _context.SaveChangesAsync();

        var liste = await KatilimlariGetirAsync(katilim.HalkGunuId);
        return liste.First(k => k.Id == katilimId);
    }

    private async Task<HalkGunuKatilim> KatilimBulAsync(long katilimId)
    {
        var katilim = await _context.HalkGunuKatilimlari
            .FirstOrDefaultAsync(k => k.Id == katilimId)
            ?? throw new EntityNotFoundException("Katılım kaydı bulunamadı.");

        await GunBulAsync(katilim.HalkGunuId);
        return katilim;
    }

    // ── ortak yardımcılar ──────────────────────────────────────────────

    /// <summary>Bir günün katılımlarını kişi bilgisiyle birlikte getirir.</summary>
    internal async Task<List<HalkGunuKatilimDto>> KatilimlariGetirAsync(long halkGunuId) =>
        await _context.HalkGunuKatilimlari
            .AsNoTracking()
            .Where(k => k.HalkGunuId == halkGunuId)
            .OrderBy(k => k.SiraNo)
            .Select(k => new HalkGunuKatilimDto
            {
                Id = k.Id,
                BasvuruId = k.BasvuruId,
                DilimId = k.DilimId,
                SiraNo = k.SiraNo,
                AdSoyad = ((k.Basvuru!.Ad ?? "") + " " + (k.Basvuru.Soyad ?? "")).Trim(),
                Telefon = k.Basvuru.Telefon,
                Adres = k.Basvuru.Adres,
                MahalleAd = k.Basvuru.Mahalle != null ? k.Basvuru.Mahalle.Ad : null,
                Konu = k.Basvuru.Konu,
                Durum = k.Durum,
                DurumAd = string.Empty,
                GorusmeNotu = k.GorusmeNotu,
                GorusmeTarihi = k.GorusmeTarihi,
                DegerlendirmeyeEsas = k.DegerlendirmeyeEsas,
                DegerlendirmeNotu = k.DegerlendirmeNotu,
                OlusanRandevuId = k.OlusanRandevuId,
                SmsTarihi = k.SmsTarihi,
            })
            .ToListAsync()
            is var liste
                ? [.. liste.Select(k => { k.DurumAd = KatilimDurumAdi(k.Durum); return k; })]
                : [];

    private void Uygula(HalkGunuBasvuru kayit, BasvuruIstegi istek)
    {
        kayit.Ad = istek.Ad.Trim();
        kayit.Soyad = Bosalt(istek.Soyad);
        kayit.Telefon = Bosalt(istek.Telefon);

        // SADE telefon yazma anında üretilir — kişiyi bulmanın tek güvenilir
        // yolu bu. Ham sütun kullanıcının yazdığı gibi kalır.
        kayit.TelefonSade = Telefon.Duzelt(istek.Telefon);

        kayit.Adres = Bosalt(istek.Adres);
        kayit.MahalleId = istek.MahalleId;
        kayit.Meslek = Bosalt(istek.Meslek);
        kayit.Konu = Bosalt(istek.Konu);
        kayit.Not = Bosalt(istek.Not);
        kayit.RandevuId = istek.RandevuId;
        if (istek.Durum is { } d) kayit.Durum = d;
    }

    private async Task<BasvuruDto> BasvuruDtoAsync(long id)
    {
        var x = await GorunurBasvurular()
            .Where(b => b.Id == id)
            .Select(b => new
            {
                Kayit = b,
                MahalleAd = b.Mahalle != null ? b.Mahalle.Ad : null,
                Gunler = b.Katilimlar.Select(k => k.HalkGunu!.Tarih).ToList(),
            })
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException("Başvuru bulunamadı.");

        return Cevir(x.Kayit, x.MahalleAd, x.Gunler);
    }

    private static BasvuruDto Cevir(HalkGunuBasvuru b, string? mahalleAd, List<DateTime> gunler) => new()
    {
        Id = b.Id,
        Ad = b.Ad,
        Soyad = b.Soyad,
        AdSoyad = $"{b.Ad} {b.Soyad}".Trim(),
        Telefon = b.Telefon,
        Adres = b.Adres,
        MahalleId = b.MahalleId,
        MahalleAd = mahalleAd,
        Meslek = b.Meslek,
        Konu = b.Konu,
        Not = b.Not,
        Durum = b.Durum,
        DurumAd = BasvuruDurumAdi(b.Durum),
        RandevuId = b.RandevuId,
        RedNedeni = b.RedNedeni,
        RedTarihi = b.RedTarihi,
        OlusturmaTarihi = b.OlusturmaTarihi,
        HalkGunleri = [.. gunler.OrderByDescending(t => t)],
    };

    private static string? Bosalt(string? m) =>
        string.IsNullOrWhiteSpace(m) ? null : m.Trim();

    // Durum etiketleri SUNUCUDA: iki istemcide ayrı kurmak, enum'a yeni değer
    // eklendiğinde birinin "bilinmeyen" göstermesi demekti.
    internal static string GunDurumAdi(HalkGunuDurumu d) => d switch
    {
        HalkGunuDurumu.Planlaniyor => "Planlanıyor",
        HalkGunuDurumu.Yayinda => "Yayında",
        HalkGunuDurumu.Tamamlandi => "Tamamlandı",
        HalkGunuDurumu.Iptal => "İptal",
        _ => "Bilinmiyor",
    };

    internal static string BasvuruDurumAdi(BasvuruDurumu d) => d switch
    {
        BasvuruDurumu.Bekliyor => "Bekliyor",
        BasvuruDurumu.Atandi => "Atandı",
        BasvuruDurumu.Gorusuldu => "Görüşüldü",
        BasvuruDurumu.Iptal => "İptal",
        BasvuruDurumu.Reddedildi => "Reddedildi",
        _ => "Bilinmiyor",
    };

    internal static string KatilimDurumAdi(KatilimDurumu d) => d switch
    {
        KatilimDurumu.Bekliyor => "Bekliyor",
        KatilimDurumu.Geldi => "Geldi",
        KatilimDurumu.Gelmedi => "Gelmedi",
        KatilimDurumu.Gorusuldu => "Görüşüldü",
        KatilimDurumu.Iptal => "İptal",
        _ => "Bilinmiyor",
    };

    /// <summary>
    /// LİSTE ÇIKTISI — ekrandaki süzgeçlerle, sayfalamasız.
    /// </summary>
    /// <remarks>
    /// Sorgu liste ile AYNI kurucudan geliyor; görünürlük kapısı ve süzgeçler
    /// birebir aynı. Üst sınır aşılırsa sessizce kırpılmaz, hata döner —
    /// gerekçesi <see cref="ListeCikti"/> üzerinde.
    /// </remarks>
    public async Task<DisaAktarmaDosyasi> BasvuruExcelAsync(BasvuruSuzgeci suzgec)
    {
        var satirlar = await BasvuruSorguKur(suzgec)
            .OrderByDescending(b => b.OlusturmaTarihi)
            .Take(ListeCikti.UstSinir + 1)
            .Select(b => new
            {
                b.Ad, b.Soyad, b.Telefon, b.Konu, b.Not, b.Durum, b.RedNedeni,
                Mahalle = b.Mahalle != null ? b.Mahalle.Ad : null,
                b.Meslek, b.OlusturmaTarihi,
                GunSayisi = b.Katilimlar.Count,
            })
            .ToListAsync();

        ListeCikti.SiniriDenetle(satirlar.Count, "başvuru");

        return ExcelTablosu.Uret(
            "Vatandaş Havuzu",
            ["Ad", "Soyad", "Telefon", "Mahalle", "Meslek", "Konu", "Not", "Durum", "Ret nedeni", "Atandığı gün", "Kayıt"],
            satirlar.Select(b => new string?[]
            {
                b.Ad, b.Soyad, Telefon.Bicimle(b.Telefon), b.Mahalle, b.Meslek,
                b.Konu, b.Not, b.Durum.ToString(), b.RedNedeni,
                b.GunSayisi.ToString(),
                b.OlusturmaTarihi.ToString("dd.MM.yyyy"),
            }),
            "vatandas-havuzu");
    }

}

/// <summary>Ret gerekçesi — gövdede tek alan.</summary>
public class RedIstegi
{
    [JsonPropertyName("neden")] public string? Neden { get; set; }
}
