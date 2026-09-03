using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using KentOS.Kalem.Application.Dto.Analiz;
using KentOS.Kalem.Application.Enums;

namespace KentOS.Kalem.Application.Dto.V2.IsTakip;

/// <summary>Doğrulama kodu isteği — portalın ilk adımı.</summary>
public class DogrulamaIstegiDto
{
    [Required(ErrorMessage = "Telefon numarası zorunlu.")]
    [MaxLength(20)]
    [JsonPropertyName("telefon")] public string Telefon { get; set; } = string.Empty;
}

/// <summary>Kod doğrulama isteği.</summary>
public class DogrulamaOnayDto
{
    [Required(ErrorMessage = "Telefon numarası zorunlu.")]
    [MaxLength(20)]
    [JsonPropertyName("telefon")] public string Telefon { get; set; } = string.Empty;

    [Required(ErrorMessage = "Doğrulama kodu zorunlu.")]
    [MaxLength(10)]
    [JsonPropertyName("kod")] public string Kod { get; set; } = string.Empty;
}

/// <summary>
/// Doğrulama sonucu — kısa ömürlü BİLET.
/// </summary>
/// <remarks>
/// Bildirim gönderirken kodu tekrar istemek yerine bir bilet veriliyor:
/// kullanıcı formu doldururken kod süresi dolabilir ve baştan başlaması
/// gerekirdi. Bilet yalnızca bu telefona ve kısa bir süreye bağlı.
/// </remarks>
public class DogrulamaSonucuDto
{
    [JsonPropertyName("bilet")] public string Bilet { get; set; } = string.Empty;
    [JsonPropertyName("gecerlilik")] public DateTime Gecerlilik { get; set; }
}

/// <summary>
/// Vatandaş bildirimi gönderme isteği.
/// </summary>
/// <remarks>
/// Fotoğraf burada YOK: çok parçalı gövde ile ayrı bir uçtan yükleniyor.
/// Base64 gömmek istek boyutunu üçte bir büyütür ve mobil bağlantıda
/// kopan bir yükleme bütün formu kaybettirirdi.
/// </remarks>
public class VatandasBildirimiIstegiDto
{
    [Required(ErrorMessage = "Ad soyad zorunlu.")]
    [MaxLength(150)]
    [JsonPropertyName("adSoyad")] public string AdSoyad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon numarası zorunlu.")]
    [MaxLength(20)]
    [JsonPropertyName("telefon")] public string Telefon { get; set; } = string.Empty;

    /// <summary>Telefon doğrulamasından alınan bilet.</summary>
    [Required(ErrorMessage = "Telefon doğrulaması gerekli.")]
    [JsonPropertyName("bilet")] public string Bilet { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konu zorunlu.")]
    [MaxLength(300)]
    [JsonPropertyName("konu")] public string Konu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama zorunlu.")]
    [MaxLength(4000)]
    [JsonPropertyName("aciklama")] public string Aciklama { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Enlem -90 ile 90 arasında olmalı.")]
    [JsonPropertyName("enlem")] public double? Enlem { get; set; }

    [Range(-180, 180, ErrorMessage = "Boylam -180 ile 180 arasında olmalı.")]
    [JsonPropertyName("boylam")] public double? Boylam { get; set; }

    [MaxLength(500)]
    [JsonPropertyName("adres")] public string? Adres { get; set; }

    [JsonPropertyName("mahalleId")] public long? MahalleId { get; set; }
}

/// <summary>
/// Bildirim yanıtı — yalnızca TAKİP NUMARASI.
/// </summary>
/// <remarks>
/// Hiçbir iç bilgi dönmüyor: birim, personel, görev kimliği. Anonim bir uçtan
/// dönen her alan, kurumun iç yapısı hakkında dışarıya verilmiş bilgidir.
/// </remarks>
public class VatandasBildirimiSonucuDto
{
    [JsonPropertyName("takipNo")] public string TakipNo { get; set; } = string.Empty;

    /// <summary>Fotoğraf yüklemek için kullanılacak kısa ömürlü anahtar.</summary>
    [JsonPropertyName("yuklemeAnahtari")] public string YuklemeAnahtari { get; set; } = string.Empty;
}

/// <summary>Karşılama ekranının satırı — PERSONELE açık, vatandaşa değil.</summary>
public class VatandasBildirimiDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("takipNo")] public string TakipNo { get; set; } = string.Empty;
    [JsonPropertyName("adSoyad")] public string AdSoyad { get; set; } = string.Empty;
    [JsonPropertyName("telefon")] public string Telefon { get; set; } = string.Empty;
    [JsonPropertyName("konu")] public string Konu { get; set; } = string.Empty;
    [JsonPropertyName("aciklama")] public string Aciklama { get; set; } = string.Empty;

