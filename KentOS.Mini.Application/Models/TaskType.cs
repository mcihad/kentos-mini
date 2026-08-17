using KentOS.Mini.Application.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Mini.Application.Models;

/// <summary>
/// GÖREV TİPİ — bir iş türünün şablonu.
/// </summary>
/// <remarks>
/// <para>
/// "Çim biçimi", "yol yaması", "aydınlatma arızası" gibi. Tip üç şeyi
/// taşır: <b>ne kadar sürmesi gerektiği</b> (hizmet standardı ve SLA),
/// <b>hangi adımlardan geçtiği</b> (<see cref="TaskTypeStage"/>) ve
/// <b>bitince kimin haberi olacağı</b> (<see cref="TaskTypeHandoff"/>).
/// </para>
///
/// <para>
/// <b>Tanım kopyalanır, bağlanmaz.</b> Görev açılırken tipin aşamaları
/// <c>gorev_asamalari</c>'na KOPYALANIR. Bağ kursaydık, tanım altı ay sonra
/// değiştiğinde geçmiş görevlerin hangi adımlardan geçtiği de değişirdi —
/// tamamlanmış bir işin kanıtı sonradan bozulamaz.
/// </para>
/// </remarks>
[Table("gorev_tipleri")]
public class TaskType
{
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Görev tipi")]
    [Column("ad")]
    public string Ad { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Açıklama")]
    [Column("aciklama")]
    public string? Aciklama { get; set; }

    /// <summary>Listelerde ve haritada ayırt edici renk (#RRGGBB).</summary>
    [MaxLength(20)]
    [Display(Name = "Renk")]
    [Column("renk")]
    public string? Renk { get; set; }

    /// <summary>
    /// HİZMET STANDARDI — kuruma ilan edilen süre, <b>gün</b>.
    /// </summary>
    /// <remarks>
    /// Vatandaşa "bu iş en geç N günde biter" diye söylenen süre. SLA'dan
    /// ayrı tutuluyor çünkü ikisi farklı şeyi ölçüyor: hizmet standardı
    /// kurumun dışarıya verdiği söz, SLA içeride izlenen hedef ve genelde
    /// daha kısa.
    /// </remarks>
    [Display(Name = "Hizmet standardı (gün)")]
    [Column("hizmet_standardi_gun")]
    public int? HizmetStandardiGun { get; set; }

    /// <summary>
    /// SLA süresi — <b>saat</b>. Görev başladığında bitiş damgası bundan
    /// hesaplanır.
    /// </summary>
    /// <remarks>
    /// Saat cinsinden: aynı gün içinde bitmesi beklenen işler (aydınlatma
    /// arızası, çöp konteyneri) gün çözünürlüğünde ölçülemiyordu.
    /// </remarks>
    [Display(Name = "SLA süresi (saat)")]
    [Column("sla_saat")]
    public int? SlaSaat { get; set; }

    /// <summary>Görev açılırken önerilen öncelik.</summary>
    [Display(Name = "Varsayılan öncelik")]
    [Column("varsayilan_oncelik")]
    public GorevOnceligi VarsayilanOncelik { get; set; } = GorevOnceligi.Normal;

    /// <summary>
    /// Bu tipteki görevlerde konum ZORUNLU mu?
    /// </summary>
    /// <remarks>
    /// Saha işlerinde konumsuz bir görev haritada görünmez ve rota kurulamaz.
    /// Büro işlerinde ise konum istemek gereksiz sürtünme — bu yüzden tipe
    /// bağlı.
    /// </remarks>
    [Display(Name = "Konum zorunlu")]
    [Column("konum_zorunlu")]
    public bool KonumZorunlu { get; set; }

    /// <summary>Kapalı tip yeni görevde SEÇİLEMEZ; geçmiş görevler etkilenmez.</summary>
    [Display(Name = "Kullanımda")]
    [Column("kullanimda")]
    public bool Kullanimda { get; set; } = true;

    /// <summary>
    /// Tipi TANIMLAYAN birim.
    /// </summary>
    /// <remarks>
    /// Kullanım hakkı ayrı: <see cref="Birimler"/> boşsa tip yalnızca sahibi
    /// birime açıktır, doluysa yalnızca listelenen birimlere.
    /// </remarks>
    [Column("birim_id")]
    public long? BirimId { get; set; }
    public Birim? Birim { get; set; }

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    [Column("guncelleme_tarihi")]
    public DateTime? GuncellemeTarihi { get; set; }

    [MaxLength(150)]
    [Column("olusturan")]
    public string? Olusturan { get; set; }

    public ICollection<TaskTypeStage> Asamalar { get; set; } = [];
    public ICollection<TaskTypeUnit> Birimler { get; set; } = [];
    public ICollection<TaskTypeHandoff> Devirler { get; set; } = [];
}

/// <summary>
/// Bir görev tipini KULLANABİLEN birim.
/// </summary>
/// <remarks>
/// Boş bırakmak "yalnızca sahibi birim" demek. Çoka çok bir tabloyla
/// yapılmasının sebebi paylaşılan tipler: "aydınlatma arızası" hem fen
/// işlerinin hem park bahçelerin işine yarayabiliyor ve iki ayrı tip
/// tanımlamak SLA'yı da ikiye bölerdi.
/// </remarks>
[Table("gorev_tipi_birimleri")]
public class TaskTypeUnit
{
    [Column("id")]
    public long Id { get; set; }

