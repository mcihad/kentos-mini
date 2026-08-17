using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Services;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>Halk günü çıktısının türü.</summary>
/// <remarks>
/// İki ayrı iş, iki ayrı kâğıt: <b>Program</b> gün başlamadan elden ele
/// dolaşıyor (kapıda, salonda, makamda) ve yalnızca "kim, kaçta, hangi konuda"
/// diyor. <b>Sonuç</b> ise gün bittikten sonra Özel Kalem'in masasında
/// kalıyor; orada görüşme notu ve takip işareti şart. Tek bir "her şey"
/// tablosu ikisinde de kullanışsızdı: programda gereksiz sütunlar satırı
/// daraltıyor, sonuçta da eksik kalıyordu.
/// </remarks>
public enum HalkGunuCiktiTuru
{
    /// <summary>Sıra · Saat · Telefon · Ad Soyad · Açıklama — gruplanmış.</summary>
    Program = 0,

    /// <summary>Programa ek olarak durum, görüşme notu ve takip işareti.</summary>
    Sonuc = 1,

    /// <summary>Salonda elle işaretlemek için boş imza/katılım sütunlu program.</summary>
    Imza = 2,
}

public interface IHalkGunuCiktiServisi
{
    /// <param name="yatay">
    /// Sayfa YATAY basılsın mı? Konu ve görüşme notu uzun olduğunda dikey A4
    /// sütunları sıkıştırıyor, metin alt alta kırpılıyordu.
    /// </param>
    Task<DisaAktarmaDosyasi> ExcelAsync(
        long halkGunuId, long? dilimId, HalkGunuCiktiTuru tur, KatilimDurumu? durum,
        bool yatay = false);

    Task<DisaAktarmaDosyasi> PdfAsync(
        long halkGunuId, long? dilimId, HalkGunuCiktiTuru tur, KatilimDurumu? durum,
        bool yatay = false);
}

