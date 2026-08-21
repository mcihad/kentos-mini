using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.Analiz;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Application.Services;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>
/// İSTATİSTİK MERKEZİ — konu başına pano.
/// </summary>
/// <remarks>
/// <para>
/// Merkez ekranındaki her kartın arkasında buradaki bir metot var. Hepsi
/// aynı <see cref="KonuIstatistigiDto"/> şeklini döndürüyor; istemcide tek
/// bir çizici olmasının sebebi bu.
/// </para>
/// <para>
/// <b>Görünürlük kapıları KORUNUR.</b> Bir istatistik ucu, listede
/// göremediğin kaydı sayıyorsa gizliliği delmiş olur: sayı da bir bilgidir
/// ("bu birimde kaç gizli görüşme var" sorusunun cevabı). Bu yüzden her
/// metot kendi modülünün kapısını birebir tekrarlar ve hangi kapıyı
/// kullandığını yorumda yazar.
/// </para>
/// <para>
/// <b>Gruplama VERİTABANINDA yapılır.</b> Kayıtları çekip bellekte saymak
/// iki yıllık veride on binlerce satır taşımak demek; her dağılım tek bir
/// <c>GroupBy</c> sorgusu.
/// </para>
/// </remarks>
public interface IIstatistikMerkeziServisi
{
    Task<KonuIstatistigiDto> HalkGunuAsync(DateTime? bas, DateTime? bit, CancellationToken iptal = default);
    Task<KonuIstatistigiDto> FormAsync(DateTime? bas, DateTime? bit, CancellationToken iptal = default);
    Task<KonuIstatistigiDto> ProtokolAsync(DateTime? bas, DateTime? bit, CancellationToken iptal = default);
    Task<KonuIstatistigiDto> CicekAsync(DateTime? bas, DateTime? bit, CancellationToken iptal = default);
    Task<KonuIstatistigiDto> OzgecmisAsync(DateTime? bas, DateTime? bit, CancellationToken iptal = default);
    Task<KonuIstatistigiDto> SistemAsync(DateTime? bas, DateTime? bit, CancellationToken iptal = default);
}

/// <inheritdoc cref="IIstatistikMerkeziServisi"/>
public class IstatistikMerkeziServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici) : IIstatistikMerkeziServisi
{
    /// <summary>Dağılım listelerinde gösterilecek en fazla dilim.</summary>
    /// <remarks>
    /// Kuyruk "Diğer"de toplanır. Sınırsız bırakılsaydı 300 mahalleli bir
    /// dağılım ekranı da yanıtı da kullanılmaz hâle getirirdi.
    /// </remarks>
    private const int EnFazlaDilim = 8;

    /// <summary>Aralık verilmediğinde bakılan geçmiş.</summary>
    private const int VarsayilanAy = 12;

    // ── HALK GÜNÜ ──────────────────────────────────────────────────────

