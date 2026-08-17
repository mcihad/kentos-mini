namespace KentOS.Mini.Web.Options;

/// <summary>
/// Kurumsal kimlik çekirdeği: renkler ve görseller.
///
/// <para>
/// SPA'nın tema motoru geri kalan bütün tonları bu üç renkten türetir; burada
/// yalnızca çekirdek tutulur. Değerler ilk boyamadan önce uygulanır, bu yüzden
/// <c>GET /api/v2/institution</c> yanıtının içinde gelir.
/// </para>
/// </summary>
public sealed class BrandOptions
{
    /// <summary>Yapılandırma bölümü adı: <c>BRAND__PRIMARY</c> → <c>Brand:Primary</c>.</summary>
    public const string SectionName = "Brand";

    /// <summary>Birincil kurumsal renk (#RRGGBB).</summary>
    public string Primary { get; set; } = "#002E6D";

    /// <summary>Vurgu rengi (#RRGGBB).</summary>
    public string Accent { get; set; } = "#A78952";

    /// <summary>Nötr/gri temel (#RRGGBB).</summary>
    public string Neutral { get; set; } = "#4D4D4F";

    /// <summary>
    /// Koyu temada birincil rengin okunabilir karşılığı. Boşsa SPA
    /// <see cref="Primary"/>'den kendisi açar.
    /// </summary>
    public string PrimaryDark { get; set; } = string.Empty;

    /// <summary>Amblem yolu (<c>wwwroot</c> köküne göre).</summary>
    public string Logo { get; set; } = "/amblem.png";

    /// <summary>Sekme simgesi yolu.</summary>
    public string Favicon { get; set; } = "/ikon/favicon-32.png";

    /// <summary>PWA/uygulama simgesi yolu.</summary>
    public string AppIcon { get; set; } = "/ikon/ikon-512.png";

    /// <summary>
    /// Çıktılarda (PDF başlığı, isim kartı) kullanılan amblem. Boşsa
    /// <see cref="Logo"/> kullanılır — çoğu kurumda ikisi aynıdır.
    /// </summary>
    public string PrintLogo { get; set; } = string.Empty;

    /// <summary>Boşsa <see cref="Logo"/>'ya düşen çıktı amblemi.</summary>
    public string ResolvedPrintLogo =>
        string.IsNullOrWhiteSpace(PrintLogo) ? Logo : PrintLogo;
}
