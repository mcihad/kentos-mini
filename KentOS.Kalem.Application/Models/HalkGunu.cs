using KentOS.Kalem.Application.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models;

/// <summary>
/// HALK GÜNÜ — vatandaşın makamda sırayla dinlendiği gün.
/// </summary>
/// <remarks>
/// <para>
/// Model üç parçaya bölünmüş durumda ve bu bölünme <c>Protokol</c> /
/// <c>Davet</c> / <c>DavetKisi</c> üçlüsünün birebir aynısı:
/// </para>
/// <list type="bullet">
/// <item><see cref="HalkGunuBasvuru"/> — <b>kişi ve talebi</b>. Halk gününden
/// bağımsız yaşar: vatandaş bugün başvurur, üç hafta sonraki güne atanır.</item>
/// <item><see cref="HalkGunu"/> — <b>gün</b>.</item>
/// <item><see cref="HalkGunuDilim"/> — günün <b>zaman dilimleri</b>.</item>
/// <item><see cref="HalkGunuKatilim"/> — bir başvurunun belli bir gündeki
/// <b>yeri ve sonucu</b>. Kişi bilgisini KOPYALAMAZ, başvurudan okur.</item>
/// </list>
/// <para>
/// Kişi bilgisini katılıma kopyalamamak bilinçli: vatandaş telefonunu
/// düzelttirdiğinde ya da adresi değiştiğinde eski katılım kayıtları da
/// güncel kalır. (Davet listesi kişi bilgisini protokolden okuyor, aynı
/// gerekçeyle.)
/// </para>
/// </remarks>
[Table("halk_gunleri")]
public class HalkGunu
{
    [Column("id")]
    public long Id { get; set; }

    /// <summary>Günün tarihi (saat kısmı kullanılmaz).</summary>
    [Column("tarih")]
    public DateTime Tarih { get; set; }

    /// <summary>"Ağustos Halk Günü" gibi; boşsa tarih başlık olur.</summary>
    [MaxLength(200)]
    [Column("baslik")]
    public string? Baslik { get; set; }

    [MaxLength(200)]
    [Column("konum")]
    public string? Konum { get; set; }

    [Column("aciklama", TypeName = "text")]
    public string? Aciklama { get; set; }

    [Column("durum")]
    public HalkGunuDurumu Durum { get; set; } = HalkGunuDurumu.Planlaniyor;

    /// <summary>Görünürlük kapısı — sistemin geri kalanıyla aynı.</summary>
    [Column("birim_id")]
    public long? BirimId { get; set; }
    public Birim? Birim { get; set; }

    /// <summary>Oluşturan kullanıcının ADI (metin) — `Ajanda`/`Davet` ile aynı biçim.</summary>
    [MaxLength(100)]
    [Column("kullanici_id")]
    public string? KullaniciId { get; set; }

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    [Column("guncelleme_tarihi")]
    public DateTime? GuncellemeTarihi { get; set; }

    public ICollection<HalkGunuDilim> Dilimler { get; set; } = [];
    public ICollection<HalkGunuKatilim> Katilimlar { get; set; } = [];
}

/// <summary>
/// Günün bir zaman dilimi — 14:00–15:00 ya da 14:05–14:15.
/// </summary>
/// <remarks>
/// İki kullanım biçimi de aynı modelle karşılanır: bir dilime on kişi sırayla
/// atanabilir (<see cref="Kapasite"/> boş) ya da tek kişilik on dilim üretilip
/// her birine bir kişi konabilir (kapasite 1). Ayrı bir "randevu tipi" alanı
/// eklemek, aynı şeyi iki kez modellemek olurdu.
/// </remarks>
[Table("halk_gunu_dilimleri")]
public class HalkGunuDilim
{
    [Column("id")]
    public long Id { get; set; }

    [Column("halk_gunu_id")]
    public long HalkGunuId { get; set; }
    public HalkGunu? HalkGunu { get; set; }

    /// <summary>Dilim başlangıcı — günün tarihiyle birlikte tam zaman.</summary>
    [Column("baslangic")]
    public DateTime Baslangic { get; set; }
    [Column("bitis")]
    public DateTime Bitis { get; set; }

    [MaxLength(200)]
    [Column("baslik")]
    public string? Baslik { get; set; }

    /// <summary>Boş ise sınırsız; dolu ise o kadar kişi atanabilir.</summary>
    [Column("kapasite")]
    public int? Kapasite { get; set; }

    [Column("sira_no")]
    public int SiraNo { get; set; }

    public ICollection<HalkGunuKatilim> Katilimlar { get; set; } = [];
}