    /// <remarks>
    /// Kapı <c>BirimId == etkin birim</c> — <c>HalkGunuServisi.GorunurGunler</c>
    /// ile birebir aynı. Ayrışırlarsa listede 8, istatistikte 80 kayıt görünür.
    /// </remarks>
    public async Task<KonuIstatistigiDto> HalkGunuAsync(
        DateTime? bas, DateTime? bit, CancellationToken iptal = default)
    {
        var (b, s) = Aralik(bas, bit);
        var birim = _kullanici.GetCurrentBirimId();

        var gunler = _context.HalkGunleri.AsNoTracking()
            .Where(h => h.BirimId == birim && h.Tarih >= b && h.Tarih < s);

        var katilimlar = _context.HalkGunuKatilimlari.AsNoTracking()
            .Where(k => k.HalkGunu != null
                && k.HalkGunu.BirimId == birim
                && k.HalkGunu.Tarih >= b && k.HalkGunu.Tarih < s);

        // Havuz gün tarihine değil BAŞVURU tarihine göre süzülür: bekleyen
        // bir başvurunun henüz bir günü yok.
        var basvurular = _context.HalkGunuBasvurulari.AsNoTracking()
            .Where(x => x.BirimId == birim && x.OlusturmaTarihi >= b && x.OlusturmaTarihi < s);

        var gunSayisi = await gunler.CountAsync(iptal);
        var katilimSayisi = await katilimlar.CountAsync(iptal);
        var gorusulen = await katilimlar.CountAsync(k => k.Durum == KatilimDurumu.Gorusuldu, iptal);
        var gelmeyen = await katilimlar.CountAsync(k => k.Durum == KatilimDurumu.Gelmedi, iptal);
        var takipli = await katilimlar.CountAsync(k => k.DegerlendirmeyeEsas, iptal);
        var talepOlan = await katilimlar.CountAsync(k => k.OlusanRandevuId != null, iptal);
        var bekleyen = await _context.HalkGunuBasvurulari.AsNoTracking()
            .CountAsync(x => x.BirimId == birim && x.Durum == BasvuruDurumu.Bekliyor, iptal);

        var durumlar = await katilimlar
            .GroupBy(k => k.Durum)
            .Select(g => new { g.Key, Adet = g.Count() })
            .ToListAsync(iptal);

        var basvuruDurumlari = await basvurular
            .GroupBy(x => x.Durum)
            .Select(g => new { g.Key, Adet = g.Count() })
            .ToListAsync(iptal);

        var mahalleler = await katilimlar
            .Where(k => k.Basvuru != null && k.Basvuru.Mahalle != null)
            .GroupBy(k => k.Basvuru!.Mahalle!.Ad)
            .Select(g => new { Etiket = g.Key, Adet = g.Count() })
            .ToListAsync(iptal);

        return new KonuIstatistigiDto
        {
            Konu = "halk-gunu",
            Baslik = "Halk Günü",
            Karolar =
            [
                Karo("Halk günü", gunSayisi, "seçili dönemde"),
                Karo("Görüşme", katilimSayisi, "atanan vatandaş"),
                Oran("Görüşülen", gorusulen, katilimSayisi, "sırası gelen"),
                Oran("Gelmeyen", gelmeyen, katilimSayisi, "atandığı hâlde", tersTon: true),
                Karo("Takibe alınan", takipli, "ilgilenilecek işareti"),
                Karo("Talebe dönüşen", talepOlan, "kayıt açıldı"),
                Karo("Havuzda bekleyen", bekleyen, "henüz atanmadı",
                    ton: bekleyen > 0 ? "uyari" : null),
            ],
            Bolumler =
            [
                Bolum("Görüşme sonucu", Dilimler(durumlar.Select(x => (Ad(x.Key), x.Adet))), "halka"),
                Bolum("Havuz durumu", Dilimler(basvuruDurumlari.Select(x => (Ad(x.Key), x.Adet)))),
                Bolum("Mahalleye göre", Dilimler(mahalleler.Select(x => (x.Etiket, x.Adet))),
                    aciklama: "En çok başvuran mahalleler"),
            ],
            Seyir = await AySeyriAsync(katilimlar.Select(k => k.HalkGunu!.Tarih), b, s, iptal),
            SeyirEtiketi = "Aylık görüşme sayısı",
        };
    }

    // ── FORM VE ANKET ──────────────────────────────────────────────────

