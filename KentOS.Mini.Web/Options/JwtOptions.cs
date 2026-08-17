namespace KentOS.Mini.Web.Options;

/// <summary>
/// Jeton üretimi ve doğrulaması.
///
/// <para>
/// <b>Anahtar adları değişmedi.</b> <c>JWT__SECRET</c> ortam değişkeni ile
/// yayındaki <c>appsettings.json</c> içindeki <c>"JWT"</c> bölümü aynı yere
/// düşer (yapılandırma büyük/küçük harf duyarsızdır). Bu, mobil uygulamanın
/// elindeki jetonların geçerliliğini korur — imza anahtarı değişirse SAHADAKİ
/// BÜTÜN OTURUMLAR düşer.
/// </para>
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Yapılandırma bölümü adı.</summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// İmza anahtarı. En az 32 karakter olmalı (HMAC-SHA256).
    /// <b>Kuruma özeldir ve depoya girmez.</b>
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Jetonu üreten taraf.</summary>
    public string ValidIssuer { get; set; } = string.Empty;

    /// <summary>Jetonun hedef kitlesi.</summary>
    public string ValidAudience { get; set; } = string.Empty;

    /// <summary>
    /// Jeton ömrü — <b>dakika</b>. Yayındaki değer 900 (15 saat) ve mobil
    /// uygulamanın oturum davranışı buna göre ayarlı; düşürmek kullanıcıları
    /// gün içinde tekrar giriş yapmaya zorlar.
    /// </summary>
    public int TokenExpiration { get; set; } = 900;
}
