using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Mini.Application.Models;

/// <summary>
/// KURUM BİLGİLERİ — uygulamayı kullanan kurumun kimliği ve kurumsal kimliği.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden veritabanında?</b> Uygulama başka belediyelere verilecek ve açık
/// kaynak olacak. Kurum adı, amblem ve renkler koda yazılırsa her kurum için
/// ayrı bir kaynak dalı gerekir; dosyaya yazılırsa değiştirmek için sunucuya
/// erişmek ve uygulamayı yeniden başlatmak gerekir. Veritabanında durunca
/// yetkili kullanıcı arayüzden düzenler, değişiklik anında yayılır.
/// </para>
///
/// <para>
/// <b>TEK SATIRLIK tablo.</b> <see cref="TekilId"/> sabit birincil anahtar;
/// ikinci bir satır açılmaz. Çok kurumluluk (aynı veritabanında birden çok
/// belediye) hedeflenmiyor — her kurumun kendi veritabanı var.
/// </para>
///
/// <para>
/// <b>Burada OLMAYANLAR ve sebebi:</b> veritabanı bağlantısı, JWT imza
/// anahtarı, SMS parolası, nesne deposu anahtarları ve uygulamanın genel
/// adresi <c>.env</c> dosyasında kalır. İkisi de mantıklı görünüyor ama biri
/// imkânsız: bu tabloyu okumak için önce veritabanına bağlanmak gerekiyor.
/// Sırların ayrıca veritabanı yedeğine düşmemesi de tercih sebebi.
/// </para>
///
/// <para>
/// <c>.env</c> yine de işe yarıyor: tablo BOŞKEN ilk satır oradaki değerlerle
/// açılır. Yani sıfırdan bir kurulum hâlâ "sadece .env doldur, çalıştır".
/// </para>
/// </remarks>
[Table("kurum_bilgileri")]
public class Institution
{
    /// <summary>Tek satırın sabit kimliği.</summary>
    public const long TekilId = 1;

    [Column("id")]
    public long Id { get; set; } = TekilId;

    // ── Kimlik ─────────────────────────────────────────────────────────

    /// <summary>Kurumun resmî adı. Örn. "Örnek Belediyesi".</summary>
    [Required]
    [MaxLength(200)]
    [Column("ad")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Dar alanlarda kullanılan kısa ad. Örn. "Örnek Bld.".</summary>
    [MaxLength(100)]
    [Column("kisa_ad")]
    public string? ShortName { get; set; }

    /// <summary>Çıktıların tepesinde basılan ad. Boşsa <see cref="Name"/>.</summary>
    [MaxLength(200)]
    [Column("gorunen_ad")]
    public string? DisplayName { get; set; }

    /// <summary>Uygulamayı işleten birim. Örn. "Başkanlık Makamı".</summary>
    [MaxLength(200)]
    [Column("birim")]
    public string? Department { get; set; }

    /// <summary>Alt bilgi/künye satırı — çıktıların ve giriş ekranının dibinde.</summary>
    [MaxLength(300)]
    [Column("kunye")]
    public string? FooterNote { get; set; }

    // ── İletişim ───────────────────────────────────────────────────────

    [MaxLength(300)]
    [Column("web_sitesi")]
    public string? Website { get; set; }

    [MaxLength(500)]
    [Column("adres")]
    public string? Address { get; set; }

    [MaxLength(50)]
    [Column("telefon")]
    public string? Phone { get; set; }

    [MaxLength(200)]
    [Column("eposta")]
    public string? Email { get; set; }

    // ── Uygulama kimliği ───────────────────────────────────────────────

    /// <summary>Uygulamanın adı. Örn. "Randevu Takip Sistemi".</summary>
    [MaxLength(200)]
    [Column("uygulama_adi")]
    public string? ApplicationName { get; set; }

    /// <summary>PWA ana ekran kısayolunda görünen kısa ad.</summary>
    [MaxLength(100)]
    [Column("uygulama_kisa_adi")]
    public string? ApplicationShortName { get; set; }

    /// <summary>Manifest açıklaması ve <c>meta description</c>.</summary>
    [MaxLength(500)]
    [Column("uygulama_aciklamasi")]
    public string? ApplicationDescription { get; set; }

    // ── Kurumsal kimlik ────────────────────────────────────────────────

    /// <summary>Birincil kurumsal renk (#RRGGBB). Tema motoru tonları bundan türetir.</summary>
    [MaxLength(20)]
    [Column("marka_birincil")]
    public string? BrandPrimary { get; set; }

    [MaxLength(20)]
    [Column("marka_vurgu")]
    public string? BrandAccent { get; set; }

    [MaxLength(20)]
    [Column("marka_notr")]
    public string? BrandNeutral { get; set; }

    /// <summary>Koyu temada birincil rengin okunabilir karşılığı.</summary>
    [MaxLength(20)]
    [Column("marka_birincil_koyu")]
    public string? BrandPrimaryDark { get; set; }

    // ── Görseller ──────────────────────────────────────────────────────

    /// <summary>Amblem yolu (<c>wwwroot</c> köküne göre ya da tam adres).</summary>
    [MaxLength(300)]
    [Column("amblem")]
    public string? Logo { get; set; }

    [MaxLength(300)]
    [Column("favicon")]
    public string? Favicon { get; set; }

    [MaxLength(300)]
    [Column("uygulama_ikonu")]
    public string? AppIcon { get; set; }

    /// <summary>Çıktılarda (PDF, isim kartı) kullanılan amblem. Boşsa <see cref="Logo"/>.</summary>
    [MaxLength(300)]
    [Column("cikti_amblemi")]
    public string? PrintLogo { get; set; }

    // ── Denetim ────────────────────────────────────────────────────────

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    [Column("guncelleme_tarihi")]
    public DateTime? GuncellemeTarihi { get; set; }

    /// <summary>Son düzenleyenin adı (metin) — sistemin geri kalanıyla aynı biçim.</summary>
    [MaxLength(200)]
    [Column("guncelleyen")]
    public string? Guncelleyen { get; set; }

    // ── Türetilenler ───────────────────────────────────────────────────

    /// <summary>Boşsa <see cref="Name"/>'e düşen görünen ad.</summary>
    [NotMapped]
    public string ResolvedDisplayName =>
        string.IsNullOrWhiteSpace(DisplayName) ? Name : DisplayName;

    /// <summary>Boşsa <see cref="Name"/>'e düşen kısa ad.</summary>
    [NotMapped]
    public string ResolvedShortName =>
        string.IsNullOrWhiteSpace(ShortName) ? Name : ShortName;

    /// <summary>Boşsa <see cref="Logo"/>'ya düşen çıktı amblemi.</summary>
    [NotMapped]
    public string? ResolvedPrintLogo =>
        string.IsNullOrWhiteSpace(PrintLogo) ? Logo : PrintLogo;
}
