using System.Text.RegularExpressions;
using KentOS.Mini.Application.Dto;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// BİLDİRİM YOLU — sunucunun gönderdiği her varlığın SPA'da karşılığı var mı?
/// </summary>
/// <remarks>
/// <para>
/// Bildirim yönlendirmesi <b>dört ayrı yerde</b> yazılı ve dördü de aynı
/// eşlemeyi tekrar ediyor:
/// </para>
/// <list type="number">
///   <item><c>fcm.ts</c> — uygulama açıkken gelen bildirim</item>
///   <item><c>NotificationCenter.tsx</c> — bildirim merkezi listesi</item>
///   <item><c>public/firebase-messaging-sw.js</c> — arka plandaki tıklama</item>
///   <item><c>NotificationBridge.tsx</c> — bayatlayan sorguların düşürülmesi
///         (yol değil, önbellek; bu yüzden bekçinin dışında)</item>
/// </list>
///
/// <para>
/// <b>Bu testin varlık sebebi ölçülmüş bir arıza.</b> İş takip modülü
/// <c>Gorev</c> varlığıyla bildirim gönderiyordu ama dört yerin HİÇBİRİNDE
/// karşılığı yoktu; üstelik üç yerde <c>action === 'None'</c> koşulu varlığa
/// bakmadan yolu kapatıyordu. Sonuç: modülün bütün bildirimleri tıklanınca
/// hiçbir şey yapmıyordu ve bunu söyleyen tek bir hata bile yoktu — sessiz
/// bir kayıp.
/// </para>
///
/// <para>
/// <b>Neden metin taraması?</b> Eşleme dört farklı dilde/dosyada duruyor
/// (biri Vite'ın hiç işlemediği ham bir service worker). Onları çalıştırıp
/// denetleyecek ortak bir yer yok; en ucuz güvenilir bekçi, varlık adının
/// dosyada geçtiğini doğrulamak. Yolun DOĞRU olduğunu değil, VAR olduğunu
/// kilitliyor — asıl kaçırılan buydu.
/// </para>
/// </remarks>
public class BildirimYoluTests
{
    /// <summary>
    /// Tıklamanın NEREYE gideceğine karar veren üç dosya.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>NotificationBridge.tsx</c> bilerek DIŞARIDA. O dosya yolu değil,
    /// bayatlayan React Query anahtarlarını düşürüyor; bir varlığın orada
    /// karşılığı olmaması bildirimin çalışmadığı anlamına gelmiyor — yalnızca
    /// açık duran bir listenin bir sonraki odaklanmada tazeleneceği anlamına
    /// geliyor. Nitekim <c>Oneri</c> modülünün SPA'da hiç önbelleği yok:
    /// ekranı yalnızca mobilde var. Onu bu listeye koymak, testi "eklenecek
    /// bir şey yok" diye kırık tutmak olurdu.
    /// </para>
    /// </remarks>
    private static readonly string[] Dosyalar =
    [
        "src/notifications/fcm.ts",
        "src/notifications/NotificationCenter.tsx",
        "public/firebase-messaging-sw.js",
    ];

    private static string OnYuzKoku()
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);

        while (dizin is not null)
        {
            var aday = Path.Combine(dizin.FullName, "KentOS.Mini.Web", "frontend");
            if (Directory.Exists(aday)) return aday;
            dizin = dizin.Parent;
        }

        throw new DirectoryNotFoundException("frontend dizini bulunamadı.");
    }

    [Fact]
    public void Her_bildirim_varligi_UC_yonlendirme_yerinde_de_taniniyor()
    {
        var kok = OnYuzKoku();

        var eksikler = new List<string>();

        foreach (var varlik in Enum.GetNames<NotificationEntity>())
        {
            // SPA eşlemeleri küçük harfle karşılaştırıyor (`toLowerCase()`).
            var aranan = varlik.ToLowerInvariant();

            foreach (var dosya in Dosyalar)
            {
                var yol = Path.Combine(kok, dosya);
                Assert.True(File.Exists(yol), $"Yönlendirme dosyası yok: {dosya}");

                if (!File.ReadAllText(yol).Contains($"'{aranan}'", StringComparison.Ordinal))
                    eksikler.Add($"{varlik} → {dosya}");
            }
        }

        Assert.True(eksikler.Count == 0,
            "Şu varlıkların SPA yönlendirmesi eksik — bildirim tıklanınca hiçbir yere " +
            "gitmez ve hata da vermez:\n  " + string.Join("\n  ", eksikler));
    }

    /// <summary>
    /// <c>action === 'None'</c> varlığa bakmadan yolu kapatmamalı.
    /// </summary>
    /// <remarks>
    /// Sunucu YENİ varlıkları bilerek <c>None</c> ile gönderiyor: yayındaki
    /// eski mobil sürümler onları tanımıyor ve <c>fromString</c>'in
    /// <c>orElse</c>'i yüzünden sessizce <c>talep</c>'e düşürüp var olmayan
    /// bir talebi açıyorlar. <c>None</c> MOBİLE "hiçbir yere gitme" demek;
    /// web'e "detayı açma" demek değil.
    ///
    /// Üç dosyada da ilk satır <c>None</c> gelen bildirimi varlığına bakmadan
    /// eliyordu. Kural geri konulursa iş takip bildirimleri yeniden ölür ve
    /// bunu söyleyen başka bir şey olmaz.
    /// </remarks>
    [Theory]
    [InlineData("src/notifications/fcm.ts")]
    [InlineData("src/notifications/NotificationCenter.tsx")]
    [InlineData("public/firebase-messaging-sw.js")]
    public void None_eylemi_yolu_kapatmiyor(string dosya)
    {
        var metin = File.ReadAllText(Path.Combine(OnYuzKoku(), dosya));

        // Yorum satırları bu kararı ANLATIYOR; aranan şey çalışan koddaki
        // erken çıkış. Yorumları eleyip öyle bakılıyor.
        var kod = Regex.Replace(metin, @"/\*.*?\*/", "", RegexOptions.Singleline);
        kod = Regex.Replace(kod, @"^\s*(//|\*).*$", "", RegexOptions.Multiline);

        Assert.DoesNotContain("action === 'None'", kod);
        Assert.DoesNotContain("action !== 'None'", kod);
        Assert.DoesNotContain("eylem === 'None'", kod);
    }
}