    [JsonPropertyName("enlem")] public double? Enlem { get; set; }
    [JsonPropertyName("boylam")] public double? Boylam { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }
    [JsonPropertyName("mahalleId")] public long? MahalleId { get; set; }
    [JsonPropertyName("mahalleAd")] public string? MahalleAd { get; set; }

    [JsonPropertyName("durum")] public VatandasBildirimDurumu Durum { get; set; }
    [JsonPropertyName("durumAd")] public string DurumAd { get; set; } = string.Empty;
    [JsonPropertyName("durumRenk")] public string DurumRenk { get; set; } = string.Empty;

    [JsonPropertyName("birimId")] public long? BirimId { get; set; }
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }
    [JsonPropertyName("gorevId")] public long? GorevId { get; set; }
    [JsonPropertyName("gorevTakipNo")] public string? GorevTakipNo { get; set; }

    [JsonPropertyName("islemNotu")] public string? IslemNotu { get; set; }
    [JsonPropertyName("isleyen")] public string? Isleyen { get; set; }
    [JsonPropertyName("islemTarihi")] public DateTime? IslemTarihi { get; set; }
    [JsonPropertyName("olusturmaTarihi")] public DateTime OlusturmaTarihi { get; set; }

    [JsonPropertyName("ekSayisi")] public int EkSayisi { get; set; }

    /// <summary>
    /// Vatandaşın gönderdiği dosyalar — <b>şikayetin fotoğrafı burada</b>.
    /// </summary>
    /// <remarks>
    /// Yalnızca sayı gönderiliyordu; karşılama personeli "2 ek" yazısını
    /// görüyor ama çukurun fotoğrafını göremiyordu. Oysa bildirimi
    /// değerlendirmenin — hangi birime gideceğine, acil olup olmadığına karar
    /// vermenin — en hızlı yolu resme bakmak.
    /// </remarks>
    [JsonPropertyName("ekler")] public List<IsEkDto> Ekler { get; set; } = [];

    /// <summary>
    /// Aynı numaradan gelen ÖNCEKİ bildirim sayısı.
    /// </summary>
    /// <remarks>
    /// Mükerrer bildirim karşılama ekranının en sık işi. Sayı görünmeseydi
    /// personel her kaydı sıfırdan değerlendirir ve aynı çukur için beş ayrı
    /// görev açılırdı.
    /// </remarks>
    [JsonPropertyName("ayniNumaradanOnceki")] public int AyniNumaradanOnceki { get; set; }
}

/// <summary>Bildirimi bir birime yönlendirme isteği.</summary>
public class BildirimYonlendirmeDto
{
    [JsonPropertyName("birimId")] public long BirimId { get; set; }

    /// <summary>Açılacak görevin tipi. Boşsa görev aşamasız açılır.</summary>
    [JsonPropertyName("gorevTipiId")] public long? GorevTipiId { get; set; }

    [JsonPropertyName("oncelik")] public GorevOnceligi? Oncelik { get; set; }

    [MaxLength(1000)]
    [JsonPropertyName("not")] public string? Not { get; set; }
}

/// <summary>Bildirimi reddetme isteği — gerekçe ZORUNLU.</summary>
public class BildirimRetDto
{
    [Required(ErrorMessage = "Ret gerekçesi zorunlu.")]
    [MaxLength(1000)]
    [JsonPropertyName("not")] public string Not { get; set; } = string.Empty;
}

/// <summary>
/// SAHA TESPİTİ — personelin yerinde gördüğü sorun.
/// </summary>
/// <remarks>
/// Vatandaş bildiriminden farklı olarak karşılama adımı YOK: tespiti yapan
/// zaten kurumun personeli ve hangi birimin işi olduğunu biliyor. Kayıt
/// doğrudan kendi biriminin görevi olarak açılıyor.
/// </remarks>
public class SahaTespitiDto
{
    [Required(ErrorMessage = "Başlık zorunlu.")]
    [MaxLength(300)]
    [JsonPropertyName("baslik")] public string Baslik { get; set; } = string.Empty;

