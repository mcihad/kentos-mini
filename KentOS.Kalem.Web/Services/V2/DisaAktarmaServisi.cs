using System.Globalization;
using System.Text.Json.Serialization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Extensions;
using KentOS.Kalem.Application.Services;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>Dışa aktarma süzgeci.</summary>
public class DisaAktarmaSuzgeci
{
    [JsonPropertyName("ara")]
    public string? Ara { get; set; }
    [JsonPropertyName("baslangic")]
    public DateTime? Baslangic { get; set; }
    [JsonPropertyName("bitis")]
    public DateTime? Bitis { get; set; }
    [JsonPropertyName("tipId")]
    public long? TipId { get; set; }
    [JsonPropertyName("durumId")]
    public long? DurumId { get; set; }
    [JsonPropertyName("altBirimlerDahil")]
    public bool AltBirimlerDahil { get; set; }
}

/// <summary>Üretilen dosya.</summary>
public record DisaAktarmaDosyasi(
    [property: JsonPropertyName("icerik")] byte[] Icerik,
    [property: JsonPropertyName("dosyaAdi")] string DosyaAdi,
    [property: JsonPropertyName("icerikTuru")] string IcerikTuru);

/// <summary>Günlük program çıktısının yerleşimi.</summary>
public enum ProgramTasarimi
{
    /// <summary>Standart tablo — saat, konu, yer.</summary>
    Standart = 1,
    /// <summary>Kompakt — tek sütun, yalnızca saat + konu.</summary>
    Kompakt = 2,
    /// <summary>Detaylı — irtibat, katılım ve hazırlık bilgileri dahil.</summary>
    Detayli = 3,
    /// <summary>Boş not sayfası — program satırlarının yanında el yazısı alanı.</summary>
    BosNot = 4,
    /// <summary>Dikey saat şeridi — günün akışı görsel olarak.</summary>
    SaatSeridi = 5,
    /// <summary>Pano — duvara asılan, uzaktan okunan büyük punto.</summary>
    Pano = 6,
}

public interface IDisaAktarmaServisi
{
    Task<DisaAktarmaDosyasi> EtkinlikExcelAsync(DisaAktarmaSuzgeci suzgec);
    Task<DisaAktarmaDosyasi> EtkinlikPdfAsync(DisaAktarmaSuzgeci suzgec);
    Task<DisaAktarmaDosyasi> TalepExcelAsync(DisaAktarmaSuzgeci suzgec);
    Task<DisaAktarmaDosyasi> TalepPdfAsync(DisaAktarmaSuzgeci suzgec);

    /// <summary>Belirli bir günün programı, seçilen yerleşimde (PDF).</summary>
    Task<DisaAktarmaDosyasi> GunlukProgramAsync(DateTime gun, ProgramTasarimi tasarim);

    /// <summary>Aynı programın yazdırılabilir HTML çıktısı.</summary>
    Task<string> GunlukProgramHtmlAsync(DateTime gun, ProgramTasarimi tasarim);
}

