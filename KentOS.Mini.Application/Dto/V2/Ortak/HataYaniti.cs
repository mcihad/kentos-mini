using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.V2.Ortak;

/// <summary>
/// v2'nin tek tip hata gövdesi — RFC 7807 (Problem Details) biçiminde.
///
/// <para>
/// v1 kendi <c>ErrorResponseDto { Code, Message }</c> biçimini kullanıyor ve o
/// mobil uygulamanın sözleşmesi olduğu için <b>değiştirilemez</b>. v2 baştan
/// standart biçimi kullanır: istemci tarafında tek bir hata işleyici yazmak,
/// alan bazlı doğrulama hatalarını forma dağıtmak bu sayede mümkün olur.
/// </para>
/// </summary>
public class HataYaniti
{
    /// <summary>Hata türünü tanımlayan URI (RFC 7807 <c>type</c>).</summary>
    [JsonPropertyName("type")]
    public string Tur { get; set; } = "about:blank";

    /// <summary>Kısa, insan-okur başlık. Örn. "Doğrulama hatası".</summary>
    [JsonPropertyName("title")]
    public string Baslik { get; set; } = string.Empty;

    /// <summary>HTTP durum kodu.</summary>
    [JsonPropertyName("status")]
    public int Durum { get; set; }

    /// <summary>Bu olaya özgü açıklama. Kullanıcıya gösterilebilir.</summary>
    [JsonPropertyName("detail")]
    public string? Ayrinti { get; set; }

    /// <summary>İsteğin yolu.</summary>
    [JsonPropertyName("instance")]
    public string? Ornek { get; set; }

    /// <summary>
    /// Alan bazlı doğrulama hataları: <c>{ "baslik": ["Başlık zorunludur"] }</c>.
    /// Yalnızca 400 doğrulama hatalarında dolu gelir.
    /// </summary>
    [JsonPropertyName("hatalar")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string[]>? Hatalar { get; set; }

    /// <summary>
    /// Sunucu hatalarında günlüklerle eşleştirmek için üretilen kimlik.
    /// Kullanıcıya "destek ekibine şu numarayı verin" demek için.
    /// </summary>
    [JsonPropertyName("izKimligi")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IzKimligi { get; set; }
}
