using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using KentOS.Mini.Application.Enums;

namespace KentOS.Mini.Application.Dto.V2.IsTakip;

/// <summary>
/// GÖREV TİPİ — hizmet standardının tanımı.
/// </summary>
/// <remarks>
/// Tip yalnızca bir etiket değil: kaç aşamadan geçileceğini, her aşamada ne
/// kanıt isteneceğini ve işin kaç saatte bitmesi gerektiğini o söylüyor.
/// Görev açılırken bunların hepsi <b>kopyalanıyor</b>.
/// </remarks>
public class GorevTipiDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("renk")] public string? Renk { get; set; }

    /// <summary>Vatandaşa taahhüt edilen süre (gün).</summary>
    [JsonPropertyName("hizmetStandardiGun")] public int? HizmetStandardiGun { get; set; }

    /// <summary>İç hedef (saat) — SLA sayacı bunu kullanır.</summary>
    [JsonPropertyName("slaSaat")] public int? SlaSaat { get; set; }

    [JsonPropertyName("varsayilanOncelik")] public GorevOnceligi VarsayilanOncelik { get; set; }
    [JsonPropertyName("varsayilanOncelikAd")] public string VarsayilanOncelikAd { get; set; } = string.Empty;
    [JsonPropertyName("konumZorunlu")] public bool KonumZorunlu { get; set; }
    [JsonPropertyName("kullanimda")] public bool Kullanimda { get; set; }

    /// <summary>Tipi tanımlayan birim. Boşsa kurum geneli.</summary>
    [JsonPropertyName("birimId")] public long? BirimId { get; set; }
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }

    [JsonPropertyName("asamalar")] public List<GorevTipiAsamaDto> Asamalar { get; set; } = [];

    /// <summary>Bu tipi kullanabilen birimler. BOŞ = herkes kullanabilir.</summary>
    [JsonPropertyName("birimIdler")] public List<long> BirimIdler { get; set; } = [];

    [JsonPropertyName("devirler")] public List<GorevTipiDevirDto> Devirler { get; set; } = [];

    /// <summary>Bu tiple açılmış görev sayısı — silme kapısı arayüzde görünsün diye.</summary>
    [JsonPropertyName("gorevSayisi")] public int GorevSayisi { get; set; }
}

/// <summary>Tipteki bir aşama tanımı.</summary>
public class GorevTipiAsamaDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("siraNo")] public int SiraNo { get; set; }

    [Required(ErrorMessage = "Aşama adı zorunlu.")]
    [MaxLength(200)]
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;

    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }

    /// <summary>Atlanabilir mi? Zorunlu aşama tamamlanmadan görev bitirilemez.</summary>
    [JsonPropertyName("zorunlu")] public bool Zorunlu { get; set; } = true;

    [JsonPropertyName("aciklamaZorunlu")] public bool AciklamaZorunlu { get; set; }
    [JsonPropertyName("fotografZorunlu")] public bool FotografZorunlu { get; set; }
    [JsonPropertyName("tahminiSaat")] public int? TahminiSaat { get; set; }
}

/// <summary>Tamamlanınca başka birime düşecek kayıt.</summary>
public class GorevTipiDevirDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("hedefBirimId")] public long HedefBirimId { get; set; }
    [JsonPropertyName("hedefBirimAd")] public string? HedefBirimAd { get; set; }

    /// <summary>İş talebi mi (kabul/ret gerekir) yoksa yalnızca bilgi mi?</summary>
    [JsonPropertyName("isTalebi")] public bool IsTalebi { get; set; }

    [JsonPropertyName("not")] public string? Not { get; set; }
    [JsonPropertyName("hedefGorevTipiId")] public long? HedefGorevTipiId { get; set; }
}

/// <summary>Görev tipi kaydetme isteği.</summary>
/// <remarks>
/// Aşamalar, birimler ve devirler <b>tam liste</b> olarak gönderilir: gövdede
/// olmayan satır silinir. Tek tek ekle/çıkar uçları açmak, arayüzün bir
/// aşamayı taşırken üç ayrı istek atmasını ve yarısı başarısız olduğunda
/// tutarsız bir tanım bırakmasını gerektirirdi.
/// </remarks>
public class GorevTipiKayitDto
{
    [Required(ErrorMessage = "Görev tipi adı zorunlu.")]
    [MaxLength(200)]
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;

    [MaxLength(1000)]
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }

    [MaxLength(20)]
    [JsonPropertyName("renk")] public string? Renk { get; set; }

    [Range(0, 3650, ErrorMessage = "Hizmet standardı 0-3650 gün arasında olmalı.")]
    [JsonPropertyName("hizmetStandardiGun")] public int? HizmetStandardiGun { get; set; }

    [Range(0, 87600, ErrorMessage = "SLA süresi 0-87600 saat arasında olmalı.")]
    [JsonPropertyName("slaSaat")] public int? SlaSaat { get; set; }

    [JsonPropertyName("varsayilanOncelik")] public GorevOnceligi VarsayilanOncelik { get; set; } = GorevOnceligi.Normal;
    [JsonPropertyName("konumZorunlu")] public bool KonumZorunlu { get; set; }
    [JsonPropertyName("kullanimda")] public bool Kullanimda { get; set; } = true;

    [JsonPropertyName("asamalar")] public List<GorevTipiAsamaDto> Asamalar { get; set; } = [];
    [JsonPropertyName("birimIdler")] public List<long> BirimIdler { get; set; } = [];
    [JsonPropertyName("devirler")] public List<GorevTipiDevirDto> Devirler { get; set; } = [];
}
