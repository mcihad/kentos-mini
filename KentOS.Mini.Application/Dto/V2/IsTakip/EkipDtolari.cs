using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.V2.IsTakip;

/// <summary>
/// EKİP — birime bağlı kalıcı çalışma grubu.
/// </summary>
/// <remarks>
/// Göreve ekip atandığında bildirim ÖNCE lidere gider; iş dağıtımını lider
/// yapar. Lider yoksa bildirim ekibin tamamına düşer — kimsenin haberi
/// olmayan bir atama, atama sayılmaz.
/// </remarks>
public class EkipDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("birimId")] public long BirimId { get; set; }
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }
    [JsonPropertyName("liderId")] public long? LiderId { get; set; }
    [JsonPropertyName("liderAd")] public string? LiderAd { get; set; }
    [JsonPropertyName("kullanimda")] public bool Kullanimda { get; set; }
    [JsonPropertyName("uyeSayisi")] public int UyeSayisi { get; set; }

    /// <summary>Ekibin üzerindeki AÇIK görev sayısı — silme kapısı ve iş yükü.</summary>
    [JsonPropertyName("acikGorevSayisi")] public int AcikGorevSayisi { get; set; }

    [JsonPropertyName("uyeler")] public List<EkipUyeDto> Uyeler { get; set; } = [];
}

/// <summary>Ekip üyesi.</summary>
public class EkipUyeDto
{
    [JsonPropertyName("kullaniciId")] public long KullaniciId { get; set; }
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }
    [JsonPropertyName("lider")] public bool Lider { get; set; }
}

/// <summary>Ekip kaydetme isteği.</summary>
/// <remarks>
/// Üye listesi <b>tam liste</b>: gövdede olmayan üye ekipten çıkarılır.
/// Görev tipi kaydıyla aynı gerekçe — yarısı başarısız olmuş bir dizi
/// ekle/çıkar isteği, ekibi kimin oluşturduğu belirsiz bir durumda bırakırdı.
/// </remarks>
public class EkipKayitDto
{
    [Required(ErrorMessage = "Ekip adı zorunlu.")]
    [MaxLength(200)]
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;

    [MaxLength(500)]
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }

    /// <summary>Ekip lideri. Üyeler arasında olmalı.</summary>
    [JsonPropertyName("liderId")] public long? LiderId { get; set; }

    [JsonPropertyName("kullanimda")] public bool Kullanimda { get; set; } = true;
    [JsonPropertyName("uyeIdler")] public List<long> UyeIdler { get; set; } = [];
}
