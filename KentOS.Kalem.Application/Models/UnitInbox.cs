using KentOS.Kalem.Application.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models;

/// <summary>
/// BİRİM GELEN KUTUSU — birimden birime iş devri.
/// </summary>
/// <remarks>
/// <para>
/// Bir görev tamamlandığında tipinde tanımlı devir kuralı tetikleniyor ve
/// hedef birimin gelen kutusuna bir kayıt düşüyor. Örnek: fen işleri yol
/// yamasını bitirir, park bahçelere "kaldırım kenarındaki çim ezildi"
/// düşer.
/// </para>
/// <para>
/// <b>Doğrudan görev açılmıyor.</b> Hedef birim kabul ederse görev doğuyor,
/// reddederse kaynağa gerekçeli bildirim gidiyor. Otomatik görev açsaydık
/// bir birim, başka bir birimin iş listesine sınırsız iş yazabilirdi ve
/// kimse "bunu kim uygun gördü?" sorusunu soramazdı.
/// </para>
/// </remarks>
[Table("birim_gelen_kutusu")]
public class UnitInbox
{
    [Column("id")]
    public long Id { get; set; }

    /// <summary>Kaydın düştüğü birim.</summary>
    [Column("hedef_birim_id")]
    public long HedefBirimId { get; set; }
    public Birim? HedefBirim { get; set; }

    /// <summary>Devri doğuran görev.</summary>
    [Column("kaynak_gorev_id")]
    public long KaynakGorevId { get; set; }

    /// <summary>Kaynak görevin birimi — ret bildirimi buraya gidiyor.</summary>
    [Column("kaynak_birim_id")]
    public long KaynakBirimId { get; set; }

    /// <summary>Uygulanan devir kuralı — iz sürmek için.</summary>
    [Column("gorev_tipi_devir_id")]
    public long? GorevTipiDevirId { get; set; }

    /// <summary>Kabul edilirse açılacak görevin tipi.</summary>
    [Column("hedef_gorev_tipi_id")]
    public long? HedefGorevTipiId { get; set; }

    [Required]
    [MaxLength(300)]
    [Display(Name = "Konu")]
    [Column("konu")]
    public string Konu { get; set; } = string.Empty;

    [Display(Name = "Açıklama")]
    [Column("aciklama", TypeName = "text")]
    public string? Aciklama { get; set; }

    /// <summary>
    /// İş TALEBİ mi yoksa yalnızca BİLGİ mi?
    /// </summary>
    /// <remarks>
    /// Bilgilendirme kaydı kabul/ret istemiyor; okundu işaretlenip kapanıyor.
    /// İkisini ayırmasaydık hedef birim her bilgilendirme için de karar
    /// vermek zorunda kalır ve gelen kutusu hızla kullanılamaz hâle gelirdi.
    /// </remarks>
    [Display(Name = "İş talebi")]
    [Column("is_talebi")]
    public bool IsTalebi { get; set; }

    [Display(Name = "Durum")]
    [Column("durum")]
    public GelenKutusuDurumu Durum { get; set; } = GelenKutusuDurumu.Bekliyor;

    /// <summary>Kabul edilince açılan görev.</summary>
    [Column("gorev_id")]
    public long? GorevId { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Gerekçe")]
    [Column("gerekce")]
    public string? Gerekce { get; set; }

    [MaxLength(150)]
    [Column("isleyen")]
    public string? Isleyen { get; set; }

    [Column("islem_tarihi")]
    public DateTime? IslemTarihi { get; set; }

    // ── konum ──────────────────────────────────────────────────────────
    //
    // Kaynak görevden KOPYALANIYOR: kabul eden birim aynı yere gidecek ve
    // koordinatı yeniden aramak zorunda kalmamalı.

    [Column("enlem")]
    public double? Enlem { get; set; }

    [Column("boylam")]
    public double? Boylam { get; set; }

    [MaxLength(500)]
    [Column("adres")]
    public string? Adres { get; set; }

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
}
