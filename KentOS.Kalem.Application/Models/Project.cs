using KentOS.Kalem.Application.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models;

/// <summary>
/// PROJE — görevlerin çatısı.
/// </summary>
/// <remarks>
/// <para>
/// Proje iş YAPMAZ; işleri toplar. Bir projenin ilerlemesi kendi alanlarından
/// değil, altındaki görevlerden hesaplanıyor — projeye ayrı bir "yüzde
/// tamamlandı" kolonu koysaydık, görevlerle çelişebilen ikinci bir gerçek
/// doğardı ve hangisine bakılacağı belirsiz kalırdı.
/// </para>
/// <para>
/// <b>Görünürlük kapısı yine birim.</b> Görevlerdeki kuralın aynısı: proje
/// bir birime ait ve yalnızca o birim (ve üst birimleri) görür.
/// </para>
/// </remarks>
[Table("projeler")]
public class Project
{
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(300)]
    [Display(Name = "Proje")]
    [Column("ad")]
    public string Ad { get; set; } = string.Empty;

    /// <summary>
    /// Kısa kod (örn. <c>PRK-2026</c>) — yazışmada ve raporda kullanılır.
    /// </summary>
    /// <remarks>
    /// Tekil DEĞİL ve bilinçli: kurum kendi kodlama düzenini kurabilsin,
    /// benzersizlik zorlaması yüzünden kod alanını boş bırakmak zorunda
    /// kalmasın. Sistemin tekil kimliği <see cref="Id"/>.
    /// </remarks>
    [MaxLength(40)]
    [Display(Name = "Kod")]
    [Column("kod")]
    public string? Kod { get; set; }

    [Display(Name = "Açıklama")]
    [Column("aciklama", TypeName = "text")]
    public string? Aciklama { get; set; }

    [MaxLength(20)]
    [Display(Name = "Renk")]
    [Column("renk")]
    public string? Renk { get; set; }

    [Display(Name = "Durum")]
    [Column("durum")]
    public ProjeDurumu Durum { get; set; } = ProjeDurumu.Planlaniyor;

    /// <summary>GÖRÜNÜRLÜK KAPISI — projeyi yürüten birim.</summary>
    [Column("birim_id")]
    public long BirimId { get; set; }
    public Birim? Birim { get; set; }

    /// <summary>
    /// Proje yöneticisi (AspNetUsers.Id).
    /// </summary>
    /// <remarks>
    /// <see cref="ProjectMember"/> listesinde de <c>Yonetici</c> rolüyle
    /// duruyor ama burada AYRICA tutuluyor: "projenin sahibi kim?" sorusunun
    /// cevabı tek bir kişi olmalı ve üye listesinde birden çok yönetici
    /// bulunabilir. Liste kimlerin yetkili olduğunu, bu alan kimin hesap
    /// vereceğini söyler.
    /// </remarks>
    [Column("yonetici_id")]
    public long? YoneticiId { get; set; }

    [Display(Name = "Başlangıç")]
    [Column("baslangic")]
    public DateTime? Baslangic { get; set; }

    [Display(Name = "Bitiş")]
    [Column("bitis")]
    public DateTime? Bitis { get; set; }

    /// <summary>Gerçekleşen tamamlanma anı — durum <c>Tamamlandi</c> olunca damgalanır.</summary>
    [Display(Name = "Tamamlanma tarihi")]
    [Column("tamamlanma_tarihi")]
    public DateTime? TamamlanmaTarihi { get; set; }

    /// <summary>
    /// Bütçe. Yuvarlama hatası kabul edilemez, bu yüzden <c>numeric</c>.
    /// </summary>
    [Display(Name = "Bütçe")]
    [Column("butce", TypeName = "numeric(18,2)")]
    public decimal? Butce { get; set; }

    // ── konum ──────────────────────────────────────────────────────────

    /// <summary>
    /// Projenin merkezi — WGS84 noktası.
    /// </summary>
    /// <remarks>
    /// <c>WorkTask</c> ile aynı düzen: ham enlem/boylam kolonları burada,
    /// PostGIS geometrisi göç dosyasında üretiliyor. <c>Application</c>
    /// katmanı NuGet bağımlılığı taşımıyor.
    /// </remarks>
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

    // ── denetim ────────────────────────────────────────────────────────

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    [Column("guncelleme_tarihi")]
    public DateTime? GuncellemeTarihi { get; set; }

    [MaxLength(150)]
    [Column("olusturan")]
    public string? Olusturan { get; set; }

    [MaxLength(150)]
    [Column("guncelleyen")]
    public string? Guncelleyen { get; set; }

    public ICollection<ProjectMember> Uyeler { get; set; } = [];
    public ICollection<Milestone> KilometreTaslari { get; set; } = [];
    public ICollection<BoardColumn> PanoSutunlari { get; set; } = [];
}

