using QuestPDF.Fluent;
using KentOS.Kalem.Application.Services;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using KentOS.Kalem.Application.Enums;

namespace KentOS.Kalem.Web.Services.V2;

public interface IDavetCiktiServisi
{
    Task<(byte[] Icerik, string DosyaAdi)> PdfAsync(
        long davetId, DavetCiktiTuru tur, long? kategoriId = null);
}

/// <summary>
/// Davet listesi PDF çıktıları.
/// </summary>
/// <remarks>
/// <para>
/// Dört ayrı çıktı, çünkü liste dört ayrı işte kullanılıyor:
/// <b>Durumlu</b> takip toplantısında, <b>Telefonlu</b> arama yaparken,
/// <b>BosKatilim</b> tören girişinde elle işaretlenirken, <b>BosProtokol</b>
/// protokol sırasını göstermek için. Tek bir "her şey" çıktısı bunların
/// hiçbirinde kullanışlı olmuyordu.
/// </para>
/// <para>
/// Çıktı <b>görünürlük kapısından geçmiş</b> veriyle üretilir
/// (<see cref="IDavetServisi.DetayAsync"/>): yazdırılan bir liste sızıntının
/// en kolay yolu.
/// </para>
/// </remarks>
public class DavetCiktiServisi(
    IDavetServisi _davet,
    ICurrentUserService _kullanici,
    IInstitutionService _kurum) : IDavetCiktiServisi
{
    // Kurum adı KURUM AYARLARINDAN; gerekçe `CiktiKimligi` içinde.

    /// <summary>
    /// Başlıktaki birim ÇIKTIYI ALANIN birimidir.
    ///
    /// Sabit "Başkanlık Makamı" yazıyordu; uygulamayı bütün müdürlükler
    /// kullanıyor ve listeler kullanıcının birimine göre süzülüyor — başka bir
    /// birimin adıyla basılan kâğıt, kimin listesi olduğunu yanlış söylüyordu.
    /// </summary>
    private const string VarsayilanBirim = "Başkanlık Makamı";

    static DavetCiktiServisi()
    {
        // QuestPDF topluluk lisansı; ayarlanmazsa ilk PDF çağrısı istisna atar
        // ve uç nokta 500 döner. `DisaAktarmaServisi` de aynı şeyi yapıyor
        // ama statik kurucu YALNIZCA kendi sınıfı için çalışır.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<(byte[] Icerik, string DosyaAdi)> PdfAsync(
        long davetId, DavetCiktiTuru tur, long? kategoriId = null)
    {
        var davet = await _davet.DetayAsync(davetId);
        var birim = await _kullanici.GetCurrentBirimAdiAsync() ?? VarsayilanBirim;
        var kimlik = await _kurum.CiktiKimligiAsync();

        var kisiler = davet.Kisiler;
        // Kategori süzgeci ADA göre değil KİMLİĞE göre olmalıydı ama DTO adı
        // taşıyor; kategori kimliği protokol kaydında. Basit tutuldu: süzgeç
        // verildiğinde o kategorinin adıyla eşleşenler yazdırılır.
        if (kategoriId is not null)
        {
            // Kimlik → ad eşlemesi için listedeki kayıtlar yeterli.
            var ad = kisiler.FirstOrDefault(k => k.ProtokolId == kategoriId)?.Kategori;
            if (ad is not null) kisiler = kisiler.Where(k => k.Kategori == ad).ToList();
        }

        var belge = Document.Create(sayfa =>
        {
            sayfa.Page(p =>
            {
                p.Size(PageSizes.A4);
                p.Margin(1.4f, Unit.Centimetre);
                p.DefaultTextStyle(t => t.FontSize(9.5f).FontFamily("Helvetica"));

                p.Header().Element(e => Baslik(e, davet, tur, birim, kimlik.Ad));
                p.Content().Element(e => Govde(e, kisiler, tur));
                p.Footer().Element(e => AltBilgi(e, birim, kimlik.Ad));
            });
        });

        var adParcasi = tur switch
        {
            DavetCiktiTuru.Telefonlu => "telefon-listesi",
            DavetCiktiTuru.BosKatilim => "katilim-listesi",
            DavetCiktiTuru.BosProtokol => "protokol-listesi",
            _ => "davet-takip",
        };

        return (belge.GeneratePdf(), $"{adParcasi}-{davetId}.pdf");
    }

    private static void Baslik(
        IContainer kap, DavetDetayDto d, DavetCiktiTuru tur, string birim, string kurumAdi)
    {
        kap.Column(s =>
        {
            s.Item().Row(r =>
            {
                r.RelativeItem().Column(c =>
                {
                    c.Item().Text(kurumAdi).FontSize(12).Bold();
                    c.Item().Text(birim).FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                r.ConstantItem(190).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(BaslikMetni(tur)).FontSize(10).Bold();
                    if (d.Tarih is { } t)
                    {
                        c.Item().AlignRight().Text(t.ToString("dd.MM.yyyy"))
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    }
                });
            });

            s.Item().PaddingTop(6).Text(d.Baslik).FontSize(13).Bold();

            if (!string.IsNullOrWhiteSpace(d.Yer))
            {
                s.Item().Text($"Yer: {d.Yer}").FontSize(9).FontColor(Colors.Grey.Darken1);
            }

            s.Item().PaddingTop(6).PaddingBottom(4)
                .BorderBottom(1).BorderColor(Colors.Grey.Lighten1);
        });
    }

    private static string BaslikMetni(DavetCiktiTuru tur) => tur switch
    {
        DavetCiktiTuru.Telefonlu => "DAVET — TELEFON LİSTESİ",
        DavetCiktiTuru.BosKatilim => "KATILIM LİSTESİ",
        DavetCiktiTuru.BosProtokol => "PROTOKOL LİSTESİ",
        _ => "DAVET TAKİP LİSTESİ",
    };

    private static void Govde(IContainer kap, List<DavetKisiDto> kisiler, DavetCiktiTuru tur)
    {
        kap.Table(t =>
        {
            t.ColumnsDefinition(s =>
            {
                s.ConstantColumn(22);          // sıra
                s.RelativeColumn(3);           // ad soyad
                s.RelativeColumn(3);           // unvan / kurum

                switch (tur)
                {
                    case DavetCiktiTuru.Telefonlu:
                        s.RelativeColumn(2);   // telefon
                        s.RelativeColumn(2);   // cep
                        break;
                    case DavetCiktiTuru.BosKatilim:
                        s.ConstantColumn(50);  // katıldı kutusu
                        s.RelativeColumn(3);   // imza
                        break;
                    case DavetCiktiTuru.BosProtokol:
                        break;
                    default:
                        s.ConstantColumn(70);  // durum
                        s.ConstantColumn(46);  // arandı
                        s.ConstantColumn(46);  // mesaj
                        s.RelativeColumn(3);   // not
                        break;
                }
            });

            var basliklar = new List<string> { "#", "Ad Soyad", "Unvan / Kurum" };
            basliklar.AddRange(tur switch
            {
                DavetCiktiTuru.Telefonlu => new[] { "Telefon", "Cep" },
                DavetCiktiTuru.BosKatilim => new[] { "Katıldı", "İmza" },
                DavetCiktiTuru.BosProtokol => [],
                _ => ["Durum", "Arandı", "Mesaj", "Not"],
            });

            t.Header(b =>
            {
                foreach (var h in basliklar)
                {
                    b.Cell().Background("#002E6D").Padding(4)
                        .Text(h).FontColor(Colors.White).Bold().FontSize(8.5f);
                }
            });

            var sira = 1;
            string? sonKategori = null;

            foreach (var k in kisiler)
            {
                // Kategori başlığı: protokol listesi kategoriye göre okunur,
                // düz bir isim dizisi törende işe yaramıyor.
                if (k.Kategori != sonKategori)
                {
                    sonKategori = k.Kategori;
                    t.Cell().ColumnSpan(SutunSayisi(tur))
                        .PaddingTop(6).PaddingBottom(2)
                        .Text(k.Kategori.ToUpperInvariant())
                        .FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                }

                Veri(t, sira.ToString());
                Veri(t, k.AdSoyad, kalin: true);
                Veri(t, string.Join(" · ", new[] { k.Unvan, k.Kurum }.Where(x => !string.IsNullOrWhiteSpace(x))));

                switch (tur)
                {
                    case DavetCiktiTuru.Telefonlu:
                        Veri(t, k.Telefon ?? "");
                        Veri(t, k.CepTelefon ?? "");
                        break;

                    case DavetCiktiTuru.BosKatilim:
                        // Elle işaretlenecek boş kutu ve imza alanı.
                        t.Cell().Border(0.5f).BorderColor(Colors.Grey.Medium).Height(20);
                        t.Cell().Border(0.5f).BorderColor(Colors.Grey.Medium).Height(20);
                        break;

                    case DavetCiktiTuru.BosProtokol:
                        break;

                    default:
                        Veri(t, DurumMetni(k.Durum));
                        Veri(t, k.Arandi ? "✓" : "");
                        Veri(t, k.MesajGonderildi ? "✓" : "");
                        Veri(t, k.Not ?? "");
                        break;
                }

                sira++;
            }
        });
    }

    private static uint SutunSayisi(DavetCiktiTuru tur) => tur switch
    {
        DavetCiktiTuru.Telefonlu => 5,
        DavetCiktiTuru.BosKatilim => 5,
        DavetCiktiTuru.BosProtokol => 3,
        _ => 7,
    };

    private static string DurumMetni(DavetDurumu d) => d switch
    {
        DavetDurumu.Katilacak => "Katılacak",
        DavetDurumu.Katilmayacak => "Katılmayacak",
        DavetDurumu.Ulasilamadi => "Ulaşılamadı",
        _ => "Beklemede",
    };

    private static void Veri(TableDescriptor t, string metin, bool kalin = false)
    {
        var h = t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3);
        var y = h.Text(metin).FontSize(9);
        if (kalin) y.SemiBold();
    }

    private static void AltBilgi(IContainer kap, string birim, string kurumAdi) =>
        kap.PaddingTop(6).BorderTop(0.5f).BorderColor(Colors.Grey.Lighten1)
            .Row(r =>
            {
                r.RelativeItem().Text($"{kurumAdi} · {birim}")
                    .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                r.RelativeItem().AlignRight().Text(x =>
                {
                    x.CurrentPageNumber().FontSize(7.5f);
                    x.Span(" / ").FontSize(7.5f);
                    x.TotalPages().FontSize(7.5f);
                });
            });
}
