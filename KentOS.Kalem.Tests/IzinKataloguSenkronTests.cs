using System.Text.RegularExpressions;
using KentOS.Kalem.Application.Identity;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// İzin adları ÜÇ YERDE aynı olmak zorunda: sunucu, web ve mobil.
/// </summary>
/// <remarks>
/// <para>
/// Bir harf farkı sessizce kaybolur: istemcide <c>iznimVar('ajanda.sıl')</c>
/// hep <c>false</c> döner, menü hiç görünmez ve kimse fark etmez — yetki
/// verilmiş sanılır ama çalışmaz.
/// </para>
/// <para>
/// Bu test istemci dosyalarını <b>okuyarak</b> karşılaştırır. Kod üretimi
/// (codegen) yerine denetim seçildi: üretilen dosyayı kimse okumuyor,
/// düşen bir test ise fark ettiriyor.
/// </para>
/// </remarks>
public class IzinKataloguSenkronTests
{
    private static string DepoKoku()
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);
        while (dizin is not null && !File.Exists(Path.Combine(dizin.FullName, "KentOS.Kalem.sln")))
        {
            dizin = dizin.Parent;
        }
        return dizin?.FullName ?? throw new InvalidOperationException("Çözüm kökü bulunamadı");
    }

    private static string[] DosyadanAdlar(string yol, string desen)
    {
        if (!File.Exists(yol))
        {
            throw new FileNotFoundException($"İstemci izin dosyası yok: {yol}");
        }

        return [.. Regex.Matches(File.ReadAllText(yol), desen)
            .Select(m => m.Groups["ad"].Value)
            .Distinct()];
    }

    [Fact]
    public void WEB_izin_sabitleri_sunucuyla_ayni()
    {
        // NOT: bu yol ÖN YÜZE uzanıyor. SPA'nın dizinleri İngilizceye
        // çevrildiğinde (`bilesenler/izin.ts` -> `components/permissions.ts`)
        // burası bayatladı ve sunucu testi düştü — ön yüzdeki hiçbir
        // doğrulama (tsc, vitest, görsel tur) bunu göremezdi.
        // Ders: SPA'da dosya taşıdıktan sonra `dotnet test` de koşulur.
        var yol = Path.Combine(DepoKoku(),
            "KentOS.Kalem.Web", "frontend", "src", "components", "permissions.ts");

        var web = DosyadanAdlar(yol, @"'(?<ad>[a-z]+\.[a-zA-Z]+)'");

        Assert.Equal(
            Izinler.Adlar.OrderBy(x => x, StringComparer.Ordinal),
            web.OrderBy(x => x, StringComparer.Ordinal));
    }

    // ── MOBİL AYNA TESTİ KALDIRILDI ────────────────────────────────────
    //
    // Test, kardeş bir dizindeki Flutter deposunu (`../workcollab/lib/consts/
    // izinler.dart`) SABİT YOLLA okuyordu. KentOS.Kalem ayrı bir depo olarak
    // ayrıldığında o yol artık çözülmüyor: test atlanmaya başladı ve
    // "atlandı" ile "geçti" arasındaki fark koşum çıktısında kayboldu —
    // bekçi kapanmıştı ama kimse fark etmiyordu.
    //
    // Mobil uygulama artık bu ürünün parçası değil; kendi deposunda yaşıyor
    // ve yalnızca DOKUNULMAYAN v1 API sözleşmesine bağlı. İzin kataloğu ise
    // v2 yüzeyine ait — mobilin okuduğu bir şey değil.
    //
    // Depo DIŞINDAKİ bir dosyaya uzanan test, klonlayan kişide ya yanlış
    // kırmızı ya da yanlış yeşil üretir; ikisi de bekçisizlikten kötüdür.

    [Fact]
    public void Her_izin_EN_AZ_BIR_ucta_kullaniliyor()
    {
        // Hiçbir uçta denetlenmeyen izin, yöneticinin verdiğini sandığı ama
        // hiçbir şey yapmayan bir onay kutusudur.
        var v2 = Path.Combine(DepoKoku(), "KentOS.Kalem.Web", "Controllers", "V2");
        var kaynak = string.Join('\n',
            Directory.GetFiles(v2, "*.cs").Select(File.ReadAllText));

        var kullanilan = Regex.Matches(kaynak, @"\[Izin\((?<liste>[^\)]*)\)\]")
            .SelectMany(m => Regex.Matches(m.Groups["liste"].Value, @"Izinler\.(?<sabit>\w+)"))
            .Select(m => m.Groups["sabit"].Value)
            .ToHashSet();

        var sabitler = typeof(Izinler)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.Name)
            .ToList();

        // Şu an tek tek uç işaretlenmemiş izinler (sınıf düzeyi izinle
        // korunuyorlar ya da henüz o ekran v2'ye taşınmadı). Liste
        // KISALMALI; uzarsa yeni bir izin hiçbir yerde denetlenmiyor demektir.
        var beklenenBoslar = new HashSet<string>
        {
            // Bunların KENDİ ucu yok; etkinlik gövdesinin alanı ya da başka
            // bir kuralın parçası olarak SERVİS katmanında denetleniyorlar.
            nameof(Izinler.AjandaGizliEtkinlik),
            nameof(Izinler.AjandaKatilimciEkle),

            // Basın daraltması bir KAPI değil SÜZGEÇ: uca konsaydı basın
            // kullanıcısı ajandayı hiç açamazdı. Denetim
            // `AjandaSorguUzantilari.GorunurOlanlar` içinde ve oradaki
            // parametrenin varsayılanı yok — derleyici her sorguyu zorluyor.
            nameof(Izinler.AjandaBasinGoruntule),

            // Dosya gönderme yetkisi servis katmanında denetleniyor
            // (`GonderimController.GonderebilenKullaniciAsync`).
            nameof(Izinler.GonderimGonder),

            // Bu ekranlar henüz tek izinle korunuyor; ayrıştırılması ayrı iş.
            nameof(Izinler.OneriYanitla),
            nameof(Izinler.TalepCiktiAl),
            nameof(Izinler.DavetCiktiAl),
        };

        var eksikler = sabitler
            .Where(s => !kullanilan.Contains(s) && !beklenenBoslar.Contains(s))
            .ToList();

        Assert.True(eksikler.Count == 0,
            $"Hiçbir uçta denetlenmeyen izin(ler): {string.Join(", ", eksikler)}");
    }
}