    /// <remarks>
    /// <b>Form BAŞINA özet zaten var</b> (<c>FormYanitServisi.OzetAsync</c>);
    /// buradaki soru farklı: "hangi formu kaç kişi doldurdu, hangisi ölü".
    /// Silinen formlar sayılmaz.
    /// </remarks>
    public async Task<KonuIstatistigiDto> FormAsync(
        DateTime? bas, DateTime? bit, CancellationToken iptal = default)
    {
        var (b, s) = Aralik(bas, bit);

        var formlar = _context.Formlar.AsNoTracking().Where(f => !f.Silindi);

        // Yanıtlar GÖNDERİM tarihine göre; taslak ve geçersiz sayılmaz.
        var yanitlar = _context.FormYanitlari.AsNoTracking()
            .Where(y => y.Durum == FormYanitDurumu.Gonderildi
                && y.GonderimTarihi >= b && y.GonderimTarihi < s);

        var toplamForm = await formlar.CountAsync(iptal);
        var yayinda = await formlar.CountAsync(f => f.Durum == FormDurumu.Yayinda, iptal);
        var toplamYanit = await yanitlar.CountAsync(iptal);

        // Taslak = başlanmış ama bitirilmemiş. Terk oranının tek göstergesi.
        var taslak = await _context.FormYanitlari.AsNoTracking()
            .CountAsync(y => y.Durum == FormYanitDurumu.Taslak
                && y.BaslamaTarihi >= b && y.BaslamaTarihi < s, iptal);

        var yanitAlmayan = await formlar
            .CountAsync(f => f.Durum == FormDurumu.Yayinda && f.YanitSayisi == 0, iptal);

        var durumlar = await formlar
            .GroupBy(f => f.Durum)
            .Select(g => new { g.Key, Adet = g.Count() })
            .ToListAsync(iptal);

        var enCokDolduran = await yanitlar
            .Where(y => y.Form != null)
            .GroupBy(y => y.Form!.Baslik)
            .Select(g => new { Etiket = g.Key, Adet = g.Count() })
            .ToListAsync(iptal);

        return new KonuIstatistigiDto
        {
            Konu = "form",
            Baslik = "Form ve Anket",
            Karolar =
            [
                Karo("Form", toplamForm, "silinmemiş"),
                Karo("Yayında", yayinda, "yanıt kabul ediyor"),
                Karo("Yanıt", toplamYanit, "seçili dönemde"),
                Karo("Yarım kalan", taslak, "gönderilmedi",
                    ton: taslak > toplamYanit ? "uyari" : null),
                Karo("Hiç yanıt almayan", yanitAlmayan, "yayında olduğu hâlde",
                    ton: yanitAlmayan > 0 ? "uyari" : null),
            ],
            Bolumler =
            [
                Bolum("Form durumu", Dilimler(durumlar.Select(x => (Ad(x.Key), x.Adet))), "halka"),
                Bolum("En çok doldurulan", Dilimler(enCokDolduran.Select(x => (x.Etiket, x.Adet))),
                    aciklama: "Seçili dönemdeki yanıt sayısına göre"),
            ],
            Seyir = await AySeyriAsync(yanitlar.Select(y => y.GonderimTarihi!.Value), b, s, iptal),
            SeyirEtiketi = "Aylık yanıt sayısı",
        };
    }

    // ── PROTOKOL VE DAVET ──────────────────────────────────────────────