/// <summary>Projenin katılımcısı.</summary>
/// <remarks>
/// <see cref="Team"/> ile karıştırılmıyor: ekip birimin KALICI yapısı, proje
/// üyeliği bir işin katılımcı listesi. Aynı kişi hem park bahçelerin budama
/// ekibinde hem üç ayrı projenin üyesi olabilir.
/// </remarks>
[Table("proje_uyeleri")]
public class ProjectMember
{
    [Column("id")]
    public long Id { get; set; }

    [Column("proje_id")]
    public long ProjeId { get; set; }
    public Project? Proje { get; set; }

    [Column("kullanici_id")]
    public long KullaniciId { get; set; }

    [Display(Name = "Rol")]
    [Column("rol")]
    public ProjeUyeRolu Rol { get; set; } = ProjeUyeRolu.Uye;

    [Column("eklenme_tarihi")]
    public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
}

/// <summary>
/// KİLOMETRE TAŞI — projenin ara hedefi.
/// </summary>
/// <remarks>
/// <para>
/// Görev DEĞİL: kilometre taşının aşaması, ataması ve SLA'sı yok. Bir tarih
/// ve o tarihe kadar bitmesi beklenen işler kümesi. Görevler
/// <c>gorevler.kilometre_tasi_id</c> ile buraya bağlanıyor.
/// </para>
/// <para>
/// <b>Tamamlanma elle işaretlenir.</b> "Bağlı görevlerin hepsi bitince
/// kendiliğinden tamamlansın" denebilirdi ama o zaman hiç görev bağlanmamış
/// bir kilometre taşı açılır açılmaz tamamlanmış görünürdü.
/// </para>
/// </remarks>
[Table("kilometre_taslari")]
public class Milestone
{
    [Column("id")]
    public long Id { get; set; }

    [Column("proje_id")]
    public long ProjeId { get; set; }
    public Project? Proje { get; set; }

    [Required]
    [MaxLength(300)]
    [Display(Name = "Kilometre taşı")]
    [Column("ad")]
    public string Ad { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Display(Name = "Açıklama")]
    [Column("aciklama")]
    public string? Aciklama { get; set; }

    [Display(Name = "Hedef tarih")]
    [Column("hedef_tarih")]
    public DateTime? HedefTarih { get; set; }

    [Display(Name = "Tamamlandı")]
    [Column("tamamlandi")]
    public bool Tamamlandi { get; set; }

    [Display(Name = "Tamamlanma tarihi")]
    [Column("tamamlanma_tarihi")]
    public DateTime? TamamlanmaTarihi { get; set; }

    [Display(Name = "Sıra")]
    [Column("sira_no")]
    public int SiraNo { get; set; }
}

/// <summary>
/// KANBAN SÜTUNU — panonun bir kolonu.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sütun bir GÖREV DURUMUNA eşlenir, ayrı bir durum kaynağı olmaz.</b>
/// Kartı sürükleyip bırakmak görevin durumunu değiştirir. Sütuna kendi
/// durumu verilseydi "panoda Tamamlandı ama listede Devam Ediyor" çelişkisi
/// doğardı ve hangisinin doğru olduğu belirsiz kalırdı.
/// </para>
/// <para>
/// Aynı duruma birden çok sütun eşlenebilir (örn. "Saha" ve "Atölye" ikisi de
/// <c>DevamEdiyor</c>): pano kurumun kendi iş bölümünü gösterebilsin diye.
/// Bu durumda sürükleme durumu değiştirmez, yalnızca kartı taşır.
/// </para>
/// </remarks>
[Table("pano_sutunlari")]
public class BoardColumn
{
    [Column("id")]
    public long Id { get; set; }

    [Column("proje_id")]
    public long ProjeId { get; set; }
    public Project? Proje { get; set; }

    [Required]
    [MaxLength(100)]
    [Display(Name = "Sütun")]
    [Column("ad")]
    public string Ad { get; set; } = string.Empty;

    [Display(Name = "Sıra")]
    [Column("sira_no")]
    public int SiraNo { get; set; }

    [MaxLength(20)]
    [Display(Name = "Renk")]
    [Column("renk")]
    public string? Renk { get; set; }

    /// <summary>Bu sütuna düşen kartın alacağı görev durumu.</summary>
    [Display(Name = "Görev durumu")]
    [Column("gorev_durumu")]
    public GorevDurumu GorevDurumu { get; set; }
}
