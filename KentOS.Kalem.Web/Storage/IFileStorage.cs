namespace KentOS.Kalem.Web.Storage;

/// <summary>
/// Dosyanın hangi erişim rejimine ait olduğu.
/// </summary>
/// <remarks>
/// Ayrım güvenlik ayrımıdır, klasör ayrımı değil: <see cref="Public"/> alandaki
/// bir nesnenin adresi bilinirse indirilir, <see cref="Private"/> alandakine
/// yalnızca kimlik denetimli uçlardan ulaşılır.
/// </remarks>
public enum StorageArea
{
    /// <summary>
    /// Etkinlik fotoğrafı, talep eki, özgeçmiş — <c>/uploads/...</c> altında.
    ///
    /// <para>
    /// <b>Bu adresler yayındaki mobil uygulamanın sözleşmesinin parçası.</b>
    /// v1 istemcileri fotoğrafları doğrudan <c>/uploads/ajanda/...</c>
    /// adresinden indiriyor; nesne deposuna geçildiğinde bile o adres çalışmaya
    /// devam etmeli (bkz. <see cref="Middleware.UzakDepoKopruSu"/>).
    /// </para>
    /// </summary>
    Public = 0,

    /// <summary>
    /// Kullanıcıdan kullanıcıya gönderilen belgeler. Hiçbir zaman statik
    /// sunulmaz; tek giriş <c>GET /api/v2/gonderim/{id}/dosya</c>.
    /// </summary>
    Private = 1,
}

/// <summary>
/// Dosya deposu soyutlaması.
///
/// <para>
/// <b>Neden soyutlama?</b> Uygulama başka kurumlara verilecek. Tek sunuculu bir
/// belediyede diske yazmak en basit ve en ucuz çözüm; birden çok uygulama
/// sunucusu olan ya da yayın klasörünü kalıcı tutamayan bir kurumda ise dosya
/// paylaşımlı bir nesne deposunda durmak zorunda. İkisi de aynı arayüzün
/// arkasında: <c>STORAGE__PROVIDER</c> ile seçiliyor, çağıran kod hiç
/// değişmiyor.
/// </para>
///
/// <para>
/// <b>Anahtar (key) biçimi:</b> eğik çizgiyle ayrılmış göreli yol —
/// <c>uploads/ajanda/8f3e….jpg</c>. Bu, veritabanında saklanan yolla birebir
/// aynı olsun diye seçildi; taşıma sırasında hiçbir kaydın güncellenmesi
/// gerekmiyor. Baştaki eğik çizgi temizlenir, <c>..</c> reddedilir.
/// </para>
/// </summary>
public interface IFileStorage
{
    /// <summary>Günlüklerde ve tanı ekranında görünen sağlayıcı adı.</summary>
    string ProviderName { get; }

    /// <summary>Depo uzakta mı (nesne deposu) — geri düşüş ara katmanı buna bakar.</summary>
    bool IsRemote { get; }

    /// <summary>Akışı depoya yazar; aynı anahtar varsa üzerine yazar.</summary>
    Task SaveAsync(StorageArea area, string key, Stream content, string? contentType,
                   CancellationToken cancellationToken = default);

    /// <summary>Baytları depoya yazar.</summary>
    Task SaveAsync(StorageArea area, string key, byte[] content, string? contentType,
                   CancellationToken cancellationToken = default);

    /// <summary>
    /// Okuma akışı açar. Nesne yoksa <c>null</c> döner — <b>fırlatmaz</b>:
    /// "dosya yok" durumunu çağıran taraf kendi diline (404, iş kuralı hatası)
    /// çevirmek zorunda.
    /// </summary>
    Task<Stream?> OpenReadAsync(StorageArea area, string key,
                                CancellationToken cancellationToken = default);

    /// <summary>Nesnenin tamamını belleğe okur; yoksa <c>null</c>.</summary>
    Task<byte[]?> ReadAllBytesAsync(StorageArea area, string key,
                                    CancellationToken cancellationToken = default);

    /// <summary>Nesne var mı?</summary>
    Task<bool> ExistsAsync(StorageArea area, string key,
                           CancellationToken cancellationToken = default);

    /// <summary>
    /// Siler. <b>Nesne yoksa sessiz geçer ve hata fırlatmaz.</b> Silme hatası
    /// veritabanı kaydının silinmesini engellememeli: yetim bir dosya zararsız,
    /// silinemeyen bir kayıt kullanıcıyı çıkışsız bırakır.
    /// </summary>
    Task DeleteAsync(StorageArea area, string key,
                     CancellationToken cancellationToken = default);
}

/// <summary>
/// Anahtar temizliği — her sağlayıcı aynı kuralı uygular.
/// </summary>
public static class StorageKey
{
    /// <summary>
    /// Baştaki eğik çizgiyi atar, ters bölü işaretlerini eğik çizgiye çevirir
    /// ve dizin dışına çıkma denemesini reddeder.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Anahtar boşsa ya da <c>..</c> segmenti içeriyorsa.
    /// </exception>
    public static string Normalize(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Dosya anahtarı boş olamaz.", nameof(key));
        }

        var temiz = key.Replace('\\', '/').TrimStart('/');

        // `..` denetimi ŞART: talep dosyasının yolu veritabanından geliyor ve
        // oraya yıllar içinde elle yazılmış kayıtlar da düşmüş olabilir.
        if (temiz.Split('/').Any(p => p == ".."))
        {
            throw new ArgumentException($"Geçersiz dosya anahtarı: {key}", nameof(key));
        }

        return temiz;
    }
}
