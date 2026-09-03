namespace KentOS.Kalem.Web.Options;

/// <summary>
/// Uygulamanın kendisine ait ayarlar — kurumdan bağımsız olanlar burada değil,
/// kuruma göre DEĞİŞENLER burada: genel adres, uygulama adı, künye bağlantısı.
/// </summary>
public sealed class ApplicationOptions
{
    /// <summary>Yapılandırma bölümü adı: <c>APP__BASEURL</c> → <c>App:BaseUrl</c>.</summary>
    public const string SectionName = "App";

    /// <summary>
    /// Uygulamanın dışarıdan görünen adresi. Bildirim yönlendirmelerinde,
    /// e-posta/SMS içindeki bağlantılarda ve <c>ProblemDetails.type</c>
    /// bağlantılarında kullanılır.
    ///
    /// <para>
    /// ESKİ ANAHTAR: bu değer daha önce kök seviyedeki <c>URL</c> anahtarından
    /// okunuyordu. Yayındaki <c>appsettings.json</c> dosyalarını bozmamak için
    /// o anahtar hâlâ geri düşüş olarak kabul ediliyor
    /// (<see cref="Configuration.OptionsRegistration"/>).
    /// </para>
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Uygulamanın adı. Örn. "Randevu Takip Sistemi".</summary>
    public string Name { get; set; } = "WorkCollab";

    /// <summary>PWA ana ekran kısayolunda görünen kısa ad.</summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>Mağaza/manifest açıklaması ve <c>&lt;meta description&gt;</c>.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>API künyesinde ve yardım ekranında gösterilen destek adresi.</summary>
    public string SupportUrl { get; set; } = string.Empty;

    /// <summary>Destek e-postası.</summary>
    public string SupportEmail { get; set; } = string.Empty;

    /// <summary>Boşsa <see cref="Name"/>'e düşen kısa ad.</summary>
    public string ResolvedShortName =>
        string.IsNullOrWhiteSpace(ShortName) ? Name : ShortName;

    /// <summary>
    /// <c>ProblemDetails.type</c> için taban adres. Sondaki eğik çizgi
    /// temizlenir; <c>{taban}/hatalar/dogrulama</c> gibi kurulur.
    /// </summary>
    public string ProblemTypeBase =>
        string.IsNullOrWhiteSpace(BaseUrl) ? "about:blank" : BaseUrl.TrimEnd('/');
}
