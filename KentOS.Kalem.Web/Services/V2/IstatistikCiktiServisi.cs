using ClosedXML.Excel;
using KentOS.Kalem.Application.Dto.Analiz;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>
/// KONU PANOSUNUN EXCEL ÇIKTISI — tek üretici, altı konu.
/// </summary>
/// <remarks>
/// <para>
/// Panolar aynı şekli döndürdüğü için çıktı da tek yerden üretiliyor. Konu
/// başına ayrı bir dışa aktarıcı yazılsaydı altı neredeyse birebir kopya
/// olurdu — bu depoda aynı hata etiket çevirisinde üç kopya olarak yaşandı
/// ve üçü ayrıştı.
/// </para>
/// <para>
/// <b>Bu bir LİSTE çıktısı değil, PANO çıktısı.</b> Kayıtların kendisi
/// değil sayılar dışa aktarılıyor: ay sonu raporuna yapıştırılacak olan bu.
/// Kayıt listesi isteyen modülün kendi çıktı ucunu kullanır
/// (<c>disa-aktar/*</c>, çiçekçi dosyası, halk günü çizelgesi…).
/// </para>
/// </remarks>
public interface IIstatistikCiktiServisi
{
    DisaAktarmaDosyasi Excel(KonuIstatistigiDto pano, DateTime? bas, DateTime? bit);
}

/// <inheritdoc cref="IIstatistikCiktiServisi"/>
public class IstatistikCiktiServisi : IIstatistikCiktiServisi
{
    private static readonly XLColor BaslikZemini = XLColor.FromHtml("#EFF3F8");

    public DisaAktarmaDosyasi Excel(KonuIstatistigiDto pano, DateTime? bas, DateTime? bit)
    {
        using var kitap = new XLWorkbook();

        // ── özet sayfası ──
        var ozet = kitap.Worksheets.Add("Özet");
        var satir = 1;

        ozet.Cell(satir, 1).Value = pano.Baslik;
        ozet.Cell(satir, 1).Style.Font.Bold = true;
        ozet.Cell(satir, 1).Style.Font.FontSize = 14;
        satir++;

        // Aralık YAZILIR: sayı tek başına anlamsız, "hangi dönem" olmadan
        // rapora yapıştırıldığında iki farklı dönemin sayıları karışıyor.
        ozet.Cell(satir, 1).Value = AralikMetni(bas, bit);
        ozet.Cell(satir, 1).Style.Font.FontColor = XLColor.FromHtml("#6B7280");
        satir += 2;

        Baslik(ozet, satir++, "Özet");

        foreach (var k in pano.Karolar)
        {
            ozet.Cell(satir, 1).Value = k.Etiket;
            ozet.Cell(satir, 2).Value = k.Deger;
            ozet.Cell(satir, 3).Value = k.AltMetin ?? string.Empty;
            satir++;
        }

        ozet.Columns(1, 3).AdjustToContents();

        // ── her dağılım AYRI SAYFA ──
        // Hepsi tek sayfaya alt alta konsaydı süzgeç ve grafik kurulamazdı;
        // Excel'e aktarmanın amacı zaten orada işlem yapmak.
        foreach (var b in pano.Bolumler.Where(x => x.Dilimler.Count > 0))
        {
            var sayfa = kitap.Worksheets.Add(SayfaAdi(kitap, b.Baslik));

            sayfa.Cell(1, 1).Value = "Etiket";
            sayfa.Cell(1, 2).Value = "Adet";
            sayfa.Cell(1, 3).Value = "Yüzde";
            BaslikSatiri(sayfa, 1, 3);

            var s = 2;

            foreach (var d in b.Dilimler)
            {
                sayfa.Cell(s, 1).Value = d.Etiket;
                sayfa.Cell(s, 2).Value = d.Deger;

                // Yüzde SAYI olarak yazılır (0-1 aralığında) ve biçimle
                // gösterilir: metin olsaydı sütunda toplam alınamazdı.
                sayfa.Cell(s, 3).Value = d.Yuzde / 100.0;
                sayfa.Cell(s, 3).Style.NumberFormat.Format = "0.0%";
                s++;
            }

            sayfa.Columns(1, 3).AdjustToContents();
        }

        // ── seyir ──
        if (pano.Seyir.Count > 0)
        {
            var sayfa = kitap.Worksheets.Add("Aylık seyir");

            sayfa.Cell(1, 1).Value = "Ay";
            sayfa.Cell(1, 2).Value = pano.SeyirEtiketi ?? "Adet";
            BaslikSatiri(sayfa, 1, 2);

            var s = 2;

            foreach (var n in pano.Seyir)
            {
                sayfa.Cell(s, 1).Value = n.Etiket;
                sayfa.Cell(s, 2).Value = n.Deger;
                s++;
            }

            sayfa.Columns(1, 2).AdjustToContents();
        }

        using var akis = new MemoryStream();
        kitap.SaveAs(akis);

        return new DisaAktarmaDosyasi(
            akis.ToArray(),
            $"{DosyaAdi(pano.Konu)}-{DateTime.Now:yyyyMMdd}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private static void Baslik(IXLWorksheet sayfa, int satir, string metin)
    {
        sayfa.Cell(satir, 1).Value = metin;
        sayfa.Cell(satir, 1).Style.Font.Bold = true;
    }

    private static void BaslikSatiri(IXLWorksheet sayfa, int satir, int sonSutun)
    {
        var aralik = sayfa.Range(satir, 1, satir, sonSutun);
        aralik.Style.Font.Bold = true;
        aralik.Style.Fill.BackgroundColor = BaslikZemini;
    }

    /// <summary>
    /// Excel sayfa adı kuralı: 31 karakter, <c>[]:*?/\</c> yasak, BENZERSİZ.
    /// </summary>
    /// <remarks>
    /// Aynı adı ikinci kez eklemek <c>ArgumentException</c> atıp çıktıyı
    /// 500'e düşürür; iki bölümün aynı başlığı taşıması olağan.
    /// </remarks>
    private static string SayfaAdi(XLWorkbook kitap, string ham)
    {
        var temiz = new string(ham.Where(c => !"[]:*?/\\".Contains(c)).ToArray()).Trim();
        if (temiz.Length == 0) temiz = "Dağılım";
        if (temiz.Length > 28) temiz = temiz[..28];

        var ad = temiz;
        var n = 2;

        while (kitap.Worksheets.Any(w => w.Name.Equals(ad, StringComparison.OrdinalIgnoreCase)))
        {
            ad = $"{temiz} {n++}";
        }

        return ad;
    }

    private static string AralikMetni(DateTime? bas, DateTime? bit)
        => (bas, bit) switch
        {
            (null, null) => "Dönem: son 12 ay",
            ({ } b, null) => $"Dönem: {b:dd.MM.yyyy} — bugün",
            (null, { } s) => $"Dönem: başlangıçtan {s:dd.MM.yyyy} tarihine",
            ({ } b, { } s) => $"Dönem: {b:dd.MM.yyyy} — {s:dd.MM.yyyy}",
        };

    private static string DosyaAdi(string konu) => konu switch
    {
        "halk-gunu" => "halk-gunu-istatistik",
        "form" => "form-istatistik",
        "protokol" => "protokol-istatistik",
        "cicek" => "cicek-istatistik",
        "ozgecmis" => "ozgecmis-istatistik",
        "sistem" => "sistem-istatistik",
        _ => "istatistik",
    };
}
