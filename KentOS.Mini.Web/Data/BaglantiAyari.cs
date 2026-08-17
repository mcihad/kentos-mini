using Npgsql;

namespace KentOS.Mini.Web.Data;

/// <summary>
/// Veritabanı bağlantı dizesini <b>havuz sınırlarıyla</b> tamamlar.
/// </summary>
/// <remarks>
/// <para>
/// Canlıda dört ayrı 500 hatası aynı kökten geldi:
/// <c>53300: remaining connection slots are reserved for roles with the
/// SUPERUSER attribute</c>. Yani PostgreSQL'in bağlantı yuvaları tükendi ve
/// uygulama yeni bir bağlantı açamadı — takvim, bildirim ve dosya gönderimi
/// uçları aynı saniye içinde arka arkaya düştü.
/// </para>
/// <para>
/// Sebep bir sızıntı değil, <b>bütçesizlik</b>: Npgsql'in varsayılan
/// <c>Maximum Pool Size</c> değeri <b>100</b> ve sunucunun
/// <c>max_connections</c> değeri de 100 (üçü süper kullanıcıya ayrılmış).
/// Yani tek bir uygulama örneği, yükseldiğinde sunucunun bütün yuvalarını
/// tek başına kaplayabiliyor. Bu sunucu <b>paylaşımlı</b>: aynı PostgreSQL
/// örneğinde başka kurum uygulamaları da var ve yuvaları tüketmek yalnızca
/// bizi değil onları da düşürüyor.
/// </para>
/// <para>
/// Bu yüzden havuz açıkça sınırlanıyor. Bekleyen istek artık <b>hata almak
/// yerine sırada bekliyor</b>: her istek birkaç milisaniyelik sorgular
/// çalıştırıyor, sıra hızlı ilerliyor. Boşta kalan bağlantılar da bir süre
/// sonra sunucuya geri veriliyor; gece boyunca 25 bağlantıyı açık tutmanın
/// kimseye faydası yok.
/// </para>
/// <para>
/// <b>Yeniden deneme (EnableRetryOnFailure) AÇILMADI.</b> EF'in yeniden
/// deneme stratejisi, kullanıcı tarafından başlatılan işlemleri (bizde
/// birkaç yerde <c>BeginTransaction</c> var) çalışma anında hata vererek
/// reddediyor. Üstelik 53300 bizim için geçici bir arıza değil, bir bütçe
/// hatası: doğru çözüm beklemek değil, hiç taşmamak.
/// </para>
/// <para>
/// Değerler <b>ancak bağlantı dizesinde yoksa</b> yazılır; dolayısıyla
/// <c>appsettings.json</c> istediğini ezebilir.
/// </para>
/// </remarks>
public static class BaglantiAyari
{
    /// <summary>Uygulamanın aynı anda tutabileceği en fazla bağlantı.</summary>
    public const int VarsayilanEnFazlaHavuz = 25;

    /// <summary>Havuzda sürekli hazır tutulan bağlantı sayısı.</summary>
    public const int VarsayilanEnAzHavuz = 1;

    /// <summary>Boşta kalan bağlantı kaç saniye sonra sunucuya bırakılır.</summary>
    public const int VarsayilanBostaOmru = 60;

    /// <summary>
    /// Ham bağlantı dizesini alır, eksik havuz ayarlarını tamamlayıp döndürür.
    /// </summary>
    public static string Tamamla(string? ham)
    {
        if (string.IsNullOrWhiteSpace(ham))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection tanımlı değil.");
        }

        var kurucu = new NpgsqlConnectionStringBuilder(ham);
        var yazili = YaziliAnahtarlar(ham);

        if (!yazili.Contains("maximumpoolsize") && !yazili.Contains("maxpoolsize"))
        {
            kurucu.MaxPoolSize = VarsayilanEnFazlaHavuz;
        }

        if (!yazili.Contains("minimumpoolsize") && !yazili.Contains("minpoolsize"))
        {
            kurucu.MinPoolSize = VarsayilanEnAzHavuz;
        }

        if (!yazili.Contains("connectionidlelifetime"))
        {
            kurucu.ConnectionIdleLifetime = VarsayilanBostaOmru;
        }

        // Bağlantıyı KİMİN açtığı `pg_stat_activity`de görünsün. Paylaşımlı
        // sunucuda "bu 40 bağlantı kimin?" sorusunun cevabı yoktu; yuvalar
        // tükendiğinde suçluyu aramak tahmin işine dönüyordu.
        if (!yazili.Contains("applicationname"))
        {
            kurucu.ApplicationName = "workcollab-web";
        }

        return kurucu.ConnectionString;
    }

    /// <summary>
    /// Bağlantı dizesinde <b>gerçekten yazılı olan</b> anahtarlar.
    /// </summary>
    /// <remarks>
    /// <c>NpgsqlConnectionStringBuilder.ContainsKey</c> KULLANILAMAZ: o,
    /// anahtarın tanınıp tanınmadığını söylüyor, yazılıp yazılmadığını değil —
    /// yani her zaman <c>true</c> dönüyor ve "kullanıcı yazmadıysa varsayılanı
    /// koy" mantığı hiçbir zaman çalışmıyordu. Havuz sınırı bu yüzden bir
    /// süre sessizce uygulanmadı; ölçüm 25 yerine 40 eşzamanlı bağlantı
    /// gösterdi.
    /// </remarks>
    private static HashSet<string> YaziliAnahtarlar(string ham)
    {
        var kume = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var parca in ham.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var esittir = parca.IndexOf('=');
            if (esittir <= 0) continue;

            // "Maximum Pool Size" ve "MaxPoolSize" aynı anahtarın iki yazımı.
            var anahtar = parca[..esittir].Replace(" ", string.Empty).Trim();
            kume.Add(anahtar);
        }

        return kume;
    }
}
