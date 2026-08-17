using ClosedXML.Excel;
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

/// <summary>Kişi geçmişindeki tek satır — dört kaynaktan biri.</summary>
public class GecmisSatiriDto
{
    /// <summary><c>talep</c> · <c>etkinlik</c> · <c>halkgunu</c></summary>
    [JsonPropertyName("tur")] public string Tur { get; set; } = string.Empty;
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("tarih")] public DateTime? Tarih { get; set; }
    [JsonPropertyName("baslik")] public string? Baslik { get; set; }
    [JsonPropertyName("durumAd")] public string? DurumAd { get; set; }
    [JsonPropertyName("not")] public string? Not { get; set; }
}

/// <summary>
/// "Bu vatandaşı daha önce gördük mü?" sorusunun cevabı.
/// </summary>
/// <remarks>
/// Halk gününe kişi eklerken ÖNCE telefon sorulur; numara girilir girilmez bu
/// özet çıkar ("3 talep · 2 etkinlik · 1 halk günü"). Kullanıcı ayrıntıya
/// girebilir ya da doğrudan kaydetmeye devam edebilir.
/// </remarks>
public class KisiGecmisiDto
{
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("telefonSade")] public string? TelefonSade { get; set; }

    [JsonPropertyName("talepSayisi")] public int TalepSayisi { get; set; }
    [JsonPropertyName("etkinlikSayisi")] public int EtkinlikSayisi { get; set; }
    [JsonPropertyName("halkGunuSayisi")] public int HalkGunuSayisi { get; set; }

    /// <summary>Halk gününde GÖRÜŞÜLMÜŞ kayıt sayısı.</summary>
    [JsonPropertyName("gorusulenSayisi")] public int GorusulenSayisi { get; set; }

    /// <summary>Kurum protokolünde kayıtlı mı (varsa adı).</summary>
    [JsonPropertyName("protokolAd")] public string? ProtokolAd { get; set; }

    /// <summary>En yeniden eskiye, karışık türlerde son kayıtlar.</summary>
    [JsonPropertyName("son")] public List<GecmisSatiriDto> Son { get; set; } = [];

    [JsonPropertyName("kayitVar")] public bool KayitVar =>
        TalepSayisi + EtkinlikSayisi + HalkGunuSayisi > 0 || ProtokolAd != null;
}

/// <summary>
/// VATANDAŞ DOSYASI — tek kişinin kurumla bütün geçmişi.
/// </summary>
/// <remarks>
/// <para>
/// Görüşme kartındaki özet ("2 talep · 3 görüşme") soruyu açıyor ama
/// cevaplamıyor: <b>neyi</b> istemişti, <b>ne oldu</b>? Başkan vatandaşla
/// otururken bunu bilmek istiyor — "siz geçen mart kaldırım için gelmiştiniz,
/// o iş Fen İşleri'ne havale edildi" diyebilmek, hatırlandığını göstermenin
/// en somut hâli.
/// </para>
/// <para>
/// Özet ucundan AYRI: özet, başvuru formunda her tuş vuruşunda çağrılıyor ve
/// hafif kalmalı. Bu uç ayrıntının tamamını getirir ve yalnızca dosya
/// açıldığında çağrılır.
/// </para>
/// </remarks>
public class KisiDosyasiDto
{
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("adSoyad")] public string? AdSoyad { get; set; }
    [JsonPropertyName("protokolAd")] public string? ProtokolAd { get; set; }

    [JsonPropertyName("talepSayisi")] public int TalepSayisi { get; set; }
    [JsonPropertyName("halkGunuSayisi")] public int HalkGunuSayisi { get; set; }
    [JsonPropertyName("gorusulenSayisi")] public int GorusulenSayisi { get; set; }
    [JsonPropertyName("etkinlikSayisi")] public int EtkinlikSayisi { get; set; }

    /// <summary>Kurumla ilk ve son teması — "ne zamandır tanışıyoruz".</summary>
    [JsonPropertyName("ilkTemas")] public DateTime? IlkTemas { get; set; }
    [JsonPropertyName("sonTemas")] public DateTime? SonTemas { get; set; }

    [JsonPropertyName("talepler")] public List<KisiTalepDto> Talepler { get; set; } = [];
    [JsonPropertyName("halkGunleri")] public List<KisiHalkGunuDto> HalkGunleri { get; set; } = [];
    [JsonPropertyName("etkinlikler")] public List<KisiEtkinlikDto> Etkinlikler { get; set; } = [];

    [JsonPropertyName("kayitVar")] public bool KayitVar =>
        TalepSayisi + HalkGunuSayisi + EtkinlikSayisi > 0 || ProtokolAd != null;
}

