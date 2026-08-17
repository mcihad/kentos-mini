using Microsoft.Extensions.Options;
using KentOS.Mini.Web.Options;

namespace KentOS.Mini.Web.Storage;

/// <summary>
/// Dosyaları sunucunun kendi diskine yazar — <b>iki yıldır yayında olan
/// davranış.</b>
///
/// <para>
/// Bu sınıf yeni bir yerleşim getirmiyor; var olan yolları (
/// <c>wwwroot/uploads/...</c> ve gönderim klasörü) tek bir arayüzün arkasına
/// alıyor. Dosyaların diskteki yeri, adı ve veritabanında saklanan yol
/// DEĞİŞMEDİ; aksi hâlde geçmişteki bütün kayıtların yolunu güncellemek
/// gerekirdi.
/// </para>
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _publicRoot;
    private readonly string _privateRoot;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(
        IWebHostEnvironment environment,
        IOptions<StorageOptions> options,
        ILogger<LocalFileStorage> logger)
    {
        _logger = logger;
        _publicRoot = ResolveWebRoot(environment);
        _privateRoot = ResolvePrivateRoot(environment, options.Value);
    }

    public string ProviderName => "Local";

    public bool IsRemote => false;

    /// <summary>
    /// Yerel sağlayıcıda genel alanın kökü — <c>wwwroot</c>.
    /// </summary>
    /// <remarks>
    /// <c>WebRootPath</c> bazı barındırma senaryolarında (özellikle test
    /// sunucusunda) boş geliyor; o zaman içerik kökünün altındaki
    /// <c>wwwroot</c> kullanılır.
    /// </remarks>
    public static string ResolveWebRoot(IWebHostEnvironment environment) =>
        string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;

    /// <summary>
    /// Gönderilen belgelerin klasörü.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Varsayılan <c>wwwroot/uploads/gonderim</c>. Bu bir KURULUM kararıdır:
    /// uygulama havuzu kimliğine <c>wwwroot/uploads</c> için yazma hakkı zaten
    /// verilmiş. Ayrı bir klasör her yayında elle izin vermeyi gerektirir ve
    /// unutulduğunda özellik sessizce çalışmaz.
    /// </para>
    /// <para>
    /// <b>Ama <c>wwwroot</c> altı kimlik doğrulanmadan servis edilir.</b> Bu
    /// yüzden <see cref="Middleware.GonderimDosyaKorumasi"/> ara katmanı
    /// <c>/uploads/gonderim</c> altına gelen HTTP isteklerini 404'ler.
    /// </para>
    /// <para>
    /// Belgeleri yayın klasörünün dışında tutmak isteyen kurulumlar
    /// <c>STORAGE__SENDDIRECTORY</c> ile başka bir yol verebilir; o zaman
    /// dosyalar sürüm değiştirdiğinde yerinde kalır.
    /// </para>
    /// </remarks>
    public static string ResolvePrivateRoot(IWebHostEnvironment environment, StorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SendDirectory))
        {
            return options.SendDirectory;
        }

        return Path.Combine(ResolveWebRoot(environment), options.UploadPath, "gonderim");
    }

    public async Task SaveAsync(StorageArea area, string key, Stream content, string? contentType,
                                CancellationToken cancellationToken = default)
    {
        var path = FullPath(area, key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var target = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(target, cancellationToken);
    }

    public async Task SaveAsync(StorageArea area, string key, byte[] content, string? contentType,
                                CancellationToken cancellationToken = default)
    {
        var path = FullPath(area, key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(StorageArea area, string key,
                                       CancellationToken cancellationToken = default)
    {
        var path = FullPath(area, key);
        Stream? stream = File.Exists(path) ? File.OpenRead(path) : null;
        return Task.FromResult(stream);
    }

    public async Task<byte[]?> ReadAllBytesAsync(StorageArea area, string key,
                                                 CancellationToken cancellationToken = default)
    {
        var path = FullPath(area, key);
        return File.Exists(path)
            ? await File.ReadAllBytesAsync(path, cancellationToken)
            : null;
    }

    public Task<bool> ExistsAsync(StorageArea area, string key,
                                  CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(FullPath(area, key)));

    public Task DeleteAsync(StorageArea area, string key,
                            CancellationToken cancellationToken = default)
    {
        try
        {
            var path = FullPath(area, key);
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dosya diskten silinemedi: {Anahtar}", key);
        }

        return Task.CompletedTask;
    }

    /// <summary>Anahtarı diskteki tam yola çevirir.</summary>
    private string FullPath(StorageArea area, string key)
    {
        var normalized = StorageKey.Normalize(key);
        var root = area == StorageArea.Private ? _privateRoot : _publicRoot;

        var full = Path.GetFullPath(Path.Combine(root, normalized));

        // İkinci savunma hattı: `Normalize` `..` reddediyor ama sembolik bağ
        // ya da mutlak yol içeren bir anahtar da kök dışına çıkabilir.
        var rootFull = Path.GetFullPath(root);
        if (!full.StartsWith(rootFull, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Dosya anahtarı kök dizinin dışına çıkıyor: {key}", nameof(key));
        }

        return full;
    }
}
