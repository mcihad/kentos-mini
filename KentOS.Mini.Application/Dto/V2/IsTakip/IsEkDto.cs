using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.V2.IsTakip;

/// <summary>Bir ek — listede ve detayda aynı biçim.</summary>
/// <remarks>
/// <para>
/// <b>Web katmanından buraya taşındı.</b> Yeri baştan burasıydı — bütün DTO'lar
/// bu projede duruyor — ama taşınmasını zorunlu kılan somut sebep şu: aşama ve
/// vatandaş bildirimi DTO'ları artık <b>ek listesini</b> taşıyor ve
/// <c>KentOS.Mini.Application</c> hiçbir şeye bağımlı değil, dolayısıyla Web
/// içindeki bir tipi göremezdi.
/// </para>
/// <para>
/// <b>JSON alan adlarının hiçbiri değişmedi</b>; taşınan yalnızca tipin ad
/// alanı. Sözleşme anlık görüntüsü Application derlemesini tarıyor, bu yüzden
/// bu tip oraya <b>eklenmiş</b> görünüyor — eski bir satır kaybolmuyor.
/// </para>
/// </remarks>
public class IsEkDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("icerikTuru")] public string? IcerikTuru { get; set; }
    [JsonPropertyName("boyut")] public long Boyut { get; set; }

    /// <summary>Tarayıcıda gösterilebilir bir görsel mi?</summary>
    /// <remarks>
    /// Sunucu karar veriyor: içerik türü beyaz listeden geçtiyse resim.
    /// İstemcinin dosya adının uzantısına bakması, ".jpg" adlı bir PDF'i
    /// <c>&lt;img&gt;</c> içine koymak olurdu.
    /// </remarks>
    [JsonPropertyName("resimMi")] public bool ResimMi { get; set; }

    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("yukleyen")] public string? Yukleyen { get; set; }
    [JsonPropertyName("tarih")] public DateTime Tarih { get; set; }
}