/// <summary>
/// Halk günü listesinin yazdırılabilir çıktıları — Excel ve PDF.
/// </summary>
/// <remarks>
/// <para>
/// Çıktı <b>gruplanmış</b>: her zaman dilimi kendi başlığıyla ve <b>kendi sıra
/// numarasıyla</b> yazılır. Salonda çağrılan şey "bugünün 14. kişisi" değil,
/// "ikinci grubun 4. kişisi"; düz numaralanmış tek bir liste kapıdaki
/// görevliye yanlış sırayı okutuyordu.
/// </para>
/// <para>
/// Veri <see cref="IHalkGunuServisi.DetayAsync"/> üzerinden, yani
/// <b>görünürlük kapısından geçmiş</b> hâlde alınır: yazdırılan bir liste
/// sızıntının en kolay yolu.
/// </para>
/// </remarks>
public class HalkGunuCiktiServisi(
    IHalkGunuServisi _halkGunu,
    ICurrentUserService _kullanici) : IHalkGunuCiktiServisi
{
    private const string Kurum = "SİVAS BELEDİYESİ";

    /// <summary>
    /// Başlıktaki birim ÇIKTIYI ALANIN birimidir; sabit bir ad basmak, kâğıdın
    /// kimin listesi olduğunu yanlış söylüyordu.
    /// </summary>
    private const string VarsayilanBirim = "Başkanlık Makamı";
    private const string Lacivert = "#002E6D";
    private const string Altin = "#A78952";

    static HalkGunuCiktiServisi()
    {
        // QuestPDF topluluk lisansı. Statik kurucu YALNIZCA kendi sınıfı için
        // çalışır; başka bir PDF sınıfındaki ayar burayı kapsamaz (ilk
        // denemede 500 verdirmişti).
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ── ortak veri ─────────────────────────────────────────────────────

    /// <summary>Yazdırılacak tek grup: başlık + o gruba ait kişiler.</summary>
    private sealed record Grup(string Baslik, string Saat, List<HalkGunuKatilimDto> Kisiler);

    private async Task<(HalkGunuDetayDto Gun, List<Grup> Gruplar)> VeriAsync(
        long halkGunuId, long? dilimId, KatilimDurumu? durum)
    {
        var gun = await _halkGunu.DetayAsync(halkGunuId);

        static List<HalkGunuKatilimDto> Suz(
            IEnumerable<HalkGunuKatilimDto> kisiler, KatilimDurumu? durum) =>
            kisiler.Where(k => durum is null || k.Durum == durum)
                   .OrderBy(k => k.SiraNo)
                   .ToList();

        var gruplar = new List<Grup>();

        foreach (var d in gun.Dilimler.Where(d => dilimId is null || d.Id == dilimId)
                                      .OrderBy(d => d.Baslangic))
        {
            var kisiler = Suz(d.Kisiler, durum);
            if (kisiler.Count == 0) continue;

            var saat = $"{d.Baslangic:HH:mm} – {d.Bitis:HH:mm}";
            gruplar.Add(new Grup(
                string.IsNullOrWhiteSpace(d.Baslik) ? saat : $"{saat} · {d.Baslik}",
                saat,
                kisiler));
        }

        // Dilime yerleştirilmemişler de basılır: gün içinde "sırası
        // belirlenmemiş" diye bir kategori var ve kâğıtta görünmezse salonda
        // kimsenin haberi olmuyor.
        if (dilimId is null)
        {
            var kalan = Suz(gun.Atanmamislar, durum);
            if (kalan.Count > 0)
                gruplar.Add(new Grup("Saati belirlenmemiş", "—", kalan));
        }

        return (gun, gruplar);
    }

    /// <summary>
    /// Yazdırılacak telefon: <c>0532 111 22 33</c>.
    /// </summary>
    /// <remarks>
    /// Veritabanında aynı numara <c>+90 541 298 34 52</c>, <c>05412983451</c>
    /// ve <c>0541 298 34 50</c> diye üç türlü duruyor (yıllar içinde farklı
    /// formlardan girildi). Basılı listede alt alta üç ayrı biçim, telefonu
    /// çeviren kişiyi her satırda yeniden okumaya zorluyor.
    /// </remarks>
    private static string TelefonBicimi(string? ham)
    {
        var sade = Telefon.Duzelt(ham);
        if (sade is null) return string.Empty;

        var rakam = new string(sade.Where(char.IsDigit).ToArray());
        return rakam.Length == 11 && rakam.StartsWith('0')
            ? $"{rakam[..4]} {rakam[4..7]} {rakam[7..9]} {rakam[9..]}"
            : sade;
    }

    private static string GunBasligi(HalkGunuDetayDto g) =>
        string.IsNullOrWhiteSpace(g.Baslik) ? $"Halk Günü {g.Tarih:dd.MM.yyyy}" : g.Baslik!;

    private static string DosyaOneki(HalkGunuCiktiTuru tur) => tur switch
    {
        HalkGunuCiktiTuru.Sonuc => "halk-gunu-sonuc",
        HalkGunuCiktiTuru.Imza => "halk-gunu-imza",
        _ => "halk-gunu-program",
    };

    private static string CiktiBasligi(HalkGunuCiktiTuru tur) => tur switch
    {
        HalkGunuCiktiTuru.Sonuc => "HALK GÜNÜ — GÖRÜŞME SONUÇLARI",
        HalkGunuCiktiTuru.Imza => "HALK GÜNÜ — KATILIM ÇİZELGESİ",
        _ => "HALK GÜNÜ PROGRAMI",
    };

    /// <summary>PDF sütunları: genişlik ve başlık TEK yerde.</summary>
    /// <remarks>
    /// Başlıklar bir dönem Excel ile ORTAK listeden (<see cref="Basliklar"/>)
    /// geliyordu, oysa iki çıktı aynı bilgiyi farklı taşıyor: Excel'de
    /// "İlgilenilecek" ayrı bir sütun, PDF'te durumun yanına konan bir ★.
    /// Sonuç raporunda 7 sütunlu tabloya 8 başlık hücresi giriyor, QuestPDF
    /// fazlalığı İKİNCİ başlık satırına sarıyordu: "İlgilenilecek" ilk sütuna
    /// düşüp "Sıra"nın altına yapışık görünüyordu.
    /// Genişlik ve başlık artık aynı diziden okunduğu için ayrışamazlar.
    /// </remarks>
    internal static (float Genislik, bool Sabit, string Baslik)[] PdfSutunlari(
        HalkGunuCiktiTuru tur)
    {
        (float, bool, string)[] ortak =
        [
            (26, true, "Sıra"),
            (64, true, "Saat"),
            (92, true, "Telefon"),
            (2.2f, false, "Ad Soyad"),
            (3.2f, false, "Açıklama"),
        ];

        return tur switch
        {
            HalkGunuCiktiTuru.Sonuc =>
                // 58pt'de "Görüşüldü ★" sığmıyor, ★ tek başına ikinci satıra
                // düşüyordu; en uzun durum adı + işaret tek satırda kalmalı.
                [.. ortak, (66, true, "Durum"), (3f, false, "Görüşme Notu")],
            HalkGunuCiktiTuru.Imza =>
                [.. ortak, (46, true, "Geldi"), (90, true, "İmza")],
            _ => ortak,
        };
    }

    /// <summary>Excel sütun başlıkları — türe göre.</summary>
    internal static string[] Basliklar(HalkGunuCiktiTuru tur) => tur switch
    {
        HalkGunuCiktiTuru.Sonuc =>
            ["Sıra", "Saat", "Telefon", "Ad Soyad", "Açıklama", "Durum", "Görüşme Notu", "İlgilenilecek"],
        HalkGunuCiktiTuru.Imza =>
            ["Sıra", "Saat", "Telefon", "Ad Soyad", "Açıklama", "Geldi", "İmza"],
        _ =>
            ["Sıra", "Saat", "Telefon", "Ad Soyad", "Açıklama"],
    };

    /// <summary>
    /// Katılım çizelgesinde "Geldi" sütununa yazılacak metin; henüz
    /// işaretlenmemiş kişide boş (elle doldurulacak).
    /// </summary>
    internal static string KatilimIsareti(KatilimDurumu durum) => durum switch
    {
        KatilimDurumu.Geldi or KatilimDurumu.Gorusuldu => "Geldi",
        KatilimDurumu.Gelmedi => "Gelmedi",
        KatilimDurumu.Iptal => "İptal",
        _ => "",
    };

    internal static string?[] Satir(
        HalkGunuKatilimDto k, int sira, string saat, HalkGunuCiktiTuru tur) => tur switch
    {
        HalkGunuCiktiTuru.Sonuc =>
        [
            sira.ToString(), saat, TelefonBicimi(k.Telefon), k.AdSoyad, k.Konu,
            k.DurumAd, k.GorusmeNotu, k.DegerlendirmeyeEsas ? "Evet" : "",
        ],
        // "Geldi" sütunu boş bırakılmıyor: kayıtta durum varsa yazılır.
        // Boş bırakıldığı sürece gelmeyen kişi de gelmiş gibi okunuyordu.
        HalkGunuCiktiTuru.Imza =>
        [
            sira.ToString(), saat, TelefonBicimi(k.Telefon), k.AdSoyad, k.Konu,
            KatilimIsareti(k.Durum), "",
        ],
        _ =>
        [
            sira.ToString(), saat, TelefonBicimi(k.Telefon), k.AdSoyad, k.Konu,
        ],
    };

    // ── Excel ──────────────────────────────────────────────────────────

    /// <summary>
    /// Gruplanmış Excel: kurum başlığı, her grup için ayrı başlık bandı ve
    /// grubun kendi içinde 1'den başlayan sıra numarası.
    /// </summary>
    /// <remarks>
    /// Otomatik süzgeç (<c>AutoFilter</c>) bilerek YOK: sayfada birden çok
    /// başlık satırı var, süzgeç ilkini "tablo" sanıp grup başlıklarını veri
    /// satırı gibi gizliyordu. Bu tablo süzülmek için değil, <b>basılmak</b>
    /// için.
    /// </remarks>
    public async Task<DisaAktarmaDosyasi> ExcelAsync(
        long halkGunuId, long? dilimId, HalkGunuCiktiTuru tur, KatilimDurumu? durum,
        bool yatay = false)
    {
        var (gun, gruplar) = await VeriAsync(halkGunuId, dilimId, durum);
        var birim = await _kullanici.GetCurrentBirimAdiAsync() ?? VarsayilanBirim;
        var basliklar = Basliklar(tur);
        var sonSutun = basliklar.Length;

        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("Halk Günü");

        var satir = 1;

        // ── kurum başlığı ──
        var kurumHucre = sayfa.Range(satir, 1, satir, sonSutun).Merge();
        kurumHucre.Value = $"{Kurum} · {birim}";
        kurumHucre.Style.Fill.BackgroundColor = XLColor.FromHtml(Lacivert);
        kurumHucre.Style.Font.FontColor = XLColor.White;
        kurumHucre.Style.Font.Bold = true;
        kurumHucre.Style.Font.FontSize = 13;
        kurumHucre.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sayfa.Row(satir).Height = 24;
        satir++;

        var altBaslik = sayfa.Range(satir, 1, satir, sonSutun).Merge();
        altBaslik.Value = string.Join("  ·  ", new[]
        {
            CiktiBasligi(tur),
            GunBasligi(gun),
            gun.Tarih.ToString("dd MMMM yyyy dddd"),
            gun.Konum,
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
        altBaslik.Style.Font.FontColor = XLColor.FromHtml(Altin);
        altBaslik.Style.Font.Bold = true;
        altBaslik.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        satir += 2;

        foreach (var grup in gruplar)
        {
            // Grup bandı
            var band = sayfa.Range(satir, 1, satir, sonSutun).Merge();
            band.Value = $"{grup.Baslik}   ({grup.Kisiler.Count} kişi)";
            band.Style.Fill.BackgroundColor = XLColor.FromHtml("#EDEBE4");
            band.Style.Font.Bold = true;
            band.Style.Font.FontColor = XLColor.FromHtml(Lacivert);
            band.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            band.Style.Border.BottomBorderColor = XLColor.FromHtml(Altin);
            satir++;

            // Sütun başlıkları — HER GRUPTA tekrarlanır: liste birkaç sayfaya
            // yayılınca ikinci sayfada hangi sütunun ne olduğu kayboluyordu.
            for (var i = 0; i < basliklar.Length; i++)
            {
                var h = sayfa.Cell(satir, i + 1);
                h.Value = basliklar[i];
                h.Style.Font.Bold = true;
                h.Style.Font.FontSize = 10;
                h.Style.Font.FontColor = XLColor.FromHtml(Lacivert);
                h.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }
            satir++;

            var sira = 1;
            foreach (var k in grup.Kisiler)
            {
                var degerler = Satir(k, sira, grup.Saat, tur);
                for (var i = 0; i < degerler.Length; i++)
                {
                    var h = sayfa.Cell(satir, i + 1);
                    h.Value = degerler[i] ?? string.Empty;
                    h.Style.Alignment.WrapText = i == 4;   // açıklama sarılır
                    if (i is 0 or 1 or 2)
                        h.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    if (i == 3) h.Style.Font.Bold = true;  // ad soyad
                }

                // İmza çizelgesinde son iki sütun elle doldurulacak: kutu
                // görünmesi için kenarlık ve yükseklik verilir.
                if (tur == HalkGunuCiktiTuru.Imza)
                {
                    sayfa.Range(satir, sonSutun - 1, satir, sonSutun)
                         .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    sayfa.Row(satir).Height = 26;
                }

                satir++;
                sira++;
            }

            satir++;   // gruplar arasında bir boş satır
        }

        if (gruplar.Count == 0)
        {
            sayfa.Cell(satir, 1).Value = "Bu süzgece uyan kayıt yok.";
        }

        sayfa.Columns().AdjustToContents();
        sayfa.Column(5).Width = 46;    // açıklama sabit genişlik: sarılsın
        // YÖN SEÇİLEBİLİR: konu ve görüşme notu uzun olduğunda dikey sayfada
        // sütunlar sıkışıyor ve metin okunmuyordu.
        sayfa.PageSetup.PageOrientation =
            yatay ? XLPageOrientation.Landscape : XLPageOrientation.Portrait;
        sayfa.PageSetup.FitToPages(1, 0);
        sayfa.PageSetup.Margins.Top = 0.5;
        sayfa.PageSetup.Margins.Bottom = 0.5;

        using var akis = new MemoryStream();
        kitap.SaveAs(akis);

        return new DisaAktarmaDosyasi(
            akis.ToArray(),
            $"{DosyaOneki(tur)}-{gun.Tarih:yyyyMMdd}-{DateTime.Now:HHmm}{(yatay ? "-yatay" : "")}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    // ── PDF ────────────────────────────────────────────────────────────

    public async Task<DisaAktarmaDosyasi> PdfAsync(
        long halkGunuId, long? dilimId, HalkGunuCiktiTuru tur, KatilimDurumu? durum,
        bool yatay = false)
    {
        var (gun, gruplar) = await VeriAsync(halkGunuId, dilimId, durum);
        var birim = await _kullanici.GetCurrentBirimAdiAsync() ?? VarsayilanBirim;

        var belge = Document.Create(b =>
        {
            b.Page(p =>
            {
                // Yatayda konu ve not sütunlarına yer açılır; içerik aynı,
                // yalnızca kâğıdın yönü değişir.
                p.Size(yatay ? PageSizes.A4.Landscape() : PageSizes.A4);
                p.Margin(1.4f, Unit.Centimetre);
                p.DefaultTextStyle(t => t.FontSize(9.5f).FontFamily("Helvetica"));

                p.Header().Element(e => Baslik(e, gun, tur, birim));
                p.Content().Element(e => Govde(e, gruplar, tur));
                p.Footer().Element(e => AltBilgi(e, birim));
            });
        });

        return new DisaAktarmaDosyasi(
            belge.GeneratePdf(),
            $"{DosyaOneki(tur)}-{gun.Tarih:yyyyMMdd}-{DateTime.Now:HHmm}{(yatay ? "-yatay" : "")}.pdf",
            "application/pdf");
    }

    private static void Baslik(
        IContainer kap, HalkGunuDetayDto g, HalkGunuCiktiTuru tur, string birim)
    {
        kap.Column(s =>
        {
            s.Item().Row(r =>
            {
                r.RelativeItem().Column(c =>
                {
                    c.Item().Text(Kurum).FontSize(12).Bold().FontColor(Lacivert);
                    c.Item().Text(birim).FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                r.ConstantItem(210).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(CiktiBasligi(tur))
                        .FontSize(9.5f).Bold().FontColor(Altin);
                    c.Item().AlignRight().Text(g.Tarih.ToString("dd MMMM yyyy dddd"))
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });

            s.Item().PaddingTop(8).Text(GunBasligi(g)).FontSize(14).Bold();

            var alt = string.Join("  ·  ", new[]
            {
                g.Konum,
                $"{g.KisiSayisi} kişi",
                g.GorusulenSayisi > 0 ? $"{g.GorusulenSayisi} görüşüldü" : null,
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            if (alt.Length > 0)
                s.Item().Text(alt).FontSize(9).FontColor(Colors.Grey.Darken1);

            // Altın saç teli: kurumsal kimliğin sayfa çizgisi.
            s.Item().PaddingTop(6).PaddingBottom(4).BorderBottom(1).BorderColor(Altin);
        });
    }

    private static void Govde(IContainer kap, List<Grup> gruplar, HalkGunuCiktiTuru tur)
    {
        if (gruplar.Count == 0)
        {
            kap.PaddingTop(30).AlignCenter()
               .Text("Bu süzgece uyan kayıt yok.").FontColor(Colors.Grey.Darken1);
            return;
        }

        kap.Column(s =>
        {
            foreach (var grup in gruplar)
            {
                s.Item().PaddingTop(10).Background("#F2EFE7").Padding(5).Row(r =>
                {
                    r.RelativeItem().Text(grup.Baslik).Bold().FontSize(10.5f)
                        .FontColor(Lacivert);
                    r.ConstantItem(70).AlignRight()
                        .Text($"{grup.Kisiler.Count} kişi").FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });

                var sutunlar = PdfSutunlari(tur);

                s.Item().PaddingTop(4).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        foreach (var (genislik, sabit, _) in sutunlar)
                        {
                            if (sabit) c.ConstantColumn(genislik);
                            else c.RelativeColumn(genislik);
                        }
                    });

                    t.Header(h =>
                    {
                        foreach (var (_, _, baslik) in sutunlar)
                        {
                            h.Cell().Background(Lacivert).Padding(4)
                                .Text(baslik).FontColor(Colors.White).Bold().FontSize(8.5f);
                        }
                    });

                    var sira = 1;
                    foreach (var k in grup.Kisiler)
                    {
                        Hucre(t, sira.ToString());
                        Hucre(t, grup.Saat);
                        Hucre(t, TelefonBicimi(k.Telefon));
                        Hucre(t, k.AdSoyad, kalin: true);
                        Hucre(t, k.Konu ?? "");

                        switch (tur)
                        {
                            case HalkGunuCiktiTuru.Sonuc:
                                Hucre(t, k.DurumAd + (k.DegerlendirmeyeEsas ? " ★" : ""));
                                Hucre(t, k.GorusmeNotu ?? "");
                                break;

                            case HalkGunuCiktiTuru.Imza:
                                KatilimHucreleri(t, k.Durum);
                                break;
                        }

                        sira++;
                    }
                });

                // ★ ayrı bir sütun değil, durumun yanına konan bir işaret.
                // Ne anlama geldiği yazılmazsa kâğıda bakan kişi bilemiyor;
                // Excel'de bunun "İlgilenilecek" diye kendi sütunu var.
                if (tur == HalkGunuCiktiTuru.Sonuc &&
                    grup.Kisiler.Any(k => k.DegerlendirmeyeEsas))
                {
                    s.Item().PaddingTop(3).Text("★ ilgilenilecek olarak işaretlendi")
                        .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                }
            }
        });
    }

    /// <summary>Katılım çizelgesinin "Geldi" ve "İmza" hücreleri.</summary>
    /// <remarks>
    /// Çizelge iki işte kullanılıyor: gün BAŞLAMADAN salonda elle işaretlenen
    /// boş form, gün BİTTİKTEN sonra da katılımın kâğıt kaydı. İkinci kullanım
    /// bozuktu: kayıtta "Gelmedi" yazan kişi de herkesle aynı BOŞ kutuyla
    /// basılıyordu, yani kâğıda bakan onu gelmiş sanıyordu. Çizelge, adı
    /// "katılım" olmasına rağmen kayıtlı durumu hiç okumuyordu.
    ///
    /// Artık henüz işaretlenmemiş kişi (Bekliyor) boş kutuyla çıkar — form
    /// işlevi bozulmasın; işaretlenmiş olan ise kararı gösterir.
    /// </remarks>
    private static void KatilimHucreleri(TableDescriptor t, KatilimDurumu durum)
    {
        (string Isaret, string Renk) gorunum = durum switch
        {
            KatilimDurumu.Geldi or KatilimDurumu.Gorusuldu => ("✓", "#2E7D5B"),
            KatilimDurumu.Gelmedi => ("✕", "#B3261E"),
            KatilimDurumu.Iptal => ("—", Colors.Grey.Darken1),
            _ => (string.Empty, Colors.Grey.Medium),
        };
        var (isaret, renk) = gorunum;

        var geldi = t.Cell().Border(0.5f).BorderColor(Colors.Grey.Medium).Height(22);
        if (isaret.Length == 0)
        {
            // Bekliyor: elle işaretlenecek, boş kalır.
            geldi.Text(string.Empty);
        }
        else
        {
            geldi.AlignCenter().AlignMiddle()
                 .Text(isaret).FontSize(11).Bold().FontColor(renk);
        }

        // İmza yalnızca gelen kişiden istenir. Gelmeyenin satırında boş bir
        // imza kutusu bırakmak, sonradan doldurulmaya açık bir boşluk demek.
        var imza = t.Cell().Border(0.5f).BorderColor(Colors.Grey.Medium).Height(22);
        if (durum is KatilimDurumu.Gelmedi or KatilimDurumu.Iptal)
            imza.Background(Colors.Grey.Lighten3).Text(string.Empty);
        else
            imza.Text(string.Empty);
    }

    private static void Hucre(TableDescriptor t, string metin, bool kalin = false)
    {
        var h = t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
        var y = h.Text(metin).FontSize(9);
        if (kalin) y.SemiBold();
    }

    private static void AltBilgi(IContainer kap, string birim)
    {
        kap.PaddingTop(6).BorderTop(0.5f).BorderColor(Colors.Grey.Lighten1)
           .PaddingTop(4).Row(r =>
           {
               r.RelativeItem().Text($"{Kurum} · {birim}")
                   .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
               r.RelativeItem().AlignCenter()
                   .Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm"))
                   .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
               r.RelativeItem().AlignRight().Text(t =>
               {
                   t.DefaultTextStyle(s => s.FontSize(7.5f).FontColor(Colors.Grey.Darken1));
                   t.CurrentPageNumber();
                   t.Span(" / ");
                   t.TotalPages();
               });
           });
    }
}
