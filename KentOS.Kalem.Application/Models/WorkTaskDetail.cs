using KentOS.Kalem.Application.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models;

/// <summary>
/// Görevin bir AŞAMASI — tipten kopyalanmış örnek.
/// </summary>
/// <remarks>
/// <para>
/// Alanlar <see cref="TaskTypeStage"/> ile aynı görünüyor ve bu bilinçli bir
/// tekrar: aşama tanımı görev açılırken KOPYALANIYOR. Tanıma bağ kursaydık,
/// altı ay sonra tipe yeni bir adım eklendiğinde tamamlanmış görevler de
/// eksik görünürdü — bir işin kanıtı sonradan değişemez.
/// </para>
/// </remarks>
[Table("gorev_asamalari")]
public class WorkTaskStage
{
    [Column("id")]
    public long Id { get; set; }

    [Column("gorev_id")]
    public long GorevId { get; set; }
    public WorkTask? Gorev { get; set; }

    /// <summary>Kopyalandığı tanım — yalnızca iz sürmek için, davranışa etkisi yok.</summary>
    [Column("gorev_tipi_asama_id")]
    public long? GorevTipiAsamaId { get; set; }

    [Display(Name = "Sıra")]
    [Column("sira_no")]
    public int SiraNo { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Aşama")]
    [Column("ad")]
    public string Ad { get; set; } = string.Empty;

    [Display(Name = "Durum")]
    [Column("durum")]
    public GorevAsamaDurumu Durum { get; set; } = GorevAsamaDurumu.Bekliyor;

    [Display(Name = "Zorunlu")]
    [Column("zorunlu")]
    public bool Zorunlu { get; set; } = true;

    [Display(Name = "Açıklama zorunlu")]
    [Column("aciklama_zorunlu")]
    public bool AciklamaZorunlu { get; set; }

    [Display(Name = "Fotoğraf zorunlu")]
    [Column("fotograf_zorunlu")]
    public bool FotografZorunlu { get; set; }

    /// <summary>Sahada yazılan not — kanıtın metin kısmı.</summary>
    [MaxLength(2000)]
    [Display(Name = "Not")]
    [Column("not", TypeName = "text")]
    public string? Not { get; set; }

    [Display(Name = "Tamamlanma tarihi")]
    [Column("tamamlanma_tarihi")]
    public DateTime? TamamlanmaTarihi { get; set; }

    [MaxLength(150)]
    [Column("tamamlayan")]
    public string? Tamamlayan { get; set; }

    [Column("tamamlayan_id")]
    public long? TamamlayanId { get; set; }
}

/// <summary>
/// Bir görevin KİŞİYE ya da EKİBE ataması.
/// </summary>
/// <remarks>
/// <para>
/// İkisi tek tabloda: bir görevde hem "şu ekip yapsın" hem "şu kişi de
/// izlesin" olabiliyor. İki ayrı tablo, "kime bildirim gidecek?" sorusunu
/// iki yerden toplamak demekti.
/// </para>
/// <para>
/// <b>İkisinden biri dolu olmalı</b> — kural veritabanında değil serviste,
/// çünkü ihlali anlamlı bir mesajla reddetmek gerekiyor.
/// </para>
/// </remarks>
[Table("gorev_atamalari")]
public class WorkTaskAssignment
{
    [Column("id")]
    public long Id { get; set; }

    [Column("gorev_id")]
    public long GorevId { get; set; }
    public WorkTask? Gorev { get; set; }

    /// <summary>Atanan kişi (AspNetUsers.Id).</summary>
    [Column("kullanici_id")]
    public long? KullaniciId { get; set; }

    /// <summary>Atanan ekip. Ekibe atama, o an ekipte olan HERKESE bildirir.</summary>
    [Column("ekip_id")]
    public long? EkipId { get; set; }
    public Team? Ekip { get; set; }

    [Display(Name = "Rol")]
    [Column("rol")]
    public GorevAtamaRolu Rol { get; set; } = GorevAtamaRolu.Sorumlu;

