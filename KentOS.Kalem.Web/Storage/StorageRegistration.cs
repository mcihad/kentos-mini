using Minio;
using KentOS.Kalem.Web.Configuration;
using KentOS.Kalem.Web.Options;

namespace KentOS.Kalem.Web.Storage;

/// <summary>
/// Depolama sağlayıcısını <c>STORAGE__PROVIDER</c> ayarına göre bağlar.
/// </summary>
public static class StorageRegistration
{
    /// <summary>
    /// Seçilen sağlayıcıyı kaydeder.
    ///
    /// <para>
    /// <b>Eksik S3 ayarında ne olur?</b> Uygulama AÇILMAZ. Sessizce yerele
    /// düşmek çok daha kötü olurdu: yükleme çalışmaya devam eder, dosyalar
    /// beklenen yere gitmez ve kimse fark etmez. Açılışta durmak, yanlış
    /// yapılandırmayı yayın anında görünür kılıyor.
    /// </para>
    /// </summary>
    public static IServiceCollection AddFileStorage(
        this IServiceCollection services, IConfiguration configuration)
    {
        var options = OptionsRegistration.Read<StorageOptions>(
            configuration, StorageOptions.SectionName);

        if (options.Provider != StorageProvider.S3)
        {
            services.AddSingleton<IFileStorage, LocalFileStorage>();
            return services;
        }

        if (!options.S3.IsComplete)
        {
            throw new InvalidOperationException(
                "STORAGE__PROVIDER=S3 seçili ama nesne deposu ayarları eksik. " +
                "STORAGE__S3__ENDPOINT, STORAGE__S3__ACCESSKEY, STORAGE__S3__SECRETKEY ve " +
                "STORAGE__S3__BUCKET değerlerini .env dosyasına yazın.");
        }

        services.AddSingleton<IMinioClient>(_ =>
        {
            var builder = new MinioClient()
                .WithEndpoint(options.S3.NormalizedEndpoint)
                .WithCredentials(options.S3.AccessKey, options.S3.SecretKey)
                .WithSSL(options.S3.UseSsl);

            if (!string.IsNullOrWhiteSpace(options.S3.Region))
            {
                builder = builder.WithRegion(options.S3.Region);
            }

            return builder.Build();
        });

        services.AddSingleton<IFileStorage, S3FileStorage>();
        return services;
    }

    /// <summary>
    /// Açılışta kovanın varlığını denetler, yoksa (ayar izin veriyorsa) açar.
    /// </summary>
    /// <remarks>
    /// Uygulamayı DURDURMAZ: nesne deposu geçici olarak erişilemez olabilir ve
    /// okuma işlevlerinin tamamı çalışmaya devam etmeli. Sorun günlüğe yazılır.
    /// </remarks>
    public static async Task EnsureBucketAsync(IServiceProvider services, ILogger logger)
    {
        var options = services.GetRequiredService<StorageOptions>();
        if (options.Provider != StorageProvider.S3) return;

        var client = services.GetRequiredService<IMinioClient>();

        try
        {
            var varMi = await client.BucketExistsAsync(
                new Minio.DataModel.Args.BucketExistsArgs().WithBucket(options.S3.Bucket));

            if (varMi)
            {
                logger.LogInformation("Nesne deposu hazır: {Uc}/{Kova}",
                    options.S3.NormalizedEndpoint, options.S3.Bucket);
                return;
            }

            if (!options.S3.CreateBucketIfMissing)
            {
                logger.LogError(
                    "Nesne deposunda {Kova} kovası yok ve otomatik oluşturma kapalı. " +
                    "Dosya yükleme çalışmayacak.", options.S3.Bucket);
                return;
            }

            var args = new Minio.DataModel.Args.MakeBucketArgs().WithBucket(options.S3.Bucket);
            if (!string.IsNullOrWhiteSpace(options.S3.Region))
            {
                args = args.WithLocation(options.S3.Region);
            }

            await client.MakeBucketAsync(args);
            logger.LogWarning("Nesne deposunda {Kova} kovası oluşturuldu.", options.S3.Bucket);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Nesne deposuna ulaşılamadı ({Uc}). Dosya yükleme ve indirme çalışmayacak.",
                options.S3.NormalizedEndpoint);
        }
    }
}
