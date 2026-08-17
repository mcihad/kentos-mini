using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using KentOS.Mini.Application.Enums;

namespace KentOS.Mini.Application.Dto.V2.IsTakip;

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
}