    [Column("gorev_tipi_id")]
    public long GorevTipiId { get; set; }
    public TaskType? GorevTipi { get; set; }

    [Column("birim_id")]
    public long BirimId { get; set; }
    public Birim? Birim { get; set; }
}

/// <summary>
/// Görev tipinin bir AŞAMASI — şablon.
/// </summary>
/// <remarks>
/// Sahadaki personelin sırayla geçeceği adımlar. Her adım kendi kanıt
/// kuralını taşır: kimi adımda fotoğraf şart (kazının kapatıldığı), kiminde
/// açıklama (neden yapılamadı), kiminde ikisi de gerekmez.
/// </remarks>
[Table("gorev_tipi_asamalari")]
public class TaskTypeStage
{
    [Column("id")]
    public long Id { get; set; }

    [Column("gorev_tipi_id")]
    public long GorevTipiId { get; set; }
    public TaskType? GorevTipi { get; set; }

    [Display(Name = "Sıra")]
    [Column("sira_no")]
    public int SiraNo { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Aşama")]
    [Column("ad")]
    public string Ad { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Açıklama")]
    [Column("aciklama")]
    public string? Aciklama { get; set; }

    /// <summary>
    /// ZORUNLU aşama atlanamaz — sonraki aşamaya geçilemez.
    /// </summary>
    /// <remarks>
    /// Zorunlu olmayan aşama <see cref="GorevAsamaDurumu.Atlandi"/> ile
    /// geçilebilir, ama gerekçe not alanına yazılır. "Atlandı" ile
    /// "tamamlandı" ayrı tutuluyor: ikisi aynı sayılsaydı hiç yapılmamış bir
    /// adım rapora yapılmış gibi girerdi.
    /// </remarks>
    [Display(Name = "Zorunlu")]
    [Column("zorunlu")]
    public bool Zorunlu { get; set; } = true;

    [Display(Name = "Açıklama zorunlu")]
    [Column("aciklama_zorunlu")]
    public bool AciklamaZorunlu { get; set; }

    /// <summary>Fotoğraf olmadan bu aşama tamamlanamaz — sahada kanıt.</summary>
    [Display(Name = "Fotoğraf zorunlu")]
    [Column("fotograf_zorunlu")]
    public bool FotografZorunlu { get; set; }

    /// <summary>Tahmini süre (saat) — planlama ve gecikme tahmini için.</summary>
    [Display(Name = "Tahmini süre (saat)")]
    [Column("tahmini_saat")]
    public int? TahminiSaat { get; set; }
}

/// <summary>
/// DEVİR KURALI — görev bitince başka bir birime düşen kayıt.
/// </summary>
/// <remarks>
/// <para>
/// Kullanıcının örneği: park bahçeler çim biçimini bitirince veterinerlik
/// müdürlüğünün haberi olmalı, hatta belki oradan yeni bir iş doğmalı.
/// </para>
/// <para>
/// Devir hedef birime <b>doğrudan görev açmaz</b>, onun GELEN KUTUSUNA
/// düşer. Sebebi basit: bir birim başka bir birime iş yazamaz — hedef birim
/// kabul ederse görev orada açılır, reddederse gerekçesiyle geri bildirilir.
/// Otomatik görev açmak, birimlerin iş yükünü birbirine yıkmasının en kolay
/// yolu olurdu.
/// </para>
/// </remarks>
[Table("gorev_tipi_devirleri")]
public class TaskTypeHandoff
{
    [Column("id")]
    public long Id { get; set; }

    [Column("gorev_tipi_id")]
    public long GorevTipiId { get; set; }
    public TaskType? GorevTipi { get; set; }

    /// <summary>Kime düşecek.</summary>
    [Column("hedef_birim_id")]
    public long HedefBirimId { get; set; }
    public Birim? HedefBirim { get; set; }

    /// <summary>
    /// Yalnızca haber mi, yoksa iş talebi mi?
    /// </summary>
    /// <remarks>
    /// Bilgilendirmede hedef birimin yapacağı bir şey yok, kutuda okunup
    /// kapatılır. İş talebinde kabul/ret beklenir.
    /// </remarks>
    [Display(Name = "İş talebi")]
    [Column("is_talebi")]
    public bool IsTalebi { get; set; }

    /// <summary>Gelen kutusunda görünecek metin; boşsa görevin başlığı.</summary>
    [MaxLength(500)]
    [Display(Name = "Not")]
    [Column("not")]
    public string? Not { get; set; }

    /// <summary>Devir sonucu görev açılırsa hangi tipte açılacağı (öneri).</summary>
    [Column("hedef_gorev_tipi_id")]
    public long? HedefGorevTipiId { get; set; }
}