public class KisiTalepDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("tarih")] public DateTime? Tarih { get; set; }
    [JsonPropertyName("konu")] public string? Konu { get; set; }
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("durumAd")] public string? DurumAd { get; set; }
    [JsonPropertyName("durumRenk")] public string? DurumRenk { get; set; }
    [JsonPropertyName("tipAd")] public string? TipAd { get; set; }
    [JsonPropertyName("mahalleAd")] public string? MahalleAd { get; set; }

    /// <summary>Talebin AİT OLDUĞU birim — dosya kurum genelini gösteriyor.</summary>
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }
    [JsonPropertyName("ajandayaEklendi")] public bool AjandayaEklendi { get; set; }
    [JsonPropertyName("arsivlendi")] public bool Arsivlendi { get; set; }
}

public class KisiHalkGunuDto
{
    [JsonPropertyName("katilimId")] public long KatilimId { get; set; }
    [JsonPropertyName("halkGunuId")] public long HalkGunuId { get; set; }
    [JsonPropertyName("tarih")] public DateTime? Tarih { get; set; }
    [JsonPropertyName("gunBaslik")] public string? GunBaslik { get; set; }
    [JsonPropertyName("konu")] public string? Konu { get; set; }
    [JsonPropertyName("gorusmeNotu")] public string? GorusmeNotu { get; set; }
    [JsonPropertyName("durumAd")] public string? DurumAd { get; set; }
    [JsonPropertyName("degerlendirmeyeEsas")] public bool DegerlendirmeyeEsas { get; set; }

    /// <summary>Bu görüşmeden bir talep açıldıysa numarası.</summary>
    [JsonPropertyName("olusanRandevuId")] public long? OlusanRandevuId { get; set; }

    /// <summary>Görüşmenin yapıldığı birim.</summary>
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }
}

public class KisiEtkinlikDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("tarih")] public DateTime? Tarih { get; set; }
    [JsonPropertyName("baslik")] public string? Baslik { get; set; }
    [JsonPropertyName("konum")] public string? Konum { get; set; }
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }
}

public class HalkGunuSmsIstegi
{
    [JsonPropertyName("mesaj")] public string Mesaj { get; set; } = string.Empty;

    /// <summary>Yalnızca bu dilim; boşsa günün tamamı.</summary>
    [JsonPropertyName("dilimId")] public long? DilimId { get; set; }

    /// <summary>Daha önce SMS gönderilmiş kişileri atla.</summary>
    [JsonPropertyName("tekrarGonderme")] public bool TekrarGonderme { get; set; } = true;
}

public class HalkGunuSmsSonucu
{
    [JsonPropertyName("gonderilen")] public int Gonderilen { get; set; }
    [JsonPropertyName("telefonsuz")] public int Telefonsuz { get; set; }
    [JsonPropertyName("atlanan")] public int Atlanan { get; set; }
}

public class TalepOlusturIstegi
{
    [JsonPropertyName("konu")] public string? Konu { get; set; }
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }

    /// <summary>Havale edilecek birim; boşsa kendi birimi.</summary>
    [JsonPropertyName("birimId")] public long? BirimId { get; set; }
    [JsonPropertyName("randevuTipId")] public long? RandevuTipId { get; set; }
}

/// <summary>Başkan görünümü — sayılar ve takip gerektirenler.</summary>
public class HalkGunuOzetiDto
{
    [JsonPropertyName("tarih")] public DateTime Tarih { get; set; }
    [JsonPropertyName("baslik")] public string? Baslik { get; set; }
    [JsonPropertyName("toplam")] public int Toplam { get; set; }
    [JsonPropertyName("gorusulen")] public int Gorusulen { get; set; }
    [JsonPropertyName("gelmeyen")] public int Gelmeyen { get; set; }
    [JsonPropertyName("bekleyen")] public int Bekleyen { get; set; }
    [JsonPropertyName("takipGerektiren")] public int TakipGerektiren { get; set; }
    [JsonPropertyName("talebeDonusen")] public int TalebeDonusen { get; set; }

    /// <summary>Mahalleye göre dağılım — "nereden geliyorlar".</summary>
    [JsonPropertyName("mahalleler")] public List<AdSayiDto> Mahalleler { get; set; } = [];

    /// <summary>Takip gerektiren görüşmeler (başkanın okumak istediği liste).</summary>
    [JsonPropertyName("takipListesi")] public List<HalkGunuKatilimDto> TakipListesi { get; set; } = [];
}

public class AdSayiDto
{
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("adet")] public int Adet { get; set; }
}

// ═══════════════════════════════════════════════════════════════ arayüz