    /// <remarks>
    /// <b>Protokol defteri KURUM GENELİ, davet listeleri BİRİME ait</b> —
    /// <c>DavetServisi.GorunurOlanlar</c> ile aynı asimetri. Defteri birime
    /// süzmek yanlış olurdu (aynı vali yardımcısını her birim ayrı
    /// saymazdı), davetleri süzmemek de yanlış: başka birimin tören
    /// listesi bu birimi ilgilendirmiyor.
    /// </remarks>
    public async Task<KonuIstatistigiDto> ProtokolAsync(
        DateTime? bas, DateTime? bit, CancellationToken iptal = default)
    {
        var (b, s) = Aralik(bas, bit);
        var birim = _kullanici.GetCurrentBirimId();

        var protokol = _context.Protokoller.AsNoTracking();
        var davetler = _context.Davetler.AsNoTracking()
            .Where(d => d.BirimId == birim && d.OlusturmaTarihi >= b && d.OlusturmaTarihi < s);

        var kisiler = _context.DavetKisileri.AsNoTracking()
            .Where(k => k.Davet != null && k.Davet.BirimId == birim
                && k.Davet.OlusturmaTarihi >= b && k.Davet.OlusturmaTarihi < s);

        var toplamKisi = await protokol.CountAsync(iptal);
        var aktifKisi = await protokol.CountAsync(p => p.Aktif, iptal);
        var davetSayisi = await davetler.CountAsync(iptal);
        var cagrilan = await kisiler.CountAsync(iptal);
        var arandi = await kisiler.CountAsync(k => k.Arandi, iptal);
        var katilacak = await kisiler.CountAsync(k => k.Durum == DavetDurumu.Katilacak, iptal);

        var kategoriler = await protokol
            .Where(p => p.Kategori != null)
            .GroupBy(p => p.Kategori!.Ad)
            .Select(g => new { Etiket = g.Key, Adet = g.Count() })
            .ToListAsync(iptal);

        var cevaplar = await kisiler
            .GroupBy(k => k.Durum)
            .Select(g => new { g.Key, Adet = g.Count() })
            .ToListAsync(iptal);

        return new KonuIstatistigiDto
        {
            Konu = "protokol",
            Baslik = "Protokol ve Davet",
            Karolar =
            [
                Karo("Protokol kaydı", toplamKisi, "kurum geneli"),
                Karo("Aktif", aktifKisi, "listelerde çıkan"),
                Karo("Davet listesi", davetSayisi, "seçili dönemde"),
                Karo("Çağrılan kişi", cagrilan, "tüm listelerde"),
                Oran("Arandı", arandi, cagrilan, "telefonla ulaşıldı"),
                Oran("Katılacak", katilacak, cagrilan, "olumlu cevap"),
            ],
            Bolumler =
            [
                Bolum("Davet cevabı", Dilimler(cevaplar.Select(x => (Ad(x.Key), x.Adet))), "halka"),
                Bolum("Protokol kategorisi", Dilimler(kategoriler.Select(x => (x.Etiket, x.Adet))),
                    aciklama: "Defterdeki kişi sayısına göre"),
            ],
            Seyir = await AySeyriAsync(davetler.Select(d => d.OlusturmaTarihi), b, s, iptal),
            SeyirEtiketi = "Aylık davet listesi",
        };
    }

    // ── ÇİÇEK ──────────────────────────────────────────────────────────

    /// <remarks>
    /// <b>Birim kapısı YOK ve bilinçli</b> — çiçekçi hesabı kurum geneli bir
    /// iş; talimatı veren birim ile ödemeyi yapan birim aynı olmayabiliyor
    /// (aynı gerekçe çiçekçi dosyası ucunda da yazılı). Gizli etkinlikler
    /// zaten çiçek talimatı üretmiyor, yani buradan gizli bir kayıt sızmaz.
    /// </remarks>
    public async Task<KonuIstatistigiDto> CicekAsync(
        DateTime? bas, DateTime? bit, CancellationToken iptal = default)
    {
        var (b, s) = Aralik(bas, bit);

        // Süzgeç TALİMATIN OLUŞTURULMA tarihine göre, gönderilmesine göre
        // değil: henüz gönderilmemiş talimatlar da dönemin içinde sayılmalı.
        var cicekler = _context.Cicekler.AsNoTracking()
            .Where(c => c.OlusturulmaTarihi >= b && c.OlusturulmaTarihi < s);

        var toplam = await cicekler.CountAsync(iptal);
        var teslim = await cicekler.CountAsync(c => c.Gonderildi, iptal);
        var fotografli = await cicekler.CountAsync(c => c.Resim != null && c.Resim != "", iptal);
        var cicekciSayisi = await _context.Cicekciler.AsNoTracking().CountAsync(iptal);

        var cicekciler = await cicekler
            .Where(c => c.CicekciId > 0)
            .GroupBy(c => c.CicekciId)
            .Select(g => new { g.Key, Adet = g.Count() })
            .ToListAsync(iptal);

        // Çiçekçi adları AYRI okunuyor: gruplamanın içinde bir navigation
        // property'ye gitmek sorguyu satır başına bir okumaya çeviriyordu.
        var adlar = await _context.Cicekciler.AsNoTracking()
            .Select(c => new { c.Id, c.AdSoyad })
            .ToDictionaryAsync(x => x.Id, x => x.AdSoyad ?? "—", iptal);

        return new KonuIstatistigiDto
        {
            Konu = "cicek",
            Baslik = "Çiçek Gönderi",
            Karolar =
            [
                Karo("Talimat", toplam, "seçili dönemde"),
                Oran("Teslim edilen", teslim, toplam, "çiçekçi işaretledi"),
                Oran("Bekleyen", toplam - teslim, toplam, "henüz teslim yok",
                    tersTon: true),
                Oran("Fotoğraflı", fotografli, teslim, "teslim kanıtı"),
                Karo("Çiçekçi", cicekciSayisi, "kayıtlı"),
            ],
            Bolumler =
            [
                Bolum("Çiçekçiye göre",
                    Dilimler(cicekciler.Select(x => (adlar.GetValueOrDefault(x.Key, "—"), x.Adet))),
                    aciklama: "Dönem içindeki talimat sayısı"),
            ],
            Seyir = await AySeyriAsync(cicekler.Select(c => c.OlusturulmaTarihi), b, s, iptal),
            SeyirEtiketi = "Aylık talimat sayısı",
        };
    }

