using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using KentOS.Mini.Application.Enums;

namespace KentOS.Mini.Application.Dto.V2.IsTakip;

/// <summary>
/// PROJE ÖZETİ — liste satırı.
/// </summary>
/// <remarks>
/// İlerleme projenin kendi alanından değil <b>altındaki görevlerden</b>
/// hesaplanıyor. Projeye ayrı bir yüzde kolonu koysaydık görevlerle
/// çelişebilen ikinci bir gerçek doğardı.
/// </remarks>
public class ProjeOzetDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("kod")] public string? Kod { get; set; }
    [JsonPropertyName("renk")] public string? Renk { get; set; }

    [JsonPropertyName("durum")] public ProjeDurumu Durum { get; set; }
    [JsonPropertyName("durumAd")] public string DurumAd { get; set; } = string.Empty;
    [JsonPropertyName("durumRenk")] public string DurumRenk { get; set; } = string.Empty;

    [JsonPropertyName("birimId")] public long BirimId { get; set; }
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }

    [JsonPropertyName("yoneticiId")] public long? YoneticiId { get; set; }
    [JsonPropertyName("yoneticiAd")] public string? YoneticiAd { get; set; }

    [JsonPropertyName("baslangic")] public DateTime? Baslangic { get; set; }
    [JsonPropertyName("bitis")] public DateTime? Bitis { get; set; }
    [JsonPropertyName("tamamlanmaTarihi")] public DateTime? TamamlanmaTarihi { get; set; }
    [JsonPropertyName("butce")] public decimal? Butce { get; set; }

    [JsonPropertyName("enlem")] public double? Enlem { get; set; }
    [JsonPropertyName("boylam")] public double? Boylam { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }

    [JsonPropertyName("uyeSayisi")] public int UyeSayisi { get; set; }

    /// <summary>Projeye bağlı toplam ve tamamlanmış görev sayısı.</summary>
    [JsonPropertyName("gorevToplam")] public int GorevToplam { get; set; }
    [JsonPropertyName("gorevBiten")] public int GorevBiten { get; set; }

    /// <summary>
    /// Projenin ilerlemesi (0–100) — bağlı görevlerin ilerleme ORTALAMASI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Eskiden yüzde <c>gorevBiten / gorevToplam</c> ile çiziliyordu, yani bir
    /// görev onaylanana kadar sıfır sayılıyordu. Beş aşamalı işin dördünü
    /// bitiren ekip, proje çubuğunda hiçbir hareket görmüyordu — ölçülüp
    /// düzeltildi.
    /// </para>
    /// <para>
    /// <c>gorevBiten</c> alanı KALDI ve kaldırılmayacak: "%62" ile
    /// "8/13 görev kapandı" iki farklı soruya cevap veriyor. İlki işin ne
    /// kadarının yapıldığını, ikincisi kaç kalemin kapandığını söyler.
    /// </para>
    /// </remarks>
    [JsonPropertyName("ilerleme")] public int Ilerleme { get; set; }

    /// <summary>Süresi aşılmış AÇIK görev sayısı — projenin risk göstergesi.</summary>
    [JsonPropertyName("gorevGeciken")] public int GorevGeciken { get; set; }

    [JsonPropertyName("kilometreTasiToplam")] public int KilometreTasiToplam { get; set; }
    [JsonPropertyName("kilometreTasiBiten")] public int KilometreTasiBiten { get; set; }

    /// <summary>
    /// Bitiş tarihi geçmiş ama kapanmamış mı?
    /// </summary>
    /// <remarks>
    /// Sunucuda hesaplanıyor: istemcinin saati yanlışsa gecikme tablosu da
    /// yanlış olurdu — görevlerdeki kuralın aynısı.
    /// </remarks>
    [JsonPropertyName("gecikti")] public bool Gecikti { get; set; }
}

