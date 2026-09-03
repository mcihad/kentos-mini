using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto.V2.Etkinlik;

/// <summary>
/// Takvim ve liste görünümleri için HAFİF etkinlik kaydı.
///
/// <para>
/// Neden ayrı bir özet tipi: v1'in <c>AjandaDto</c>'su fotoğrafları, çiçeği ve
/// katılımcıları da taşıyor. Bir ay görünümü yüzlerce kayıt çeker; detay
/// verisini de taşımak ağı ve belleği gereksiz doldurur.
/// </para>
///
/// <para>
/// <c>seriId</c> ve <c>seriAyrik</c> ZORUNLU: sürükleme işleminde, bırakma
/// anından ÖNCE tekrar kapsamı sorulup sorulmayacağına bunlarla karar verilir.
/// </para>
/// </summary>
public class EtkinlikOzetDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("baslik")] public string Baslik { get; set; } = string.Empty;
    [JsonPropertyName("baslangic")] public DateTime Baslangic { get; set; }
    [JsonPropertyName("bitis")] public DateTime? Bitis { get; set; }
    [JsonPropertyName("tumGun")] public bool TumGun { get; set; }
    [JsonPropertyName("konum")] public string? Konum { get; set; }

    [JsonPropertyName("tipId")] public long? TipId { get; set; }
    [JsonPropertyName("durumId")] public long? DurumId { get; set; }
    [JsonPropertyName("statu")] public int Statu { get; set; }

    /// <summary>Etkinlik tipinin adı — istemcide ikinci bir arama gerekmesin diye.</summary>
    [JsonPropertyName("tipAd")] public string? TipAd { get; set; }

    /// <summary>Etkinlik tipinin rengi (`#RRGGBB`).</summary>
    [JsonPropertyName("tipRenk")] public string? TipRenk { get; set; }

    [JsonPropertyName("durumAd")] public string? DurumAd { get; set; }

    /// <summary>
    /// Etkinlik durumunun rengi — <b>takvimin birincil renk kaynağı</b>.
    /// </summary>
    /// <remarks>
    /// Eski arayüz de aynı değeri kullanıyordu (<c>Durum.Renk</c> + `80`
    /// saydamlığı). Renk kayıtla birlikte geliyor; istemci <c>durumId</c>'yi
    /// ayrı bir listede aramak zorunda kalmıyor ve durumu SİLİNMİŞ bir
    /// etkinlik de doğru renkte kalıyor.
    /// </remarks>
    [JsonPropertyName("durumRenk")] public string? DurumRenk { get; set; }

    /// <summary>Gizli etkinlik. Buraya gelen her kayıt zaten görünürlük süzgecinden geçmiştir.</summary>
    [JsonPropertyName("gizli")] public bool Gizli { get; set; }

    [JsonPropertyName("seriId")] public long? SeriId { get; set; }
    [JsonPropertyName("seriAyrik")] public bool SeriAyrik { get; set; }

    /// <summary>
    /// "Fotoğraf eklenecek" HAZIRLIK BAYRAĞI — yüklenmiş fotoğraf değil.
    /// </summary>
    /// <remarks>
    /// Adı yanıltıyor: bu alan kullanıcının formda işaretlediği bir İSTEK
    /// ("bu etkinlikte fotoğraf çekilecek"), kayıtta gerçekten fotoğraf olup
    /// olmadığını söylemiyor. Gerçek durum için <see cref="ResimSayisi"/>.
    /// Ad v1 sözleşmesinin parçası olduğu için değiştirilmiyor.
    /// </remarks>
    [JsonPropertyName("resimVar")] public bool ResimVar { get; set; }

    /// <summary>
    /// Etkinliğe YÜKLENMİŞ fotoğraf sayısı — bayraktan bağımsız gerçek durum.
    /// </summary>
    /// <remarks>
    /// Alan eklendi çünkü ikisi ayrışıyordu: bir kullanıcı hazırlık kutusunu
    /// işaretlemeden fotoğraf yüklediğinde kayıtta fotoğraf VAR ama listede
    /// kamera işareti çıkmıyor, detayda rozet hiç çizilmiyor ve fotoğraf
    /// sorgusu bile açılmıyordu (istemci sorguyu <c>resimVar</c>'a bağlıyordu)
    /// — yüklenen dosyalar hiçbir ekranda görünmüyordu.
    /// </remarks>
    [JsonPropertyName("resimSayisi")] public int ResimSayisi { get; set; }

    [JsonPropertyName("basinKatilsin")] public bool BasinKatilsin { get; set; }

    /// <summary>Etkinliğin SAHİBİ birim.</summary>
    [JsonPropertyName("birimId")] public long? BirimId { get; set; }

    /// <summary>
    /// Sahip birimin adı — "bu kayıt kimin ajandasından?"
    /// </summary>
    /// <remarks>
    /// Davet edilen birim, etkinliği kendi kayıtlarıyla YAN YANA görüyor;
    /// hangisinin kendi işi, hangisinin davet olduğu ayırt edilemiyordu.
    /// </remarks>
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }

    /// <summary>
    /// Kaydın silindiği an — YALNIZCA silinmiş liste ucunda dolar.
    /// </summary>
    /// <remarks>
    /// Silinmiş listesi bugüne kadar yalnızca etkinliğin KENDİ tarihini
    /// gösteriyordu; ekrana bakan kişi "bunlar silinmiş mi, yoksa geçmiş
    /// etkinlikler mi?" diye ayırt edemiyordu. Alan eklemek v1 sözleşmesini
    /// bozmaz; diğer uçlarda <c>null</c> kalır ve istemci sütunu çizmez.
    /// </remarks>
    [JsonPropertyName("silinmeTarihi")] public DateTime? SilinmeTarihi { get; set; }
}

/// <summary>Takvim penceresi isteği.</summary>
public class AralikIstegi
{
    [JsonPropertyName("baslangic")] public DateTime Baslangic { get; set; }
    [JsonPropertyName("bitis")] public DateTime Bitis { get; set; }

    [JsonPropertyName("tipIdler")] public long[]? TipIdler { get; set; }
    [JsonPropertyName("durumIdler")] public long[]? DurumIdler { get; set; }
    [JsonPropertyName("arama")] public string? Arama { get; set; }
}

/// <summary>Yıl görünümü için gün başına sayaç.</summary>
public class GunSayaciDto
{
    [JsonPropertyName("gun")] public DateTime Gun { get; set; }
    [JsonPropertyName("adet")] public int Adet { get; set; }
}

/// <summary>
/// Sürükleme ile zaman değişikliği.
///
/// <para>
/// <b>Bilerek yalnızca zaman taşır.</b> Tekrar kuralı (<c>rrule</c>) ASLA
/// gönderilmez: istemciler kuralı formun başlangıç tarihinden türettiği için,
/// bir tekrarı başka güne sürüklemek sunucuda "kural değişti" gibi okunuyor,
/// seri bölünüyor ve etkinlik kaybolmuş görünüyordu. Bu uç nokta o yolu
/// yapısal olarak kapatır.
/// </para>
/// </summary>
public class ZamanIstegi
{
    [JsonPropertyName("baslangic")] public DateTime Baslangic { get; set; }
    [JsonPropertyName("bitis")] public DateTime? Bitis { get; set; }

    /// <summary>0 = yalnızca bu, 1 = bu ve sonrakiler, 2 = tüm seri.</summary>
    [JsonPropertyName("kapsam")] public int Kapsam { get; set; }
}