    [MaxLength(4000)]
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }

    [JsonPropertyName("gorevTipiId")] public long? GorevTipiId { get; set; }
    [JsonPropertyName("oncelik")] public GorevOnceligi? Oncelik { get; set; }

    [Range(-90, 90, ErrorMessage = "Enlem -90 ile 90 arasında olmalı.")]
    [JsonPropertyName("enlem")] public double? Enlem { get; set; }

    [Range(-180, 180, ErrorMessage = "Boylam -180 ile 180 arasında olmalı.")]
    [JsonPropertyName("boylam")] public double? Boylam { get; set; }

    [MaxLength(500)]
    [JsonPropertyName("adres")] public string? Adres { get; set; }

    [JsonPropertyName("mahalleId")] public long? MahalleId { get; set; }

    /// <summary>Tespiti yapan kendine atansın mı? Sahada varsayılan EVET.</summary>
    [JsonPropertyName("kendimeAta")] public bool KendimeAta { get; set; } = true;
}

/// <summary>
/// Harita noktası — görev ya da bildirim.
/// </summary>
/// <remarks>
/// Ad <c>IsHaritaNoktasiDto</c>, sade <c>HaritaNoktasiDto</c> DEĞİL: o ad
/// randevu haritasında zaten kullanılıyor. Kısa DTO adları global tekil
/// olmalı — çakışma <c>/swagger/v2/swagger.json</c>'ı 500'e düşürüyor ve
/// hata yalnızca belge üretilirken görünüyor.
/// </remarks>
public class IsHaritaNoktasiDto
{
    [JsonPropertyName("id")] public long Id { get; set; }

    /// <summary><c>gorev</c> ya da <c>bildirim</c>.</summary>
    [JsonPropertyName("tur")] public string Tur { get; set; } = string.Empty;

    [JsonPropertyName("takipNo")] public string TakipNo { get; set; } = string.Empty;
    [JsonPropertyName("baslik")] public string Baslik { get; set; } = string.Empty;
    [JsonPropertyName("enlem")] public double Enlem { get; set; }
    [JsonPropertyName("boylam")] public double Boylam { get; set; }
    [JsonPropertyName("renk")] public string Renk { get; set; } = string.Empty;
    [JsonPropertyName("durumAd")] public string DurumAd { get; set; } = string.Empty;
    [JsonPropertyName("gecikti")] public bool Gecikti { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }

    /// <summary>
    /// Kaydın temsilî fotoğrafının API adresi — yoksa <c>null</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Haritada bir noktaya dokunan kişinin ilk sorusu "burada ne var?"
    /// Adres ve durum bunu ancak kısmen anlatıyor; çukurun ya da kırık
    /// direğin fotoğrafı tek karede anlatıyor.
    /// </para>
    /// <para>
    /// <b>Adresi sunucu kuruyor.</b> İndirme ucu kayıt türüne göre değişiyor:
    /// görev ekleri <c>gorev.goruntule</c>, vatandaş bildirimi ekleri
    /// <c>bildirim.karsila</c> istiyor. İstemcinin bu eşlemeyi bilmesi
    /// gerekmiyor ve bilmesi, izin kurallarının ikinci bir kopyasını orada
    /// tutmak olurdu.
    /// </para>
    /// </remarks>
    [JsonPropertyName("fotograf")] public string? Fotograf { get; set; }
}

/// <summary>Gelen kutusu kaydı — birimden birime devir.</summary>
public class GelenKutusuDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("hedefBirimId")] public long HedefBirimId { get; set; }
    [JsonPropertyName("hedefBirimAd")] public string? HedefBirimAd { get; set; }

    [JsonPropertyName("kaynakGorevId")] public long KaynakGorevId { get; set; }
    [JsonPropertyName("kaynakTakipNo")] public string? KaynakTakipNo { get; set; }
    [JsonPropertyName("kaynakBirimId")] public long KaynakBirimId { get; set; }
    [JsonPropertyName("kaynakBirimAd")] public string? KaynakBirimAd { get; set; }
    [JsonPropertyName("hedefGorevTipiId")] public long? HedefGorevTipiId { get; set; }

    [JsonPropertyName("konu")] public string Konu { get; set; } = string.Empty;
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }

    /// <summary>İş talebi mi (kabul/ret gerekir) yoksa yalnızca bilgi mi?</summary>
    [JsonPropertyName("isTalebi")] public bool IsTalebi { get; set; }

    [JsonPropertyName("durum")] public GelenKutusuDurumu Durum { get; set; }
    [JsonPropertyName("durumAd")] public string DurumAd { get; set; } = string.Empty;
    [JsonPropertyName("durumRenk")] public string DurumRenk { get; set; } = string.Empty;

    [JsonPropertyName("gorevId")] public long? GorevId { get; set; }
    [JsonPropertyName("gorevTakipNo")] public string? GorevTakipNo { get; set; }
    [JsonPropertyName("gerekce")] public string? Gerekce { get; set; }
    [JsonPropertyName("isleyen")] public string? Isleyen { get; set; }
    [JsonPropertyName("islemTarihi")] public DateTime? IslemTarihi { get; set; }

    [JsonPropertyName("enlem")] public double? Enlem { get; set; }
    [JsonPropertyName("boylam")] public double? Boylam { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }
    [JsonPropertyName("olusturmaTarihi")] public DateTime OlusturmaTarihi { get; set; }
}