/// <summary>Projenin tam detayı.</summary>
public class ProjeDetayDto : ProjeOzetDto
{
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("olusturan")] public string? Olusturan { get; set; }
    [JsonPropertyName("olusturmaTarihi")] public DateTime OlusturmaTarihi { get; set; }

    [JsonPropertyName("uyeler")] public List<ProjeUyeDto> Uyeler { get; set; } = [];
    [JsonPropertyName("kilometreTaslari")] public List<KilometreTasiDto> KilometreTaslari { get; set; } = [];
    [JsonPropertyName("panoSutunlari")] public List<PanoSutunuDto> PanoSutunlari { get; set; } = [];
}

/// <summary>Proje üyesi.</summary>
public class ProjeUyeDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("kullaniciId")] public long KullaniciId { get; set; }
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("rol")] public ProjeUyeRolu Rol { get; set; }
    [JsonPropertyName("rolAd")] public string RolAd { get; set; } = string.Empty;
    [JsonPropertyName("yoneticiMi")] public bool YoneticiMi { get; set; }
}

/// <summary>Kilometre taşı.</summary>
public class KilometreTasiDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("siraNo")] public int SiraNo { get; set; }

    [Required(ErrorMessage = "Kilometre taşı adı zorunlu.")]
    [MaxLength(300)]
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;

    [MaxLength(1000)]
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }

    [JsonPropertyName("hedefTarih")] public DateTime? HedefTarih { get; set; }
    [JsonPropertyName("tamamlandi")] public bool Tamamlandi { get; set; }
    [JsonPropertyName("tamamlanmaTarihi")] public DateTime? TamamlanmaTarihi { get; set; }

    /// <summary>Bu taşa bağlı görev sayıları — gantt çubuğunun doluluğu.</summary>
    [JsonPropertyName("gorevToplam")] public int GorevToplam { get; set; }
    [JsonPropertyName("gorevBiten")] public int GorevBiten { get; set; }

    /// <summary>Bağlı görevlerin ilerleme ortalaması (0–100).</summary>
    [JsonPropertyName("ilerleme")] public int Ilerleme { get; set; }

    /// <summary>Hedef tarihi geçmiş ve hâlâ tamamlanmamış mı?</summary>
    [JsonPropertyName("gecikti")] public bool Gecikti { get; set; }
}

/// <summary>Kanban sütunu.</summary>
public class PanoSutunuDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("siraNo")] public int SiraNo { get; set; }

    [Required(ErrorMessage = "Sütun adı zorunlu.")]
    [MaxLength(100)]
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;

    [MaxLength(20)]
    [JsonPropertyName("renk")] public string? Renk { get; set; }

    /// <summary>Karta düşünce uygulanacak görev durumu.</summary>
    [JsonPropertyName("gorevDurumu")] public GorevDurumu GorevDurumu { get; set; }
    [JsonPropertyName("gorevDurumuAd")] public string GorevDurumuAd { get; set; } = string.Empty;
}

/// <summary>
/// Proje kaydetme isteği.
/// </summary>
/// <remarks>
/// Üye, kilometre taşı ve pano sütunu listeleri <b>tam liste</b>: gövdede
/// olmayan satır silinir. Görev tipi ve ekip kaydıyla aynı gerekçe — yarısı
/// başarısız olmuş bir dizi ekle/çıkar isteği, projeyi kimin yürüttüğü
/// belirsiz bir durumda bırakırdı.
/// </remarks>
public class ProjeKayitDto
{
    [Required(ErrorMessage = "Proje adı zorunlu.")]
    [MaxLength(300)]
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;

    [MaxLength(40)]
    [JsonPropertyName("kod")] public string? Kod { get; set; }

    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }

    [MaxLength(20)]
    [JsonPropertyName("renk")] public string? Renk { get; set; }

    [JsonPropertyName("durum")] public ProjeDurumu Durum { get; set; } = ProjeDurumu.Planlaniyor;
    [JsonPropertyName("yoneticiId")] public long? YoneticiId { get; set; }
    [JsonPropertyName("baslangic")] public DateTime? Baslangic { get; set; }
    [JsonPropertyName("bitis")] public DateTime? Bitis { get; set; }

    [Range(0, 999_999_999_999, ErrorMessage = "Bütçe negatif olamaz.")]
    [JsonPropertyName("butce")] public decimal? Butce { get; set; }

    [Range(-90, 90, ErrorMessage = "Enlem -90 ile 90 arasında olmalı.")]
    [JsonPropertyName("enlem")] public double? Enlem { get; set; }

    [Range(-180, 180, ErrorMessage = "Boylam -180 ile 180 arasında olmalı.")]
    [JsonPropertyName("boylam")] public double? Boylam { get; set; }

    [MaxLength(500)]
    [JsonPropertyName("adres")] public string? Adres { get; set; }

    [JsonPropertyName("uyeler")] public List<ProjeUyeIstegiDto> Uyeler { get; set; } = [];
    [JsonPropertyName("kilometreTaslari")] public List<KilometreTasiDto> KilometreTaslari { get; set; } = [];
    [JsonPropertyName("panoSutunlari")] public List<PanoSutunuDto> PanoSutunlari { get; set; } = [];
}