    // ── ÖZGEÇMİŞ ───────────────────────────────────────────────────────

    /// <remarks>
    /// <b>Birim süzgeci YOK — kasıtlı istisna.</b> Havuzun varlık sebebi
    /// kaydın birimler arasında dolaşabilmesi; birim süzgeci eklemek modülü
    /// işlevsiz bırakır (<c>OzgecmisHavuzuTests.Havuz_birim_suzgecinden_gecmez</c>
    /// bunu kilitliyor). Birim yine de <b>dağılım</b> olarak gösteriliyor.
    /// </remarks>
    public async Task<KonuIstatistigiDto> OzgecmisAsync(
        DateTime? bas, DateTime? bit, CancellationToken iptal = default)
    {
        var (b, s) = Aralik(bas, bit);

        var kayitlar = _context.Ozgecmisler.AsNoTracking()
            .Where(o => !o.IsDeleted && o.OlusturmaTarihi >= b && o.OlusturmaTarihi < s);

        var toplam = await kayitlar.CountAsync(iptal);
        var talepten = await kayitlar.CountAsync(o => o.RandevuId != null, iptal);

        var paylasimlar = _context.OzgecmisPaylasimlari.AsNoTracking()
            .Where(p => p.Tarih >= b && p.Tarih < s);

        var paylasimSayisi = await paylasimlar.CountAsync(iptal);
        var goruldu = await paylasimlar.CountAsync(p => p.GoruntulemeTarihi != null, iptal);

        var meslekler = await kayitlar
            .Where(o => o.MeslekAd != null && o.MeslekAd != "")
            .GroupBy(o => o.MeslekAd!)
            .Select(g => new { Etiket = g.Key, Adet = g.Count() })
            .ToListAsync(iptal);

        var birimler = await kayitlar
            .Where(o => o.BirimId != null)
            .GroupBy(o => o.BirimId!.Value)
            .Select(g => new { g.Key, Adet = g.Count() })
            .ToListAsync(iptal);

        var birimAdlari = await _context.Birimler.AsNoTracking()
            .Select(x => new { x.Id, x.Ad })
            .ToDictionaryAsync(x => x.Id, x => x.Ad ?? "—", iptal);

        return new KonuIstatistigiDto
        {
            Konu = "ozgecmis",
            Baslik = "Özgeçmiş Havuzu",
            Karolar =
            [
                Karo("Özgeçmiş", toplam, "seçili dönemde"),
                Oran("Talepten gelen", talepten, toplam, "iş talebiyle"),
                Karo("Havuza eklenen", toplam - talepten, "doğrudan yükleme"),
                Karo("Paylaşım", paylasimSayisi, "birimler arası"),
                Oran("Açıldı", goruldu, paylasimSayisi, "alıcı görüntüledi"),
            ],
            Bolumler =
            [
                Bolum("Mesleğe göre", Dilimler(meslekler.Select(x => (x.Etiket, x.Adet))),
                    aciklama: "\"Elimizde kaynakçı var mı\" sorusunun cevabı"),
                Bolum("Yükleyen birim",
                    Dilimler(birimler.Select(x => (birimAdlari.GetValueOrDefault(x.Key, "—"), x.Adet))),
                    aciklama: "Kayıt birime kilitli DEĞİL; yalnızca nereden geldiğini gösterir"),
            ],
            Seyir = await AySeyriAsync(kayitlar.Select(o => o.OlusturmaTarihi), b, s, iptal),
            SeyirEtiketi = "Aylık özgeçmiş",
        };
    }

