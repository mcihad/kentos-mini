using Microsoft.Extensions.Configuration;
using KentOS.Kalem.Web.Configuration;
using KentOS.Kalem.Web.Options;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// YAPILANDIRMA SÖZLEŞMESİ.
///
/// <para>
/// Uygulama başka belediyelere verilecek ve tek kurulum adımı <c>.env</c>
/// dosyasını doldurmak olacak. Bu testler o vaadin ayakta kaldığını
/// denetliyor: anahtar adlarının <c>.env.example</c> ile birebir eşleştiğini,
/// eski anahtarların hâlâ okunduğunu ve hiçbir varsayılanın kuruma özel bilgi
/// taşımadığını.
/// </para>
/// </summary>
public class YapilandirmaTests
{
    private static IConfiguration Kur(Dictionary<string, string?> degerler) =>
        new ConfigurationBuilder().AddInMemoryCollection(degerler).Build();

    // ─────────────────────────────────────────────── `__` → `:` çevrimi

    /// <summary>
    /// <c>.env</c> satırları ortam değişkenine yüklenip .NET'in kendi
    /// sağlayıcısıyla okunuyor; ekstra eşleme kodu YOK. Bu test o zincirin
    /// beklendiği gibi çalıştığını gösterir: iki alt çizgi bölüm ayırıcıdır ve
    /// İÇ İÇE bölümlerde de geçerlidir.
    /// </summary>
    [Fact]
    public void Cift_alt_cizgi_ic_ice_bolume_cevrilir()
    {
        var yapilandirma = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Add(new Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationSource())
            .Build();

        // Süreç ortamına yaz — `.env` yüklemesinin yaptığı şeyin aynısı.
        Environment.SetEnvironmentVariable("Storage__S3__Endpoint", "ornek:9000");
        Environment.SetEnvironmentVariable("Storage__Provider", "S3");

        try
        {
            var taze = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            var ayar = OptionsRegistration.Read<StorageOptions>(taze, StorageOptions.SectionName);

            Assert.Equal(StorageProvider.S3, ayar.Provider);
            Assert.Equal("ornek:9000", ayar.S3.Endpoint);
        }
        finally
        {
            Environment.SetEnvironmentVariable("Storage__S3__Endpoint", null);
            Environment.SetEnvironmentVariable("Storage__Provider", null);
            _ = yapilandirma;
        }
    }

    // ─────────────────────────────────────────────── eski anahtarlar

    /// <summary>
    /// Yayındaki <c>appsettings.json</c> dosyaları bu sürümle güncellenmeyecek.
    /// Eski anahtarlar yeni karşılıkları boşken okunmaya devam etmeli — aksi
    /// hâlde yükseltme, çalışan bir kurulumu sessizce bozar.
    /// </summary>
    [Fact]
    public void Eski_anahtarlar_yeni_karsiliklarina_tasinir()
    {
        var yapilandirma = Kur(new()
        {
            ["URL"] = "https://eski.ornek.test",
            ["Depolama:GonderimDizini"] = "/veri/gonderim",
            ["Randevu:HalkGunuTipId"] = "7",
        });

        OptionsRegistration.ApplyLegacyKeys(yapilandirma);

        Assert.Equal("https://eski.ornek.test",
            OptionsRegistration.Read<ApplicationOptions>(yapilandirma, ApplicationOptions.SectionName).BaseUrl);
        Assert.Equal("/veri/gonderim",
            OptionsRegistration.Read<StorageOptions>(yapilandirma, StorageOptions.SectionName).SendDirectory);
        Assert.Equal(7,
            OptionsRegistration.Read<RequestOptions>(yapilandirma, RequestOptions.SectionName).PublicDayTypeId);
    }

    /// <summary>
    /// Yeni anahtar verilmişse eski anahtar onu EZMEMELİ; yoksa yeni biçime
    /// geçen bir kurulumda eski değer geri gelirdi.
    /// </summary>
    [Fact]
    public void Yeni_anahtar_varsa_eskisi_ezmez()
    {
        var yapilandirma = Kur(new()
        {
            ["URL"] = "https://eski.ornek.test",
            ["App:BaseUrl"] = "https://yeni.ornek.test",
        });

        OptionsRegistration.ApplyLegacyKeys(yapilandirma);

        Assert.Equal("https://yeni.ornek.test",
            OptionsRegistration.Read<ApplicationOptions>(yapilandirma, ApplicationOptions.SectionName).BaseUrl);
    }