public interface IHalkGunuIslemServisi
{
    /// <param name="haricKatilimId">
    /// Sayılmayacak katılım kaydı — EKRANDA AÇIK OLAN görüşmenin kendisi.
    /// </param>
    Task<KisiGecmisiDto> KisiGecmisiAsync(
        string? telefon, string? ad, long? haricKatilimId = null);
    /// <summary>Tek vatandaşın kurumla bütün geçmişi — ayrıntılı dosya.</summary>
    Task<KisiDosyasiDto> KisiDosyasiAsync(
        string? telefon, string? ad, long? haricKatilimId = null);

    Task<HalkGunuSmsSonucu> SmsGonderAsync(long halkGunuId, HalkGunuSmsIstegi istek);
    Task<long> TalebeDonusturAsync(long katilimId, TalepOlusturIstegi istek);
    Task<HalkGunuOzetiDto> OzetAsync(long halkGunuId);
}

// ══════════════════════════════════════════════════════════════ uygulama

/// <summary>
/// Halk gününün "iş" katmanı: kişi geçmişi, toplu SMS, Excel, talebe
/// dönüştürme ve özet.
/// </summary>
/// <remarks>
/// CRUD'dan ayrı bir dosyada: <see cref="HalkGunuServisi"/> günü ve listeyi
/// yönetiyor, burası o listeden doğan işleri yapıyor. Tek dosyada toplamak
/// bin satırı aşan, iki farklı sebeple değişen bir sınıf üretirdi.
/// </remarks>
public class HalkGunuIslemServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici,
    IHalkGunuServisi _halkGunu,
    IMessageService _mesaj) : IHalkGunuIslemServisi
{
    // ── kişi geçmişi ───────────────────────────────────────────────────

    /// <summary>
    /// Vatandaşı telefon (ve/veya ad) ile dört kaynakta arar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Telefon karşılaştırması ham sütunda YAPILAMAZ.</b> `randevular.telefon`
    /// içinde <c>0541 298 34 50</c>, <c>05412983450</c>, <c>+90 541…</c> karışık
    /// duruyor; bugünkü `ILIKE '%…%'` araması numarayı bitişik yazınca
    /// bulamıyor. Bu yüzden karşılaştırma SQL'de
    /// <c>regexp_replace(telefon,'\D','','g')</c> ile yapılıyor — EF bu işlevi
    /// çeviremediği için ham sorgu.
    /// </para>
    /// <para>
    /// Kapsam KULLANICININ BİRİMİ: sistemin geri kalanı böyle çalışıyor ve
    /// başka birimin vatandaş kaydını göstermek yeni bir sızıntı yüzeyi
    /// açardı.
    /// </para>
    /// </remarks>
    public async Task<KisiGecmisiDto> KisiGecmisiAsync(
        string? telefon, string? ad, long? haricKatilimId = null)
    {
        var sade = Telefon.Duzelt(telefon);
        var adDesen = string.IsNullOrWhiteSpace(ad) ? null : $"%{ad.Trim()}%";
        var birimId = _kullanici.GetCurrentBirimId();

        var sonuc = new KisiGecmisiDto { Telefon = telefon, TelefonSade = sade };

        // Ne telefon ne ad verilmişse boş dön: tüm tabloyu taramanın anlamı yok.
        if (sade is null && adDesen is null) return sonuc;

        // Son 8 haneyi karşılaştırıyoruz: kayıtların bir kısmı başında 0
        // olmadan (`541…`), bir kısmı ülke koduyla (`90541…`) girilmiş.
        var kuyruk = sade is { Length: >= 8 } ? sade[^8..] : sade;

        // ── talepler ────────────────────────────────────────────────────
        var talepler = await _context.Database
            .SqlQuery<GecmisHamSatir>($"""
                SELECT 'talep' AS tur, r.id AS id, r.baslangic_tarih AS tarih,
                       r.konu AS baslik, d.durum_ad AS durum_ad, NULL::text AS not_
                FROM randevular r
                LEFT JOIN randevu_durumlar d ON d.id = r.randevu_durum_id
                WHERE r.birim_id = {birimId}
                  AND (
                        ({kuyruk}::text IS NOT NULL
                         AND regexp_replace(coalesce(r.telefon,''), '\D', '', 'g') LIKE '%' || {kuyruk}::text)
                     OR ({adDesen}::text IS NOT NULL
                         AND (coalesce(r.ad,'') || ' ' || coalesce(r.soyad,'')) ILIKE {adDesen}::text)
                      )
                ORDER BY r.baslangic_tarih DESC NULLS LAST
                LIMIT 20
                """)
            .ToListAsync();

        // ── etkinlik irtibat kişisi ─────────────────────────────────────
        //
        // `Ajanda.IrtibatKisi/IrtibatTelefon` talepten kopyalanıyor ve hiçbir
        // arama ucunda süzülmüyordu; kullanıcı "bu kişi bir etkinliğe irtibat
        // olarak yazılmış mıydı" sorusunu hiç soramıyordu.
        var etkinlikler = await _context.Database
            .SqlQuery<GecmisHamSatir>($"""
                SELECT 'etkinlik' AS tur, a.id AS id, a.baslangic_tarihi AS tarih,
                       a.baslik AS baslik, NULL::text AS durum_ad, a.irtibat_kisi AS not_
                FROM ajandalar a
                WHERE a.birim_id = {birimId}
                  AND NOT a.is_deleted
                  AND NOT a.gizli
                  AND (
                        ({kuyruk}::text IS NOT NULL
                         AND regexp_replace(coalesce(a.irtibat_telefon,''), '\D', '', 'g') LIKE '%' || {kuyruk}::text)
                     OR ({adDesen}::text IS NOT NULL
                         AND coalesce(a.irtibat_kisi,'') ILIKE {adDesen}::text)
                      )
                ORDER BY a.baslangic_tarihi DESC
                LIMIT 20
                """)
            .ToListAsync();

        // ── önceki halk günleri ─────────────────────────────────────────
        var halkGunleri = await _context.Database
            .SqlQuery<GecmisHamSatir>($"""
                SELECT 'halkgunu' AS tur, k.id AS id, h.tarih AS tarih,
                       coalesce(h.baslik, 'Halk Günü') AS baslik,
                       k.durum::text AS durum_ad, k.gorusme_notu AS not_
                FROM halk_gunu_katilimlari k
                JOIN halk_gunleri h ON h.id = k.halk_gunu_id
                JOIN halk_gunu_basvurulari b ON b.id = k.basvuru_id
                WHERE h.birim_id = {birimId}
                  -- O ANKİ GÖRÜŞME GEÇMİŞE SAYILMAZ. Sayaç kendi kaydını da
                  -- topluyordu: ilk kez gelen vatandaşta bile "1 halk günü",
                  -- görüşme kaydedilince "1 kez görüşülmüş" yazıyordu.
                  AND ({haricKatilimId}::bigint IS NULL OR k.id <> {haricKatilimId}::bigint)
                  AND (
                        ({kuyruk}::text IS NOT NULL
                         AND regexp_replace(coalesce(b.telefon,''), '\D', '', 'g') LIKE '%' || {kuyruk}::text)
                     OR ({adDesen}::text IS NOT NULL
                         AND (coalesce(b.ad,'') || ' ' || coalesce(b.soyad,'')) ILIKE {adDesen}::text)
                      )
                ORDER BY h.tarih DESC
                LIMIT 20
                """)
            .ToListAsync();

        // ── protokol kaydı ──────────────────────────────────────────────
        //
        // Protokol KURUM GENELİ (birim süzgeci yok) — listenin kendisi zaten
        // öyle çalışıyor. Burada yalnızca "bu kişi protokolde mi" bilgisi
        // veriliyor, iletişim bilgisi değil.
        var protokol = await _context.Database
            .SqlQuery<string>($"""
                SELECT p.ad_soyad AS "Value"
                FROM protokoller p
                WHERE p.aktif
                  AND (
                        ({kuyruk}::text IS NOT NULL
                         AND (regexp_replace(coalesce(p.telefon,''), '\D', '', 'g') LIKE '%' || {kuyruk}::text
                           OR regexp_replace(coalesce(p.cep_telefon,''), '\D', '', 'g') LIKE '%' || {kuyruk}::text))
                     OR ({adDesen}::text IS NOT NULL AND p.ad_soyad ILIKE {adDesen}::text)
                      )
                LIMIT 1
                """)
            .ToListAsync();

        sonuc.TalepSayisi = talepler.Count;
        sonuc.EtkinlikSayisi = etkinlikler.Count;
        sonuc.HalkGunuSayisi = halkGunleri.Count;
        sonuc.GorusulenSayisi = halkGunleri.Count(h =>
            h.Durum_ad == ((int)KatilimDurumu.Gorusuldu).ToString());
        sonuc.ProtokolAd = protokol.FirstOrDefault();

        sonuc.Son = [.. talepler.Concat(etkinlikler).Concat(halkGunleri)
            .OrderByDescending(x => x.Tarih ?? DateTime.MinValue)
            .Take(15)
            .Select(x => new GecmisSatiriDto
            {
                Tur = x.Tur,
                Id = x.Id,
                Tarih = x.Tarih,
                Baslik = x.Baslik,
                // Halk günü satırında durum sayı olarak geliyor (enum),
                // etikete SUNUCUDA çevriliyor.
                DurumAd = x.Tur == "halkgunu" && int.TryParse(x.Durum_ad, out var s)
                    ? HalkGunuServisi.KatilimDurumAdi((KatilimDurumu)s)
                    : x.Durum_ad,
                Not = x.Not_,
            })];

        return sonuc;
    }

    // ── vatandaş dosyası ───────────────────────────────────────────────

    /// <summary>
    /// Kişinin talep, halk günü ve etkinlik geçmişini AYRINTISIYLA getirir.
    /// </summary>
    /// <remarks>
    /// Özet ucuyla aynı eşleştirme kuralını kullanır (son 8 hane, ham
    /// sütunda değil <c>regexp_replace</c> ile) — iki uç farklı kişi bulursa
    /// karttaki sayı ile dosyadaki liste çelişirdi.
    /// </remarks>
    public async Task<KisiDosyasiDto> KisiDosyasiAsync(
        string? telefon, string? ad, long? haricKatilimId = null)
    {
        var sade = Telefon.Duzelt(telefon);
        var adDesen = string.IsNullOrWhiteSpace(ad) ? null : $"%{ad.Trim()}%";

        var dosya = new KisiDosyasiDto { Telefon = telefon, AdSoyad = ad?.Trim() };
        if (sade is null && adDesen is null) return dosya;

        var kuyruk = sade is { Length: >= 8 } ? sade[^8..] : sade;

        // KURUM GENELİ — birim süzgeci YOK.
        //
        // Sistemin geri kalanı birime göre daraltıyor ama bu dosyanın işi
        // farklı: vatandaşın karşısında oturan kişi "siz daha önce ne için
        // gelmiştiniz" sorusunun cevabını istiyor ve vatandaş geçen sefer
        // BAŞKA bir müdürlüğe gitmiş olabilir. Birime göre daraltmak dosyayı
        // yarım gösteriyordu — yarım bir geçmiş de yanlış konuşturur
        // ("hiç gelmemişsiniz", oysa üç kez gelmiş).
        //
        // Karşılığında her satır HANGİ BİRİM olduğunu yazıyor. (Kurum geneline
        // açılan bilinçli ikinci istisna; birincisi özgeçmiş havuzu.)
        dosya.Talepler = await _context.Database
            .SqlQuery<KisiTalepDto>($"""
                SELECT r.id AS id, r.baslangic_tarih AS tarih,
                       r.konu AS konu, r.aciklama AS aciklama,
                       d.durum_ad AS durum_ad, d.renk AS durum_renk,
                       t.ad AS tip_ad, m.ad AS mahalle_ad, bi.ad AS birim_ad,
                       coalesce(r.ajanda_durum, false) AS ajandaya_eklendi,
                       coalesce(r.arsivlendi, false) AS arsivlendi
                FROM randevular r
                LEFT JOIN randevu_durumlar d ON d.id = r.randevu_durum_id
                LEFT JOIN randevu_tipleri t ON t.id = r.randevu_tip_id
                LEFT JOIN mahalleler m ON m.id = r.mahalle_id
                LEFT JOIN birimler bi ON bi.id = r.birim_id
                WHERE (
                        ({kuyruk}::text IS NOT NULL
                         AND regexp_replace(coalesce(r.telefon,''), '\D', '', 'g') LIKE '%' || {kuyruk}::text)
                     OR ({adDesen}::text IS NOT NULL
                         AND (coalesce(r.ad,'') || ' ' || coalesce(r.soyad,'')) ILIKE {adDesen}::text)
                      )
                ORDER BY r.baslangic_tarih DESC NULLS LAST
                LIMIT 100
                """)
            .ToListAsync();

        dosya.HalkGunleri = await _context.Database
            .SqlQuery<KisiHalkGunuDto>($"""
                SELECT k.id AS katilim_id, h.id AS halk_gunu_id, h.tarih AS tarih,
                       coalesce(h.baslik, 'Halk Günü') AS gun_baslik,
                       b.konu AS konu, k.gorusme_notu AS gorusme_notu,
                       k.durum::text AS durum_ad, bi.ad AS birim_ad,
                       coalesce(k.degerlendirmeye_esas, false) AS degerlendirmeye_esas,
                       k.olusan_randevu_id AS olusan_randevu_id
                FROM halk_gunu_katilimlari k
                JOIN halk_gunleri h ON h.id = k.halk_gunu_id
                JOIN halk_gunu_basvurulari b ON b.id = k.basvuru_id
                LEFT JOIN birimler bi ON bi.id = h.birim_id
                WHERE ({haricKatilimId}::bigint IS NULL OR k.id <> {haricKatilimId}::bigint)
                  AND (
                        ({kuyruk}::text IS NOT NULL
                         AND regexp_replace(coalesce(b.telefon,''), '\D', '', 'g') LIKE '%' || {kuyruk}::text)
                     OR ({adDesen}::text IS NOT NULL
                         AND (coalesce(b.ad,'') || ' ' || coalesce(b.soyad,'')) ILIKE {adDesen}::text)
                      )
                ORDER BY h.tarih DESC
                LIMIT 100
                """)
            .ToListAsync();

        // Durum sayı olarak geliyor (enum); etiket SUNUCUDA üretilir.
        foreach (var g in dosya.HalkGunleri)
        {
            if (int.TryParse(g.DurumAd, out var kod))
                g.DurumAd = HalkGunuServisi.KatilimDurumAdi((KatilimDurumu)kod);
        }

        // GİZLİ etkinlik dosyaya GİRMEZ: liste kurum geneline açıldı, gizlilik
        // kuralı sistemin geri kalanıyla aynı kalmalı.
        dosya.Etkinlikler = await _context.Database
            .SqlQuery<KisiEtkinlikDto>($"""
                SELECT a.id AS id, a.baslangic_tarihi AS tarih,
                       a.baslik AS baslik, a.konum AS konum, bi.ad AS birim_ad
                FROM ajandalar a
                LEFT JOIN birimler bi ON bi.id = a.birim_id
                WHERE NOT a.is_deleted
                  AND NOT a.gizli
                  AND (
                        ({kuyruk}::text IS NOT NULL
                         AND regexp_replace(coalesce(a.irtibat_telefon,''), '\D', '', 'g') LIKE '%' || {kuyruk}::text)
                     OR ({adDesen}::text IS NOT NULL
                         AND coalesce(a.irtibat_kisi,'') ILIKE {adDesen}::text)
                      )
                ORDER BY a.baslangic_tarihi DESC
                LIMIT 100
                """)
            .ToListAsync();

        var protokol = await _context.Database
            .SqlQuery<string>($"""
                SELECT p.ad_soyad AS "Value"
                FROM protokoller p
                WHERE p.aktif
                  AND (
                        ({kuyruk}::text IS NOT NULL
                         AND (regexp_replace(coalesce(p.telefon,''), '\D', '', 'g') LIKE '%' || {kuyruk}::text
                           OR regexp_replace(coalesce(p.cep_telefon,''), '\D', '', 'g') LIKE '%' || {kuyruk}::text))
                     OR ({adDesen}::text IS NOT NULL AND p.ad_soyad ILIKE {adDesen}::text)
                      )
                LIMIT 1
                """)
            .ToListAsync();

        dosya.ProtokolAd = protokol.FirstOrDefault();
        dosya.TalepSayisi = dosya.Talepler.Count;
        dosya.HalkGunuSayisi = dosya.HalkGunleri.Count;
        dosya.EtkinlikSayisi = dosya.Etkinlikler.Count;
        dosya.GorusulenSayisi = dosya.HalkGunleri.Count(h => h.DurumAd == "Görüşüldü");

        // "Ne zamandır tanışıyoruz" — üç kaynağın en eski ve en yeni tarihi.
        var tarihler = dosya.Talepler.Select(x => x.Tarih)
            .Concat(dosya.HalkGunleri.Select(x => x.Tarih))
            .Concat(dosya.Etkinlikler.Select(x => x.Tarih))
            .Where(t => t is not null)
            .Select(t => t!.Value)
            .ToList();
        if (tarihler.Count > 0)
        {
            dosya.IlkTemas = tarihler.Min();
            dosya.SonTemas = tarihler.Max();
        }

        return dosya;
    }

    /// <summary>Ham sorgu satırı — kolon adları SQL'deki takma adlarla eşleşir.</summary>
    private sealed class GecmisHamSatir
    {
        public string Tur { get; set; } = string.Empty;
        public long Id { get; set; }
        public DateTime? Tarih { get; set; }
        public string? Baslik { get; set; }
        public string? Durum_ad { get; set; }
        public string? Not_ { get; set; }
    }

    // ── toplu SMS ──────────────────────────────────────────────────────

    /// <summary>
    /// Listedeki herkese randevu saatini içeren SMS gönderir.
    /// </summary>
    /// <remarks>
    /// Alıcı bir sistem kullanıcısı değil, VATANDAŞ. `Messages.UserId`
    /// zorunlu olduğu için katılım kimliği yazılıyor — çiçekçiye gönderim de
    /// aynı yolu kullanıyor (<c>AjandaService</c>, çiçekçi kimliği).
    /// <c>NotifikasyonTip.Always</c>: vatandaşın bildirim tercihi yok.
    /// </remarks>
    public async Task<HalkGunuSmsSonucu> SmsGonderAsync(long halkGunuId, HalkGunuSmsIstegi istek)
    {
        if (string.IsNullOrWhiteSpace(istek.Mesaj))
        {
            throw new BusinessRuleException("Mesaj metni boş olamaz.");
        }

        var gun = await GunBulAsync(halkGunuId);

        var sorgu = _context.HalkGunuKatilimlari
            .Where(k => k.HalkGunuId == halkGunuId && k.Durum != KatilimDurumu.Iptal);

        if (istek.DilimId is { } dilimId) sorgu = sorgu.Where(k => k.DilimId == dilimId);

        var kayitlar = await sorgu
            .OrderBy(k => k.SiraNo)
            .Select(k => new
            {
                k.Id,
                k.SiraNo,
                k.SmsTarihi,
                Ad = k.Basvuru!.Ad,
                k.Basvuru.Soyad,
                Telefon = k.Basvuru.Telefon,
                Baslangic = k.Dilim != null ? k.Dilim.Baslangic : (DateTime?)null,
            })
            .ToListAsync();

        var sonuc = new HalkGunuSmsSonucu();
        var gonderilenler = new List<long>();

        foreach (var k in kayitlar)
        {
            if (istek.TekrarGonderme && k.SmsTarihi is not null)
            {
                sonuc.Atlanan++;
                continue;
            }

            // Telefonu olmayan atlanır ve SAYILIR: sekreterin eksik numarayı
            // görüp tamamlaması gerekiyor. Sessizce geçmek, kimin haber
            // almadığını görünmez kılardı.
            if (string.IsNullOrWhiteSpace(k.Telefon))
            {
                sonuc.Telefonsuz++;
                continue;
            }

            var degerler = SmsYerTutucu.HalkGunuDegerleri(
                ad: k.Ad,
                soyad: k.Soyad,
                tarih: gun.Tarih,
                saat: k.Baslangic,
                sira: k.SiraNo,
                konum: gun.Konum);

            var metin = SmsYerTutucu.Uygula(istek.Mesaj, degerler);

            await _mesaj.CreateAsync(
                k.Id, k.Telefon, $"{k.Ad} {k.Soyad}".Trim(), metin,
                SendMessageType.SMS, NotifikasyonTip.Always, null);

            gonderilenler.Add(k.Id);
            sonuc.Gonderilen++;
        }

        if (gonderilenler.Count > 0)
        {
            var simdi = DateTime.Now;
            await _context.HalkGunuKatilimlari
                .Where(k => gonderilenler.Contains(k.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(k => k.SmsTarihi, simdi));
        }

        await _context.SaveChangesAsync();
        return sonuc;
    }

    // ── Excel ──────────────────────────────────────────────────────────


    // ── talebe dönüştürme ──────────────────────────────────────────────

    /// <summary>
    /// "İlgilenilecek" işaretli bir görüşmeyi TALEBE çevirir.
    /// </summary>
    /// <remarks>
    /// Otomatik değil, elle: halk gününde konuşulan her şey iş değil ve her
    /// görüşmeden talep doğurmak talep listesini hızla kullanılmaz yapardı.
    /// Aynı katılım iki kez dönüştürülemez — ikinci çağrı hata verir, yoksa
    /// aynı iş birden çok birime düşerdi.
    /// </remarks>
    public async Task<long> TalebeDonusturAsync(long katilimId, TalepOlusturIstegi istek)
    {
        var katilim = await _context.HalkGunuKatilimlari
            .Include(k => k.Basvuru)
            .Include(k => k.HalkGunu)
            .FirstOrDefaultAsync(k => k.Id == katilimId)
            ?? throw new EntityNotFoundException("Katılım kaydı bulunamadı.");

        await GunBulAsync(katilim.HalkGunuId);

        if (katilim.OlusanRandevuId is { } mevcut)
        {
            throw new BusinessRuleException(
                $"Bu görüşme zaten {mevcut} numaralı talebe dönüştürülmüş.");
        }

        var basvuru = katilim.Basvuru!;
        var gun = katilim.HalkGunu!;

        // ŞEMA TUZAĞI: `Randevu` modelinde `MahalleId`/`RandevuDurumId`
        // `long?` görünüyor ama veritabanında NOT NULL. Halk günü başvurusunda
        // mahalle isteğe bağlı (vatandaş yalnızca telefon bırakmış olabilir),
        // dolayısıyla boş bırakırsak INSERT `23502` ile düşüyor ve kullanıcı
        // "talebe dönüştür" düğmesinde açıklamasız 500 alıyordu.
        var durumId = await _context.RandevuDurumlar
            .OrderBy(d => d.Id).Select(d => (long?)d.Id).FirstOrDefaultAsync();
        var tipId = istek.RandevuTipId ?? await _context.RandevuTipleri
            .OrderBy(t => t.Id).Select(t => (long?)t.Id).FirstOrDefaultAsync();

        var mahalleId = basvuru.MahalleId ?? await _context.Mahalleler
            .OrderBy(m => m.Id).Select(m => (long?)m.Id).FirstOrDefaultAsync();

        if (durumId is null || tipId is null || mahalleId is null)
        {
            throw new BusinessRuleException(
                "Talep oluşturmak için sistemde en az bir talep durumu, tipi ve mahalle tanımlı olmalı.");
        }

        var talep = new Randevu
        {
            Konu = Kisalt(istek.Konu ?? basvuru.Konu ?? $"Halk günü görüşmesi ({gun.Tarih:dd.MM.yyyy})", 100),
            Ad = basvuru.Ad,
            Soyad = basvuru.Soyad,
            Telefon = basvuru.Telefon,
            Adres = basvuru.Adres,
            Meslek = basvuru.Meslek,
            MahalleId = mahalleId,
            RandevuTipId = tipId,
            RandevuDurumId = durumId,
            BirimId = istek.BirimId ?? _kullanici.GetCurrentBirimId(),
            BaslangicTarih = gun.Tarih,
            BitisTarih = gun.Tarih,
            // Açıklamada görüşme notu taşınır: talebi alan birim, vatandaşın
            // ne anlattığını okumadan işe başlayamaz.
            Aciklama = string.Join("\n\n", new[]
            {
                istek.Aciklama,
                katilim.GorusmeNotu is null ? null : $"Halk günü görüşme notu:\n{katilim.GorusmeNotu}",
                katilim.DegerlendirmeNotu is null ? null : $"Değerlendirme:\n{katilim.DegerlendirmeNotu}",
            }.Where(x => !string.IsNullOrWhiteSpace(x))),
            OlusturmaTarih = DateTime.Now,
            Olusturan = _kullanici.GetUsername(),
        };

        _context.Randevular.Add(talep);
        await _context.SaveChangesAsync();

        katilim.OlusanRandevuId = talep.Id;
        katilim.GuncellemeTarihi = DateTime.Now;

        // Başvuru da talebe bağlanır: bir sonraki geçmiş sorgusunda iki kayıt
        // aynı kişi olarak görünsün.
        basvuru.RandevuId ??= talep.Id;

        await _context.SaveChangesAsync();
        return talep.Id;
    }

    // ── özet ───────────────────────────────────────────────────────────

    public async Task<HalkGunuOzetiDto> OzetAsync(long halkGunuId)
    {
        var gun = await GunBulAsync(halkGunuId);
        var katilimlar = await ((HalkGunuServisi)_halkGunu).KatilimlariGetirAsync(halkGunuId);

        var mahalleler = katilimlar
            .GroupBy(k => string.IsNullOrWhiteSpace(k.MahalleAd) ? "Belirtilmemiş" : k.MahalleAd!)
            .OrderByDescending(g => g.Count())
            .Take(15)
            .Select(g => new AdSayiDto { Ad = g.Key, Adet = g.Count() })
            .ToList();

        return new HalkGunuOzetiDto
        {
            Tarih = gun.Tarih,
            Baslik = gun.Baslik,
            Toplam = katilimlar.Count,
            Gorusulen = katilimlar.Count(k => k.Durum == KatilimDurumu.Gorusuldu),
            Gelmeyen = katilimlar.Count(k => k.Durum == KatilimDurumu.Gelmedi),
            Bekleyen = katilimlar.Count(k => k.Durum is KatilimDurumu.Bekliyor or KatilimDurumu.Geldi),
            TakipGerektiren = katilimlar.Count(k => k.DegerlendirmeyeEsas),
            TalebeDonusen = katilimlar.Count(k => k.OlusanRandevuId != null),
            Mahalleler = mahalleler,
            TakipListesi = [.. katilimlar.Where(k => k.DegerlendirmeyeEsas).OrderBy(k => k.SiraNo)],
        };
    }

    // ── ortak ──────────────────────────────────────────────────────────

    private async Task<HalkGunu> GunBulAsync(long id) =>
        await _context.HalkGunleri
            .FirstOrDefaultAsync(h => h.Id == id
                                   && h.BirimId == _kullanici.GetCurrentBirimId())
        ?? throw new EntityNotFoundException("Halk günü bulunamadı.");

    private static string Kisalt(string m, int enFazla) =>
        m.Length <= enFazla ? m : m[..enFazla];
}
