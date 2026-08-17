using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using KentOS.Mini.Application.Enums;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>Kesme kartı çıktısının ayarları.</summary>
/// <remarks>
/// Kolon ve satır sayısı KULLANICIDAN geliyor çünkü kartın boyu işe göre
/// değişiyor: koltuk arkasına yapıştırılan etiket küçük (3×12), masaya konan
/// isimlik büyük (1×5). Sabit tek bir ölçü, ötekini kullanılmaz yapıyordu.
/// </remarks>
public record KesmeKartAyari(
    [property: JsonPropertyName("sutun")] int Sutun,
    [property: JsonPropertyName("satir")] int Satir,
    /// <summary>Tasarım anahtarı — <see cref="KartTasarimlari.Kesme"/>.</summary>
    [property: JsonPropertyName("tasarim")] string? Tasarim,
    [property: JsonPropertyName("unvanGoster")] bool UnvanGoster,
    [property: JsonPropertyName("kurumGoster")] bool KurumGoster,
    /// <summary>Kesme çizgileri basılsın mı — makasla kesmek için kılavuz.</summary>
    [property: JsonPropertyName("kesmeCizgisi")] bool KesmeCizgisi,
    [property: JsonPropertyName("logoGoster")] bool LogoGoster,
    /// <summary>Amblemin yeri — 0 yok · 1 üst · 2 sol · 3 sağ.</summary>
    [property: JsonPropertyName("logoYeri")] int LogoYeri,
    /// <summary>Amblem boyu — 0 küçük · 1 orta · 2 büyük.</summary>
    [property: JsonPropertyName("logoBoyu")] int LogoBoyu,
    [property: JsonPropertyName("antetGoster")] bool AntetGoster);

/// <summary>Masa (çadır) kartı ayarları.</summary>
public record MasaKartAyari(
    /// <summary>Sayfaya kaç kart — 1 (büyük) ya da 2 (orta).</summary>
    [property: JsonPropertyName("sayfaBasi")] int SayfaBasi,
    [property: JsonPropertyName("tasarim")] string? Tasarim,
    [property: JsonPropertyName("unvanGoster")] bool UnvanGoster,
    [property: JsonPropertyName("kurumGoster")] bool KurumGoster,
    /// <summary>Ad KATLANAN İKİ YÜZE de basılsın mı.</summary>
    /// <remarks>
    /// Çadır kartın arka yüzü konuğun kendisine bakıyor; oraya da ad basmak
    /// "doğru yere mi oturdum" sorusunu masaya eğilmeden cevaplıyor. Kâğıt
    /// tasarrufu isteyen kapatabiliyor.
    /// </remarks>
    [property: JsonPropertyName("ciftYuz")] bool CiftYuz,
    [property: JsonPropertyName("logoGoster")] bool LogoGoster,
    /// <summary>Amblemin yeri — 0 yok · 1 üst · 2 sol · 3 sağ.</summary>
    [property: JsonPropertyName("logoYeri")] int LogoYeri,
    /// <summary>Amblem boyu — 0 küçük · 1 orta · 2 büyük.</summary>
    [property: JsonPropertyName("logoBoyu")] int LogoBoyu,
    [property: JsonPropertyName("antetGoster")] bool AntetGoster);

/// <summary>Kartta basılacak tek kişi.</summary>
public record KartKisisi(
    [property: JsonPropertyName("adSoyad")] string AdSoyad,
    [property: JsonPropertyName("unvan")] string? Unvan,
    [property: JsonPropertyName("kurum")] string? Kurum);

public interface IIsimKartiServisi
{
    /// <summary>Davetin KESME kartları — sandalyeye yapıştırılan etiketler.</summary>
    /// <param name="durum">
    /// Yalnızca bu cevabı verenler. <c>null</c> ise davetin tamamı. İsimlik
    /// dizilirken aranan neredeyse her zaman "katılacak" diyenler; bütün
    /// davetliyi basmak boş sandalyelere isimlik koymak demek.
    /// </param>
    Task<DisaAktarmaDosyasi> KesmeKartPdfAsync(
        long davetId, DavetDurumu? durum, KesmeKartAyari ayar);

