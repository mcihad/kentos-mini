namespace KentOS.Mini.Web.Filters;

/// <summary>
/// <c>ProblemDetails.type</c> bağlantıları.
/// </summary>
/// <remarks>
/// <para>
/// Bu adresler daha önce her hata noktasına <b>kurumun alan adıyla birlikte</b>
/// elle yazılıyordu (<c>https://randevu.…/hatalar/kimlik</c>). Uygulama başka
/// kurumlara verileceği için koda alan adı yazılamaz; taban artık
/// <c>APP__BASEURL</c> ayarından geliyor.
/// </para>
/// <para>
/// <b>Neden statik?</b> Bu değer bir istek boyunca değişmiyor ve hata
/// filtrelerinin çoğu <c>IServiceProvider</c> görmeyen yollardan çağrılıyor.
/// Açılışta bir kez kurulur, sonra salt okunur.
/// </para>
/// <para>
/// <b>İstemciler bu alanı okumuyor</b> — SPA hatayı <c>title</c>/<c>detail</c>
/// üzerinden gösteriyor, v1 mobil sözleşmesi ProblemDetails hiç kullanmıyor.
/// Dolayısıyla taban değiştiğinde kırılan bir şey yok; alan tanı amaçlı.
/// </para>
/// </remarks>
public static class HataTurleri
{
    private static string _taban = string.Empty;

    /// <summary>Açılışta bir kez çağrılır.</summary>
    public static void Kur(string? tabanAdres) =>
        _taban = string.IsNullOrWhiteSpace(tabanAdres) ? string.Empty : tabanAdres.TrimEnd('/');

    /// <summary>
    /// Verilen kod için tam adres. Taban kurulmamışsa GÖRELİ yol döner —
    /// RFC 7807 buna izin veriyor ve uydurma bir alan adı yazmaktan iyidir.
    /// </summary>
    public static string Olustur(string kod) => $"{_taban}/hatalar/{kod}";

    public static string Kimlik => Olustur("kimlik");
    public static string Dogrulama => Olustur("dogrulama");
    public static string Bulunamadi => Olustur("bulunamadi");
    public static string IsKurali => Olustur("is-kurali");
    public static string Yetkisiz => Olustur("yetkisiz");
    public static string Sunucu => Olustur("sunucu");
}