    // ── SİSTEM ─────────────────────────────────────────────────────────

    /// <remarks>
    /// <b>Yalnızca <c>Sistem</c> rolü.</b> Kapı controller'da; burada ek bir
    /// süzgeç yok çünkü sistem sağlığı kurum geneli. Yığın izi, istek gövdesi
    /// ve IP <b>dönmüyor</b> — pano sayı gösteriyor, kanıt göstermiyor;
    /// ayrıntı için hata ekranı var.
    /// </remarks>
    public async Task<KonuIstatistigiDto> SistemAsync(
        DateTime? bas, DateTime? bit, CancellationToken iptal = default)
    {
        var (b, s) = Aralik(bas, bit);

        var hatalar = _context.SistemHatalari.AsNoTracking()
            .Where(h => h.SonGorulme >= b && h.SonGorulme < s);

        var farkliHata = await hatalar.CountAsync(iptal);
        var toplamDusme = await hatalar.SumAsync(h => (int?)h.Adet, iptal) ?? 0;
        var cozulen = await hatalar.CountAsync(h => h.Cozuldu, iptal);

        var oturumlar = _context.OturumKayitlari.AsNoTracking()
            .Where(o => o.Tarih >= b && o.Tarih < s);

        var girisSayisi = await oturumlar.CountAsync(iptal);
        var basarisiz = await oturumlar.CountAsync(o => !o.Basarili, iptal);

        var enSikHatalar = await hatalar
            .GroupBy(h => h.Tur)
            .Select(g => new { Etiket = g.Key, Adet = g.Sum(x => x.Adet) })
            .ToListAsync(iptal);

        var uclar = await hatalar
            .Where(h => h.Yol != null && h.Yol != "")
            .GroupBy(h => h.Yol!)
            .Select(g => new { Etiket = g.Key, Adet = g.Sum(x => x.Adet) })
            .ToListAsync(iptal);

        var kodlar = await hatalar
            .GroupBy(h => h.DurumKodu)
            .Select(g => new { g.Key, Adet = g.Sum(x => x.Adet) })
            .ToListAsync(iptal);

        return new KonuIstatistigiDto
        {
            Konu = "sistem",
            Baslik = "Sistem Sağlığı",
            Karolar =
            [
                Karo("Farklı hata", farkliHata, "parmakizine göre",
                    ton: farkliHata > 0 ? "uyari" : "iyi"),
                Karo("Toplam düşme", toplamDusme, "aynı hata tekrarı dahil"),
                Oran("Çözüldü", cozulen, farkliHata, "işaretlenen"),
                Karo("Giriş denemesi", girisSayisi, "seçili dönemde"),
                Oran("Başarısız giriş", basarisiz, girisSayisi, "hatalı parola",
                    tersTon: true),
            ],
            Bolumler =
            [
                Bolum("En sık hata türü", Dilimler(enSikHatalar.Select(x => (Kisalt(x.Etiket), x.Adet)))),
                Bolum("Hata veren uç", Dilimler(uclar.Select(x => (x.Etiket, x.Adet))),
                    aciklama: "Düşme sayısına göre"),
                Bolum("HTTP kodu",
                    Dilimler(kodlar.Select(x => (x.Key == 0 ? "—" : x.Key.ToString(), x.Adet))), "halka"),
            ],
            Seyir = await AySeyriAsync(hatalar.Select(h => h.SonGorulme), b, s, iptal),
            SeyirEtiketi = "Aylık farklı hata",
        };
    }

