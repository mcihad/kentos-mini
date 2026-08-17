using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.V2.Referans;

/// <summary>
/// Renkli bir tanım kaydı (etkinlik tipi, etkinlik durumu, talep durumu).
///
/// <para>
/// Üç varlık da aynı şekle sahip ama sütun adları farklı
/// (<c>RandevuDurum.DurumAd</c> / <c>Simge</c>, diğerlerinde <c>Ad</c> /
/// <c>Icon</c>). Tek DTO kullanmak istemciyi bu tarihsel tutarsızlıktan
/// korur; eşleme sunucuda yapılır.
/// </para>
/// </summary>
public class TanimDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("ad")]
    public string Ad { get; set; } = string.Empty;

    /// <summary>
    /// Onaltılık renk (`#RRGGBB`). Takvim, listeler ve rozetler bunu kullanır.
    /// </summary>
    /// <remarks>
    /// Bu alan <b>kullanıcı tanımlıdır</b> ve arayüzün renk kaynağıdır —
    /// tema tokenı değildir. Eski sistemde takvim etkinlikleri
    /// <c>AjandaDurum.Renk</c> + `80` (yarı saydam) ile boyanıyordu; yeni
    /// arayüz de rengi buradan alır.
    /// </remarks>
    [JsonPropertyName("renk")]
    public string? Renk { get; set; }

    [JsonPropertyName("simge")]
    public string? Simge { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }

    /// <summary>Bu tanımı kullanan kayıt sayısı — silmeden önce uyarı için.</summary>
    [JsonPropertyName("kullanimSayisi")]
    public int KullanimSayisi { get; set; }
}

/// <summary>Tanım oluşturma/güncelleme isteği.</summary>
public class TanimIstegi
{
    [JsonPropertyName("ad")]
    public string Ad { get; set; } = string.Empty;

    [JsonPropertyName("renk")]
    public string? Renk { get; set; }

    [JsonPropertyName("simge")]
    public string? Simge { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }
}

/// <summary>Yalnızca adı olan basit kayıt (mahalle, meslek).</summary>
public class AdKaydiDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("ad")]
    public string Ad { get; set; } = string.Empty;

    /// <summary>Bu kaydı kullanan talep sayısı (meslekte hesaplanmaz).</summary>
    [JsonPropertyName("kullanimSayisi")]
    public int KullanimSayisi { get; set; }
}

/// <summary>Ad kaydı oluşturma/güncelleme isteği.</summary>
public class AdKaydiIstegi
{
    [JsonPropertyName("ad")]
    public string Ad { get; set; } = string.Empty;
}

/// <summary>Toplu içe aktarma isteği.</summary>
/// <remarks>
/// Eski arayüz `.txt` dosyası yüklüyordu. v2 satır listesi alır: dosya
/// ayrıştırma istemci tarafında kalır, sunucu yalnızca veriyi görür. Böylece
/// aynı uç kopyala-yapıştır ile de kullanılabilir.
/// </remarks>
public class TopluIceAktarmaIstegi
{
    [JsonPropertyName("satirlar")]
    public List<string> Satirlar { get; set; } = [];

    /// <summary>Var olan adlar atlanır (varsayılan). Kapalıysa kopya oluşur.</summary>
    [JsonPropertyName("kopyalariAtla")]
    public bool KopyalariAtla { get; set; } = true;
}

/// <summary>Toplu içe aktarma sonucu.</summary>
public class TopluIceAktarmaSonucu
{
    [JsonPropertyName("okunanSatir")]
    public int OkunanSatir { get; set; }

    [JsonPropertyName("eklenen")]
    public int Eklenen { get; set; }

    [JsonPropertyName("atlanan")]
    public int Atlanan { get; set; }

    [JsonPropertyName("mesaj")]
    public string Mesaj { get; set; } = string.Empty;
}
