using System.Globalization;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>
/// İsim kartlarının tipografi ve yerleşim katalogu.
/// </summary>
/// <remarks>
/// <para>
/// Kart iki türde basılıyor: <b>kesme kartı</b> A4'e ızgara hâlinde dizilip
/// makasla kesiliyor ve sandalyeye yapıştırılıyor; <b>masa kartı</b> ise
/// ortadan katlanıp masaya konuyor (çadır kart). İkisi de aynı tasarım
/// dilinden besleniyor — tören boyunca sandalyedeki etiketle masadaki isimlik
/// aynı görünsün diye.
/// </para>
/// <para>
/// <b>Fontlar depoda.</b> Sistem fontuna adıyla güvenmek riskliydi:
/// geliştirme makinesinde duran bir aile yayın sunucusunda olmayınca PDF
/// sessizce başka bir fonta düşüyor ve kart bambaşka görünüyordu. Beş aile
/// <c>Fontlar/</c> altında ve açılışta kaydediliyor.
/// </para>
/// </remarks>
public static class KartTasarimlari
{
    /// <summary>
    /// TÜRKÇE BÜYÜK HARF.
    /// </summary>
    /// <remarks>
    /// <c>ToUpperInvariant()</c> Türkçede YANLIŞ sonuç veriyor: "Vali
    /// Yardımcısı" → <b>"VALI YARDıMCıSı"</b>. Değişmez kültür noktasız
    /// <c>ı</c>'yı hiç büyütmüyor ve <c>i</c>'yi <c>İ</c> yerine <c>I</c>
    /// yapıyor. Kartın üstündeki en büyük yazı buydu ve baskıda hemen göze
    /// çarpıyordu. <c>tr-TR</c> ile: "VALİ YARDIMCISI".
    /// </remarks>
    private static readonly CultureInfo Turkce = new("tr-TR");

    public static string BuyukHarfe(string metin) => metin.ToUpper(Turkce);

    public const string Lacivert = "#002E6D";
    public const string Altin = "#A78952";
    public const string Gri = "#4D4D4F";

    // ── Font aileleri ──────────────────────────────────────────────────

    public const string Sans = "Fira Sans";
    public const string SansDar = "Fira Sans Condensed";
    public const string Serif = "PT Serif";
    public const string SerifZarif = "Crimson Text";
    public const string SansKlasik = "PT Sans";

    private static bool _kayitli;
    private static readonly Lock _kilit = new();

    /// <summary>
    /// Font dosyalarını QuestPDF'e kaydeder. Açılışta bir kez çağrılır.
    /// </summary>
    /// <remarks>
    /// Kayıt YAPILAMAZSA çıktı yine üretilir — QuestPDF varsayılan aileye
    /// düşer. Font eksikliği yüzünden PDF ucunun 500 vermesi, kartın biraz
    /// farklı görünmesinden çok daha kötü.
    /// </remarks>
    public static void FontlariKaydet(string kokDizin)
    {
        lock (_kilit)
        {
            if (_kayitli) return;
            _kayitli = true;

            var dizin = Path.Combine(kokDizin, "Fontlar");
            if (!Directory.Exists(dizin)) return;

            foreach (var dosya in Directory.GetFiles(dizin, "*.ttf"))
            {
                try
                {
                    using var akis = File.OpenRead(dosya);
                    FontManager.RegisterFont(akis);
                }
                catch
                {
                    /* Bozuk ya da okunamayan font çıktıyı düşürmesin. */
                }
            }
        }
    }

    // ── Tasarımlar ─────────────────────────────────────────────────────

