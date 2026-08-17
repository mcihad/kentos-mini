using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using KentOS.Mini.Web.Options;

namespace KentOS.Mini.Web.Storage;

/// <summary>
/// Dosyaları S3 uyumlu bir nesne deposunda tutar (MinIO, AWS S3, Ceph…).
///
/// <para>
/// <b>Ne zaman gerekir?</b> Uygulama birden çok sunucuda çalışıyorsa (yük
/// dengeleyici arkasında) diske yazmak işe yaramaz — bir sunucuya yüklenen
/// dosyayı diğeri göremez. Kapsayıcıyla dağıtılan kurulumlarda da yayın
/// klasörü kalıcı değildir. İki durumda da dosyalar uygulamanın dışında
/// durmalı.
/// </para>
///
/// <para>
/// <b>Yerleşim yerel sağlayıcıyla AYNI:</b> nesne adı, veritabanında saklanan
/// yolun ta kendisi (<c>uploads/ajanda/8f3e….jpg</c>). Gizli alan
/// <c>gonderim/</c> öneki altında. Bu sayede yerelden nesne deposuna geçiş
/// dosyaları kopyalamaktan ibaret; tek bir veritabanı kaydı değişmiyor.
/// </para>
/// </summary>
public sealed class S3FileStorage : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3FileStorage> _logger;

    /// <summary>
    /// Gizli alanın nesne adı öneki. Genel alanla aynı kovada durur ama kova
    /// politikası bu ön eki dışarı açmamalıdır.
    /// </summary>
    public const string PrivatePrefix = "gonderim/";

    public S3FileStorage(
        IMinioClient client,
        IOptions<StorageOptions> options,
        ILogger<S3FileStorage> logger)
    {
        _client = client;
        _options = options.Value.S3;
        _logger = logger;
    }

    public string ProviderName => "S3";

    public bool IsRemote => true;

    public async Task SaveAsync(StorageArea area, string key, Stream content, string? contentType,
                                CancellationToken cancellationToken = default)
    {
        // Uzunluk bilinmeden yükleme yapılamıyor; akış geri sarılamıyorsa
        // belleğe alınır. Yüklenen dosyalar zaten 25 MB ile sınırlı.
        if (!content.CanSeek)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            await PutAsync(area, key, buffer, buffer.Length, contentType, cancellationToken);
            return;
        }

        var uzunluk = content.Length - content.Position;
        await PutAsync(area, key, content, uzunluk, contentType, cancellationToken);
    }

    public async Task SaveAsync(StorageArea area, string key, byte[] content, string? contentType,
                                CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content, writable: false);
        await PutAsync(area, key, stream, content.LongLength, contentType, cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(StorageArea area, string key,
                                             CancellationToken cancellationToken = default)
    {
        var bytes = await ReadAllBytesAsync(area, key, cancellationToken);
        return bytes is null ? null : new MemoryStream(bytes, writable: false);
    }

    /// <remarks>
    /// MinIO istemcisi akışı geri döndürmüyor, bir geri çağrıya yazıyor. Bu
    /// yüzden nesne belleğe alınıp öyle veriliyor. Yükleme sınırları (fotoğraf
    /// 5 MB, talep eki 20 MB, gönderim 25 MB) bunu güvenli kılıyor; sınırsız
    /// boyutlu bir dosya türü eklenirse burası akış temelli yazılmalı.
    /// </remarks>
    public async Task<byte[]?> ReadAllBytesAsync(StorageArea area, string key,
                                                 CancellationToken cancellationToken = default)
    {
        var objectName = ObjectName(area, key);

        try
        {
            using var buffer = new MemoryStream();

            await _client.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_options.Bucket)
                    .WithObject(objectName)
                    .WithCallbackStream((stream, ct) => stream.CopyToAsync(buffer, ct)),
                cancellationToken);

            return buffer.ToArray();
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
        catch (BucketNotFoundException)
        {
            _logger.LogError("Nesne deposunda kova yok: {Kova}", _options.Bucket);
            return null;
        }
    }

    public async Task<bool> ExistsAsync(StorageArea area, string key,
                                        CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(_options.Bucket)
                    .WithObject(ObjectName(area, key)),
                cancellationToken);

            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (BucketNotFoundException)
        {
            return false;
        }
    }

    public async Task DeleteAsync(StorageArea area, string key,
                                  CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.RemoveObjectAsync(
                new RemoveObjectArgs()
                    .WithBucket(_options.Bucket)
                    .WithObject(ObjectName(area, key)),
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Yetim nesne zararsız; silinemeyen kayıt kullanıcıyı çıkışsız bırakır.
            _logger.LogWarning(ex, "Nesne depodan silinemedi: {Anahtar}", key);
        }
    }

    private Task PutAsync(StorageArea area, string key, Stream content, long length,
                          string? contentType, CancellationToken cancellationToken)
    {
        var args = new PutObjectArgs()
            .WithBucket(_options.Bucket)
            .WithObject(ObjectName(area, key))
            .WithStreamData(content)
            .WithObjectSize(length)
            .WithContentType(string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType);

        return _client.PutObjectAsync(args, cancellationToken);
    }

    /// <summary>Anahtarı kovadaki nesne adına çevirir.</summary>
    public string ObjectName(StorageArea area, string key)
    {
        var normalized = StorageKey.Normalize(key);
        var alan = area == StorageArea.Private ? PrivatePrefix : string.Empty;
        return $"{_options.Prefix}{alan}{normalized}";
    }
}
