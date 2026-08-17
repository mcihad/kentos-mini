using KentOS.Mini.Application.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Mini.Application.Models;

/// <summary>
/// GÖREV — birimin yaptığı işin tek kaydı.
/// </summary>
/// <remarks>
/// <para>
/// Üç ayrı iş akışı burada buluşuyor: vatandaştan gelen şikayet, sahada
/// tespit edilen sorun ve birimin kendi planı. Fark yalnızca
/// <see cref="Kaynak"/> alanında. Ayrı tablolar kursaydık aynı SLA, aynı
/// aşama ve aynı bildirim mantığını üç kez yazmak gerekirdi.
/// </para>
///
/// <para>
/// <b>Ağaç yapısı:</b> <see cref="UstGorevId"/> ile alt görevler. Ekip
/// yöneticisi büyük bir işi parçalayıp personeline dağıtabilsin diye.
/// Derinlik sınırlanmıyor ama arayüz ağacı yalnızca detayda açıyor —
/// listede düz.
/// </para>
///
/// <para>
/// <b>Konum PostGIS geometrisi.</b> Eski modüllerdeki
/// <c>varchar "enlem,boylam"</c> biçimi tekrarlanmıyor: yarıçap sorgusu,
/// alan içi arama, rota ve ısı haritası metinle yapılamıyor. Eski kolonlara
/// dokunulmadı — onlar v1 sözleşmesinin parçası.
/// </para>
/// </remarks>
[Table("gorevler")]
public class WorkTask
{
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// İnsan tarafından okunan takip numarası (örn. <c>GRV-2026-000142</c>).
    /// </summary>
    /// <remarks>
    /// Telefonda söylenebilmeli. Sayısal kimlik verilmiyor: sıralı bir kimliği
    /// dışarıya söylemek sistemdeki toplam iş sayısını da söyler.
    /// </remarks>
    [Required]
    [MaxLength(30)]
    [Display(Name = "Takip numarası")]
    [Column("takip_no")]
    public string TakipNo { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    [Display(Name = "Başlık")]
    [Column("baslik")]
    public string Baslik { get; set; } = string.Empty;

    [Display(Name = "Açıklama")]
    [Column("aciklama", TypeName = "text")]
    public string? Aciklama { get; set; }

    // ── sınıflandırma ──────────────────────────────────────────────────

    [Column("gorev_tipi_id")]
    public long? GorevTipiId { get; set; }
    public TaskType? GorevTipi { get; set; }

    [Display(Name = "Durum")]
    [Column("durum")]
    public GorevDurumu Durum { get; set; } = GorevDurumu.Yeni;

    [Display(Name = "Öncelik")]
    [Column("oncelik")]
    public GorevOnceligi Oncelik { get; set; } = GorevOnceligi.Normal;

    [Display(Name = "Kaynak")]
    [Column("kaynak")]
    public GorevKaynagi Kaynak { get; set; } = GorevKaynagi.Manuel;

    /// <summary>
    /// Kaynak kaydın kimliği — talep, etkinlik ya da vatandaş bildirimi.
    /// </summary>
    /// <remarks>
    /// Yabancı anahtar DEĞİL: hangi tabloyu gösterdiği <see cref="Kaynak"/>
    /// alanına bağlı. Beş ayrı isteğe bağlı FK kolonu açmak yerine bu ikili
    /// tercih edildi; aynı gerekçe <see cref="WorkAttachment"/> için de
    /// yazılı.
    /// </remarks>
    [Column("kaynak_id")]
    public long? KaynakId { get; set; }

    // ── sahiplik ve görünürlük ─────────────────────────────────────────

    /// <summary>GÖRÜNÜRLÜK KAPISI — işi yürüten birim.</summary>
    [Column("birim_id")]
    public long BirimId { get; set; }
    public Birim? Birim { get; set; }

    /// <summary>Ağaç — üst görev. Kök görevlerde <c>null</c>.</summary>
    [Column("ust_gorev_id")]
    public long? UstGorevId { get; set; }
    public WorkTask? UstGorev { get; set; }
    public ICollection<WorkTask> AltGorevler { get; set; } = [];

    // ── proje bağı ─────────────────────────────────────────────────────

    [Column("proje_id")]
    public long? ProjeId { get; set; }

    [Column("kilometre_tasi_id")]
    public long? KilometreTasiId { get; set; }

    // ── konum ──────────────────────────────────────────────────────────

    /// <summary>
    /// İşin yeri — WGS84 (EPSG:4326) noktası.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tür <c>NetTopologySuite.Geometries.Point</c> ama bu proje NuGet
    /// bağımlılığı taşımıyor (<c>Application</c> katmanının değişmez kuralı).
    /// Bu yüzden burada <b>ham koordinat</b> tutuluyor ve geometri kolonu
    /// <c>Web</c> katmanında Fluent yapılandırmayla eşleniyor.
    /// </para>
    /// <para>
    /// Enlem/boylam ayrı kolonlar olarak da duruyor: haritaya basmak için
    /// geometriyi çözmek gereksiz iş ve JSON'a iki sayı yazmak, WKT
    /// göndermekten hem küçük hem istemci tarafında ayrıştırmasız.
    /// </para>
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

    [Column("mahalle_id")]
    public long? MahalleId { get; set; }
    public Mahalle? Mahalle { get; set; }

    // ── zaman ve SLA ───────────────────────────────────────────────────

    [Display(Name = "Planlanan başlangıç")]
    [Column("planlanan_baslangic")]
    public DateTime? PlanlananBaslangic { get; set; }

    [Display(Name = "Planlanan bitiş")]
    [Column("planlanan_bitis")]
    public DateTime? PlanlananBitis { get; set; }

    [Display(Name = "Başlama tarihi")]
    [Column("baslama_tarihi")]
    public DateTime? BaslamaTarihi { get; set; }

    [Display(Name = "Tamamlanma tarihi")]
    [Column("tamamlanma_tarihi")]
    public DateTime? TamamlanmaTarihi { get; set; }

    /// <summary>
    /// SLA'nın dolacağı an — görev BAŞLADIĞINDA tipin süresinden hesaplanır.
    /// </summary>
    /// <remarks>
    /// Açılışta değil başlangıçta damgalanıyor: atanmayı bekleyen bir görevin
    /// SLA'sını işletmek, henüz kimseye verilmemiş işi geciktirdi diye
    /// personele yazmak olurdu.
    /// </remarks>
    [Display(Name = "SLA bitişi")]
    [Column("sla_bitis")]
    public DateTime? SlaBitis { get; set; }

    /// <summary>
    /// <see cref="GorevDurumu.Beklemede"/> geçirilen toplam dakika.
    /// </summary>
    /// <remarks>
    /// SLA hesabından DÜŞÜLÜR: malzeme bekleyen bir işi geciktirdi diye
    /// personele yazmak ölçümü anlamsız kılar.
    /// </remarks>
    [Column("bekleme_dakika")]
    public int BeklemeDakika { get; set; }

    // ── akış ───────────────────────────────────────────────────────────

    /// <summary>İade ya da ret gerekçesi — ikisinde de ZORUNLU.</summary>
    [MaxLength(1000)]
    [Display(Name = "Gerekçe")]
    [Column("gerekce")]
    public string? Gerekce { get; set; }

    [MaxLength(150)]
    [Column("onaylayan")]
    public string? Onaylayan { get; set; }

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

    /// <summary>
    /// Kayıt hangi birim ADINA açıldı — vekâlet izi.
    /// </summary>
    /// <remarks>
    /// Başkan yardımcısı bir müdürlük adına iş açtığında <see cref="BirimId"/>
    /// o müdürlüğü gösterir; kimin açtığı ise burada kalır. İkisi ayrı
    /// tutulmazsa "bu işi bize kim yazdı?" sorusunun cevabı kaybolur.
    /// </remarks>
    [Column("olusturan_birim_id")]
    public long? OlusturanBirimId { get; set; }

    public ICollection<WorkTaskStage> Asamalar { get; set; } = [];
    public ICollection<WorkTaskAssignment> Atamalar { get; set; } = [];
}
