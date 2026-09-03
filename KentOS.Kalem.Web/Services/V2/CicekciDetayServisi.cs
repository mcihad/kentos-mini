using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using KentOS.Kalem.Web.Exceptions;
using KentOS.Kalem.Web.Data;
using System.Text.Json.Serialization;

namespace KentOS.Kalem.Web.Services.V2;

// ── DTO'lar ────────────────────────────────────────────────────────────────

/// <summary>Çiçekçinin tek bir talimatı — bağlı olduğu programla birlikte.</summary>
/// <remarks>
/// <c>GET cicekciler/{id}/talimatlar</c> yalnızca çiçeğin kendi alanlarını
/// döndürüyordu: ad, adres, gönderildi mi. "Hangi program içindi" bilgisi
/// yoktu ve çiçekçiyle hesaplaşan kişi her satır için ayrıca etkinliğe bakmak
/// zorundaydı. Program bilgisi burada satırın içinde.
/// </remarks>
public record CicekciTalimatDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("ad")] string? Ad,
    [property: JsonPropertyName("aciklama")] string? Aciklama,
    [property: JsonPropertyName("adres")] string? Adres,
    [property: JsonPropertyName("gonderildi")] bool Gonderildi,
    [property: JsonPropertyName("olusturulmaTarihi")] DateTime OlusturulmaTarihi,
    [property: JsonPropertyName("gonderilmeTarihi")] DateTime? GonderilmeTarihi,
    [property: JsonPropertyName("olusturan")] string? Olusturan,
    [property: JsonPropertyName("etkinlikId")] long? EtkinlikId,
    [property: JsonPropertyName("etkinlikBaslik")] string? EtkinlikBaslik,
    [property: JsonPropertyName("etkinlikTarihi")] DateTime? EtkinlikTarihi,
    [property: JsonPropertyName("etkinlikKonum")] string? EtkinlikKonum);

/// <summary>Çiçekçi detay ekranının tek yanıtı.</summary>
public record CicekciDetayDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("adSoyad")] string AdSoyad,
    [property: JsonPropertyName("telefon")] string? Telefon,
    [property: JsonPropertyName("adres")] string? Adres,
    [property: JsonPropertyName("aktif")] bool Aktif,
    /// <summary>Süzgeçten geçen talimat sayısı.</summary>
    [property: JsonPropertyName("toplam")] int Toplam,
    [property: JsonPropertyName("gonderilen")] int Gonderilen,
    [property: JsonPropertyName("bekleyen")] int Bekleyen,
    /// <summary>Talimatın bağlı olduğu FARKLI program sayısı.</summary>
    [property: JsonPropertyName("programSayisi")] int ProgramSayisi,
    [property: JsonPropertyName("ilkTalimat")] DateTime? IlkTalimat,
    [property: JsonPropertyName("sonTalimat")] DateTime? SonTalimat,
    [property: JsonPropertyName("talimatlar")] List<CicekciTalimatDto> Talimatlar);

public interface ICicekciDetayServisi
{
    Task<CicekciDetayDto> DetayAsync(long cicekciId, DateTime? baslangic, DateTime? bitis);

    Task<DisaAktarmaDosyasi> ExcelAsync(long cicekciId, DateTime? baslangic, DateTime? bitis);

    Task<DisaAktarmaDosyasi> PdfAsync(long cicekciId, DateTime? baslangic, DateTime? bitis);
}

