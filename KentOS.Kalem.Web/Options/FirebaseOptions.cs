namespace KentOS.Kalem.Web.Options;

/// <summary>
/// Firebase — hem sunucu tarafı yönetici SDK'sı hem de istemcilerin ihtiyaç
/// duyduğu web yapılandırması.
///
/// <para>
/// <b>İstemci alanları neden sunucudan geliyor?</b> Web push için gereken
/// <c>apiKey</c>/<c>appId</c>/<c>vapidPublicKey</c> değerleri gizli değil ama
/// KURUMA ÖZEL. SPA'nın derlemesine gömülürse her kurum için ayrı bir ön yüz
/// derlemesi gerekir. Bu yüzden <c>GET /api/v2/institution</c> yanıtında
/// taşınır ve SPA bildirimleri o değerlerle başlatır.
/// </para>
/// </summary>
public sealed class FirebaseOptions
{
    /// <summary>Yapılandırma bölümü adı: <c>FIREBASE__PROJECTID</c> → <c>Firebase:ProjectId</c>.</summary>
    public const string SectionName = "Firebase";

    /// <summary>
    /// Yönetici SDK'sı için hizmet hesabı JSON dosyasının yolu. Göreli yol
    /// verilirse uygulama kök dizinine göre çözülür.
    ///
    /// <para>
    /// Boş bırakılırsa bildirim gönderimi devre dışı kalır ve uygulama
    /// AYAKTA KALIR — dosyanın yokluğu bir kurulumu tamamen durdurmamalı.
    /// </para>
    /// </summary>
    public string CredentialsPath { get; set; } = string.Empty;

    /// <summary>Firebase proje kimliği.</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Web istemcisi API anahtarı.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Kimlik doğrulama alan adı (<c>proje.firebaseapp.com</c>).</summary>
    public string AuthDomain { get; set; } = string.Empty;

    /// <summary>Depolama kovası (<c>proje.firebasestorage.app</c>).</summary>
    public string StorageBucket { get; set; } = string.Empty;

    /// <summary>Mesajlaşma gönderen kimliği.</summary>
    public string MessagingSenderId { get; set; } = string.Empty;

    /// <summary>Web uygulaması kimliği.</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>Web push için VAPID ortak anahtarı.</summary>
    public string VapidPublicKey { get; set; } = string.Empty;

    /// <summary>Tarayıcı tarafında bildirim başlatmak için yeterli bilgi var mı?</summary>
    public bool IsWebPushConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ProjectId) &&
        !string.IsNullOrWhiteSpace(MessagingSenderId) &&
        !string.IsNullOrWhiteSpace(AppId) &&
        !string.IsNullOrWhiteSpace(VapidPublicKey);
}