    /// <summary>Davetin MASA kartları — ortadan katlanan çadır kartlar.</summary>
    Task<DisaAktarmaDosyasi> MasaKartPdfAsync(
        long davetId, DavetDurumu? durum, MasaKartAyari ayar);
}

/// <summary>
/// Davet listesinden isim kartı üretir — kesme etiketi ve masa kartı.
/// </summary>
/// <remarks>
/// <para>
/// Törenlerde isimlikler sandalyelere yapıştırılıyor ya da masaya konuyor. Bu
/// iş bugüne kadar listeyi Word'e kopyalayıp elle tablo kurarak yapılıyordu:
/// her seferinde yeniden, her seferinde başka ölçüde. Kart artık davetle aynı
/// kaynaktan basılıyor — listede cevap değişince kart da değişiyor.
/// </para>
/// <para>
/// <b>Kaynak PROTOKOL DEFTERİ değil, DAVET.</b> Masaya konacak isimlik
/// kurumun bütün protokol listesi değil, o törene çağrılanlar; defterin
/// tamamını basmak yüzlerce gereksiz kart demekti.
/// </para>
/// <para>
/// <b>Kesme payı KART İÇİNDE değil, ARASINDA.</b> Hücrelerin dış kenarına
/// çizgi çizip aralarına boşluk bırakmak, makasın iki çizgi arasında gitmesini
/// gerektiriyor ve kartlar farklı boyda çıkıyordu. Izgara bitişik, çizgiler
/// ortak: makas tek çizgiyi takip ediyor.
/// </para>
/// </remarks>
public class IsimKartiServisi(
    IDavetServisi _davet,
    IWebHostEnvironment _ortam) : IIsimKartiServisi
{
    private const string KurumAdi = "SİVAS BELEDİYESİ";

    static IsimKartiServisi()
    {
        // QuestPDF topluluk lisansı — statik kurucu yalnızca kendi sınıfını
        // kapsıyor, başka PDF sınıfındaki ayar burayı kurtarmaz.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ── Ortak ──────────────────────────────────────────────────────────

    /// <summary>Amblem — yoksa <c>null</c> ve kart logosuz basılır.</summary>
    private byte[]? Amblem()
    {
        try
        {
            var yol = Path.Combine(_ortam.WebRootPath ?? "wwwroot", "amblem.png");
            return File.Exists(yol) ? File.ReadAllBytes(yol) : null;
        }
        catch
        {
            // Logo okunamıyorsa çıktı yine üretilsin: amblemsiz bir kart,
            // hiç basılmayan bir karttan iyidir.
            return null;
        }
    }

    private async Task<(string Baslik, string Alt, List<KartKisisi> Kisiler)> VeriAsync(
        long davetId, DavetDurumu? durum)
    {
        var davet = await _davet.DetayAsync(davetId);

        // Kategori süzgeci YOK: davet zaten seçilmiş bir liste, ikinci bir
        // daraltma "davet ettim ama isimliğini basmadım" durumunu üretirdi.
        var kisiler = davet.Kisiler
            .Where(k => durum is null || k.Durum == durum)
            .OrderBy(k => k.SiraNo)
            .Select(k => new KartKisisi(k.AdSoyad, k.Unvan, k.Kurum))
            .ToList();

        var parcalar = new List<string> { davet.Baslik };
        if (davet.Tarih is not null) parcalar.Add(davet.Tarih.Value.ToString("dd.MM.yyyy"));
        if (!string.IsNullOrWhiteSpace(davet.Yer)) parcalar.Add(davet.Yer!);

        return (davet.Baslik, string.Join(" · ", parcalar), kisiler);
    }

    private static string Slug(string metin)
    {
        var harfler = metin.ToLowerInvariant()
            .Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g')
            .Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c')
            .Select(k => char.IsLetterOrDigit(k) ? k : '-');
        return string.Join(string.Empty, harfler).Trim('-');
    }

    // ── Kesme kartı ────────────────────────────────────────────────────

    public async Task<DisaAktarmaDosyasi> KesmeKartPdfAsync(
        long davetId, DavetDurumu? durum, KesmeKartAyari ayar)
    {
        var (baslik, alt, kisiler) = await VeriAsync(davetId, durum);
        var tasarim = KartTasarimlari.KesmeBul(ayar.Tasarim);
        var amblem = ayar.LogoGoster || ayar.AntetGoster ? Amblem() : null;

        // Sınırlar sunucuda: 5 kolon A4'te okunmaz bir şeride, 20 satır da
        // 1 cm'lik kartlara dönüyor. İstemci de kısıtlıyor ama tek doğru yer
        // burası.
        var sutun = Math.Clamp(ayar.Sutun, 1, 4);
        var satir = Math.Clamp(ayar.Satir, 1, 12);
        var sayfaBasi = sutun * satir;

        /*
          Punto hücre yüksekliğine göre: 10 satırlık ızgarada 20 punto ad
          taşıyor, 4 satırlıkta 12 punto kaybolur. Ölçü A4'ün kullanılabilir
          yüksekliğinden türetiliyor; antet varsa ondan da bir tutam gidiyor.
        */
        var kullanilabilir = ayar.AntetGoster ? 23.5 : 25.0;
        var hucreYuksekligiCm = kullanilabilir / satir;
        var adPunto = (float)Math.Clamp(hucreYuksekligiCm * 5.2, 9, 26);
        var altPunto = (float)Math.Clamp(adPunto * 0.52, 7, 14);
        var olcek = (float)Math.Clamp(hucreYuksekligiCm / 2.5, 0.7, 1.6);

        var belge = Document.Create(kap =>
        {
            for (var sayfaNo = 0; sayfaNo * sayfaBasi < Math.Max(kisiler.Count, 1); sayfaNo++)
            {
                var dilim = kisiler.Skip(sayfaNo * sayfaBasi).Take(sayfaBasi).ToList();

                kap.Page(sayfa =>
                {
                    sayfa.Size(PageSizes.A4);
                    // Kenar boşluğu dar: yazıcıların çoğu 1 cm'i basabiliyor ve
                    // kalan her milimetre karta gidiyor.
                    sayfa.Margin(1, Unit.Centimetre);
                    sayfa.DefaultTextStyle(x => x.FontFamily(KartTasarimlari.Sans));

                    if (ayar.AntetGoster)
                    {
                        // Antet IZGARANIN ÜSTÜNDE: kesildikten sonra atılıyor;
                        // asıl işi baskıyı alan kişinin hangi listeye baktığını
                        // görmesi.
                        sayfa.Header().Element(k =>
                            KartTasarimlari.Antet(k, KurumAdi, alt, amblem));
                    }

                    sayfa.Content().Table(tablo =>
                    {
                        tablo.ColumnsDefinition(s =>
                        {
                            for (var i = 0; i < sutun; i++) s.RelativeColumn();
                        });

                        for (var i = 0; i < sayfaBasi; i++)
                        {
                            var kisi = i < dilim.Count ? dilim[i] : null;

                            var hucre = tablo.Cell()
                                .Height((float)hucreYuksekligiCm, Unit.Centimetre);

                            if (ayar.KesmeCizgisi)
                            {
                                // Bitişik ızgara: her hücre kendi çerçevesini
                                // çiziyor, komşularla çizgi paylaşılıyor.
                                hucre = hucre.Border(0.5f).BorderColor(Colors.Grey.Medium);
                            }

                            KartTasarimlari.Ciz(
                                hucre,
                                kisi is null
                                    ? null
                                    : new KartTasarimlari.Icerik(kisi.AdSoyad, kisi.Unvan, kisi.Kurum),
                                tasarim, adPunto, altPunto,
                                ayar.UnvanGoster, ayar.KurumGoster,
                                ayar.LogoGoster ? amblem : null,
                                olcek,
                                (KartTasarimlari.LogoYeri)ayar.LogoYeri,
                                (KartTasarimlari.LogoBoyu)ayar.LogoBoyu);
                        }
                    });
                });
            }
        });

        return new DisaAktarmaDosyasi(
            belge.GeneratePdf(),
            $"kesme-kartlari-{Slug(baslik)}-{sutun}x{satir}.pdf",
            "application/pdf");
    }

    // ── Masa kartı ─────────────────────────────────────────────────────

    /// <remarks>
    /// <para>
    /// Çadır kart sayfanın ORTASINDAN katlanıyor. Tek kartlı seçimde sayfa
    /// yatay (29,7 × 21 cm) ve katlandığında 29,7 × 10,5 cm'lik iki yüz
    /// oluyor; iki kartlı seçimde sayfa dikey ve ortadan ikiye bölünüyor.
    /// </para>
    /// <para>
    /// Üst yarı <b>180° dönük</b> basılır: kâğıt katlandığında o yüz düz
    /// duruyor ve konuğun kendisine bakıyor, alt yarı salona bakıyor. Dönük
    /// basılmasaydı katlanan yüz baş aşağı çıkardı.
    /// </para>
    /// </remarks>
    public async Task<DisaAktarmaDosyasi> MasaKartPdfAsync(
        long davetId, DavetDurumu? durum, MasaKartAyari ayar)
    {
        var (baslik, alt, kisiler) = await VeriAsync(davetId, durum);
        var tasarim = KartTasarimlari.MasaBul(ayar.Tasarim);
        var amblem = ayar.LogoGoster || ayar.AntetGoster ? Amblem() : null;

        var sayfaBasi = ayar.SayfaBasi >= 2 ? 2 : 1;

        /*
          ÖLÇÜLER SABİT — `Extend()` DEĞİL.

          İlk sürüm yarımları `Extend()` ile büyütüyor ve üst yüzü
          `Rotate(180)` ile çeviriyordu. İkisi de QuestPDF'te "serbest"
          öğeler: çocuğa sınır vermiyorlar. Sonuç, içerik sayfaya sığmayıp
          bir sonrakine taşıyordu — 6 kişilik davet 8 sayfa basıyordu
          (beklenen 6 ve 3).

          Artık her yarım santimetre cinsinden AÇIKÇA ölçülendiriliyor ve
          döndürme `RotateLeft()` çiftiyle yapılıyor; bu ikili düzeni
          koruyor (boyutları takas edip geri alıyor).
        */
        const float kenar = 1f;                       // sayfa kenar boşluğu (cm)
        var yatay = sayfaBasi == 1;
        var icGenislik = (yatay ? 29.7f : 21.0f) - kenar * 2;
        var icYukseklik = (yatay ? 21.0f : 29.7f) - kenar * 2;

        // İki kartlı sayfada aradaki kesme çizgisi için bir tutam pay.
        var ayirici = sayfaBasi == 2 ? 0.6f : 0f;
        // 0.1 cm pay: kart yüksekliği kullanılabilir alana TAM eşit olunca
        // en küçük yuvarlama farkı bile taşmaya dönüşüyor.
        var kartYuksekligi = (icYukseklik - 0.1f - ayirici * (sayfaBasi - 1)) / sayfaBasi;
        var yarimYukseklik = (kartYuksekligi - 0.05f) / 2f;

        // Masa kartı UZAKTAN okunuyor: puntolar kesme kartının iki katı.
        var adPunto = yatay ? 34f : 22f;
        var altPunto = yatay ? 15f : 11f;
        var olcek = yatay ? 2.2f : 1.4f;

        void Yuz(IContainer kutu, KartKisisi? kisi, bool antetli)
        {
            var icerik = kisi is null
                ? null
                : new KartTasarimlari.Icerik(kisi.AdSoyad, kisi.Unvan, kisi.Kurum);

            if (antetli && icerik is not null)
            {
                kutu.Column(ic =>
                {
                    ic.Item().Element(a => KartTasarimlari.Antet(a, KurumAdi, alt, amblem));
                    ic.Item().Extend().Element(g => KartTasarimlari.Ciz(
                        g, icerik, tasarim, adPunto, altPunto,
                        ayar.UnvanGoster, ayar.KurumGoster,
                        ayar.LogoGoster ? amblem : null, olcek,
                        (KartTasarimlari.LogoYeri)ayar.LogoYeri,
                        (KartTasarimlari.LogoBoyu)ayar.LogoBoyu));
                });
                return;
            }

            KartTasarimlari.Ciz(
                kutu, icerik, tasarim, adPunto, altPunto,
                ayar.UnvanGoster, ayar.KurumGoster,
                ayar.LogoGoster ? amblem : null, olcek,
                (KartTasarimlari.LogoYeri)ayar.LogoYeri,
                (KartTasarimlari.LogoBoyu)ayar.LogoBoyu);
        }

        var belge = Document.Create(kap =>
        {
            for (var i = 0; i < Math.Max(kisiler.Count, 1); i += sayfaBasi)
            {
                var dilim = kisiler.Skip(i).Take(sayfaBasi).ToList();

                kap.Page(sayfa =>
                {
                    sayfa.Size(yatay ? PageSizes.A4.Landscape() : PageSizes.A4);
                    sayfa.Margin(kenar, Unit.Centimetre);
                    sayfa.DefaultTextStyle(x => x.FontFamily(KartTasarimlari.Sans));

                    sayfa.Content().Column(sayfaSutunu =>
                    {
                        for (var k = 0; k < sayfaBasi; k++)
                        {
                            var kisi = k < dilim.Count ? dilim[k] : null;

                            sayfaSutunu.Item()
                                .Height(kartYuksekligi, Unit.Centimetre)
                                .Column(kart =>
                            {
                                // ── Üst yarı: katlandığında KONUĞA bakan yüz.
                                //    180° = iki kez sola çevirme; `RotateLeft`
                                //    düzeni koruyor, `Rotate(180)` korumuyordu.
                                kart.Item()
                                    .Height(yarimYukseklik, Unit.Centimetre)
                                    .Element(y =>
                                    {
                                        if (!ayar.CiftYuz) return;
                                        // İki `RotateLeft` = 180° ve düzeni
                                        // koruyor (boyutları takas edip geri
                                        // alıyor). İçeri AYRICA yükseklik
                                        // vermek çakışan kısıt üretiyordu:
                                        // dış kutu zaten `yarimYukseklik`.
                                        y.RotateLeft().RotateLeft()
                                         .Element(ters => Yuz(ters, kisi, false));
                                    });

                                // ── Katlama çizgisi
                                kart.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                                // ── Alt yarı: SALONA bakan yüz
                                kart.Item()
                                    .Height(yarimYukseklik, Unit.Centimetre)
                                    .Element(y => Yuz(y, kisi, ayar.AntetGoster));
                            });

                            // İki kartlı sayfada aradaki KESME çizgisi.
                            if (sayfaBasi == 2 && k == 0)
                            {
                                sayfaSutunu.Item()
                                    .Height(ayirici, Unit.Centimetre)
                                    .AlignMiddle()
                                    .LineHorizontal(0.8f).LineColor(Colors.Grey.Medium);
                            }
                        }
                    });
                });
            }
        });

        return new DisaAktarmaDosyasi(
            belge.GeneratePdf(),
            $"masa-kartlari-{Slug(baslik)}.pdf",
            "application/pdf");
    }
}