/// <summary>
/// Çiçekçi dosyası — gönderilen çiçekler, bağlı programlar ve dökümü.
/// </summary>
/// <remarks>
/// <para>
/// Çiçekçiyle ay sonunda hesaplaşılıyor: "şu tarihler arasında kaç çiçek
/// gönderdin, hangi programlara?" Bu sorunun cevabı sistemde vardı ama
/// dağınıktı — talimatlar çiçekçiye, program bilgisi etkinliğe bağlıydı ve
/// tarih aralığı süzgeci hiç yoktu.
/// </para>
/// <para>
/// <b>Görünürlük kapısı UYGULANMAZ ve bu bilinçli.</b> Çiçek talimatı zaten
/// yalnızca GİZLİ OLMAYAN etkinliklerde üretiliyor (bkz. AjandaService); yani
/// listede gizli bir etkinliğin başlığı hiç oluşamaz. Buna karşılık çiçekçi
/// hesabı kurum geneli bir iş: talimatı veren birim ile ödemeyi yapan birim
/// aynı olmayabiliyor ve birim süzgeci konsaydı fatura hiç tutmazdı.
/// </para>
/// </remarks>
public class CicekciDetayServisi(
    AppDbContext _db,
    IInstitutionService _kurum) : ICicekciDetayServisi
{
    private const string Lacivert = "#002E6D";
    private const string Altin = "#A78952";

    static CicekciDetayServisi()
    {
        // QuestPDF topluluk lisansı. Statik kurucu YALNIZCA kendi sınıfı için
        // çalışır; başka bir PDF sınıfındaki ayar burayı kapsamaz.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<CicekciDetayDto> DetayAsync(long cicekciId, DateTime? baslangic, DateTime? bitis)
    {
        var cicekci = await _db.Cicekciler.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cicekciId)
            ?? throw new EntityNotFoundException("Çiçekçi bulunamadı.");

        var talimatlar = await TalimatlariGetirAsync(cicekciId, baslangic, bitis);

        return new CicekciDetayDto(
            cicekci.Id,
            cicekci.AdSoyad,
            cicekci.Telefon,
            cicekci.Adres,
            cicekci.Aktif,
            talimatlar.Count,
            talimatlar.Count(t => t.Gonderildi),
            talimatlar.Count(t => !t.Gonderildi),
            talimatlar.Where(t => t.EtkinlikId is not null).Select(t => t.EtkinlikId).Distinct().Count(),
            talimatlar.Count == 0 ? null : talimatlar.Min(t => t.OlusturulmaTarihi),
            talimatlar.Count == 0 ? null : talimatlar.Max(t => t.OlusturulmaTarihi),
            talimatlar);
    }

    /// <remarks>
    /// Süzgeç TALİMATIN OLUŞTURULMA tarihine göre, gönderilme tarihine göre
    /// değil: henüz gönderilmemiş talimatlar da dönemin içinde sayılmalı,
    /// yoksa "bu ay kaç iş verdik" sorusu eksik cevaplanır.
    /// </remarks>
    private async Task<List<CicekciTalimatDto>> TalimatlariGetirAsync(
        long cicekciId, DateTime? baslangic, DateTime? bitis)
    {
        var sorgu = _db.Cicekler.AsNoTracking().Where(c => c.CicekciId == cicekciId);

        if (baslangic is not null) sorgu = sorgu.Where(c => c.OlusturulmaTarihi >= baslangic);
        // Bitiş GÜNÜ DAHİL: kullanıcı "31 Ekim"e kadar dediğinde 31 Ekim'deki
        // kayıtları dışarıda bırakmak, her ay sonunda eksik liste demekti.
        if (bitis is not null) sorgu = sorgu.Where(c => c.OlusturulmaTarihi < bitis.Value.Date.AddDays(1));

        // Etkinlik bilgisi LEFT JOIN: talimat etkinliksiz de açılabiliyor
        // (elle girilen sipariş) ve o kayıtların listeden düşmemesi gerekiyor.
        var satirlar = await sorgu
            .OrderByDescending(c => c.OlusturulmaTarihi)
            .Select(c => new
            {
                c.Id,
                c.Ad,
                c.Aciklama,
                c.Adres,
                c.Gonderildi,
                c.OlusturulmaTarihi,
                c.GonderilmeTarihi,
                c.Olusturan,
                c.AjandaId,
                Etkinlik = _db.Ajandalar
                    .IgnoreQueryFilters()
                    .Where(a => a.Id == c.AjandaId)
                    .Select(a => new { a.Baslik, a.BaslangicTarihi, a.Konum })
                    .FirstOrDefault(),
            })
            .ToListAsync();

        return satirlar.Select(c => new CicekciTalimatDto(
            c.Id,
            c.Ad,
            c.Aciklama,
            c.Adres,
            c.Gonderildi,
            c.OlusturulmaTarihi,
            // Gönderilmemiş kayıtta bu alan `default(DateTime)` kalıyor;
            // istemciye 0001-01-01 göndermek "1 Ocak 1'de gönderildi" diye
            // okunuyordu.
            c.Gonderildi && c.GonderilmeTarihi != default ? c.GonderilmeTarihi : null,
            c.Olusturan,
            c.AjandaId,
            c.Etkinlik?.Baslik,
            c.Etkinlik?.BaslangicTarihi,
            c.Etkinlik?.Konum)).ToList();
    }

    // ── Çıktılar ───────────────────────────────────────────────────────

    private static string AralikMetni(DateTime? baslangic, DateTime? bitis) =>
        (baslangic, bitis) switch
        {
            (null, null) => "Tüm kayıtlar",
            (not null, null) => $"{baslangic:dd.MM.yyyy} sonrası",
            (null, not null) => $"{bitis:dd.MM.yyyy} öncesi",
            _ => $"{baslangic:dd.MM.yyyy} – {bitis:dd.MM.yyyy}",
        };

    public async Task<DisaAktarmaDosyasi> ExcelAsync(long cicekciId, DateTime? baslangic, DateTime? bitis)
    {
        var detay = await DetayAsync(cicekciId, baslangic, bitis);

        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("Çiçek talimatları");

        string[] basliklar =
            ["Tarih", "Program", "Program tarihi", "Çiçek", "Teslim adresi", "Durum", "Gönderim tarihi", "Talimatı veren"];

        for (var s = 0; s < basliklar.Length; s++)
        {
            var hucre = sayfa.Cell(1, s + 1);
            hucre.Value = basliklar[s];
            hucre.Style.Font.Bold = true;
            hucre.Style.Fill.BackgroundColor = XLColor.FromHtml(Lacivert);
            hucre.Style.Font.FontColor = XLColor.White;
        }

        var satir = 2;
        foreach (var t in detay.Talimatlar)
        {
            sayfa.Cell(satir, 1).Value = t.OlusturulmaTarihi.ToString("dd.MM.yyyy");
            sayfa.Cell(satir, 2).Value = t.EtkinlikBaslik ?? "—";
            sayfa.Cell(satir, 3).Value = t.EtkinlikTarihi?.ToString("dd.MM.yyyy") ?? "—";
            sayfa.Cell(satir, 4).Value = t.Ad ?? "—";
            sayfa.Cell(satir, 5).Value = t.Adres ?? "—";
            sayfa.Cell(satir, 6).Value = t.Gonderildi ? "Gönderildi" : "Bekliyor";
            sayfa.Cell(satir, 7).Value = t.GonderilmeTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "—";
            sayfa.Cell(satir, 8).Value = t.Olusturan ?? "—";
            satir++;
        }

        sayfa.Columns().AdjustToContents();
        sayfa.SheetView.FreezeRows(1);
        sayfa.Range(1, 1, Math.Max(1, satir - 1), basliklar.Length).SetAutoFilter();

        using var bellek = new MemoryStream();
        kitap.SaveAs(bellek);

        return new DisaAktarmaDosyasi(
            bellek.ToArray(),
            $"cicekci-{Slug(detay.AdSoyad)}-{DateTime.Now:yyyyMMdd}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    public async Task<DisaAktarmaDosyasi> PdfAsync(long cicekciId, DateTime? baslangic, DateTime? bitis)
    {
        var detay = await DetayAsync(cicekciId, baslangic, bitis);
        var aralik = AralikMetni(baslangic, bitis);

        // Kurum adı KURUM AYARLARINDAN; gerekçe `CiktiKimligi` içinde.
        var kurumAdi = (await _kurum.CiktiKimligiAsync()).Ad;

        var belge = Document.Create(kap =>
        {
            kap.Page(sayfa =>
            {
                sayfa.Size(PageSizes.A4);
                sayfa.Margin(1.4f, Unit.Centimetre);
                sayfa.DefaultTextStyle(x => x.FontSize(9).FontFamily("Helvetica"));

                sayfa.Header().Column(k =>
                {
                    k.Item().Text(kurumAdi).FontSize(8).FontColor(Altin)
                        .SemiBold().LetterSpacing(0.12f);
                    k.Item().PaddingTop(2).Text(detay.AdSoyad).FontSize(15).Bold()
                        .FontColor(Lacivert);

                    var altBilgi = new List<string> { aralik };
                    if (!string.IsNullOrWhiteSpace(detay.Telefon)) altBilgi.Add(detay.Telefon!);
                    k.Item().PaddingTop(1).Text(string.Join("  ·  ", altBilgi))
                        .FontSize(8).FontColor(Colors.Grey.Darken1);

                    k.Item().PaddingTop(4).Text(
                            $"{detay.Toplam} talimat  ·  {detay.Gonderilen} gönderildi  ·  " +
                            $"{detay.Bekleyen} bekliyor  ·  {detay.ProgramSayisi} program")
                        .FontSize(8).SemiBold().FontColor(Lacivert);

                    k.Item().PaddingTop(6).LineHorizontal(1).LineColor(Altin);
                });

                sayfa.Content().PaddingTop(8).Table(tablo =>
                {
                    tablo.ColumnsDefinition(s =>
                    {
                        s.RelativeColumn(1.1f);  // tarih
                        s.RelativeColumn(3.2f);  // program
                        s.RelativeColumn(2.4f);  // çiçek
                        s.RelativeColumn(2.6f);  // adres
                        s.RelativeColumn(1.2f);  // durum
                    });

                    // Başlık HER SAYFADA tekrarlanır: liste ikinci sayfaya
                    // taşınca hangi sütunun ne olduğu kayboluyordu.
                    tablo.Header(b =>
                    {
                        foreach (var baslik in new[] { "Tarih", "Program", "Çiçek", "Teslim adresi", "Durum" })
                        {
                            b.Cell().Background(Lacivert).Padding(4)
                                .Text(baslik).FontColor(Colors.White).SemiBold().FontSize(8);
                        }
                    });

                    var cift = false;
                    foreach (var t in detay.Talimatlar)
                    {
                        var zemin = cift ? "#F5F4F0" : "#FFFFFF";
                        cift = !cift;

                        tablo.Cell().Background(zemin).Padding(4)
                            .Text(t.OlusturulmaTarihi.ToString("dd.MM.yyyy")).FontSize(8);

                        tablo.Cell().Background(zemin).Padding(4).Column(k =>
                        {
                            k.Item().Text(t.EtkinlikBaslik ?? "—").FontSize(8);
                            if (t.EtkinlikTarihi is not null)
                            {
                                k.Item().Text(t.EtkinlikTarihi.Value.ToString("dd.MM.yyyy HH:mm"))
                                    .FontSize(7).FontColor(Colors.Grey.Darken1);
                            }
                        });

                        tablo.Cell().Background(zemin).Padding(4).Text(t.Ad ?? "—").FontSize(8);
                        tablo.Cell().Background(zemin).Padding(4).Text(t.Adres ?? "—").FontSize(8);
                        tablo.Cell().Background(zemin).Padding(4)
                            .Text(t.Gonderildi ? "Gönderildi" : "Bekliyor").FontSize(8);
                    }
                });

                sayfa.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Darken1);
                    x.Span(" / ").FontSize(7).FontColor(Colors.Grey.Darken1);
                    x.TotalPages().FontSize(7).FontColor(Colors.Grey.Darken1);
                });
            });
        });

        return new DisaAktarmaDosyasi(
            belge.GeneratePdf(),
            $"cicekci-{Slug(detay.AdSoyad)}-{DateTime.Now:yyyyMMdd}.pdf",
            "application/pdf");
    }

    /// <summary>Dosya adında kullanılabilir sade metin.</summary>
    private static string Slug(string metin)
    {
        var harfler = metin.ToLowerInvariant()
            .Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g')
            .Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c')
            .Select(k => char.IsLetterOrDigit(k) ? k : '-');
        return string.Join(string.Empty, harfler).Trim('-');
    }
}
