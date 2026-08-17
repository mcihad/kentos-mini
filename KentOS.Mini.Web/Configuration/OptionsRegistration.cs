using KentOS.Mini.Web.Options;

namespace KentOS.Mini.Web.Configuration;

/// <summary>
/// Bütün ayar sınıflarını tek yerden bağlar.
///
/// <para>
/// <b>Neden tek yer?</b> "Bu ayar nereden geliyor?" sorusunun cevabı tek
/// dosyada olsun diye. Ayarlar <c>Program.cs</c> içine dağıldığında, yeni bir
/// kurulum yaparken hangi anahtarın gerçekten okunduğunu anlamak için bütün
/// dosyayı taramak gerekiyordu.
/// </para>
///
/// <para>
/// <b>ESKİ ANAHTAR GERİ DÜŞÜŞLERİ.</b> Yayında çalışan
/// <c>appsettings.json</c> dosyaları var ve bu sürümle birlikte
/// güncellenmeyecekler. Aşağıdaki eşleştirmeler, yeni İngilizce anahtar
/// verilmediğinde eski anahtarı okur. Yeni anahtar VARSA o kazanır.
/// </para>
/// </summary>
public static class OptionsRegistration
{
    /// <summary>
    /// Eski (Türkçe/kısaltılmış) anahtar → yeni anahtar eşleştirmesi.
    /// Testler bu listeyi doğrudan denetliyor.
    /// </summary>
    public static readonly (string Legacy, string Current)[] LegacyKeys =
    [
        ("URL", "App:BaseUrl"),
        ("Depolama:GonderimDizini", "Storage:SendDirectory"),
        ("Randevu:HalkGunuTipId", "Requests:PublicDayTypeId"),
    ];

    /// <summary>
    /// Eski anahtarları, karşılıkları boşsa yeni anahtarlara kopyalar.
    /// <see cref="AddApplicationOptions"/> içinden çağrılır; ayrıca test
    /// edilebilsin diye ayrı durur.
    /// </summary>
    public static void ApplyLegacyKeys(IConfiguration configuration)
    {
        foreach (var (legacy, current) in LegacyKeys)
        {
            if (!string.IsNullOrWhiteSpace(configuration[current])) continue;

            var value = configuration[legacy];
            if (!string.IsNullOrWhiteSpace(value)) configuration[current] = value;
        }
    }

    /// <summary>
    /// Ayar sınıflarını DI'ya bağlar. <c>IOptions&lt;T&gt;</c> yerine doğrudan
    /// <c>T</c> de çözülebilir: ayarlar çalışma anında değişmiyor ve
    /// servislerin imzasını <c>IOptions</c> sarmalıyla kalabalıklaştırmanın
    /// karşılığı yok.
    /// </summary>
    public static IServiceCollection AddApplicationOptions(
        this IServiceCollection services, IConfiguration configuration)
    {
        ApplyLegacyKeys(configuration);

        Bind<InstitutionOptions>(services, configuration, InstitutionOptions.SectionName);
        Bind<BrandOptions>(services, configuration, BrandOptions.SectionName);
        Bind<ApplicationOptions>(services, configuration, ApplicationOptions.SectionName);
        Bind<StorageOptions>(services, configuration, StorageOptions.SectionName);
        Bind<SmsOptions>(services, configuration, SmsOptions.SectionName);
        Bind<JwtOptions>(services, configuration, JwtOptions.SectionName);
        Bind<FirebaseOptions>(services, configuration, FirebaseOptions.SectionName);
        Bind<DatabaseOptions>(services, configuration, DatabaseOptions.SectionName);
        Bind<RequestOptions>(services, configuration, RequestOptions.SectionName);

        return services;
    }

    /// <summary>
    /// Yapılandırma bölümünü okur; bulunamayan alanlar sınıftaki varsayılanla
    /// kalır.
    /// </summary>
    public static T Read<T>(IConfiguration configuration, string sectionName)
        where T : class, new()
    {
        var value = new T();
        configuration.GetSection(sectionName).Bind(value);
        return value;
    }

    private static void Bind<T>(IServiceCollection services, IConfiguration configuration, string section)
        where T : class, new()
    {
        services.Configure<T>(configuration.GetSection(section));
        services.AddSingleton(_ => Read<T>(configuration, section));
    }
}
