namespace KentOS.Kalem.Web.Options;

/// <summary>
/// SMS sağlayıcısı ayarları.
///
/// <para>
/// Anahtar adları BİLİNÇLİ olarak eski <c>SMS:*</c> bölümüyle aynı bırakıldı;
/// .NET yapılandırması büyük/küçük harf duyarsız olduğu için yayındaki
/// <c>appsettings.json</c> ve yeni <c>SMS__*</c> ortam değişkenleri aynı
/// bölüme düşer. Böylece geçiş sırasında hiçbir kurulum SMS'siz kalmaz.
/// </para>
/// </summary>
public sealed class SmsOptions
{
    /// <summary>Yapılandırma bölümü adı: <c>SMS__URL</c> → <c>Sms:Url</c>.</summary>
    public const string SectionName = "Sms";

    /// <summary>Sağlayıcının gönderim uç noktası.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Sağlayıcı hesabı kullanıcı adı.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Sağlayıcı hesabı parolası.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Mesajın başında görünen gönderici adı (originator). Sağlayıcıda kayıtlı
    /// olmayan bir başlıkla gönderim reddedilir.
    /// </summary>
    public string Sender { get; set; } = string.Empty;

    /// <summary>Gönderim için gereken alanlar dolu mu?</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url) &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password);
}
