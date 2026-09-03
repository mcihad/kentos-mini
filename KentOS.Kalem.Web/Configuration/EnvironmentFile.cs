namespace KentOS.Kalem.Web.Configuration;

/// <summary>
/// `.env` dosyasını süreç ortam değişkenlerine yükler.
///
/// <para>
/// <b>Neden ortam değişkeni, neden ayrı bir yapılandırma sağlayıcısı değil?</b>
/// .NET'in <c>AddEnvironmentVariables()</c> sağlayıcısı <c>Bolum__Alt</c>
/// biçimindeki değişkenleri <c>Bolum:Alt</c> yapılandırma anahtarına zaten
/// çeviriyor. Dosyayı ortam değişkenine yüklemek, aynı ayarın hem `.env` ile
/// hem de IIS/Docker/systemd ortam değişkeniyle verilebilmesi demek — tek bir
/// okuma yolu, iki farklı kaynak.
/// </para>
///
/// <para>
/// <b>Yükleme SIRASI kritiktir:</b> <c>WebApplication.CreateBuilder</c>
/// çağrılırken ortam değişkenleri okunur. Bu yüzden <see cref="Load"/>
/// builder'dan ÖNCE çağrılmalıdır; sonra çağrılırsa dosya sessizce etkisiz
/// kalır.
/// </para>
///
/// <para>
/// <b>Var olan ortam değişkeni EZİLMEZ.</b> Sunucuda gerçekten tanımlanmış bir
/// değişken, depoya sızmış ya da eski kalmış bir `.env` satırından her zaman
/// güçlüdür. Bu, yayın makinesinde "dosyada ne yazıyorsa o çalışır" sürprizini
/// önler.
/// </para>
/// </summary>
public static class EnvironmentFile
{
    /// <summary>Aranan dosya adı.</summary>
    public const string FileName = ".env";

    /// <summary>
    /// <paramref name="startDirectory"/> ve üst dizinlerinde <c>.env</c> arar,
    /// bulduğu ilk dosyayı yükler.
    ///
    /// <para>
    /// Yukarı doğru arama şart: geliştirmede çalışma dizini web projesi,
    /// testlerde <c>bin/Debug/net10.0</c>, yayında yayın klasörü oluyor. Dosya
    /// çözüm kökünde duruyor ve üçünde de bulunabilmeli.
    /// </para>
    /// </summary>
    /// <returns>Yüklenen dosyanın tam yolu; dosya yoksa <c>null</c>.</returns>
    public static string? Load(string startDirectory)
    {
        var path = Find(startDirectory);
        if (path is null) return null;

        // clobberExistingVars: false → gerçek ortam değişkeni kazanır.
        // onlyExactPath: true → DotNetEnv kendi başına dizin gezmesin,
        // aramayı biz yaptık ve hangi dosyanın yüklendiğini bilmek istiyoruz.
        DotNetEnv.Env.Load(path, new DotNetEnv.LoadOptions(
            setEnvVars: true,
            clobberExistingVars: false,
            onlyExactPath: true));

        return path;
    }

    /// <summary>
    /// Verilen dizinden başlayıp kök dizine kadar <c>.env</c> arar.
    /// </summary>
    public static string? Find(string startDirectory)
    {
        if (string.IsNullOrWhiteSpace(startDirectory)) return null;

        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, FileName);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        return null;
    }
}
