using ClosedXML.Excel;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>
/// DÜZ TABLO EXCEL'İ — bütün liste çıktılarının ortak kurulumu.
/// </summary>
/// <remarks>
/// <para>
/// Başlık şeridi, donmuş ilk satır, otomatik süzgeç ve sütun genişliği tek
/// yerde. Etkinlik/talep çıktıları bunu zaten kullanıyordu; görev, proje,
/// özgeçmiş, protokol ve halk günü havuzu çıktıları eklenirken kopyalanmak
/// yerine buraya çıkarıldı.
/// </para>
/// <para>
/// <b>Otomatik süzgeç ve donmuş satır bilinçli:</b> bu tablolar süzülmek ve
/// sayılmak için var. Aynı kararın tersi form yanıt çıktısında yazılı —
/// orada sayfada birden çok başlık satırı olduğu için süzgeç KONMUYOR.
/// </para>
/// </remarks>
internal static class ExcelTablosu
{
    private const string ExcelTuru =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static DisaAktarmaDosyasi Uret(
        string sayfaAdi, string[] basliklar, IEnumerable<string?[]> satirlar, string dosyaOneki)
    {
        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add(sayfaAdi);

        for (var s = 0; s < basliklar.Length; s++)
        {
            var hucre = sayfa.Cell(1, s + 1);
            hucre.Value = basliklar[s];
            hucre.Style.Font.Bold = true;
            // Kurumsal lacivert başlık şeridi.
            hucre.Style.Fill.BackgroundColor = XLColor.FromHtml("#002E6D");
            hucre.Style.Font.FontColor = XLColor.White;
        }

        var satir = 2;
        foreach (var s in satirlar)
        {
            for (var i = 0; i < s.Length; i++)
            {
                sayfa.Cell(satir, i + 1).Value = s[i] ?? string.Empty;
            }
            satir++;
        }

        sayfa.Columns().AdjustToContents();
        // Başlık satırı kaydırırken sabit kalsın — uzun listelerde şart.
        sayfa.SheetView.FreezeRows(1);
        sayfa.Range(1, 1, Math.Max(1, satir - 1), basliklar.Length).SetAutoFilter();

        using var bellek = new MemoryStream();
        kitap.SaveAs(bellek);

        return new DisaAktarmaDosyasi(
            bellek.ToArray(),
            $"{dosyaOneki}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx",
            ExcelTuru);
    }

}