/// <summary>
/// BEKLEYENLER HAVUZU — halk gününde görüşmek isteyen vatandaş.
/// </summary>
/// <remarks>
/// Vatandaş makama gelir, başkanla görüşemez; "halk gününde görüşmek ister
/// misiniz?" diye sorulur ve isterse buraya yazılır. Kayıt bir güne
/// atanmadan da yaşar.
/// </remarks>
[Table("halk_gunu_basvurulari")]
public class HalkGunuBasvuru
{
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("ad")]
    public string Ad { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("soyad")]
    public string? Soyad { get; set; }

    [MaxLength(50)]
    [Column("telefon")]
    public string? Telefon { get; set; }

    /// <summary>
    /// Yalnızca rakamlardan oluşan telefon — <b>kişiyi bulmanın anahtarı</b>.
    /// </summary>
    /// <remarks>
    /// Sistemde vatandaş tablosu yok; aynı kişi her kayıtta yeniden yazılıyor
    /// ve eşleştirilebilecek tek doğal anahtar telefon. Ama `randevular.telefon`
    /// sütununda <c>0541 298 34 50</c>, <c>05412983450</c>, <c>+90 541…</c>
    /// karışık duruyor ve <c>ILIKE '%…%'</c> araması numarayı bitişik yazınca
    /// bulmuyor. Bu kolon yazma anında <c>Telefon.Duzelt</c> ile
    /// normalleştirilir; arama buradan yapılır.
    /// </remarks>
    [MaxLength(20)]
    [Column("telefon_sade")]
    public string? TelefonSade { get; set; }

    [MaxLength(255)]
    [Column("adres")]
    public string? Adres { get; set; }

    [Column("mahalle_id")]
    public long? MahalleId { get; set; }
    public Mahalle? Mahalle { get; set; }

    [MaxLength(100)]
    [Column("meslek")]
    public string? Meslek { get; set; }

    /// <summary>Vatandaşın ne için görüşmek istediği.</summary>
    [Column("konu", TypeName = "text")]
    public string? Konu { get; set; }

    [Column("not", TypeName = "text")]
    public string? Not { get; set; }

    [Column("durum")]
    public BasvuruDurumu Durum { get; set; } = BasvuruDurumu.Bekliyor;

    /// <summary>
    /// Görüşme neden UYGUN GÖRÜLMEDİ.
    /// </summary>
    /// <remarks>
    /// Gerekçesiz bir ret, aynı vatandaş bir sonraki ay yeniden
    /// başvurduğunda "bunu neden geri çevirmiştik?" sorusunu cevapsız
    /// bırakıyordu.
    /// </remarks>
    [Column("red_nedeni", TypeName = "text")]
    public string? RedNedeni { get; set; }

    [Column("red_tarihi")]
    public DateTime? RedTarihi { get; set; }

    /// <summary>
    /// Var olan bir talebe bağ — isteğe bağlı.
    /// </summary>
    /// <remarks>
    /// Vatandaş zaten talep açmışsa halk günü kaydı ona bağlanır; böylece
    /// geçmiş sorgusu iki kaydı tek kişi olarak gösterebilir. FK
    /// <c>Restrict</c>: talep silinse bile halk günü geçmişi kaybolmamalı.
    /// </remarks>
    [Column("randevu_id")]
    public long? RandevuId { get; set; }
    public Randevu? Randevu { get; set; }

    [Column("birim_id")]
    public long? BirimId { get; set; }
    public Birim? Birim { get; set; }

    [MaxLength(100)]
    [Column("olusturan")]
    public string? Olusturan { get; set; }

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    [Column("guncelleme_tarihi")]
    public DateTime? GuncellemeTarihi { get; set; }

    public ICollection<HalkGunuKatilim> Katilimlar { get; set; } = [];
}

/// <summary>
/// Bir başvurunun BELLİ bir halk günündeki yeri ve sonucu.
/// </summary>
[Table("halk_gunu_katilimlari")]
public class HalkGunuKatilim
{
    [Column("id")]
    public long Id { get; set; }

    [Column("halk_gunu_id")]
    public long HalkGunuId { get; set; }
    public HalkGunu? HalkGunu { get; set; }

    /// <summary>Atanmadıysa boş — gün içinde ama henüz dilimi yok.</summary>
    [Column("dilim_id")]
    public long? DilimId { get; set; }
    public HalkGunuDilim? Dilim { get; set; }

    [Column("basvuru_id")]
    public long BasvuruId { get; set; }
    public HalkGunuBasvuru? Basvuru { get; set; }

    /// <summary>Dilim içindeki sıra — aynı saate atanan on kişinin görüşme sırası.</summary>
    [Column("sira_no")]
    public int SiraNo { get; set; }

    [Column("durum")]
    public KatilimDurumu Durum { get; set; } = KatilimDurumu.Bekliyor;

    [Column("gorusme_notu", TypeName = "text")]
    public string? GorusmeNotu { get; set; }

    [Column("gorusme_tarihi")]
    public DateTime? GorusmeTarihi { get; set; }

    /// <summary>
    /// "Bu iş ilgilenilecek" işareti.
    /// </summary>
    /// <remarks>
    /// Halk gününde konuşulan her şey iş değil; bir kısmı takip gerektiriyor.
    /// Özel Kalem bu işaretlileri listeler ve uygun olanları talebe çevirir.
    /// Otomatik talep açılmıyor: çoğu görüşme kayıt olarak kalıyor ve her
    /// görüşmeden talep doğurmak talep listesini hızla kullanılmaz yapardı.
    /// </remarks>
    [Column("degerlendirmeye_esas")]
    public bool DegerlendirmeyeEsas { get; set; }

    [Column("degerlendirme_notu", TypeName = "text")]
    public string? DegerlendirmeNotu { get; set; }

    /// <summary>Talebe dönüştürüldüyse oluşan talebin kimliği.</summary>
    [Column("olusan_randevu_id")]
    public long? OlusanRandevuId { get; set; }

    /// <summary>Bilgilendirme SMS'i gönderildiyse zamanı.</summary>
    [Column("sms_tarihi")]
    public DateTime? SmsTarihi { get; set; }

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    [Column("guncelleme_tarihi")]
    public DateTime? GuncellemeTarihi { get; set; }
}
