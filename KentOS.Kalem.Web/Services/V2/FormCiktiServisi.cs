using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto.V2.Form;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Web.Data;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>Form yanıtlarının dışa aktarımı.</summary>
public interface IFormCiktiServisi
{
    Task<(byte[] Icerik, string DosyaAdi)> ExcelAsync(long formId, CancellationToken iptal = default);
}

/// <summary>
/// YANIT EXCEL'İ — sütunlar TANIMDAN türetilir.
/// </summary>
/// <remarks>
/// <para>
/// Sabit bir sütun listesi yazılamaz: her formun soruları farklı. Sütunlar
/// yayındaki tanımın alan sırasından üretiliyor, hücreler her yanıtın
/// JSONB'sinden okunuyor.
/// </para>
/// <para>
/// <b>Sütun anahtarı alan KİMLİĞİ, başlığı alan ETİKETİ.</b> Etiketle
/// eşleştirilseydi iki sorunun aynı metni taşıması (çok olağan: "Açıklama")
/// sütunları birbirine karıştırırdı.
/// </para>
/// <para>
/// <b>Yalnızca YAYINDAKİ sürümün alanları sütun olur.</b> Eski sürümlerde
/// var olup kaldırılmış bir soru sütun açmıyor; o cevap yanıt detayında
/// duruyor. Alternatif, bütün sürümlerin birleşimini sütuna çevirmekti ve
/// çok düzenlenmiş bir formda tablo okunamaz hâle geliyordu.
/// </para>
/// </remarks>
public sealed class FormCiktiServisi(
    AppDbContext _context,
    IFormServisi _formServisi) : IFormCiktiServisi
{
    public async Task<(byte[] Icerik, string DosyaAdi)> ExcelAsync(
        long formId, CancellationToken iptal = default)
    {
        var form = await ((FormServisi)_formServisi).ErisebilirMiAsync(formId, iptal);

        var surum = form.YayinSurumId is { } id
            ? await _context.FormSurumleri.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, iptal)
            : await _context.FormSurumleri.AsNoTracking()
                .Where(s => s.FormId == formId)
                .OrderByDescending(s => s.SurumNo)
                .FirstOrDefaultAsync(iptal);

        var tanim = FormServisi.TanimiCoz(surum?.Tanim);

        var alanlar = FormDogrulayici.TumAlanlar(tanim)
            .Where(a => !FormDogrulayici.BlokMu(a.Tip))
            .ToList();

        var yanitlar = await _context.FormYanitlari.AsNoTracking()
            .Where(y => y.FormId == formId && y.Durum == FormYanitDurumu.Gonderildi)
            .OrderBy(y => y.GonderimTarihi)
            .Select(y => new
            {
                y.TakipNo, y.AdSoyad, y.Telefon, y.Eposta, y.GonderimTarihi, y.Cevaplar,
            })
            .ToListAsync(iptal);

        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("Yanıtlar");

        // ── başlık satırı ──
        var sabitler = new[] { "Takip No", "Gönderim", "Ad Soyad", "Telefon", "E-posta" };

        for (var i = 0; i < sabitler.Length; i++)
        {
            sayfa.Cell(1, i + 1).Value = sabitler[i];
        }

        for (var i = 0; i < alanlar.Count; i++)
        {
            sayfa.Cell(1, sabitler.Length + i + 1).Value = alanlar[i].Etiket;
        }

        var baslikAraligi = sayfa.Range(1, 1, 1, sabitler.Length + alanlar.Count);
        baslikAraligi.Style.Font.Bold = true;
        baslikAraligi.Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF3F8");

        // ── satırlar ──
        var satir = 2;

        foreach (var y in yanitlar)
        {
            var cevaplar = FormYanitServisi.CevaplariCoz(y.Cevaplar);

            sayfa.Cell(satir, 1).Value = y.TakipNo;
            sayfa.Cell(satir, 2).Value = y.GonderimTarihi;
            sayfa.Cell(satir, 2).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
            sayfa.Cell(satir, 3).Value = y.AdSoyad ?? string.Empty;
            sayfa.Cell(satir, 4).Value = y.Telefon ?? string.Empty;
            sayfa.Cell(satir, 5).Value = y.Eposta ?? string.Empty;

            for (var i = 0; i < alanlar.Count; i++)
            {
                var alan = alanlar[i];
                cevaplar.TryGetValue(alan.Kimlik, out var ham);

                var hucre = sayfa.Cell(satir, sabitler.Length + i + 1);
                var deger = HucreDegeri(alan, ham);

                /*
                  METİN OLARAK YAZILIYOR, formül olarak değil.

                  "=1+1" ya da "@SUM(...)" ile başlayan bir cevap Excel'de
                  FORMÜL olarak yorumlanıyor — vatandaşın yazdığı metnin
                  kurumun makinesinde çalışması demek (CSV/Excel enjeksiyonu).
                  `SetValue<string>` içeriği düz metin sayıyor.
                */
                if (deger is string m && m.Length > 0 && "=+-@\t\r".Contains(m[0]))
                {
                    hucre.SetValue("'" + m);
                }
                else
                {
                    hucre.Value = XLCellValue.FromObject(deger);
                }
            }

            satir++;
        }

        if (yanitlar.Count == 0)
        {
            sayfa.Cell(2, 1).Value = "Bu forma henüz yanıt gelmedi.";
        }

        sayfa.Columns().AdjustToContents(8.0, 45.0);
        sayfa.SheetView.FreezeRows(1);

        using var akis = new MemoryStream();
        kitap.SaveAs(akis);

        var ad = $"{Temizle(form.Baslik)}-yanitlar-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
        return (akis.ToArray(), ad);
    }

    /// <summary>
    /// JSONB değerini hücreye yazılabilir hâle getirir.
    /// </summary>
    /// <remarks>
    /// Seçim alanlarında JSONB'de seçenek KİMLİĞİ duruyor; tabloya "sec_3"
    /// yazmak raporu okunamaz kılardı.
    /// </remarks>
    private static object HucreDegeri(FormAlaniDto alan, object? sarmal)
        => FormDegerMetni.Hucre(alan, sarmal);

    private static string Temizle(string ad)
    {
        var gecersiz = Path.GetInvalidFileNameChars();
        var temiz = new string(ad.Where(c => !gecersiz.Contains(c) && c != ' ').ToArray());
        return temiz.Length > 40 ? temiz[..40] : (temiz.Length > 0 ? temiz : "form");
    }
}