/// <summary>Gelen kutusu kaydını kabul etme isteği.</summary>
public class GelenKutusuKabulDto
{
    /// <summary>Açılacak görevin tipi. Boşsa devir kuralındaki tip kullanılır.</summary>
    [JsonPropertyName("gorevTipiId")] public long? GorevTipiId { get; set; }

    [JsonPropertyName("oncelik")] public GorevOnceligi? Oncelik { get; set; }
}

/// <summary>Gelen kutusu kaydını reddetme isteği — gerekçe ZORUNLU.</summary>
public class GelenKutusuRetDto
{
    [Required(ErrorMessage = "Ret gerekçesi zorunlu.")]
    [MaxLength(1000)]
    [JsonPropertyName("gerekce")] public string Gerekce { get; set; } = string.Empty;
}

/// <summary>
/// BİRİM KARNESİ — gecikme panosunun satırı.
/// </summary>
/// <remarks>
/// Mevcut <c>TalepIstatistikDto</c> genişletilmedi: tek DTO'ya sıkıştırmak,
/// iki ekranın da ötekinin alanlarını taşıması demekti.
/// </remarks>
public class BirimKarnesiDto
{
    [JsonPropertyName("birimId")] public long BirimId { get; set; }
    [JsonPropertyName("birimAd")] public string BirimAd { get; set; } = string.Empty;

    [JsonPropertyName("acik")] public int Acik { get; set; }
    [JsonPropertyName("tamamlanan")] public int Tamamlanan { get; set; }
    [JsonPropertyName("geciken")] public int Geciken { get; set; }

    /// <summary>Süresinde tamamlananların oranı (0-100). Ölçülemiyorsa <c>null</c>.</summary>
    [JsonPropertyName("zamanindaOran")] public int? ZamanindaOran { get; set; }

    /// <summary>Tamamlanan işlerin ortalama süresi (saat). Ölçülemiyorsa <c>null</c>.</summary>
    [JsonPropertyName("ortalamaSaat")] public double? OrtalamaSaat { get; set; }
}

/// <summary>Gecikme panosu.</summary>
public class IsIstatistikDto
{
    [JsonPropertyName("acik")] public int Acik { get; set; }
    [JsonPropertyName("geciken")] public int Geciken { get; set; }
    [JsonPropertyName("onayBekleyen")] public int OnayBekleyen { get; set; }
    [JsonPropertyName("atanmamis")] public int Atanmamis { get; set; }
    [JsonPropertyName("bugunTamamlanan")] public int BugunTamamlanan { get; set; }

    /// <summary>Bekleyen vatandaş bildirimi — karşılama kuyruğu.</summary>
    [JsonPropertyName("bekleyenBildirim")] public int BekleyenBildirim { get; set; }

    /// <summary>Bekleyen gelen kutusu kaydı.</summary>
    [JsonPropertyName("bekleyenDevir")] public int BekleyenDevir { get; set; }

    [JsonPropertyName("birimler")] public List<BirimKarnesiDto> Birimler { get; set; } = [];

    /// <summary>Durum dağılımı — mevcut dilim tipi YENİDEN KULLANILIYOR.</summary>
    [JsonPropertyName("durumDagilimi")] public List<IstatistikDilimDto> DurumDagilimi { get; set; } = [];

    /// <summary>En çok geciken görevler — panonun eyleme dönük kısmı.</summary>
    [JsonPropertyName("gecikenler")] public List<GorevOzetDto> Gecikenler { get; set; } = [];
}
