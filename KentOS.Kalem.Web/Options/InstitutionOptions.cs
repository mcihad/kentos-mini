namespace KentOS.Kalem.Web.Options;

/// <summary>
/// Uygulamayı kullanan kurumun kimliği.
///
/// <para>
/// <b>Bu sınıfın var olma sebebi:</b> uygulama birden çok kuruma verilecek ve
/// açık kaynak olacak. Kurum adı, alan adı, adres gibi bilgiler koda
/// yazıldığında her kurum için ayrı bir kaynak dalı gerekir. Hepsi buraya
/// toplandı; kurum değiştirmek <c>.env</c> dosyasını değiştirmekten ibaret.
/// </para>
///
/// <para>
/// Değerler ÇALIŞMA ANINDA okunur ve <c>GET /api/v2/institution</c> ile
/// istemcilere verilir — SPA'nın derlemesine gömülmez. Gömülseydi her kurum
/// için ayrı bir ön yüz derlemesi gerekirdi.
/// </para>
/// </summary>
public sealed class InstitutionOptions
{
    /// <summary>Yapılandırma bölümü adı: <c>INSTITUTION__NAME</c> → <c>Institution:Name</c>.</summary>
    public const string SectionName = "Institution";

    /// <summary>Kurumun resmî adı. Örn. "Örnek Belediyesi".</summary>
    public string Name { get; set; } = "Kurum";

    /// <summary>Dar alanlarda (SMS başlığı, rozet) kullanılan kısa ad.</summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>
    /// Çıktıların (PDF/Excel) tepesinde basılan ad. Boşsa <see cref="Name"/>
    /// kullanılır.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Uygulamayı işleten birim. Örn. "Başkanlık Makamı".</summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>Künye/alt bilgi satırı. Örn. yazılımı geliştiren müdürlük.</summary>
    public string FooterNote { get; set; } = string.Empty;

    /// <summary>Kurumun genel ağ sitesi.</summary>
    public string Website { get; set; } = string.Empty;

    /// <summary>Posta adresi (çıktı alt bilgisinde kullanılabilir).</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Santral telefonu.</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>İletişim e-postası. API künyesinde de geçer.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Boşsa <see cref="Name"/>'e düşen görünen ad.</summary>
    public string ResolvedDisplayName =>
        string.IsNullOrWhiteSpace(DisplayName) ? Name : DisplayName;

    /// <summary>Boşsa <see cref="Name"/>'e düşen kısa ad.</summary>
    public string ResolvedShortName =>
        string.IsNullOrWhiteSpace(ShortName) ? Name : ShortName;
}