    /// <summary>Bir kart tasarımının kimliği ve tipografisi.</summary>
    /// <param name="Anahtar">İstemciyle paylaşılan sabit ad.</param>
    /// <param name="Ad">Kullanıcıya görünen ad.</param>
    /// <param name="Font">Ad için kullanılacak aile.</param>
    /// <param name="AltFont">Unvan/kurum için aile.</param>
    /// <param name="BuyukHarf">Ad büyük harfe çevrilsin mi.</param>
    /// <param name="HarfAraligi">Ad harf aralığı (em cinsinden).</param>
    /// <param name="Cerceve">Kart çerçevesinin biçimi.</param>
    /// <param name="Vurgu">Vurgu rengi — şerit, çizgi ve süsleme.</param>
    public record Tasarim(
        [property: JsonPropertyName("anahtar")] string Anahtar,
        [property: JsonPropertyName("ad")] string Ad,
        [property: JsonPropertyName("font")] string Font,
        [property: JsonPropertyName("altFont")] string AltFont,
        [property: JsonPropertyName("buyukHarf")] bool BuyukHarf,
        [property: JsonPropertyName("harfAraligi")] float HarfAraligi,
        [property: JsonPropertyName("cerceve")] CerceveBicimi Cerceve,
        [property: JsonPropertyName("vurgu")] string Vurgu);

    public enum CerceveBicimi
    {
        /// <summary>Süsleme yok — yalnızca ad ve unvan.</summary>
        Yok,
        /// <summary>Adın altında ince çizgi.</summary>
        AltCizgi,
        /// <summary>Üstte ve altta çift çizgi.</summary>
        CiftCizgi,
        /// <summary>Sol kenarda dolu şerit.</summary>
        SolSerit,
        /// <summary>Üst kenarda dolu şerit.</summary>
        UstSerit,
        /// <summary>Kartın içinde saç teli çerçeve.</summary>
        IcCerceve,
        /// <summary>Dört köşede kısa açı işaretleri.</summary>
        KoseIsareti,
    }

    /// <summary>
    /// KESME KARTI tasarımları — sandalyeye yapıştırılan düz etiketler.
    /// </summary>
    /// <remarks>
    /// On tasarım, üç aileden: sade/modern (Fira), kurumsal (PT Sans) ve
    /// klasik/zarif (PT Serif, Crimson). Tören resmiyetine göre seçiliyor;
    /// tek bir "standart" kart açılış törenine de anma programına da
    /// uymuyordu.
    /// </remarks>
    public static readonly Tasarim[] Kesme =
    [
        new("sade",       "Sade",            Sans,       Sans,        false, 0f,     CerceveBicimi.Yok,         Lacivert),
        new("altcizgi",   "Alt çizgili",     Sans,       Sans,        false, 0f,     CerceveBicimi.AltCizgi,    Altin),
        new("kurumsal",   "Kurumsal şerit",  SansKlasik, SansKlasik,  false, 0f,     CerceveBicimi.SolSerit,    Lacivert),
        new("ustserit",   "Üst şerit",       SansKlasik, SansKlasik,  false, 0f,     CerceveBicimi.UstSerit,    Lacivert),
        new("modern",     "Modern",          Sans,       Sans,        true,  0.14f,  CerceveBicimi.Yok,         Lacivert),
        new("dar",        "Dar (uzun ad)",   SansDar,    SansDar,     true,  0.08f,  CerceveBicimi.AltCizgi,    Lacivert),
        new("klasik",     "Klasik",          Serif,      Serif,       false, 0f,     CerceveBicimi.CiftCizgi,   Lacivert),
        new("zarif",      "Zarif",           SerifZarif, SerifZarif,  false, 0.03f,  CerceveBicimi.IcCerceve,   Altin),
        new("resmi",      "Resmî",           Serif,      SansKlasik,  true,  0.1f,   CerceveBicimi.CiftCizgi,   Altin),
        new("kose",       "Köşe işaretli",   SerifZarif, Sans,        false, 0f,     CerceveBicimi.KoseIsareti, Altin),
    ];

