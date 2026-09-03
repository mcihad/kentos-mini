using System.Text.RegularExpressions;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// ÇIKTILARDAKİ KURUM ADI KODA YAZILMAZ.
/// </summary>
/// <remarks>
/// <para>
/// Kurum adı beş PDF üreticisinin <b>dördünde</b> sabit yazılıydı:
/// <c>const string Kurum = "SİVAS BELEDİYESİ"</c>. Uygulama başka
/// belediyelere de veriliyor; kurum ayarlarında adını değiştiren bir müdürlük
/// halk günü listesini, davetiye dökümünü, isim kartını ve çiçek talimatını
/// hâlâ başka bir belediyenin adıyla basıyordu.
/// </para>
/// <para>
/// Bu tür bir hata <b>derlemede görünmez</b>: kod çalışır, PDF üretilir,
/// yalnızca üstündeki ad yanlıştır — ve o adı fark edecek kişi çıktıyı eline
/// alan kurum çalışanıdır. Bekçi, kurum adı gibi kuruma özel bir dizginin
/// çıktı üreticilerine geri sızmasını engelliyor.
/// </para>
/// <para>
/// <b>Yorumlar taranmıyor.</b> Bu kararın gerekçesi kaynak dosyalarda eski
/// sabiti alıntılayarak anlatılıyor; anlatının kendisi ihlal sayılamaz.
/// </para>
/// </remarks>
public class CiktiKurumAdiTests
{
    /// <summary>Çıktı üreten servisler — hepsi kurum kaydından okumalı.</summary>
    private static readonly string[] CiktiServisleri =
    [
        "HalkGunuCiktiServisi.cs",
        "DavetCiktiServisi.cs",
        "IsimKartiServisi.cs",
        "CicekciDetayServisi.cs",
        "DisaAktarmaServisi.cs",
        "GunlukProgramHtml.cs",
        "KartTasarimlari.cs",
    ];

    /// <summary>
    /// Kuruma özel, koda yazılmaması gereken diziler.
    /// </summary>
    /// <remarks>
    /// Liste bilerek DAR: "Belediye" tek başına yasak değil — "Belediye
    /// Başkanlığı" gibi bir birim varsayılanı meşru olabilir. Yasaklanan şey
    /// belirli bir KURUMUN adı.
    /// </remarks>
    private static readonly string[] Yasak = ["SİVAS", "Sivas"];

    private static string KaynakKoku()
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);

        while (dizin is not null)
        {
            var aday = Path.Combine(dizin.FullName, "KentOS.Kalem.Web", "Services", "V2");
            if (Directory.Exists(aday)) return aday;
            dizin = dizin.Parent;
        }

        throw new DirectoryNotFoundException("Services/V2 dizini bulunamadı.");
    }

    [Fact]
    public void Cikti_servislerinde_kuruma_ozel_ad_YOK()
    {
        var kok = KaynakKoku();
        var ihlaller = new List<string>();

        foreach (var dosya in CiktiServisleri)
        {
            var yol = Path.Combine(kok, dosya);
            Assert.True(File.Exists(yol), $"Çıktı servisi bulunamadı: {dosya}");

            // Yorumlar bu kararı ANLATIYOR; aranan şey çalışan koddaki sabit.
            var kod = Regex.Replace(File.ReadAllText(yol), @"/\*.*?\*/", "", RegexOptions.Singleline);
            kod = Regex.Replace(kod, @"^\s*(//|///).*$", "", RegexOptions.Multiline);

            foreach (var dizgi in Yasak)
            {
                if (kod.Contains(dizgi, StringComparison.Ordinal))
                    ihlaller.Add($"{dosya} → \"{dizgi}\"");
            }
        }

        Assert.True(ihlaller.Count == 0,
            "Çıktı üreticisinde kuruma özel ad KODA YAZILMIŞ. Kurum adı "
            + "`IInstitutionService.CiktiKimligiAsync()` üzerinden okunur:\n  "
            + string.Join("\n  ", ihlaller));
    }
}