    // ── ortak yardımcılar ──────────────────────────────────────────────

    /// <summary>
    /// Aralığı normalize eder; bitiş günü DAHİL.
    /// </summary>
    /// <remarks>
    /// Üst sınır <c>&lt; bitiş + 1 gün</c> olarak kuruluyor: <c>&lt;=</c>
    /// kullanılsaydı son günün saat 00:00'dan sonraki kayıtları dışarıda
    /// kalırdı — zaman damgaları <c>timestamp without time zone</c> ve saat
    /// bilgisi taşıyor.
    /// </remarks>
    private static (DateTime Bas, DateTime Bit) Aralik(DateTime? bas, DateTime? bit)
    {
        var ustGun = bit?.Date ?? DateTime.Now.Date;
        var son = ustGun.AddDays(1);

        // Varsayılan AY BAŞINDAN başlar: `son.AddMonths(-12)` dendiğinde ilk
        // ve son aylar yarım kalıyor ve seyir 12 yerine 13 sütun çiziyordu —
        // "son 12 ay" diyen bir grafikte 13 sütun okuyucuya yanlış geliyor.
        var ilk = bas?.Date
            ?? new DateTime(ustGun.Year, ustGun.Month, 1).AddMonths(-(VarsayilanAy - 1));

        return ilk < son ? (ilk, son) : (new DateTime(ustGun.Year, ustGun.Month, 1), son);
    }

    /// <remarks>
    /// Biçim <b>açıkça tr-TR</b>: süreç kültürüne bırakılsaydı yayın
    /// makinesinde binlik ayracı noktadan virgüle dönebilir ve "1.348"
    /// ekranda "1,348" diye okunurdu. Ay kısaltmalarında da aynı kültür.
    /// </remarks>
    private static IstatistikKarosuDto Karo(
        string etiket, int deger, string? altMetin = null, string? ton = null)
        => new()
        {
            Etiket = etiket,
            Deger = deger.ToString("N0", TurkceKultur),
            AltMetin = altMetin,
            Ton = ton,
        };

    /// <summary>Sayı + yüzde karosu; payda sıfırsa yüzde YAZILMAZ.</summary>
    /// <remarks>
    /// Payda sıfırken "%0" yazmak yanlış bir şey söylüyor: hiç kayıt yokken
    /// "hiçbiri teslim edilmedi" değil "ölçecek bir şey yok" doğru cümle.
    /// </remarks>
    private static IstatistikKarosuDto Oran(
        string etiket, int deger, int toplam, string? altMetin = null, bool tersTon = false)
    {
        var yuzde = toplam > 0 ? Math.Round(deger * 100.0 / toplam, 1) : (double?)null;

        return new IstatistikKarosuDto
        {
            Etiket = etiket,
            Deger = deger.ToString("N0", TurkceKultur),
            AltMetin = yuzde is { } y
                ? $"%{y.ToString("0.#", TurkceKultur)} · {altMetin}"
                : altMetin,
            Ton = yuzde is null ? null
                : tersTon ? (y2(yuzde.Value) ? "kotu" : null)
                : (yuzde.Value >= 80 ? "iyi" : null),
        };

        static bool y2(double y) => y >= 20;
    }

    private static IstatistikBolumuDto Bolum(
        string baslik, List<IstatistikDilimDto> dilimler,
        string gorunum = "cubuk", string? aciklama = null)
        => new() { Baslik = baslik, Dilimler = dilimler, Gorunum = gorunum, Aciklama = aciklama };