/// <summary>
/// Etkinlik ve talep listelerinin Excel / PDF çıktısı.
///
/// <para>
/// Eski arayüzde dört ayrı controller eylemi vardı ve her biri kendi
/// süzgeç ayrıştırmasını, kendi başlık satırını, kendi tarih biçimini
/// yazıyordu. Aynı listenin Excel'i ile PDF'i farklı sonuç verebiliyordu.
/// Burada süzgeç TEK yerde kuruluyor, biçimlendirme ayrı.
/// </para>
///
/// <para>
/// <b>Gizlilik:</b> etkinlik sorgusu <c>GorunurOlanlar</c> ile başlar. Dışa
/// aktarma, gizli etkinlikleri sızdırmanın en kolay yoludur — dosya bir kez
/// üretildikten sonra kimin eline geçtiği izlenemez.
/// </para>
/// </summary>
public class DisaAktarmaServisi(
    AppDbContext _context,
    ICurrentUserService _mevcutKullanici,
    IInstitutionService _kurum) : IDisaAktarmaServisi
{
    private const string ExcelTuru = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string PdfTuru = "application/pdf";

    /// <summary>Başlıkları büyütürken Türkçe kuralları geçerli (İ / ı).</summary>
    private static readonly System.Globalization.CultureInfo TrKultur = new("tr-TR");

    static DisaAktarmaServisi()
    {
        // QuestPDF topluluk lisansı; ayarlanmazsa ilk çağrıda istisna atar.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ─────────────────────────────────────────────────── etkinlik

    private record EtkinlikSatiri(
        DateTime Baslangic, DateTime? Bitis, bool TumGun, string Baslik,
        string? Tip, string? Durum, string? Konum, string? Birim, string? Olusturan, bool Gizli);

    private async Task<List<EtkinlikSatiri>> EtkinlikSatirlariAsync(DisaAktarmaSuzgeci s)
    {
        var kullaniciId = await _mevcutKullanici.GetUserIdAsync();
        var kullaniciAdi = _mevcutKullanici.GetUsername();
        var birimId = _mevcutKullanici.GetCurrentBirimId();

        var birimIdleri = s.AltBirimlerDahil
            ? _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList()
            : [birimId];

        // GİZLİLİK KAPISI — bu satır kaldırılırsa dışa aktarma sistemdeki
        // bütün gizli etkinlikleri bir dosyaya döker.
        // Basın kullanıcısı ajandanın yalnızca basına açık kısmını görür.
        // Kapı `ICurrentUserService`te: bu sorguların hepsinde zaten var.
        var yalnizcaBasin = await _mevcutKullanici.YalnizcaBasinMiAsync();

        var sorgu = _context.Ajandalar
            .AsNoTracking()
            .GorunurOlanlar(kullaniciId, kullaniciAdi, yalnizcaBasin)
            .Where(a => birimIdleri.Contains(a.BirimId ?? 0));

        if (s.Baslangic is { } bas) sorgu = sorgu.Where(a => a.BaslangicTarihi >= bas.Date);
        if (s.Bitis is { } bit) sorgu = sorgu.Where(a => a.BaslangicTarihi < bit.Date.AddDays(1));
        if (s.TipId is { } tip) sorgu = sorgu.Where(a => a.RandevuTipId == tip);
        if (s.DurumId is { } durum) sorgu = sorgu.Where(a => a.DurumId == durum);

        if (!string.IsNullOrWhiteSpace(s.Ara))
        {
            var k = $"%{s.Ara.Trim()}%";
            sorgu = sorgu.Where(a =>
                EF.Functions.ILike(a.Baslik, k) ||
                (a.Konum != null && EF.Functions.ILike(a.Konum, k)) ||
                (a.Aciklama != null && EF.Functions.ILike(a.Aciklama, k)));
        }

        return await sorgu
            .OrderBy(a => a.BaslangicTarihi)
            .Select(a => new EtkinlikSatiri(
                a.BaslangicTarihi, a.BitisTarihi, a.TumGun, a.Baslik,
                a.RandevuTip == null ? null : a.RandevuTip.Ad,
                a.Durum == null ? null : a.Durum.Ad,
                a.Konum,
                a.Birim == null ? null : a.Birim.Ad,
                a.KullaniciId,
                a.Gizli))
            .ToListAsync();
    }

    public async Task<DisaAktarmaDosyasi> EtkinlikExcelAsync(DisaAktarmaSuzgeci suzgec)
    {
        var satirlar = await EtkinlikSatirlariAsync(suzgec);

        var basliklar = new[] { "Tarih", "Saat", "Başlık", "Tip", "Durum", "Konum", "Birim", "Oluşturan", "Gizli" };
        var veri = satirlar.Select(e => new string?[]
        {
            e.Baslangic.ToString("dd.MM.yyyy"),
            e.TumGun ? "Tüm gün" : SaatAraligi(e.Baslangic, e.Bitis),
            e.Baslik,
            e.Tip,
            e.Durum,
            e.Konum,
            e.Birim,
            e.Olusturan,
            e.Gizli ? "Evet" : "Hayır",
        });

        return ExcelUret("Etkinlikler", basliklar, veri, "etkinlikler");
    }

    /// <remarks>
    /// <b>Tip ve durum sütunları YOK</b> — kâğıda basılan bir listede ikisi de
    /// iç takip alanı ve "Beklemede" damgalı bir satır, programı elinde tutan
    /// kişiye yanlış bir şey söylüyor. Excel çıktısında ikisi de DURUYOR:
    /// tablo süzülmek ve sayılmak için var, elden ele dolaşmak için değil.
    /// Sütun kalkınca kalanlara yer açıldı; başlık ve konum kırpılmıyor.
    /// </remarks>
    public async Task<DisaAktarmaDosyasi> EtkinlikPdfAsync(DisaAktarmaSuzgeci suzgec)
    {
        var satirlar = await EtkinlikSatirlariAsync(suzgec);
        var kimlik = await KurumsalKimlikAsync();

        return PdfUret(
            "Etkinlik Listesi",
            AralikMetni(suzgec),
            [("Tarih", 1.1f), ("Saat", 1f), ("Başlık", 3.6f), ("Konum", 2.4f)],
            satirlar.Select(e => new[]
            {
                e.Baslangic.ToString("dd.MM.yyyy"),
                e.TumGun ? "Tüm gün" : SaatAraligi(e.Baslangic, e.Bitis),
                e.Baslik,
                e.Konum ?? "—",
            }),
            "etkinlikler",
            await BirimAdiAsync(),
            kimlik.Ad, kimlik.AnaRenk, kimlik.VurguRenk);
    }

    // ────────────────────────────────────────────────────── talep

    private record TalepSatiri(
        DateTime? Tarih, string Konu, string? Ad, string? Soyad, string? Meslek,
        string? Telefon, string? Mahalle, string? Tip, string? Durum, string? Birim, bool Arsiv);

    private async Task<List<TalepSatiri>> TalepSatirlariAsync(DisaAktarmaSuzgeci s)
    {
        var birimId = _mevcutKullanici.GetCurrentBirimId();
        var birimIdleri = s.AltBirimlerDahil
            ? _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList()
            : [birimId];

        var sorgu = _context.Randevular
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => birimIdleri.Contains(r.BirimId ?? 0));

        if (s.Baslangic is { } bas) sorgu = sorgu.Where(r => r.BaslangicTarih >= bas.Date);
        if (s.Bitis is { } bit) sorgu = sorgu.Where(r => r.BaslangicTarih < bit.Date.AddDays(1));
        if (s.TipId is { } tip) sorgu = sorgu.Where(r => r.RandevuTipId == tip);
        if (s.DurumId is { } durum) sorgu = sorgu.Where(r => r.RandevuDurumId == durum);

        if (!string.IsNullOrWhiteSpace(s.Ara))
        {
            var k = $"%{s.Ara.Trim()}%";
            sorgu = sorgu.Where(r =>
                EF.Functions.ILike(r.Konu, k) ||
                EF.Functions.ILike(r.Ad, k) ||
                EF.Functions.ILike(r.Soyad, k));
        }

        return await sorgu
            .OrderByDescending(r => r.BaslangicTarih)
            .Select(r => new TalepSatiri(
                r.BaslangicTarih, r.Konu, r.Ad, r.Soyad, r.Meslek, r.Telefon,
                r.Mahalle == null ? null : r.Mahalle.Ad,
                r.RandevuTip == null ? null : r.RandevuTip.Ad,
                r.RandevuDurum == null ? null : r.RandevuDurum.DurumAd,
                r.Birim == null ? null : r.Birim.Ad,
                r.Arsivlendi))
            .ToListAsync();
    }

    public async Task<DisaAktarmaDosyasi> TalepExcelAsync(DisaAktarmaSuzgeci suzgec)
    {
        var satirlar = await TalepSatirlariAsync(suzgec);

        var basliklar = new[]
        {
            "Tarih", "Konu", "Ad", "Soyad", "Meslek", "Telefon",
            "Mahalle", "Tip", "Durum", "Birim", "Arşiv",
        };

        var veri = satirlar.Select(t => new string?[]
        {
            t.Tarih?.ToString("dd.MM.yyyy") ?? "—",
            t.Konu, t.Ad, t.Soyad, t.Meslek, t.Telefon,
            t.Mahalle, t.Tip, t.Durum, t.Birim,
            t.Arsiv ? "Evet" : "Hayır",
        });

        return ExcelUret("Talepler", basliklar, veri, "talepler");
    }

    public async Task<DisaAktarmaDosyasi> TalepPdfAsync(DisaAktarmaSuzgeci suzgec)
    {
        var satirlar = await TalepSatirlariAsync(suzgec);
        var kimlik = await KurumsalKimlikAsync();

        return PdfUret(
            "Talep Listesi",
            AralikMetni(suzgec),
            [("Tarih", 1.1f), ("Konu", 3.4f), ("Ad Soyad", 2f), ("Meslek", 1.6f)],
            satirlar.Select(t => new[]
            {
                t.Tarih?.ToString("dd.MM.yyyy") ?? "—",
                t.Konu,
                $"{t.Ad} {t.Soyad}".Trim(),
                t.Meslek ?? "—",
            }),
            "talepler",
            await BirimAdiAsync(),
            kimlik.Ad, kimlik.AnaRenk, kimlik.VurguRenk);
    }

    /// <summary>
    /// Çıktı başlıklarındaki birim — oturum sahibinin birimi.
    /// </summary>
    /// <remarks>
    /// Kullanıcının birimi çözülemezse kurum kaydındaki birim adına düşer;
    /// oraya da yazılmamışsa kurumun kendi adı basılır. Sabit "Başkanlık
    /// Makamı" yazmak, başka bir kurumda yanlış bir şey söylerdi.
    /// </remarks>
    private async Task<string> BirimAdiAsync()
    {
        var birim = await _mevcutKullanici.GetCurrentBirimAdiAsync();
        if (!string.IsNullOrWhiteSpace(birim)) return birim;

        var kurum = await _kurum.GetAsync();
        return string.IsNullOrWhiteSpace(kurum.Department) ? kurum.Name : kurum.Department;
    }

    /// <summary>
    /// Çıktılarda kullanılan kurum adı ve kurumsal renkler.
    /// </summary>
    /// <remarks>
    /// Renkler eskiden PDF üreticisine sabit yazılıydı (#002E6D / #A78952).
    /// Kurum kaydı boş bırakılırsa aynı değerlere düşülür — mevcut çıktıların
    /// görünümü değişmesin diye.
    /// </remarks>
    private async Task<KurumsalKimlik> KurumsalKimlikAsync()
    {
        var kurum = await _kurum.GetAsync();

        return new KurumsalKimlik(
            kurum.ResolvedDisplayName,
            string.IsNullOrWhiteSpace(kurum.BrandPrimary) ? "#002E6D" : kurum.BrandPrimary,
            string.IsNullOrWhiteSpace(kurum.BrandAccent) ? "#A78952" : kurum.BrandAccent,
            kurum.ResolvedPrintLogo);
    }

    /// <summary>Çıktılarda kullanılan kurum kimliği — tek yerden taşınır.</summary>
    private sealed record KurumsalKimlik(string Ad, string AnaRenk, string VurguRenk, string? Amblem);

    // ─────────────────────────────────────────────────── biçimleyici

    private static string SaatAraligi(DateTime bas, DateTime? bit) =>
        bit is null ? bas.ToString("HH:mm") : $"{bas:HH:mm} - {bit:HH:mm}";

    private static string AralikMetni(DisaAktarmaSuzgeci s)
    {
        if (s.Baslangic is null && s.Bitis is null) return "Tüm kayıtlar";
        var b = s.Baslangic?.ToString("dd.MM.yyyy") ?? "başlangıç";
        var t = s.Bitis?.ToString("dd.MM.yyyy") ?? "bugün";
        return $"{b} – {t}";
    }

    /// <remarks>
    /// Tablo kurulumu <see cref="ExcelTablosu"/> içinde: liste çıktıları
    /// (görev, proje, özgeçmiş, protokol, halk günü havuzu) da aynı başlık
    /// şeridini, donmuş satırı ve otomatik süzgecini kullanıyor. İkinci bir
    /// kopya, birinde düzeltilen bir biçimin ötekinde kalması demekti.
    /// </remarks>
    private static DisaAktarmaDosyasi ExcelUret(
        string sayfaAdi, string[] basliklar, IEnumerable<string?[]> satirlar, string dosyaOneki)
        => ExcelTablosu.Uret(sayfaAdi, basliklar, satirlar, dosyaOneki);

    /// <param name="birim">
    /// Altbilgide yazan birim — ÇIKTIYI ALANIN birimi. Sabit "Başkanlık
    /// Makamı" basmak, tablonun kimin verisi olduğunu yanlış söylüyordu.
    /// </param>
    /// <param name="kurumAdi">
    /// Altbilgide yazan kurum. Koda YAZILMAZ — kurum kaydından gelir;
    /// uygulama başka belediyelerde de çalışacak.
    /// </param>
    /// <param name="anaRenk">Başlık ve tablo başlığı rengi — kurumsal kimlik.</param>
    /// <param name="vurguRenk">Başlık altındaki çizgi.</param>
    private static DisaAktarmaDosyasi PdfUret(
        string baslik,
        string altBaslik,
        (string Ad, float Agirlik)[] sutunlar,
        IEnumerable<string[]> satirlar,
        string dosyaOneki,
        string birim,
        string kurumAdi,
        string anaRenk,
        string vurguRenk)
    {
        var veri = satirlar.ToList();

        var belge = Document.Create(kurgu =>
        {
            kurgu.Page(sayfa =>
            {
                // Yatay A4: bu tablolar dikey sayfaya sığmıyor ve sütunlar
                // üst üste biniyordu.
                sayfa.Size(PageSizes.A4.Landscape());
                sayfa.Margin(1.2f, Unit.Centimetre);
                sayfa.DefaultTextStyle(t => t.FontSize(9).FontFamily(Fonts.Calibri));

                sayfa.Header().Column(s =>
                {
                    s.Item().Text(baslik).FontSize(15).Bold().FontColor(anaRenk);
                    s.Item().PaddingTop(2).Text($"{altBaslik} · {veri.Count} kayıt · {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                    s.Item().PaddingTop(6).LineHorizontal(1.2f).LineColor(vurguRenk);
                });

                sayfa.Content().PaddingVertical(8).Table(tablo =>
                {
                    tablo.ColumnsDefinition(k =>
                    {
                        foreach (var s in sutunlar) k.RelativeColumn(s.Agirlik);
                    });

                    tablo.Header(h =>
                    {
                        foreach (var s in sutunlar)
                        {
                            h.Cell().Background(anaRenk).Padding(4)
                                .Text(s.Ad).FontColor(Colors.White).Bold().FontSize(8.5f);
                        }
                    });

                    var tek = false;
                    foreach (var satir in veri)
                    {
                        tek = !tek;
                        foreach (var hucre in satir)
                        {
                            // Zebra: uzun tablolarda satır takibini kolaylaştırır.
                            tablo.Cell()
                                .Background(tek ? Colors.White : "#F5F5F3")
                                .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .Padding(3.5f)
                                .Text(hucre ?? "—").FontSize(8.5f);
                        }
                    }
                });

                sayfa.Footer().AlignCenter().Text(t =>
                {
                    t.Span($"{kurumAdi} · {birim}  —  ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.CurrentPageNumber().FontSize(8);
                    t.Span(" / ").FontSize(8);
                    t.TotalPages().FontSize(8);
                });
            });
        });

        return new DisaAktarmaDosyasi(
            belge.GeneratePdf(),
            $"{dosyaOneki}-{DateTime.Now:yyyyMMdd-HHmm}.pdf",
            PdfTuru);
    }

    // ──────────────────────────────────────────── günlük program

    private record ProgramSatiri(
        DateTime Baslangic, DateTime? Bitis, bool TumGun, string Baslik,
        string? Konum, string? Tip, string? Durum, string? IrtibatKisi,
        string? IrtibatTelefon, bool BasinKatilsin, bool KonusmaMetni,
        bool BilgiNotu, bool Gizli);

    public async Task<DisaAktarmaDosyasi> GunlukProgramAsync(DateTime gun, ProgramTasarimi tasarim)
    {
        var kullaniciId = await _mevcutKullanici.GetUserIdAsync();
        var kullaniciAdi = _mevcutKullanici.GetUsername();
        var birimId = _mevcutKullanici.GetCurrentBirimId();

        var bas = gun.Date;
        var bit = bas.AddDays(1);

        // GİZLİLİK KAPISI — basılan bir program elden ele dolaşır; gizli
        // etkinliğin buraya sızması geri alınamaz.
        // Basın kullanıcısı ajandanın yalnızca basına açık kısmını görür.
        // Kapı `ICurrentUserService`te: bu sorguların hepsinde zaten var.
        var yalnizcaBasin = await _mevcutKullanici.YalnizcaBasinMiAsync();

        var satirlar = await _context.Ajandalar
            .AsNoTracking()
            .GorunurOlanlar(kullaniciId, kullaniciAdi, yalnizcaBasin)
            .Where(a => a.BirimId == birimId
                     && a.BaslangicTarihi >= bas && a.BaslangicTarihi < bit)
            .OrderBy(a => a.TumGun ? 0 : 1)
            .ThenBy(a => a.BaslangicTarihi)
            .Select(a => new ProgramSatiri(
                a.BaslangicTarihi, a.BitisTarihi, a.TumGun, a.Baslik, a.Konum,
                a.RandevuTip == null ? null : a.RandevuTip.Ad,
                a.Durum == null ? null : a.Durum.Ad,
                a.IrtibatKisi, a.IrtibatTelefon,
                a.BasinKatilsin, a.KonusmaMetniDurum, a.BilgiNotuDurum, a.Gizli))
            .ToListAsync();

        var birim = await BirimAdiAsync();
        var kimlik = await KurumsalKimlikAsync();

        return tasarim switch
        {
            ProgramTasarimi.Kompakt => KompaktProgram(gun, satirlar, birim, kimlik),
            ProgramTasarimi.Detayli => DetayliProgram(gun, satirlar, birim, kimlik),
            ProgramTasarimi.BosNot => BosNotProgrami(gun, satirlar, birim, kimlik),
            // Saat şeridi ve pano yerleşimleri HTML'e özgü; PDF'te en yakın
            // karşılıkları kullanılır — kullanıcı boş dosya indirmez.
            ProgramTasarimi.SaatSeridi => KompaktProgram(gun, satirlar, birim, kimlik),
            ProgramTasarimi.Pano => KompaktProgram(gun, satirlar, birim, kimlik),
            _ => StandartProgram(gun, satirlar, birim, kimlik),
        };
    }

    public async Task<string> GunlukProgramHtmlAsync(DateTime gun, ProgramTasarimi tasarim)
    {
        var kullaniciId = await _mevcutKullanici.GetUserIdAsync();
        var kullaniciAdi = _mevcutKullanici.GetUsername();
        var birimId = _mevcutKullanici.GetCurrentBirimId();

        var bas = gun.Date;
        var bit = bas.AddDays(1);

        // GİZLİLİK KAPISI — HTML çıktı da basılıp elden ele dolaşıyor.
        // Basın kullanıcısı ajandanın yalnızca basına açık kısmını görür.
        // Kapı `ICurrentUserService`te: bu sorguların hepsinde zaten var.
        var yalnizcaBasin = await _mevcutKullanici.YalnizcaBasinMiAsync();

        var satirlar = await _context.Ajandalar
            .AsNoTracking()
            .GorunurOlanlar(kullaniciId, kullaniciAdi, yalnizcaBasin)
            .Where(a => a.BirimId == birimId
                     && a.BaslangicTarihi >= bas && a.BaslangicTarihi < bit)
            .OrderBy(a => a.TumGun ? 0 : 1)
            .ThenBy(a => a.BaslangicTarihi)
            .Select(a => new ProgramSatirVerisi(
                a.BaslangicTarihi, a.BitisTarihi, a.TumGun, a.Baslik, a.Konum,
                a.RandevuTip == null ? null : a.RandevuTip.Ad,
                a.RandevuTip == null ? null : a.RandevuTip.Renk,
                a.Durum == null ? null : a.Durum.Ad,
                a.Durum == null ? null : a.Durum.Renk,
                a.IrtibatKisi, a.IrtibatTelefon,
                a.BasinKatilsin, a.KonusmaMetniDurum, a.BilgiNotuDurum))
            .ToListAsync();

        var kimlik = await KurumsalKimlikAsync();
        return GunlukProgramHtml.Uret(
            gun, tasarim, satirlar, await BirimAdiAsync(), kimlik.Ad, kimlik.Amblem);
    }

    /// <summary>Günün Türkçe uzun biçimi — `12 Ağustos 2026 Çarşamba`.</summary>
    private static string GunBasligi(DateTime g)
    {
        var tr = new CultureInfo("tr-TR");
        return g.ToString("d MMMM yyyy dddd", tr);
    }

    private static string SaatHucresi(ProgramSatiri s) =>
        s.TumGun ? "Tüm gün" : SaatAraligi(s.Baslangic, s.Bitis);

    /// <summary>Tasarım 1 — saat / konu / yer tablosu.</summary>
    private static DisaAktarmaDosyasi StandartProgram(
        DateTime gun, List<ProgramSatiri> satirlar, string birim, KurumsalKimlik kimlik) =>
        ProgramBelgesi(gun, "GÜNLÜK PROGRAM", satirlar, (tablo, veri) =>
        {
            tablo.ColumnsDefinition(k =>
            {
                k.ConstantColumn(88);
                k.RelativeColumn(3);
                k.RelativeColumn(2);
            });

            tablo.Header(h =>
            {
                foreach (var b in new[] { "Saat", "Konu", "Yer" })
                {
                    h.Cell().Background("#002E6D").Padding(6)
                        .Text(b).FontColor(Colors.White).Bold().FontSize(10);
                }
            });

            foreach (var s in veri)
            {
                tablo.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(7)
                    .Text(SaatHucresi(s)).Bold().FontSize(10.5f);
                tablo.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(7)
                    .Text(s.Baslik).FontSize(10.5f);
                tablo.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(7)
                    .Text(s.Konum ?? "—").FontSize(10).FontColor(Colors.Grey.Darken2);
            }
        }, birim, kimlik);

    /// <summary>Tasarım 2 — tek sütun, geniş satır aralığı (uzaktan okunur).</summary>
    private static DisaAktarmaDosyasi KompaktProgram(
        DateTime gun, List<ProgramSatiri> satirlar, string birim, KurumsalKimlik kimlik) =>
        ProgramBelgesi(gun, "GÜNLÜK PROGRAM", satirlar, (tablo, veri) =>
        {
            tablo.ColumnsDefinition(k =>
            {
                k.ConstantColumn(96);
                k.RelativeColumn();
            });

            foreach (var s in veri)
            {
                tablo.Cell().BorderBottom(1).BorderColor("#E3E1DA").PaddingVertical(11)
                    .Text(SaatHucresi(s)).Bold().FontSize(13).FontColor("#002E6D");
                tablo.Cell().BorderBottom(1).BorderColor("#E3E1DA").PaddingVertical(11).Column(c =>
                {
                    c.Item().Text(s.Baslik).FontSize(13);
                    if (!string.IsNullOrWhiteSpace(s.Konum))
                    {
                        c.Item().PaddingTop(2).Text(s.Konum).FontSize(10).FontColor(Colors.Grey.Darken1);
                    }
                });
            }
        }, birim, kimlik);

    /// <summary>Tasarım 3 — irtibat ve hazırlık bilgileriyle.</summary>
    private static DisaAktarmaDosyasi DetayliProgram(
        DateTime gun, List<ProgramSatiri> satirlar, string birim, KurumsalKimlik kimlik) =>
        ProgramBelgesi(gun, "GÜNLÜK PROGRAM — DETAYLI", satirlar, (tablo, veri) =>
        {
            tablo.ColumnsDefinition(k =>
            {
                k.ConstantColumn(84);
                k.RelativeColumn(3);
                k.RelativeColumn(2);
                k.RelativeColumn(2);
                k.ConstantColumn(78);
            });

            tablo.Header(h =>
            {
                foreach (var b in new[] { "Saat", "Konu", "Yer", "İrtibat", "Hazırlık" })
                {
                    h.Cell().Background("#002E6D").Padding(5)
                        .Text(b).FontColor(Colors.White).Bold().FontSize(9);
                }
            });

            foreach (var s in veri)
            {
                // Hazırlık bayrakları kısaltmayla: basılı programda ikon yok,
                // harfler bir bakışta okunuyor.
                var hazirlik = string.Join(" ", new[]
                {
                    s.BasinKatilsin ? "BASIN" : null,
                    s.KonusmaMetni ? "KM" : null,
                    s.BilgiNotu ? "BN" : null,
                }.Where(x => x is not null));

                void H(string metin, bool kalin = false, float boyut = 9.5f)
                {
                    var hucre = tablo.Cell()
                        .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5);
                    var t = hucre.Text(metin).FontSize(boyut);
                    if (kalin) t.Bold();
                }

                H(SaatHucresi(s), kalin: true, boyut: 10);
                H(s.Baslik);
                H(s.Konum ?? "—");
                H(string.Join(" · ", new[] { s.IrtibatKisi, s.IrtibatTelefon }
                    .Where(x => !string.IsNullOrWhiteSpace(x))) is { Length: > 0 } irtibat
                    ? irtibat : "—");
                H(string.IsNullOrEmpty(hazirlik) ? "—" : hazirlik, boyut: 8.5f);
            }
        }, birim, kimlik);

    /// <summary>Boş not sayfası — her etkinliğin yanında el yazısı alanı.</summary>
    private static DisaAktarmaDosyasi BosNotProgrami(
        DateTime gun, List<ProgramSatiri> satirlar, string birim, KurumsalKimlik kimlik) =>
        ProgramBelgesi(gun, "GÜNLÜK PROGRAM — NOT SAYFASI", satirlar, (tablo, veri) =>
        {
            tablo.ColumnsDefinition(k =>
            {
                k.ConstantColumn(78);
                k.RelativeColumn(2);
                k.RelativeColumn(3);
            });

            tablo.Header(h =>
            {
                foreach (var b in new[] { "Saat", "Konu", "Notlar" })
                {
                    h.Cell().Background("#002E6D").Padding(5)
                        .Text(b).FontColor(Colors.White).Bold().FontSize(9.5f);
                }
            });

            foreach (var s in veri)
            {
                // Sabit yükseklik: elle not almak için yeterli alan bırakır.
                tablo.Cell().Border(0.5f).BorderColor("#CDCAC1").MinHeight(46).Padding(5)
                    .Text(SaatHucresi(s)).Bold().FontSize(10);
                tablo.Cell().Border(0.5f).BorderColor("#CDCAC1").MinHeight(46).Padding(5)
                    .Text(s.Baslik).FontSize(10);
                tablo.Cell().Border(0.5f).BorderColor("#CDCAC1").MinHeight(46).Padding(5).Text("");
            }

            // Program bittikten sonra serbest not için birkaç boş satır.
            for (var i = 0; i < 4; i++)
            {
                tablo.Cell().Border(0.5f).BorderColor("#CDCAC1").MinHeight(46).Padding(5).Text("");
                tablo.Cell().Border(0.5f).BorderColor("#CDCAC1").MinHeight(46).Padding(5).Text("");
                tablo.Cell().Border(0.5f).BorderColor("#CDCAC1").MinHeight(46).Padding(5).Text("");
            }
        }, birim, kimlik);

    /// <summary>
    /// Günlük program belgelerinin ortak çerçevesi.
    /// </summary>
    /// <remarks>
    /// Dört tasarımın da başlığı, altın kuralı, boş durumu ve altbilgisi
    /// AYNI; yalnızca tablo gövdesi değişiyor. Ortak çerçeve, dört şablonun
    /// zamanla birbirinden ayrışmasını engelliyor — eski arayüzde tam olarak
    /// bu olmuştu.
    /// </remarks>
    /// <summary>Program çıktısının logosu; dosya yoksa <c>null</c>.</summary>
    /// <remarks>
    /// Bir kez okunup bellekte tutulur: her sayfa için diskten okumak, çok
    /// sayfalı bir programda gereksiz IO demek. Dosya yoksa çıktı logosuz
    /// üretilir — eksik bir görsel yüzünden program hiç alınamaması kabul
    /// edilemez.
    /// </remarks>
    private static byte[]? ProgramLogosu(string? amblemYolu)
    {
        // Önbellek YOLA GÖRE: kurum amblemi arayüzden değiştirilebiliyor ve
        // koşulsuz önbellek, değişikliği sunucu yeniden başlayana kadar
        // görünmez kılardı.
        if (_logoOkundu && _logoYolu == amblemYolu) return _logo;

        _logoOkundu = true;
        _logoYolu = amblemYolu;
        try
        {
            if (string.IsNullOrWhiteSpace(amblemYolu)) return null;

            // Amblem KURUM KAYDINDAN gelir; yol `wwwroot` köküne göre.
            var gorece = amblemYolu.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var yol = Path.Combine(AppContext.BaseDirectory, "wwwroot", gorece);

            if (!File.Exists(yol))
            {
                yol = Path.Combine("wwwroot", gorece);
            }

            _logo = File.Exists(yol) ? File.ReadAllBytes(yol) : null;
        }
        catch
        {
            _logo = null;
        }

        return _logo;
    }

    private static byte[]? _logo;
    private static bool _logoOkundu;
    private static string? _logoYolu;

    private static DisaAktarmaDosyasi ProgramBelgesi(
        DateTime gun,
        string baslik,
        List<ProgramSatiri> satirlar,
        Action<TableDescriptor, List<ProgramSatiri>> govde,
        string birim,
        KurumsalKimlik kimlik)
    {
        var belge = Document.Create(kurgu =>
        {
            kurgu.Page(sayfa =>
            {
                // Dikey A4: bu çıktı basılıp masaya konuyor.
                sayfa.Size(PageSizes.A4);
                sayfa.Margin(1.6f, Unit.Centimetre);
                sayfa.DefaultTextStyle(t => t.FontSize(10).FontFamily(Fonts.Calibri));

                sayfa.Header().Column(s =>
                {
                    // Logo SOLDA, kurum adı ortada — eski çıktının yerleşimi.
                    // Yeni sürümde logo hiç basılmıyordu ve basılan program
                    // kurumsal bir evrak gibi durmuyordu.
                    s.Item().Row(r =>
                    {
                        r.ConstantItem(58).AlignMiddle().Element(e =>
                        {
                            var logo = ProgramLogosu(kimlik.Amblem);
                            if (logo is not null)
                            {
                                e.Width(52).Image(logo);
                            }
                        });

                        r.RelativeItem().Column(orta =>
                        {
                            orta.Item().AlignCenter().Text(kimlik.Ad.ToUpper(TrKultur))
                                .FontSize(11).Bold().FontColor("#002E6D").LetterSpacing(0.08f);
                            // Türkçe büyük harf: ToUpperInvariant "İ"yi "I" yapıyor.
                            orta.Item().AlignCenter().Text(birim.ToUpper(TrKultur))
                                .FontSize(9).FontColor(Colors.Grey.Darken1).LetterSpacing(0.06f);
                        });

                        // Sağda logo kadar boşluk: başlık gerçekten ortalanır,
                        // yoksa logonun genişliği kadar sağa kayardı.
                        r.ConstantItem(58);
                    });

                    s.Item().PaddingTop(10).AlignCenter().Text(baslik)
                        .FontSize(16).Bold().FontColor("#002E6D");
                    s.Item().PaddingTop(3).AlignCenter().Text(GunBasligi(gun))
                        .FontSize(11).FontColor(Colors.Grey.Darken2);

                    s.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor("#A78952");
                });

                sayfa.Content().PaddingVertical(12).Element(e =>
                {
                    if (satirlar.Count == 0)
                    {
                        e.PaddingTop(60).AlignCenter()
                            .Text("Bu güne ait program kaydı bulunmuyor.")
                            .FontSize(11).FontColor(Colors.Grey.Darken1);
                        return;
                    }

                    e.Table(tablo => govde(tablo, satirlar));
                });

                sayfa.Footer().Column(s =>
                {
                    s.Item().LineHorizontal(0.5f).LineColor("#CDCAC1");
                    s.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text($"{satirlar.Count} etkinlik")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                        r.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}  —  ")
                                .FontSize(8).FontColor(Colors.Grey.Darken1);
                            t.CurrentPageNumber().FontSize(8);
                            t.Span(" / ").FontSize(8);
                            t.TotalPages().FontSize(8);
                        });
                    });
                });
            });
        });

        return new DisaAktarmaDosyasi(
            belge.GeneratePdf(),
            $"gunluk-program-{gun:yyyyMMdd}.pdf",
            PdfTuru);
    }
}
