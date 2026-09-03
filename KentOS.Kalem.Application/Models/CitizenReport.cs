using KentOS.Kalem.Application.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models;

/// <summary>
/// VATANDAŞ BİLDİRİMİ — dışarıdan gelen ilk kayıt.
/// </summary>
/// <remarks>
/// <para>
/// Görev DEĞİL ve bilinçli olarak öyle: gelen her bildirim bir iş değil.
/// Kayıt önce <b>karşılama</b> ekranına düşüyor, bir personel okuyup ilgili
/// birime yönlendiriyor ve ANCAK O ZAMAN görev doğuyor. Doğrudan görev
/// açsaydık, mükerrer ve konusuz bildirimler birimlerin iş listesini
/// kullanılamaz hâle getirirdi.
/// </para>
/// <para>
/// <b>Kişisel veri taşıyor</b> (ad, telefon, konum, fotoğraf). Fotoğraflar
/// <c>StorageArea.Private</c>'a yazılıyor: <c>wwwroot/uploads</c> kimlik
/// doğrulamadan servis ediliyor ve bir vatandaşın evinin önünü gösteren
/// fotoğrafın bağlantısı tahmin edilebilir olmamalı.
/// </para>
/// </remarks>
[Table("vatandas_bildirimleri")]
public class CitizenReport
{
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// Vatandaşa verilen takip numarası (örn. <c>VB-2026-000142</c>).
    /// </summary>
    /// <remarks>
    /// Yanıtta dönen TEK iç bilgi bu. Görev kimliği, birim ya da personel adı
    /// dışarıya sızmıyor — bildirim sahibinin bilmesi gereken tek şey kaydının
    /// alındığı.
    /// </remarks>
    [Required]
    [MaxLength(30)]
    [Display(Name = "Takip numarası")]
    [Column("takip_no")]
    public string TakipNo { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Display(Name = "Ad soyad")]
    [Column("ad_soyad")]
    public string AdSoyad { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Display(Name = "Telefon")]
    [Column("telefon")]
    public string Telefon { get; set; } = string.Empty;

    /// <summary>
    /// Yalnızca rakamlardan oluşan hâli — arama ve hız sınırı için.
    /// </summary>
    /// <remarks>
    /// Vatandaş numarayı beş farklı biçimde yazıyor (<c>0532 123 45 67</c>,
    /// <c>+90532...</c>, <c>532-123-4567</c>). Ham metinle eşleştirme
    /// yapılsaydı aynı kişi her biçimde yeniden hız sınırı hakkı kazanırdı.
    /// </remarks>
    [Required]
    [MaxLength(20)]
    [Column("telefon_sade")]
    public string TelefonSade { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    [Display(Name = "Konu")]
    [Column("konu")]
    public string Konu { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Açıklama")]
    [Column("aciklama", TypeName = "text")]
    public string Aciklama { get; set; } = string.Empty;

    // ── konum ──────────────────────────────────────────────────────────

    [Display(Name = "Enlem")]
    [Column("enlem")]
    public double? Enlem { get; set; }

    [Display(Name = "Boylam")]
    [Column("boylam")]
    public double? Boylam { get; set; }

    [MaxLength(500)]
    [Display(Name = "Adres")]
    [Column("adres")]
    public string? Adres { get; set; }

    [Column("mahalle_id")]
    public long? MahalleId { get; set; }
    public Mahalle? Mahalle { get; set; }

    // ── akış ───────────────────────────────────────────────────────────

    [Display(Name = "Durum")]
    [Column("durum")]
    public VatandasBildirimDurumu Durum { get; set; } = VatandasBildirimDurumu.Yeni;

    /// <summary>Yönlendirildiği birim. Karşılama personeli seçer.</summary>
    [Column("birim_id")]
    public long? BirimId { get; set; }
    public Birim? Birim { get; set; }

    /// <summary>Bu bildirimden doğan görev.</summary>
    [Column("gorev_id")]
    public long? GorevId { get; set; }

    /// <summary>Reddedilme ya da yönlendirme gerekçesi.</summary>
    [MaxLength(1000)]
    [Display(Name = "Not")]
    [Column("islem_notu")]
    public string? IslemNotu { get; set; }

    [MaxLength(150)]
    [Column("isleyen")]
    public string? Isleyen { get; set; }

    [Column("islem_tarihi")]
    public DateTime? IslemTarihi { get; set; }

    // ── denetim ────────────────────────────────────────────────────────

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    /// <summary>
    /// Kaydın geldiği IP.
    /// </summary>
    /// <remarks>
    /// Kötüye kullanım araştırması için. Hız sınırı zaten IP başına
    /// çalışıyor ama sınırı aşmadan sistematik sahte bildirim gönderen bir
    /// kaynağı sonradan bulmanın başka yolu yok.
    /// </remarks>
    [MaxLength(64)]
    [Column("ip")]
    public string? Ip { get; set; }
}

/// <summary>
/// TELEFON DOĞRULAMASI — tek kullanımlık kod.
/// </summary>
/// <remarks>
/// <para>
/// <b>Kod açık saklanmıyor.</b> Veritabanına erişen biri bekleyen bütün
/// doğrulama kodlarını okuyabilseydi, telefon doğrulaması bir güvenlik
/// önlemi değil bir formalite olurdu.
/// </para>
/// <para>
/// Deneme sayısı sınırlı ve süre kısa: dört haneli bir kodu deneyerek bulmak
/// sınırsız denemede saniyeler sürer.
/// </para>
/// </remarks>
[Table("telefon_dogrulamalari")]
public class PhoneVerification
{
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("telefon_sade")]
    public string TelefonSade { get; set; } = string.Empty;

    /// <summary>Kodun karması — <c>SHA256(telefon + ":" + kod)</c>.</summary>
    /// <remarks>
    /// Telefon karmaya DAHİL: yalnızca kodu karmalasaydık, dört haneli bütün
    /// kodların karması önceden hesaplanabilirdi ve karma açık metinden daha
    /// güvenli olmazdı.
    /// </remarks>
    [Required]
    [MaxLength(128)]
    [Column("kod_karmasi")]
    public string KodKarmasi { get; set; } = string.Empty;

    [Column("gecerlilik")]
    public DateTime Gecerlilik { get; set; }

    [Column("deneme")]
    public int Deneme { get; set; }

    /// <summary>Doğrulandı mı? Bir kod bir kez kullanılır.</summary>
    [Column("dogrulandi")]
    public bool Dogrulandi { get; set; }

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    [MaxLength(64)]
    [Column("ip")]
    public string? Ip { get; set; }
}