    /// <summary>
    /// MASA KARTI tasarımları — ortadan katlanıp masaya konan çadır kartlar.
    /// </summary>
    /// <remarks>
    /// Masa kartı uzaktan okunuyor: puntolar kesme kartının iki katı ve
    /// süslemeler daha belirgin. Aynı adları taşıyorlar ki tören ekibi
    /// "zarif" derken ikisinde de aynı şeyi kastetsin.
    /// </remarks>
    public static readonly Tasarim[] Masa =
    [
        new("sade",       "Sade",            Sans,       Sans,        false, 0f,     CerceveBicimi.Yok,         Lacivert),
        new("altcizgi",   "Alt çizgili",     Sans,       Sans,        false, 0f,     CerceveBicimi.AltCizgi,    Altin),
        new("kurumsal",   "Kurumsal şerit",  SansKlasik, SansKlasik,  false, 0f,     CerceveBicimi.SolSerit,    Lacivert),
        new("ustserit",   "Üst şerit",       SansKlasik, SansKlasik,  false, 0f,     CerceveBicimi.UstSerit,    Lacivert),
        new("modern",     "Modern",          Sans,       Sans,        true,  0.16f,  CerceveBicimi.Yok,         Lacivert),
        new("dar",        "Dar (uzun ad)",   SansDar,    SansDar,     true,  0.1f,   CerceveBicimi.AltCizgi,    Lacivert),
        new("klasik",     "Klasik",          Serif,      Serif,       false, 0f,     CerceveBicimi.CiftCizgi,   Lacivert),
        new("zarif",      "Zarif",           SerifZarif, SerifZarif,  false, 0.04f,  CerceveBicimi.IcCerceve,   Altin),
        new("resmi",      "Resmî",           Serif,      SansKlasik,  true,  0.12f,  CerceveBicimi.CiftCizgi,   Altin),
        new("kose",       "Köşe işaretli",   SerifZarif, Sans,        false, 0f,     CerceveBicimi.KoseIsareti, Altin),
    ];

    public static Tasarim KesmeBul(string? anahtar) =>
        Kesme.FirstOrDefault(t => t.Anahtar == anahtar) ?? Kesme[0];

    public static Tasarim MasaBul(string? anahtar) =>
        Masa.FirstOrDefault(t => t.Anahtar == anahtar) ?? Masa[0];

    // ── Çizim ──────────────────────────────────────────────────────────

    /// <summary>Kartın içeriği — kaynağı ne olursa olsun aynı üç alan.</summary>
    public record Icerik(
        [property: JsonPropertyName("adSoyad")] string AdSoyad,
        [property: JsonPropertyName("unvan")] string? Unvan,
        [property: JsonPropertyName("kurum")] string? Kurum);

    /// <summary>Amblemin karttaki yeri.</summary>
    /// <remarks>
    /// İlk sürümde tek seçenek vardı (adın üstünde) ve amblem 9 punto, yani
    /// üç milimetreydi — "logo var" demekten öteye geçmiyordu. Artık hem yeri
    /// hem boyu seçiliyor: geniş kartta sol yan, dar kartta üst daha iyi
    /// oturuyor.
    /// </remarks>
    public enum LogoYeri
    {
        Yok = 0,
        Ust = 1,
        Sol = 2,
        Sag = 3,
    }

    /// <summary>Amblem boyu — ad puntosuna oranla.</summary>
    public enum LogoBoyu
    {
        Kucuk = 0,
        Orta = 1,
        Buyuk = 2,
    }

