namespace KentOS.Kalem.Web.Options;

/// <summary>Desteklenen dosya depolama sağlayıcıları.</summary>
public enum StorageProvider
{
    /// <summary>
    /// Dosyalar sunucunun kendi diskinde, <c>wwwroot</c> altında. İki yıldır
    /// yayında olan davranış budur ve VARSAYILANDIR: ayar verilmemiş bir
    /// kurulum bugünkü gibi çalışmaya devam eder.
    /// </summary>
    Local = 0,

    /// <summary>
    /// S3 uyumlu nesne deposu (MinIO, AWS S3, Ceph, Wasabi…). Birden çok
    /// sunucuya yayılan ya da yayın klasörünü kalıcı tutamayan kurulumlar için.
    /// </summary>
    S3 = 1,
}

/// <summary>
/// Dosya depolama ayarları.
///
/// <para>
/// <b>Seçim tek satırla yapılır:</b> <c>STORAGE__PROVIDER=Local</c> ya da
/// <c>STORAGE__PROVIDER=S3</c>. Dosya tabanlı seçildiğinde mevcut sistem
/// aynen çalışır; S3 seçildiğinde bütün yükleme/indirme/silme işlemleri nesne
/// deposuna gider ve eski <c>/uploads/...</c> adresleri
/// <see cref="Middleware.UzakDepoKopruSu"/> ara katmanıyla ayakta kalır —
/// yayındaki mobil uygulama o adresleri kullanıyor.
/// </para>
/// </summary>
public sealed class StorageOptions
{
    /// <summary>Yapılandırma bölümü adı: <c>STORAGE__PROVIDER</c> → <c>Storage:Provider</c>.</summary>
    public const string SectionName = "Storage";

    /// <summary>Hangi sağlayıcı kullanılacak.</summary>
    public StorageProvider Provider { get; set; } = StorageProvider.Local;

    /// <summary>
    /// Herkese açık yüklemelerin <c>wwwroot</c> altındaki kök klasörü.
    /// Değiştirmek, veritabanındaki mevcut <c>/uploads/...</c> yollarını
    /// geçersiz kılar; var olan bir kurulumda DOKUNMAYIN.
    /// </summary>
    public string UploadPath { get; set; } = "uploads";

    /// <summary>
    /// Kullanıcıdan kullanıcıya gönderilen belgelerin klasörü (yerel sağlayıcı).
    ///
    /// <para>
    /// Boş bırakılabilir: boşken <c>wwwroot/uploads/gonderim</c> kullanılır ve
    /// orası zaten yazılabilir. Belgelerin sürümden sürüme taşınmasını
    /// istemiyorsanız yayın klasörünün dışında bir yol verin, örn.
    /// <c>D:\workcollab-veri\gonderim</c>.
    /// </para>
    ///
    /// <para>
    /// ESKİ ANAHTAR: <c>Depolama:GonderimDizini</c>. Yayındaki
    /// <c>appsettings.json</c> dosyaları bozulmasın diye geri düşüş olarak
    /// hâlâ okunuyor.
    /// </para>
    /// </summary>
    public string SendDirectory { get; set; } = string.Empty;

    /// <summary>S3 uyumlu depo ayarları. <see cref="Provider"/> S3 değilse yok sayılır.</summary>
    public S3StorageOptions S3 { get; set; } = new();

    /// <summary>S3 sağlayıcısı seçili mi?</summary>
    public bool UsesObjectStorage => Provider == StorageProvider.S3;
}

/// <summary>
/// S3 uyumlu nesne deposu ayarları. MinIO da AWS S3 de aynı alanları kullanır;
/// fark yalnızca <see cref="Endpoint"/> ve <see cref="UseSsl"/> değerlerinde.
/// </summary>
public sealed class S3StorageOptions
{
    /// <summary>
    /// Uç nokta — <b>şemasız</b> ana bilgisayar ve port. MinIO için
    /// <c>127.0.0.1:9000</c>, AWS için <c>s3.eu-central-1.amazonaws.com</c>.
    /// Başına <c>https://</c> yazılırsa temizlenir (<see cref="NormalizedEndpoint"/>).
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Erişim anahtarı (MinIO'da kök kullanıcı adı).</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>Gizli anahtar (MinIO'da kök parola).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Kova adı. Yoksa açılışta oluşturulmaya çalışılır.</summary>
    public string Bucket { get; set; } = "workcollab";

    /// <summary>AWS bölgesi. MinIO'da boş bırakılabilir.</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>TLS kullanılsın mı? MinIO yerel kurulumlarında genelde <c>false</c>.</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Bütün nesne adlarının başına eklenen önek. Aynı kovayı birden çok kurum
    /// paylaşacaksa ayırıcı olarak kullanılır (örn. <c>kurum-a/</c>).
    /// </summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Açılışta kova yoksa oluşturulsun mu? Üretimde kovayı yönetici elle
    /// açıp politika verdiyse <c>false</c> yapın.
    /// </summary>
    public bool CreateBucketIfMissing { get; set; } = true;

    /// <summary>Şemadan arındırılmış uç nokta — MinIO istemcisi şema kabul etmez.</summary>
    public string NormalizedEndpoint =>
        Endpoint.Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
                .TrimEnd('/');

    /// <summary>Zorunlu alanlar dolu mu?</summary>
    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(NormalizedEndpoint) &&
        !string.IsNullOrWhiteSpace(AccessKey) &&
        !string.IsNullOrWhiteSpace(SecretKey) &&
        !string.IsNullOrWhiteSpace(Bucket);
}