    [MaxLength(150)]
    [Column("atayan")]
    public string? Atayan { get; set; }

    [Column("atama_tarihi")]
    public DateTime AtamaTarihi { get; set; } = DateTime.Now;
}

/// <summary>
/// İŞ ZAMAN ÇİZELGESİ — append-only.
/// </summary>
/// <remarks>
/// <para>
/// <c>AjandaOlay</c> kalıbının birebir kopyası: tip ayrı bir alan,
/// değişiklikler serbest metin değil YAPISAL JSON
/// (<c>[{"alan":"Durum","eski":"…","yeni":"…"}]</c>).
/// </para>
/// <para>
/// <b>Güncellenmez, silinmez.</b> Yazma hatası yutulur — çizelge yardımcı
/// bir kayıttır, asıl iş akışını düşürmesine izin verilmez.
/// </para>
/// <para>
/// Proje olayları da buraya yazılıyor; ayrım <see cref="VarlikTuru"/>'nde.
/// </para>
/// </remarks>
[Table("is_olaylari")]
public class WorkEvent
{
    [Column("id")]
    public long Id { get; set; }

    [Column("varlik_turu")]
    public IsVarligi VarlikTuru { get; set; }

    [Column("varlik_id")]
    public long VarlikId { get; set; }

    [Column("tip")]
    public GorevOlayTipi Tip { get; set; }

    [MaxLength(500)]
    [Column("aciklama")]
    public string? Aciklama { get; set; }

    /// <summary><c>[{"alan":"…","eski":"…","yeni":"…"}]</c>; fark yoksa <c>null</c>.</summary>
    [Column("degisiklikler_json", TypeName = "text")]
    public string? DegisikliklerJson { get; set; }

    [MaxLength(150)]
    [Column("kullanici")]
    public string? Kullanici { get; set; }

    /// <summary>İşlem hangi birim adına yapıldı — vekâlet izi.</summary>
    [Column("birim_id")]
    public long? BirimId { get; set; }

    [Column("tarih")]
    public DateTime Tarih { get; set; } = DateTime.Now;
}

/// <summary>
/// EKİP — birime bağlı KALICI çalışma grubu.
/// </summary>
/// <remarks>
/// Projeye özel değil: park bahçelerin "budama ekibi" her projede aynı ekip.
/// Proje ekibi ayrı bir kavram (<c>ProjectMember</c>) ve ikisi
/// karıştırılmıyor — biri kurumun yapısı, öteki bir işin katılımcı listesi.
/// </remarks>
[Table("ekipler")]
public class Team
{
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Ekip")]
    [Column("ad")]
    public string Ad { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Açıklama")]
    [Column("aciklama")]
    public string? Aciklama { get; set; }

    [Column("birim_id")]
    public long BirimId { get; set; }
    public Birim? Birim { get; set; }

    /// <summary>
    /// Ekip lideri. Göreve ekip atandığında bildirim ÖNCE buna gider.
    /// </summary>
    /// <remarks>
    /// Kullanıcının tarifi: "ekip varsa ekip başına" bildirim gider, iş
    /// dağıtımını lider yapar.
    /// </remarks>
    [Column("lider_id")]
    public long? LiderId { get; set; }

    [Display(Name = "Kullanımda")]
    [Column("kullanimda")]
    public bool Kullanimda { get; set; } = true;

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    [Column("guncelleme_tarihi")]
    public DateTime? GuncellemeTarihi { get; set; }

    public ICollection<TeamMember> Uyeler { get; set; } = [];
}

/// <summary>Ekip üyesi.</summary>
[Table("ekip_uyeleri")]
public class TeamMember
{
    [Column("id")]
    public long Id { get; set; }

    [Column("ekip_id")]
    public long EkipId { get; set; }
    public Team? Ekip { get; set; }

    [Column("kullanici_id")]
    public long KullaniciId { get; set; }

    [Column("eklenme_tarihi")]
    public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
}
