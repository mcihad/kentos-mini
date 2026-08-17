using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.V2.Oturum;

/// <summary>
/// v2 giriş isteği.
///
/// <para>
/// <b>DataAnnotations YOK.</b> Olsaydı <c>[ApiController]</c> kendi otomatik
/// 400'ünü üretir ve v2'nin tek tip <c>HataYaniti</c> gövdesini atlardı.
/// Kurallar <c>GirisIstegiDogrulayici</c> içinde.
/// </para>
/// </summary>
public class GirisIstegi
{
    [JsonPropertyName("kullaniciAdi")]
    public string KullaniciAdi { get; set; } = string.Empty;

    [JsonPropertyName("parola")]
    public string Parola { get; set; } = string.Empty;
}

/// <summary>v2 giriş yanıtı.</summary>
public class GirisYaniti
{
    [JsonPropertyName("jeton")]
    public string Jeton { get; set; } = string.Empty;

    /// <summary>Jetonun geçerlilik sonu. İstemci bunu erken yenileme için kullanır.</summary>
    [JsonPropertyName("gecerlilikSonu")]
    public DateTime GecerlilikSonu { get; set; }
}

/// <summary>Oturum açan kullanıcının kimliği ve yetkileri.</summary>
public class BenYaniti
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("kullaniciAdi")]
    public string KullaniciAdi { get; set; } = string.Empty;

    [JsonPropertyName("ad")]
    public string? Ad { get; set; }

    [JsonPropertyName("soyad")]
    public string? Soyad { get; set; }

    [JsonPropertyName("tamAd")]
    public string TamAd { get; set; } = string.Empty;

    [JsonPropertyName("unvan")]
    public string? Unvan { get; set; }

    [JsonPropertyName("eposta")]
    public string? Eposta { get; set; }

    [JsonPropertyName("birimId")]
    public long? BirimId { get; set; }

    [JsonPropertyName("birimAd")]
    public string? BirimAd { get; set; }

    [JsonPropertyName("roller")]
    public string[] Roller { get; set; } = [];

    /// <summary>
    /// Gizli etkinlik OLUŞTURMA yetkisi — arayüz alanları buna göre gösterilir.
    /// </summary>
    /// <remarks>
    /// Görünürlükle ilgisi YOK: bu bayrak başkalarının gizli etkinliklerini
    /// açmaz. Arayüz yalnızca "gizli işaretle" kutusunu ve ona bağlı katılımcı
    /// uyarısını gösterip gizlemek için okur; asıl denetim
    /// <c>AjandaService.GizliEkleyebilirMiKontrolAsync</c> içinde.
    /// </remarks>
    [JsonPropertyName("gizliEtkinlikEkleyebilir")]
    public bool GizliEtkinlikEkleyebilir { get; set; }

    /// <summary>Dosya gönderimi başlatma yetkisi.</summary>
    /// <remarks>Almak yetki istemez; yalnızca göndermek.</remarks>
    [JsonPropertyName("dosyaGonderebilir")]
    public bool DosyaGonderebilir { get; set; }

    /// <summary>
    /// Sunucudaki yetki politikalarının istemci karşılığı.
    /// </summary>
    /// <remarks>
    /// Arayüzün hangi menüleri göstereceğine bununla karar verilir. Örneğin
    /// <c>Ajanda</c> politikası yalnızca Admin/Sekreter/Yonetici/Baskan'a açık;
    /// bunu bilmeyen bir arayüz, diğer rollere takvimi gösterip 403 duvarına
    /// çarptırırdı. <b>Yetkinin kaynağı yine sunucudur</b> — bu liste sadece
    /// arayüzü şekillendirir.
    /// </remarks>
    [JsonPropertyName("yetkiler")]
    public string[] Yetkiler { get; set; } = [];

    /// <summary>
    /// Kullanıcının sahip olduğu <b>izinler</b> — rollerinden çözülmüş hâli.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Yetkiler"/> beş politikadan ibaret ve "ajanda modülüne
    /// girebilir mi" diyor; bu ise "etkinlik silebilir mi" gibi tek tek
    /// eylemleri söylüyor. Arayüz düğmeleri buna göre gizler.
    /// </para>
    /// <para>
    /// <b>Yetkinin kaynağı yine sunucudur.</b> Bu liste yalnızca arayüzü
    /// şekillendirir; her uç kendi iznini ayrıca denetler. İzinler jetona
    /// GİRMEZ — geri alınan bir yetkinin 15 saat daha çalışması kabul
    /// edilemezdi.
    /// </para>
    /// </remarks>
    [JsonPropertyName("izinler")]
    public string[] Izinler { get; set; } = [];
}
