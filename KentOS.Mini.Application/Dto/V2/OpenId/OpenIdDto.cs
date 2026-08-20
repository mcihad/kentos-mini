using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.V2.OpenId;

/// <summary>
/// Yetkilinin gördüğü ayar — <b>istemci sırrı hariç</b>.
/// </summary>
public sealed class OpenIdAyarDto
{
    [JsonPropertyName("etkin")]
    public bool Etkin { get; set; }

    [JsonPropertyName("gorunenAd")]
    public string? GorunenAd { get; set; }

    [JsonPropertyName("yetkili")]
    public string? Yetkili { get; set; }

    [JsonPropertyName("istemciId")]
    public string? IstemciId { get; set; }

    /// <summary>
    /// Sır KENDİSİ değil, VARLIĞI. Ekran "tanımlı" yazabilsin diye.
    /// </summary>
    /// <remarks>
    /// Sırrı maskeli de olsa göndermek gereksiz: ekranın tek ihtiyacı
    /// "yeniden yazmam gerekiyor mu?" sorusunun cevabı.
    /// </remarks>
    [JsonPropertyName("sirTanimli")]
    public bool SirTanimli { get; set; }

    [JsonPropertyName("kapsamlar")]
    public string? Kapsamlar { get; set; }

    [JsonPropertyName("kullaniciAdiTalebi")]
    public string? KullaniciAdiTalebi { get; set; }

    [JsonPropertyName("otomatikKullaniciOlustur")]
    public bool OtomatikKullaniciOlustur { get; set; }

    /// <summary>
    /// Sağlayıcıya kaydedilecek dönüş adresi — ekranda gösterilir.
    /// </summary>
    /// <remarks>
    /// Yapılandırmanın en sık yanlış giren parçası bu ve sağlayıcı tarafında
    /// birebir eşleşmesi gerekiyor. Ekranda kopyalanabilir durması, "redirect
    ///_uri mismatch" hatasını baştan kesiyor.
    /// </remarks>
    [JsonPropertyName("donusAdresi")]
    public string? DonusAdresi { get; set; }
}

/// <summary>Ayar yazma isteği.</summary>
public sealed class OpenIdAyarIstegi
{
    [JsonPropertyName("etkin")]
    public bool Etkin { get; set; }

    [JsonPropertyName("gorunenAd")]
    public string? GorunenAd { get; set; }

    [JsonPropertyName("yetkili")]
    public string? Yetkili { get; set; }

    [JsonPropertyName("istemciId")]
    public string? IstemciId { get; set; }

    /// <summary>
    /// Yeni istemci sırrı. <b>Boş bırakmak "değiştirme" demektir.</b>
    /// </summary>
    /// <remarks>
    /// Okuma ucu sırrı dönmediği için ekran onu forma dolduramıyor; boş
    /// gönderileni "sil" saymak, ayarı her açıp kaydedende girişi bozardı.
    /// </remarks>
    [JsonPropertyName("istemciSirri")]
    public string? IstemciSirri { get; set; }

    [JsonPropertyName("kapsamlar")]
    public string? Kapsamlar { get; set; }

    [JsonPropertyName("kullaniciAdiTalebi")]
    public string? KullaniciAdiTalebi { get; set; }

    [JsonPropertyName("otomatikKullaniciOlustur")]
    public bool OtomatikKullaniciOlustur { get; set; }
}

/// <summary>
/// Giriş ekranının gördüğü — <b>anonim</b>, yalnızca düğmeyi çizmeye yetecek kadar.
/// </summary>
public sealed class OpenIdGirisDto
{
    /// <summary>Düğme çizilsin mi? Ayar açık VE sağlayıcı erişilebilir ise.</summary>
    [JsonPropertyName("kullanilabilir")]
    public bool Kullanilabilir { get; set; }

    /// <summary>Düğme metni: "<c>{GorunenAd}</c> ile giriş yap".</summary>
    [JsonPropertyName("gorunenAd")]
    public string? GorunenAd { get; set; }
}

/// <summary>Ayar ekranındaki "bağlantıyı sına" sonucu.</summary>
public sealed class OpenIdSinamaDto
{
    [JsonPropertyName("basarili")]
    public bool Basarili { get; set; }

    [JsonPropertyName("mesaj")]
    public string Mesaj { get; set; } = string.Empty;

    /// <summary>Keşif belgesinden okunan yetkilendirme adresi.</summary>
    [JsonPropertyName("yetkilendirmeAdresi")]
    public string? YetkilendirmeAdresi { get; set; }
}