/// <summary>Üyelik isteği.</summary>
public class ProjeUyeIstegiDto
{
    [JsonPropertyName("kullaniciId")] public long KullaniciId { get; set; }
    [JsonPropertyName("rol")] public ProjeUyeRolu Rol { get; set; } = ProjeUyeRolu.Uye;
}

/// <summary>Kanban panosu — sütunlar ve içindeki kartlar.</summary>
public class PanoDto
{
    [JsonPropertyName("projeId")] public long ProjeId { get; set; }
    [JsonPropertyName("sutunlar")] public List<PanoSutunKartlariDto> Sutunlar { get; set; } = [];

    /// <summary>
    /// Hiçbir sütuna düşmeyen görevler.
    /// </summary>
    /// <remarks>
    /// Sütunlar görev durumlarına eşleniyor; panoda karşılığı olmayan bir
    /// durumdaki görev (örn. sütunu silinmiş "Beklemede") hiçbir yerde
    /// görünmezdi. Kayıp iş, panoyu yanlış okutan en sinsi şey.
    /// </remarks>
    [JsonPropertyName("dagitilmayanlar")] public List<GorevOzetDto> Dagitilmayanlar { get; set; } = [];
}

/// <summary>Bir sütun ve kartları.</summary>
public class PanoSutunKartlariDto
{
    [JsonPropertyName("sutun")] public PanoSutunuDto Sutun { get; set; } = new();
    [JsonPropertyName("kartlar")] public List<GorevOzetDto> Kartlar { get; set; } = [];
}

/// <summary>Kart taşıma isteği — kanban sürükle-bırak.</summary>
public class KartTasimaDto
{
    [JsonPropertyName("gorevId")] public long GorevId { get; set; }
    [JsonPropertyName("hedefSutunId")] public long HedefSutunId { get; set; }
}

/// <summary>
/// GANTT satırı.
/// </summary>
/// <remarks>
/// Kilometre taşları ve görevler AYNI listede, <see cref="Tur"/> ile
/// ayrılıyor. İki ayrı uç açmak, çizimde iki listeyi tarih eksenine göre
/// istemcide birleştirmeyi gerektirirdi ve sıralama iki yerde yapılırdı.
/// </remarks>
public class GanttSatiriDto
{
    [JsonPropertyName("id")] public long Id { get; set; }

    /// <summary><c>kilometreTasi</c> ya da <c>gorev</c>.</summary>
    [JsonPropertyName("tur")] public string Tur { get; set; } = string.Empty;

    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("baslangic")] public DateTime? Baslangic { get; set; }
    [JsonPropertyName("bitis")] public DateTime? Bitis { get; set; }
    [JsonPropertyName("renk")] public string Renk { get; set; } = string.Empty;

    /// <summary>0–100. Görevde aşama oranı, kilometre taşında bağlı görev oranı.</summary>
    [JsonPropertyName("ilerleme")] public int Ilerleme { get; set; }

    [JsonPropertyName("gecikti")] public bool Gecikti { get; set; }
    [JsonPropertyName("durumAd")] public string? DurumAd { get; set; }

    /// <summary>Görev satırında bağlı olduğu kilometre taşı — çizimde öbekleme.</summary>
    [JsonPropertyName("kilometreTasiId")] public long? KilometreTasiId { get; set; }
}
