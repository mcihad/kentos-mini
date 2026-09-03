using KentOS.Kalem.Application.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models;

/// <summary>
/// İş takip modülünün ORTAK yorum tablosu — <b>iç içe</b>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="WorkAttachment"/> ile aynı gerekçe: beş varlık için beş ayrı
/// yorum tablosu yerine tek tablo, tek servis, tek ön yüz bileşeni.
/// Bağ <c>(VarlikTuru, VarlikId)</c>.
/// </para>
/// <para>
/// <b>İç içe yorum</b> <see cref="UstYorumId"/> ile. Tek seviye zorlanmıyor;
/// derinliği ön yüz sınırlıyor (girinti 3. seviyeden sonra okunmaz hâle
/// geliyor ve 390px'te yatay taşma üretiyor).
/// </para>
/// <para>
/// <b>Silme YUMUŞAK.</b> Sert silme, altındaki yanıtları yetim bırakır ve
/// konuşmanın ortasında boşluk açar — okuyan kişi neye cevap verildiğini
/// anlayamaz. Silinen yorumun metni boşaltılır, iskeleti "bu yorum silindi"
/// olarak kalır.
/// </para>
/// </remarks>
[Table("is_yorumlari")]
public class WorkComment
{
    [Column("id")]
    public long Id { get; set; }

    [Column("varlik_turu")]
    public IsVarligi VarlikTuru { get; set; }

    /// <summary>Yabancı anahtar DEĞİL — bkz. <see cref="WorkAttachment"/> notu.</summary>
    [Column("varlik_id")]
    public long VarlikId { get; set; }

    /// <summary>Yanıtlanan yorum. Kök yorumlarda <c>null</c>.</summary>
    [Column("ust_yorum_id")]
    public long? UstYorumId { get; set; }
    public WorkComment? UstYorum { get; set; }
    public ICollection<WorkComment> Yanitlar { get; set; } = [];

    [Required]
    [Display(Name = "Yorum")]
    [Column("metin", TypeName = "text")]
    public string Metin { get; set; } = string.Empty;

    /// <summary>Yazan kullanıcının SAYISAL kimliği — "benim yorumum mu" için.</summary>
    [Column("yazan_id")]
    public long? YazanId { get; set; }

    /// <summary>Yazanın görünen adı — kullanıcı silinse de yorum okunabilir kalsın.</summary>
    [MaxLength(150)]
    [Column("yazan")]
    public string? Yazan { get; set; }

    [Column("silindi")]
    public bool Silindi { get; set; }

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    [Column("guncelleme_tarihi")]
    public DateTime? GuncellemeTarihi { get; set; }
}
