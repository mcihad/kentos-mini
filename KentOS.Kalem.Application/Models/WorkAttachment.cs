using KentOS.Kalem.Application.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models;

/// <summary>
/// İş takip modülünün ORTAK ek (dosya/resim) tablosu.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden çok biçimli (polymorphic)?</b> Proje, görev, aşama, kilometre
/// taşı ve vatandaş bildiriminin her biri için ayrı bir dosya tablosu
/// açsaydık beş tablo, beş servis yolu, beş yükleme ucu ve beş ayrı ön yüz
/// bileşeni olurdu — hepsi aynı işi yapan. Bağ <c>(VarlikTuru, VarlikId)</c>
/// ikilisiyle kuruluyor.
/// </para>
/// <para>
/// <b>Bedeli açıkça kabul ediliyor:</b> yabancı anahtar YOK, dolayısıyla
/// veritabanı bütünlüğü koruma altında değil ve <c>CASCADE</c> silme
/// çalışmıyor. Varlık silinirken ekleri servis katmanında temizlenir
/// (<c>IEkServisi.VarligaAitleriSilAsync</c>). Bu, evin geri kalanındaki
/// kuraldan (her modül kendi tablosu) bilinçli bir sapmadır ve YALNIZCA bu
/// modülle sınırlıdır.
/// </para>
/// <para>
/// Dosyanın kendisi <c>IFileStorage</c> üzerinde durur; burada saklanan
/// <see cref="DosyaYolu"/> depo anahtarının ta kendisidir — yerel diskten
/// nesne deposuna geçişte tek bir kayıt değişmesin diye.
/// </para>
/// </remarks>
[Table("is_ekleri")]
public class WorkAttachment
{
    [Column("id")]
    public long Id { get; set; }

    /// <summary>Hangi tür kayda ait.</summary>
    [Column("varlik_turu")]
    public IsVarligi VarlikTuru { get; set; }

    /// <summary>O kaydın kimliği. Yabancı anahtar DEĞİL — bkz. sınıf notu.</summary>
    [Column("varlik_id")]
    public long VarlikId { get; set; }

    /// <summary>Kullanıcının gördüğü ad — yüklerken verdiği dosya adı.</summary>
    [Required]
    [MaxLength(260)]
    [Display(Name = "Dosya adı")]
    [Column("ad")]
    public string Ad { get; set; } = string.Empty;

    /// <summary>
    /// Depodaki anahtar (örn. <c>uploads/is/8f3e….jpg</c>).
    /// </summary>
    /// <remarks>
    /// Sunucuda ÜRETİLİR; istemciden gelen ad hiç kullanılmaz, böylece
    /// <c>../</c> içeren bir ad dizin dışına yazamaz.
    /// </remarks>
    [Required]
    [MaxLength(400)]
    [Column("dosya_yolu")]
    public string DosyaYolu { get; set; } = string.Empty;

    [MaxLength(150)]
    [Column("icerik_turu")]
    public string? IcerikTuru { get; set; }

    [Display(Name = "Boyut")]
    [Column("boyut")]
    public long Boyut { get; set; }

    /// <summary>Resim mi? Görüntüleyicide açılıp açılmayacağını belirler.</summary>
    [Column("resim_mi")]
    public bool ResimMi { get; set; }

    /// <summary>İsteğe bağlı açıklama — "hasarın yakın çekimi" gibi.</summary>
    [MaxLength(500)]
    [Display(Name = "Açıklama")]
    [Column("aciklama")]
    public string? Aciklama { get; set; }

    [MaxLength(150)]
    [Column("yukleyen")]
    public string? Yukleyen { get; set; }

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
}
