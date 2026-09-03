using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto.V2.Cicek;

/// <summary>
/// Çiçekçinin SMS'teki bağlantıdan gördüğü kart — <b>giriş gerektirmez</b>.
/// </summary>
/// <remarks>
/// <para>
/// Çiçekçi kurumun kullanıcısı değil: hesabı, rolü, jetonu yok. Kart bağlantısı
/// ona SMS ile gidiyor ve tek yetki belirteci <b>tahmin edilemez GUID</b>.
/// Bu yüzden yanıt, işi yapmaya yetecek en az bilgiyi taşır.
/// </para>
/// <para>
/// <b>Doğrulama kodu BURADA YOKTUR.</b> Eski uç tam <c>CicekDto</c> döndürüyordu
/// ve içinde <c>dogrulamaKodu</c> vardı; bağlantıyı açan kart sayfası kodu
/// kendisi görüyordu. Kod, çiçeği gerçekten teslim edenin elinde olduğunu
/// kanıtlayan tek şey — kartla birlikte gösterilirse doğrulama adımı hiçbir şey
/// doğrulamaz.
/// </para>
/// <para>
/// Etkinliğin tamamı da dönmez: çiçekçinin katılımcı listesine, notlara ya da
/// irtibat telefonlarına ihtiyacı yok. Yalnızca hangi etkinlik, ne zaman ve
/// nereye.
/// </para>
/// </remarks>
public class CicekTeslimKartiDto
{
    /// <summary>Etkinliğin başlığı — çiçeğin hangi iş için olduğunu söyler.</summary>
    [JsonPropertyName("etkinlikBasligi")] public string EtkinlikBasligi { get; set; } = string.Empty;

    /// <summary>Etkinliğin başlangıcı.</summary>
    [JsonPropertyName("etkinlikTarihi")] public DateTime EtkinlikTarihi { get; set; }

    /// <summary>Etkinliğin yeri — çiçeğin gideceği adres bundan ayrı olabilir.</summary>
    [JsonPropertyName("etkinlikKonumu")] public string? EtkinlikKonumu { get; set; }

    /// <summary>Çiçeğin gönderileceği kişi ya da kurum.</summary>
    [JsonPropertyName("alici")] public string? Alici { get; set; }

    /// <summary>Teslim adresi.</summary>
    [JsonPropertyName("adres")] public string? Adres { get; set; }

    /// <summary>Kart notu — çiçeğin üzerine yazılacak metin.</summary>
    [JsonPropertyName("not")] public string? Not { get; set; }

    /// <summary>Kurum adı — çiçeğin kimin adına gönderildiği.</summary>
    [JsonPropertyName("kurumAdi")] public string? KurumAdi { get; set; }

    /// <summary>Teslim edildi olarak işaretlendi mi?</summary>
    [JsonPropertyName("teslimEdildi")] public bool TeslimEdildi { get; set; }

    /// <summary>Teslim zamanı — işaretlendiyse.</summary>
    /// <summary>
    /// Çiçekçinin yüklediği teslim fotoğrafının adresi.
    /// </summary>
    /// <remarks>
    /// Teslimin kanıtı: makam "çiçek gitti mi, nasıl gitti" sorusunu
    /// çiçekçiyi aramadan görüyor. Boşsa fotoğraf yüklenmemiş demektir —
    /// fotoğraf isteğe bağlı, teslimi engellemiyor.
    /// </remarks>
    [JsonPropertyName("fotograf")]
    public string? Fotograf { get; set; }

    [JsonPropertyName("teslimTarihi")] public DateTime? TeslimTarihi { get; set; }
}