    /// <summary>
    /// Tek bir kartı çizer: çerçeve + logo + ad + unvan + kurum.
    /// </summary>
    /// <param name="olcek">
    /// Punto çarpanı. Kesme kartı hücre yüksekliğine göre, masa kartı sabit
    /// büyük ölçekle çağırıyor.
    /// </param>
    public static void Ciz(
        IContainer kap,
        Icerik? icerik,
        Tasarim t,
        float adPunto,
        float altPunto,
        bool unvanGoster,
        bool kurumGoster,
        byte[]? logo,
        float olcek = 1f,
        LogoYeri logoYeri = LogoYeri.Ust,
        LogoBoyu logoBoyu = LogoBoyu.Orta)
    {
        // Çerçeve süslemeleri ARKA PLANDA: metin katmanı üstte kalmalı ki
        // uzun bir ad şeridin altına girmesin.
        var govde = t.Cerceve switch
        {
            CerceveBicimi.SolSerit => kap.BorderLeft(3.5f * olcek).BorderColor(t.Vurgu),
            CerceveBicimi.UstSerit => kap.BorderTop(3.5f * olcek).BorderColor(t.Vurgu),
            CerceveBicimi.IcCerceve => kap.Padding(3 * olcek).Border(0.7f).BorderColor(t.Vurgu),
            _ => kap,
        };

        // Sol/sağ yerleşimde amblem metnin YANINDA duruyor; bunun için
        // gövde bir satıra sarılıyor ve metin sütunu kalan yeri alıyor.
        var yanLogo = logo is not null && logoYeri is LogoYeri.Sol or LogoYeri.Sag;

        var ic = govde.Padding(5 * olcek).AlignMiddle();

        if (yanLogo)
        {
            ic.Row(satir =>
            {
                var yanBoy = adPunto * logoBoyu switch
                {
                    LogoBoyu.Kucuk => 1.1f,
                    LogoBoyu.Buyuk => 2.6f,
                    _ => 1.7f,
                };

                if (logoYeri == LogoYeri.Sol)
                {
                    satir.ConstantItem(yanBoy).AlignMiddle().Image(logo!).FitArea();
                    satir.ConstantItem(6 * olcek);
                }

                satir.RelativeItem().AlignMiddle()
                    .Element(m => MetinSutunu(m, icerik, t, adPunto, altPunto,
                                              unvanGoster, kurumGoster, olcek));

                if (logoYeri == LogoYeri.Sag)
                {
                    satir.ConstantItem(6 * olcek);
                    satir.ConstantItem(yanBoy).AlignMiddle().Image(logo!).FitArea();
                }
            });
            return;
        }

        ic.AlignCenter().Column(k =>
        {
            if (icerik is null) return;

            if (t.Cerceve == CerceveBicimi.CiftCizgi)
            {
                k.Item().PaddingBottom(3 * olcek).LineHorizontal(1.2f * olcek).LineColor(t.Vurgu);
            }

            // Amblem boyu AD PUNTOSUNA oranlı: kart büyüdükçe logo da
            // büyüyor. Sabit punto vermek, 34 puntoluk masa kartında
            // amblemi noktaya çeviriyordu.
            var logoYuksekligi = adPunto * logoBoyu switch
            {
                LogoBoyu.Kucuk => 0.7f,
                LogoBoyu.Buyuk => 1.8f,
                _ => 1.15f,
            };

            if (logo is not null && logoYeri == LogoYeri.Ust)
            {
                k.Item().AlignCenter().PaddingBottom(3 * olcek)
                    .Height(logoYuksekligi).Image(logo).FitHeight();
            }

            var ad = t.BuyukHarf ? BuyukHarfe(icerik.AdSoyad) : icerik.AdSoyad;

            k.Item().AlignCenter().Text(ad)
                .FontFamily(t.Font)
                .FontSize(adPunto)
                .Bold()
                .LetterSpacing(t.HarfAraligi)
                .FontColor(Lacivert);

            if (t.Cerceve == CerceveBicimi.AltCizgi)
            {
                // Çizgi adın ALTINDA ve dar: tam genişlik çizgi kartı ikiye
                // bölüyor, kısa çizgi adı imzalıyor.
                k.Item().PaddingVertical(2.5f * olcek).AlignCenter()
                    .Width(28 * olcek).LineHorizontal(1f * olcek).LineColor(t.Vurgu);
            }

            if (unvanGoster && !string.IsNullOrWhiteSpace(icerik.Unvan))
            {
                k.Item().PaddingTop(t.Cerceve == CerceveBicimi.AltCizgi ? 0 : 2 * olcek)
                    .AlignCenter().Text(icerik.Unvan)
                    .FontFamily(t.AltFont).FontSize(altPunto).FontColor(Gri);
            }

            if (kurumGoster && !string.IsNullOrWhiteSpace(icerik.Kurum))
            {
                k.Item().PaddingTop(1 * olcek).AlignCenter().Text(icerik.Kurum)
                    .FontFamily(t.AltFont).FontSize(altPunto * 0.85f)
                    .FontColor(Colors.Grey.Darken1);
            }

            if (t.Cerceve == CerceveBicimi.CiftCizgi)
            {
                k.Item().PaddingTop(3 * olcek).LineHorizontal(1.2f * olcek).LineColor(t.Vurgu);
            }
        });
    }

