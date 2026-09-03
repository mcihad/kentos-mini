using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Web.Data;

namespace KentOS.Kalem.Web.Services.V2;

public interface IWebBildirimServisi
{
    Task JetonKaydetAsync(long kullaniciId, string jeton);
    Task JetonSilAsync(long kullaniciId, string jeton);

    /// <summary>Mobil (uygulama) push jetonu — <c>AppUser.FcmToken</c>.</summary>
    /// <remarks>
    /// Web jetonundan AYRI sütun: bir kullanıcının hem telefonu hem tarayıcısı
    /// bildirim alabilmeli. v1'in <c>GET /api/SettingsApi/UpdateFcmToken</c>
    /// ucu da aynı sütuna yazar; ikisi bir arada çalışır.
    /// </remarks>
    Task MobilJetonKaydetAsync(long kullaniciId, string jeton);

    Task MobilJetonSilAsync(long kullaniciId, string jeton);
}

/// <summary>
/// Tarayıcı (SPA) push jetonunun kaydı ve temizliği.
/// </summary>
public class WebBildirimServisi(AppDbContext _context, ILogger<WebBildirimServisi> _logger)
    : IWebBildirimServisi
{
    /// <summary>
    /// Jetonu bu kullanıcıya bağlar.
    ///
    /// <para>
    /// <b>Önce jetonu başka kullanıcılardan söker.</b> Bu adım güvenlik
    /// açısından zorunlu: FCM web jetonu tarayıcı profiline bağlıdır,
    /// kullanıcıya değil. Ortak bir bilgisayarda A çıkış yapmadan B giriş
    /// yaparsa <c>getToken()</c> aynı jetonu döndürür; bu satır olmasaydı
    /// jeton iki kullanıcıda birden kalır ve <b>A'nın gizli etkinlik
    /// bildirimleri B'nin ekranına düşerdi</b>.
    /// </para>
    /// </summary>
    public async Task JetonKaydetAsync(long kullaniciId, string jeton)
    {
        var sokulen = await _context.Users
            .Where(u => u.WebFcmToken == jeton && u.Id != kullaniciId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.WebFcmToken, (string?)null));

        if (sokulen > 0)
        {
            _logger.LogInformation(
                "Web push jetonu {Adet} başka kullanıcıdan alındı ve {KullaniciId} kullanıcısına bağlandı.",
                sokulen, kullaniciId);
        }

        await _context.Users
            .Where(u => u.Id == kullaniciId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.WebFcmToken, jeton));
    }

    /// <summary>
    /// Çıkışta jetonu temizler.
    ///
    /// <para>
    /// Eşleşme kontrollü: yalnızca kullanıcının ÜZERİNDEKİ jeton buysa siler.
    /// Aksi hâlde eski bir sekmeden gelen gecikmiş bir çıkış isteği, yeni
    /// kaydedilmiş geçerli bir jetonu silebilirdi.
    /// </para>
    /// </summary>
    public async Task JetonSilAsync(long kullaniciId, string jeton)
    {
        await _context.Users
            .Where(u => u.Id == kullaniciId && u.WebFcmToken == jeton)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.WebFcmToken, (string?)null));
    }

    // ───────────────────────────────────────────────────────── mobil

    /// <summary>
    /// Mobil push jetonunu bu kullanıcıya bağlar.
    /// </summary>
    /// <remarks>
    /// Web tarafındaki "geri çalma" adımı burada da uygulanır: FCM jetonu
    /// CİHAZA bağlıdır, kullanıcıya değil. Ortak bir telefonda A çıkış
    /// yapmadan B giriş yaparsa jeton iki kullanıcıda birden kalır ve
    /// <b>A'nın gizli etkinlik bildirimleri B'nin telefonuna düşer</b>.
    /// v1'in ucu bu adımı atlıyor; v2 atlamıyor.
    /// </remarks>
    public async Task MobilJetonKaydetAsync(long kullaniciId, string jeton)
    {
        var sokulen = await _context.Users
            .Where(u => u.FcmToken == jeton && u.Id != kullaniciId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.FcmToken, (string?)null));

        if (sokulen > 0)
        {
            _logger.LogInformation(
                "Mobil push jetonu {Adet} başka kullanıcıdan alındı ve {KullaniciId} kullanıcısına bağlandı.",
                sokulen, kullaniciId);
        }

        await _context.Users
            .Where(u => u.Id == kullaniciId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.FcmToken, jeton));
    }

    /// <summary>Çıkışta mobil jetonu temizler — eşleşme kontrollü.</summary>
    public async Task MobilJetonSilAsync(long kullaniciId, string jeton)
    {
        await _context.Users
            .Where(u => u.Id == kullaniciId && u.FcmToken == jeton)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.FcmToken, (string?)null));
    }
}
