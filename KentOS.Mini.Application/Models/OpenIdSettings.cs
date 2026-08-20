using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Mini.Application.Models;

/// <summary>
/// KURUMSAL KİMLİK SAĞLAYICI (OpenID Connect) ayarları — tek satır.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden veritabanında, <c>.env</c>'de değil.</b> Kurulum kuralı şu:
/// okumak için zaten veritabanına bağlanmak gereken şeyler <c>.env</c>'de,
/// yetkilinin arayüzden değiştirmesi gereken şeyler veritabanında. Kimlik
/// sağlayıcı ikincisi — kurum Keycloak'tan Azure AD'ye geçtiğinde ya da
/// istemci sırrı döndüğünde sunucuya girip dosya düzenlemek ve uygulamayı
/// yeniden başlatmak gerekmemeli.
/// </para>
/// <para>
/// <b>İstemci sırrı dışarı VERİLMEZ.</b> Okuma ucu onu maskeler; yazma
/// ucunda boş bırakmak "değiştirme" demektir. Aksi hâlde ayar ekranını her
/// açıp kaydeden kişi sırrı yanlışlıkla siler.
/// </para>
/// </remarks>
[Table("openid_ayarlari")]
public class OpenIdSettings
{
    /// <summary>Tek satır; kurum kaydıyla aynı desen.</summary>
    public const long TekilId = 1;

    [Column("id")]
    public long Id { get; set; } = TekilId;

    /// <summary>
    /// Sağlayıcı ile giriş açık mı?
    /// </summary>
    /// <remarks>
    /// Kapalıyken giriş ekranında düğme çıkmaz ve uçlar 404 döner — yarım
    /// yapılandırılmış bir sağlayıcıya yönlendirmek, kullanıcıyı geri
    /// dönemeyeceği bir sayfada bırakıyor.
    /// </remarks>
    [Column("etkin")]
    public bool Etkin { get; set; }

    /// <summary>
    /// Giriş düğmesinde yazan metin — "<c>{0}</c> ile giriş yap".
    /// </summary>
    /// <remarks>
    /// Kuruma göre değişiyor: "Kurum Hesabı", "e-Devlet", "Azure AD".
    /// Koda yazılamaz.
    /// </remarks>
    [Column("gorunen_ad")]
    public string? GorunenAd { get; set; }

    /// <summary>
    /// Sağlayıcının kök adresi (<c>authority</c>).
    /// </summary>
    /// <remarks>
    /// Keşif belgesi buradan türetilir:
    /// <c>{Yetkili}/.well-known/openid-configuration</c>. Uç adreslerini tek
    /// tek yazdırmak, sağlayıcı bir adresini değiştirdiğinde sessizce
    /// bozulan bir yapılandırma demekti.
    /// </remarks>
    [Column("yetkili")]
    public string? Yetkili { get; set; }

    [Column("istemci_id")]
    public string? IstemciId { get; set; }

    /// <summary>İstemci sırrı. Yanıtlarda ASLA dönmez.</summary>
    [Column("istemci_sirri")]
    public string? IstemciSirri { get; set; }

    /// <summary>
    /// İstenecek kapsamlar, boşlukla ayrılmış. Varsayılan
    /// <c>openid profile email</c>.
    /// </summary>
    [Column("kapsamlar")]
    public string? Kapsamlar { get; set; }

    /// <summary>
    /// Sağlayıcıdan gelen hangi talep, yerel kullanıcıyla eşleştirilecek.
    /// </summary>
    /// <remarks>
    /// Varsayılan <c>preferred_username</c>. Kurumlar farklı davranıyor:
    /// bazısı <c>upn</c>, bazısı <c>email</c> gönderiyor. Yanlış talep
    /// seçildiğinde giriş "kullanıcı bulunamadı" ile bitiyor ve sebebi
    /// ekrandan anlaşılmıyor — bu yüzden ayarlanabilir.
    /// </remarks>
    [Column("kullanici_adi_talebi")]
    public string? KullaniciAdiTalebi { get; set; }

    /// <summary>
    /// Sağlayıcıda olup burada olmayan kullanıcı otomatik açılsın mı?
    /// </summary>
    /// <remarks>
    /// <b>Varsayılan KAPALI ve bu bilinçli.</b> Açıksa, sağlayıcıda hesabı
    /// olan HERKES uygulamaya girebilir — kurumsal dizinde binlerce hesap
    /// var ve uygulamayı kullanması gereken kişi sayısı onlarca. Kapalıyken
    /// kullanıcı önce yönetimden tanımlanır, sağlayıcı yalnızca parolayı
    /// doğrular.
    /// </remarks>
    [Column("otomatik_kullanici_olustur")]
    public bool OtomatikKullaniciOlustur { get; set; }

    [Column("guncelleme_tarihi")]
    public DateTime? GuncellemeTarihi { get; set; }
}
