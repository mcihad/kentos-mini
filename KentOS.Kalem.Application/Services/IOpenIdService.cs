using KentOS.Kalem.Application.Dto.V2.OpenId;

namespace KentOS.Kalem.Application.Services;

/// <summary>
/// Kurumsal kimlik sağlayıcı (OpenID Connect) ayarları ve giriş akışı.
/// </summary>
public interface IOpenIdService
{
    /// <summary>Yetkilinin gördüğü ayar; istemci sırrı taşımaz.</summary>
    Task<OpenIdAyarDto> AyarAsync();

    /// <summary>Ayarı kaydeder. Boş istemci sırrı "değiştirme" demektir.</summary>
    Task<OpenIdAyarDto> KaydetAsync(OpenIdAyarIstegi istek);

    /// <summary>
    /// Giriş ekranının sorusu: düğme çizilsin mi?
    /// </summary>
    /// <remarks>
    /// <b>Anonim</b> çağrılır ve yalnızca "kullanılabilir mi" + düğme metni
    /// döner. Ayarın kendisi (yetkili adres, istemci kimliği) buradan
    /// sızmaz.
    /// </remarks>
    Task<OpenIdGirisDto> GirisDurumuAsync();

    /// <summary>Sağlayıcıya gerçekten ulaşılıyor mu — ayar ekranındaki sınama.</summary>
    Task<OpenIdSinamaDto> SinaAsync();

    /// <summary>
    /// Yetkilendirme adresini kurar (state + nonce üretir).
    /// </summary>
    /// <returns>Kullanıcının yönlendirileceği tam adres.</returns>
    Task<string> YetkilendirmeAdresiAsync(string? donusYolu);

    /// <summary>
    /// Sağlayıcıdan dönen kodu jetona çevirir.
    /// </summary>
    /// <returns>Uygulamanın kendi JWT'si, geçerlilik sonu ve dönülecek yol.</returns>
    Task<(string Jeton, DateTime? GecerlilikSonu, string DonusYolu)> GeriDonusAsync(
        string kod, string durum);
}