    // ─────────────────────────────────────── kuruma özel varsayılan yok

    /// <summary>
    /// AYAR SINIFLARINDA KURUM BİLGİSİ OLMAZ.
    /// </summary>
    /// <remarks>
    /// Uygulama açık kaynak olacak. Bir varsayılana kurumun adı ya da alan adı
    /// sızarsa, ayarı doldurmayan bir kurulum başka bir belediyenin adını
    /// gösterir — ve bunu kimse fark etmez.
    /// </remarks>
    [Fact]
    public void Varsayilanlar_kuruma_ozel_bilgi_tasimaz()
    {
        var yasakli = new[] { "sivas", "belediyesi", "bel.tr", "randevu.sivas" };

        var metinler = new List<string?>();
        void Topla(object nesne)
        {
            foreach (var ozellik in nesne.GetType().GetProperties())
            {
                if (ozellik.PropertyType == typeof(string))
                    metinler.Add(ozellik.GetValue(nesne) as string);
            }
        }

        Topla(new InstitutionOptions());
        Topla(new BrandOptions());
        Topla(new ApplicationOptions());
        Topla(new SmsOptions());
        Topla(new JwtOptions());
        Topla(new FirebaseOptions());
        Topla(new StorageOptions());
        Topla(new S3StorageOptions());

        var sizanlar = metinler
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Where(m => yasakli.Any(y => m!.Contains(y, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.True(sizanlar.Count == 0,
            "Ayar varsayılanlarına kuruma özel bilgi sızmış:\n  " + string.Join("\n  ", sizanlar));
    }

    // ────────────────────────────────────────── .env.example kapsaması

    /// <summary>
    /// <c>.env.example</c> ŞABLONU EKSİKSİZ OLMALI.
    /// </summary>
    /// <remarks>
    /// Yeni bir ayar eklenip şablona yazılmadığında, o ayarı bilmeyen bir
    /// kurulum sessizce varsayılanla çalışır. Bu test şablonu kodun kendisiyle
    /// karşılaştırır: her ayar sınıfının her alanı orada geçmeli.
    /// </remarks>
    [Fact]
    public void Ornek_env_dosyasi_butun_ayarlari_iceriyor()
    {
        var yol = OrnekEnvYolu();
        var icerik = File.ReadAllText(yol);

        var eksikler = new List<string>();

        void Denetle(Type tur, string bolum)
        {
            foreach (var ozellik in tur.GetProperties())
            {
                // Türetilmiş (salt okunur) alanlar ayar değil.
                if (!ozellik.CanWrite) continue;

                // İç içe ayar sınıfı — kendi bölümüyle ayrıca denetlenir.
                if (ozellik.PropertyType == typeof(S3StorageOptions))
                {
                    Denetle(typeof(S3StorageOptions), $"{bolum}__{ozellik.Name}");
                    continue;
                }

                var anahtar = $"{bolum}__{ozellik.Name}";
                if (!icerik.Contains(anahtar, StringComparison.OrdinalIgnoreCase))
                {
                    eksikler.Add(anahtar);
                }
            }
        }

        Denetle(typeof(InstitutionOptions), InstitutionOptions.SectionName);
        Denetle(typeof(BrandOptions), BrandOptions.SectionName);
        Denetle(typeof(ApplicationOptions), ApplicationOptions.SectionName);
        Denetle(typeof(StorageOptions), StorageOptions.SectionName);
        Denetle(typeof(SmsOptions), SmsOptions.SectionName);
        Denetle(typeof(JwtOptions), JwtOptions.SectionName);
        Denetle(typeof(FirebaseOptions), FirebaseOptions.SectionName);
        Denetle(typeof(DatabaseOptions), DatabaseOptions.SectionName);
        Denetle(typeof(RequestOptions), RequestOptions.SectionName);

        Assert.True(eksikler.Count == 0,
            $"`.env.example` içinde geçmeyen ayar(lar) — şablona ekleyin ({yol}):\n  "
            + string.Join("\n  ", eksikler));
    }

    /// <summary>
    /// Şablon dosyası GERÇEK değer içermemeli — depoya giriyor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Denetim <b>olumlu</b>: "şu bilinen sırlar geçmesin" yerine "her değer
    /// tanınabilir bir yer tutucu olsun". Kara liste iki yönden kötüydü —
    /// yeni bir sağlayıcının anahtarını yakalayamıyor, üstelik yakalamak
    /// istediği gerçek değerin bir parçasını testin içine yazmayı
    /// gerektiriyordu.
    /// </para>
    /// <para>
    /// Denetim yalnızca <b>kimlik bilgisi taşıyan</b> anahtarlara uygulanır:
    /// parola, gizli anahtar, erişim anahtarı, bağlantı dizesi. "Uygulama adı"
    /// ya da "kova adı" gibi zararsız varsayılanlar şablonda dursun — onları
    /// da yer tutucuya zorlamak, şablonu okunmaz ve işe yaramaz kılardı.
    /// </para>
    /// </remarks>
    [Fact]
    public void Ornek_env_dosyasindaki_her_deger_yer_tutucu()
    {
        var satirlar = File.ReadAllLines(OrnekEnvYolu());
        var supheliler = new List<string>();

        foreach (var ham in satirlar)
        {
            var satir = ham.Trim();
            if (satir.Length == 0 || satir.StartsWith('#')) continue;

            var esittir = satir.IndexOf('=');
            if (esittir <= 0) continue;

            var anahtar = satir[..esittir].Trim();
            var deger = satir[(esittir + 1)..].Trim().Trim('"');

            if (!KimlikBilgisiMi(anahtar)) continue;
            if (YerTutucuMu(deger)) continue;
            supheliler.Add($"{anahtar} = {deger}");
        }

        Assert.True(supheliler.Count == 0,
            "`.env.example` depoya giriyor; kimlik bilgisi taşıyan her değer " +
            "tanınabilir bir yer tutucu olmalı. Gerçek değer gibi görünenler:\n  "
            + string.Join("\n  ", supheliler));
    }

    /// <summary>Bu anahtar bir sır taşıyor mu?</summary>
    private static bool KimlikBilgisiMi(string anahtar)
    {
        string[] isaretler =
            ["Secret", "Password", "AccessKey", "SecretKey", "ApiKey", "Token",
             "ConnectionStrings", "Username", "VapidPublicKey", "AppId",
             "MessagingSenderId"];

        return isaretler.Any(i => anahtar.Contains(i, StringComparison.OrdinalIgnoreCase));
    }

    private static bool YerTutucuMu(string deger)
    {
        if (string.IsNullOrWhiteSpace(deger)) return true;

        // Sayı, mantıksal değer, renk kodu, yol.
        if (bool.TryParse(deger, out _)) return true;
        if (long.TryParse(deger, out _)) return true;
        if (System.Text.RegularExpressions.Regex.IsMatch(deger, "^#[0-9A-Fa-f]{6}$")) return true;
        if (deger.StartsWith('/')) return true;

        // Açıkça yer tutucu olduğunu söyleyenler.
        // Karşılaştırma KÜLTÜRE DUYARLI: "Örnek" ile "ornek" ordinal
        // karşılaştırmada eşleşmez ve Türkçe yazılmış bir yer tutucu
        // gerçek değer sanılırdı.
        string[] isaretler = ["DEGISTIR", "DEĞİŞTİR", "ornek", "örnek", "example", "..."];
        if (isaretler.Any(i => deger.Contains(i, StringComparison.OrdinalIgnoreCase))) return true;

        // Yerel adresler ve bağlantı dizesi kalıbı.
        if (deger.Contains("127.0.0.1") || deger.Contains("localhost")) return true;

        return false;
    }

    private static string OrnekEnvYolu()
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);
        while (dizin is not null)
        {
            var aday = Path.Combine(dizin.FullName, ".env.example");
            if (File.Exists(aday)) return aday;
            dizin = dizin.Parent;
        }

        throw new FileNotFoundException(
            "`.env.example` bulunamadı — çözüm kökünde durmalı.");
    }
}
