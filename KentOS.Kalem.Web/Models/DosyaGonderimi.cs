using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models;

/// <summary>
/// Kullanıcıdan kullanıcıya dosya gönderimi.
///
/// <para>
/// Sohbet DEĞİL: tek bir konu (bir dosya ve onun etrafındaki yazışma) üzerine
/// kurulu. Gönderen bir dosya ve not gönderir, alıcı aynı konu altında
/// cevaplayabilir. Yeni bir dosya göndermek yeni bir gönderim açar.
/// </para>
///
/// <para>
/// <b>Görünürlük:</b> bir gönderimi yalnızca <see cref="GonderenId"/> ve
/// <see cref="AliciId"/> görebilir. Rol bypass'ı YOKTUR — Admin bile
/// başkalarının gönderimlerini göremez. Gizli etkinlik kuralıyla aynı
/// felsefe.
/// </para>
/// </summary>
[Table("dosya_gonderimleri")]
public class DosyaGonderimi
{
    [Column("id")]
    public long Id { get; set; }

    [Column("gonderen_id")]
    public long GonderenId { get; set; }
    public AppUser? Gonderen { get; set; }

    [Column("alici_id")]
    public long AliciId { get; set; }
    public AppUser? Alici { get; set; }

    /// <summary>Konu başlığı — listede bunu görürler.</summary>
    [Column("konu")]
    public string Konu { get; set; } = string.Empty;

    /// <summary>Kullanıcının verdiği ad. Diskteki ad ayrı (GUID).</summary>
    [Column("dosya_adi")]
    public string DosyaAdi { get; set; } = string.Empty;

    /// <summary>`/uploads/gonderim/<guid>.<uzanti>` biçiminde göreli yol.</summary>
    [Column("dosya_yolu")]
    public string DosyaYolu { get; set; } = string.Empty;

    [Column("icerik_turu")]
    public string? IcerikTuru { get; set; }
    [Column("boyut")]
    public long Boyut { get; set; }

    /// <summary>Alıcı dosyayı ilk ne zaman gördü — okundu bilgisi.</summary>
    [Column("okunma_tarihi")]
    public DateTime? OkunmaTarihi { get; set; }

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    /// <summary>Konu altındaki notlar (ilk not gönderenindir).</summary>
    public ICollection<DosyaGonderimiNotu> Notlar { get; set; } = new List<DosyaGonderimiNotu>();
}

/// <summary>Bir gönderim konusuna eklenen not.</summary>
[Table("dosya_gonderimi_notlari")]
public class DosyaGonderimiNotu
{
    [Column("id")]
    public long Id { get; set; }

    [Column("gonderim_id")]
    public long GonderimId { get; set; }
    public DosyaGonderimi? Gonderim { get; set; }

    /// <summary>Notu yazan — gönderen ya da alıcı olabilir.</summary>
    [Column("yazan_id")]
    public long YazanId { get; set; }
    public AppUser? Yazan { get; set; }

    [Column("metin")]
    public string Metin { get; set; } = string.Empty;

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
}
