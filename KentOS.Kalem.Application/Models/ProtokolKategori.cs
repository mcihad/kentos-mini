using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models;

/// <summary>
/// Protokol kategorisi — "Mülki İdare", "Yerel Yönetim", "Adli Teşkilat"…
/// </summary>
/// <remarks>
/// <para>
/// Başlangıçta kategori <see cref="Protokol"/> üzerinde serbest METİNDİ.
/// Sonuç: aynı kategori "Mülki İdare", "Mülki idare", "MÜLKİ İDARE" diye üç
/// ayrı grup üretiyor, liste bölünüyordu. Kategoriyi tabloya almak yazımı tek
/// noktada birleştiriyor ve sıralamayı da kayıt altına alıyor.
/// </para>
/// <para>
/// <see cref="SiraNo"/> kategorilerin kendi arasındaki sırası: protokol
/// listesinde mülki idare yerel yönetimden önce gelir ve bu alfabetik değil,
/// teamüle bağlı bir sıradır.
/// </para>
/// </remarks>
[Table("protokol_kategorileri")]
public class ProtokolKategori
{
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("ad")]
    public string Ad { get; set; } = string.Empty;

    /// <summary>Kategoriler arası sıra — küçük numara üstte.</summary>
    [Column("sira_no")]
    public int SiraNo { get; set; }

    /// <summary>Pasif kategori yeni kayıtlarda seçilemez, mevcutlar korunur.</summary>
    [Column("aktif")]
    public bool Aktif { get; set; } = true;

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    public ICollection<Protokol> Protokoller { get; set; } = [];
}