    /// <summary>
    /// Ham sayımı yüzdeli dilimlere çevirir; kuyruğu "Diğer"de toplar.
    /// </summary>
    private static List<IstatistikDilimDto> Dilimler(IEnumerable<(string Etiket, int Adet)> kaynak)
    {
        var liste = kaynak.Where(x => x.Adet > 0).OrderByDescending(x => x.Adet).ToList();
        var toplam = liste.Sum(x => x.Adet);
        if (toplam == 0) return [];

        var gosterilen = liste.Take(EnFazlaDilim).ToList();
        var kuyruk = liste.Skip(EnFazlaDilim).Sum(x => x.Adet);

        if (kuyruk > 0) gosterilen.Add(("Diğer", kuyruk));

        return [.. gosterilen.Select(x => new IstatistikDilimDto
        {
            Etiket = string.IsNullOrWhiteSpace(x.Etiket) ? "—" : x.Etiket,
            Deger = x.Adet,
            Yuzde = Math.Round(x.Adet * 100.0 / toplam, 1),
        })];
    }

    /// <summary>
    /// Aylık seyir — BOŞ AYLAR DA DOLDURULUR.
    /// </summary>
    /// <remarks>
    /// Yalnızca kaydı olan aylar dönseydi grafik, hiç kayıt gelmeyen bir ayı
    /// atlar ve çizgi "kesintisiz devam ediyor" gibi okunurdu.
    /// </remarks>
    private static async Task<List<IstatistikSeriNoktasiDto>> AySeyriAsync(
        IQueryable<DateTime> tarihler, DateTime bas, DateTime bit, CancellationToken iptal)
    {
        var sayimlar = await tarihler
            .GroupBy(t => new { t.Year, t.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Adet = g.Count() })
            .ToListAsync(iptal);

        var harita = sayimlar.ToDictionary(x => (x.Year, x.Month), x => x.Adet);
        var nokta = new List<IstatistikSeriNoktasiDto>();

        for (var ay = new DateTime(bas.Year, bas.Month, 1); ay < bit; ay = ay.AddMonths(1))
        {
            nokta.Add(new IstatistikSeriNoktasiDto
            {
                Etiket = ay.ToString("MMM yy", TurkceKultur),
                Tarih = ay.ToString("yyyy-MM-dd"),
                Deger = harita.GetValueOrDefault((ay.Year, ay.Month)),
            });
        }

        return nokta;
    }

    /// <summary>Ay kısaltmaları Türkçe olmalı — "Oca", "Şub".</summary>
    private static readonly System.Globalization.CultureInfo TurkceKultur = new("tr-TR");

    /// <summary>Uzun tip adını okunur kılar: <c>Foo.Bar.BazException</c> → <c>BazException</c>.</summary>
    private static string Kisalt(string tam)
    {
        var i = tam.LastIndexOf('.');
        return i >= 0 && i < tam.Length - 1 ? tam[(i + 1)..] : tam;
    }

    private static string Ad(KatilimDurumu d) => d switch
    {
        KatilimDurumu.Bekliyor => "Bekliyor",
        KatilimDurumu.Geldi => "Geldi",
        KatilimDurumu.Gelmedi => "Gelmedi",
        KatilimDurumu.Gorusuldu => "Görüşüldü",
        KatilimDurumu.Iptal => "İptal",
        _ => "—",
    };

    private static string Ad(BasvuruDurumu d) => d switch
    {
        BasvuruDurumu.Bekliyor => "Havuzda bekliyor",
        BasvuruDurumu.Atandi => "Bir güne atandı",
        BasvuruDurumu.Gorusuldu => "Görüşüldü",
        BasvuruDurumu.Iptal => "İptal",
        _ => "Reddedildi",
    };

    private static string Ad(FormDurumu d) => d switch
    {
        FormDurumu.Taslak => "Taslak",
        FormDurumu.Yayinda => "Yayında",
        FormDurumu.Kapali => "Kapalı",
        _ => "Arşiv",
    };

    private static string Ad(DavetDurumu d) => d switch
    {
        DavetDurumu.Beklemede => "Cevap yok",
        DavetDurumu.Katilacak => "Katılacak",
        DavetDurumu.Katilmayacak => "Katılmayacak",
        _ => "—",
    };
}
