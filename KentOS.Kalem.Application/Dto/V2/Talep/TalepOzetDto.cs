using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto.V2.Talep;

/// <summary>
/// Talep listesi satırı.
///
/// <para>
/// v1'in <c>RandevuListDto</c>'su yerine ayrı bir DTO var çünkü liste
/// ekranının <b>renk</b> ve <b>ad</b> alanlarına ihtiyacı var. O DTO canlı
/// mobil sözleşmesinin parçası; alan eklemek teknik olarak zararsız olsa da
/// v1'i olduğu gibi bırakmak sözleşmeyi tartışmasız kılıyor.
/// </para>
/// </summary>
public class TalepOzetDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("konu")] public string Konu { get; set; } = string.Empty;

    [JsonPropertyName("ad")] public string? Ad { get; set; }
    [JsonPropertyName("soyad")] public string? Soyad { get; set; }
    [JsonPropertyName("adSoyad")] public string AdSoyad => $"{Ad} {Soyad}".Trim();
    [JsonPropertyName("meslek")] public string? Meslek { get; set; }
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }

    [JsonPropertyName("baslangicTarih")] public DateTime? BaslangicTarih { get; set; }
    [JsonPropertyName("bitisTarih")] public DateTime? BitisTarih { get; set; }

    [JsonPropertyName("birimId")] public long? BirimId { get; set; }
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }

    [JsonPropertyName("tipId")] public long? TipId { get; set; }
    [JsonPropertyName("tipAd")] public string? TipAd { get; set; }

    /// <summary>Tipin rengi (`#RRGGBB`) — liste ve rozet renklendirmesi.</summary>
    [JsonPropertyName("tipRenk")] public string? TipRenk { get; set; }

    [JsonPropertyName("durumId")] public long? DurumId { get; set; }
    [JsonPropertyName("durumAd")] public string? DurumAd { get; set; }

    /// <summary>
    /// Durumun rengi — <b>talep listesinin birincil renk kaynağı</b>.
    /// </summary>
    /// <remarks>
    /// Etkinliklerde olduğu gibi renk kayıtla birlikte gelir; istemci ayrı bir
    /// durum listesinde arama yapmak zorunda kalmaz ve durumu silinmiş bir
    /// talep de doğru renkte kalır.
    /// </remarks>
    [JsonPropertyName("durumRenk")] public string? DurumRenk { get; set; }

    [JsonPropertyName("mahalleId")] public long? MahalleId { get; set; }
    [JsonPropertyName("mahalleAd")] public string? MahalleAd { get; set; }

    [JsonPropertyName("ozgecmisVar")] public bool OzgecmisVar { get; set; }
    [JsonPropertyName("ajandayaEklendi")] public bool AjandayaEklendi { get; set; }
    [JsonPropertyName("arsivlendi")] public bool Arsivlendi { get; set; }

    [JsonPropertyName("notSayisi")] public int NotSayisi { get; set; }
    [JsonPropertyName("dosyaSayisi")] public int DosyaSayisi { get; set; }
}