    /// <summary>
    /// Ad + unvan + kurum sütunu — süslemesiz.
    /// </summary>
    /// <remarks>
    /// Yan logolu yerleşimde çerçeve süslemeleri (çift çizgi, alt çizgi) satırın
    /// dışında kalıyor: amblemin yanındaki metnin altına çizgi çekmek kartı
    /// ikiye bölüyordu.
    /// </remarks>
    private static void MetinSutunu(
        IContainer kap, Icerik? icerik, Tasarim t,
        float adPunto, float altPunto, bool unvanGoster, bool kurumGoster, float olcek)
    {
        kap.Column(k =>
        {
            if (icerik is null) return;

            var ad = t.BuyukHarf ? BuyukHarfe(icerik.AdSoyad) : icerik.AdSoyad;

            k.Item().Text(ad)
                .FontFamily(t.Font).FontSize(adPunto).Bold()
                .LetterSpacing(t.HarfAraligi).FontColor(Lacivert);

            if (unvanGoster && !string.IsNullOrWhiteSpace(icerik.Unvan))
            {
                k.Item().PaddingTop(2 * olcek).Text(icerik.Unvan)
                    .FontFamily(t.AltFont).FontSize(altPunto).FontColor(Gri);
            }

            if (kurumGoster && !string.IsNullOrWhiteSpace(icerik.Kurum))
            {
                k.Item().PaddingTop(1 * olcek).Text(icerik.Kurum)
                    .FontFamily(t.AltFont).FontSize(altPunto * 0.85f)
                    .FontColor(Colors.Grey.Darken1);
            }
        });
    }

    /// <summary>
    /// ANTET — kâğıdın üstündeki kurum şeridi.
    /// </summary>
    /// <remarks>
    /// Yalnızca istendiğinde basılır. Kesme kartlarında antet <b>ızgaranın
    /// üstünde</b> duruyor ve kesildikten sonra atılıyor; asıl işi baskıyı
    /// alan kişinin hangi listeye baktığını görmesi. Masa kartında ise antet
    /// kartın kendi içinde, çünkü orada kesilecek bir şey yok.
    /// </remarks>
    public static void Antet(IContainer kap, string kurumAdi, string? altBaslik, byte[]? logo)
    {
        kap.PaddingBottom(6).Column(k =>
        {
            k.Item().Row(satir =>
            {
                if (logo is not null)
                {
                    // Antet amblemi 26 punto (≈9 mm) idi ve kartın kendi
                    // amblemi yanında minicik kalıyordu; aynı kâğıtta iki
                    // farklı boyda logo görmek özensiz duruyordu.
                    satir.ConstantItem(40).Height(40).Image(logo).FitArea();
                    satir.ConstantItem(10);
                }

                satir.RelativeItem().AlignMiddle().Column(ic =>
                {
                    ic.Item().Text(BuyukHarfe(kurumAdi))
                        .FontFamily(SansKlasik).FontSize(13).Bold()
                        .LetterSpacing(0.1f).FontColor(Lacivert);

                    if (!string.IsNullOrWhiteSpace(altBaslik))
                    {
                        ic.Item().Text(altBaslik)
                            .FontFamily(Sans).FontSize(9).FontColor(Gri);
                    }
                });
            });

            k.Item().PaddingTop(4).LineHorizontal(1).LineColor(Altin);
        });
    }
}
