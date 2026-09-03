using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KentOS.Kalem.Application.Enums;

namespace KentOS.Kalem.Application.Models;

/// <summary>
/// Davet listesi — bir tören/etkinlik için protokolden seçilen kişiler.
/// </summary>
/// <remarks>
/// <para>
/// Protokol listesi <b>kimin nerede durduğunu</b> söyler; davet listesi
/// <b>kimin çağrıldığını ve ne cevap verdiğini</b> takip eder. İkisi ayrı:
/// aynı protokol kaydı onlarca davete girer, her davette farklı bir cevap
/// verir.
/// </para>
/// <para>
/// Bir etkinliğe (<see cref="Ajanda"/>) bağlanabilir ama <b>zorunlu
/// değildir</b>: davet çoğu zaman etkinlik ajandaya girmeden önce
/// hazırlanmaya başlıyor.
/// </para>
/// </remarks>
[Table("davetler")]
public class Davet
{
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("baslik")]
    public string Baslik { get; set; } = string.Empty;

    /// <summary>Törenin/etkinliğin tarihi — çıktının başlığında yazar.</summary>
    [Column("tarih")]
    public DateTime? Tarih { get; set; }

    [MaxLength(200)]
    [Column("yer")]
    public string? Yer { get; set; }

    [MaxLength(1000)]
    [Column("aciklama")]
    public string? Aciklama { get; set; }

    /// <summary>İlgili etkinlik — bağlanmışsa.</summary>
    [Column("ajanda_id")]
    public long? AjandaId { get; set; }
    public Ajanda? Ajanda { get; set; }

    /// <summary>Daveti hazırlayan birim — listeler birime göre süzülür.</summary>
    [Column("birim_id")]
    public long? BirimId { get; set; }
    public Birim? Birim { get; set; }

    /// <summary>Oluşturan kullanıcının ADI (Ajanda.KullaniciId ile aynı biçim).</summary>
    [MaxLength(100)]
    [Column("kullanici_id")]
    public string? KullaniciId { get; set; }

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    [Column("guncelleme_tarihi")]
    public DateTime? GuncellemeTarihi { get; set; }

    public ICollection<DavetKisi> Kisiler { get; set; } = [];
}

/// <summary>
/// Davet listesindeki tek kişi ve ona dair takip bilgisi.
/// </summary>
/// <remarks>
/// <para>
/// <b>Arama/mesaj ile SONUÇ ayrı tutulur.</b> "Arandı" bir eylem, "katılacak"
/// bir cevaptır; tek bir enum'a sıkıştırılsaydı "arandı ama henüz cevap yok"
/// ile "hiç aranmadı" ayırt edilemezdi — oysa listeyi takip edenin ilk
/// sorusu tam olarak bu.
/// </para>
/// <para>
/// Kişi bilgisi protokol kaydından OKUNUR, kopyalanmaz: unvan değişirse
/// davet listesi de güncel kalır.
/// </para>
/// </remarks>
[Table("davet_kisileri")]
public class DavetKisi
{
    [Column("id")]
    public long Id { get; set; }

    [Column("davet_id")]
    public long DavetId { get; set; }
    public Davet? Davet { get; set; }

    [Column("protokol_id")]
    public long ProtokolId { get; set; }
    public Protokol? Protokol { get; set; }

    /// <summary>Cevap durumu.</summary>
    [Column("durum")]
    public DavetDurumu Durum { get; set; } = DavetDurumu.Beklemede;

    [Column("arandi")]
    public bool Arandi { get; set; }
    [Column("arandi_tarihi")]
    public DateTime? ArandiTarihi { get; set; }

    [Column("mesaj_gonderildi")]
    public bool MesajGonderildi { get; set; }
    [Column("mesaj_tarihi")]
    public DateTime? MesajTarihi { get; set; }

    /// <summary>Serbest not — "yurt dışında", "sekreterine bırakıldı" gibi.</summary>
    [MaxLength(500)]
    [Column("not")]
    public string? Not { get; set; }

    /// <summary>Listedeki sıra; verilmezse protokol sırası kullanılır.</summary>
    [Column("sira_no")]
    public int SiraNo { get; set; }

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    [Column("guncelleme_tarihi")]
    public DateTime? GuncellemeTarihi { get; set; }
}
