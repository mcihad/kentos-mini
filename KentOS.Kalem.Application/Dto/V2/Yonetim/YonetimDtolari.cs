using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using KentOS.Kalem.Application.Dto.V2;

namespace KentOS.Kalem.Application.Dto.V2.Yonetim;

/// <summary>Yönetim listesinde bir kullanıcı.</summary>
public class KullaniciOzetDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("kullaniciAdi")]
    public string KullaniciAdi { get; set; } = string.Empty;

    [JsonPropertyName("ad")]
    public string? Ad { get; set; }

    [JsonPropertyName("soyad")]
    public string? Soyad { get; set; }

    [JsonPropertyName("unvan")]
    public string? Unvan { get; set; }

    [JsonPropertyName("eposta")]
    public string? Eposta { get; set; }

    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }

    [JsonPropertyName("birimId")]
    public long? BirimId { get; set; }

    [JsonPropertyName("birimAdi")]
    public string? BirimAdi { get; set; }

    [JsonPropertyName("roller")]
    public List<string> Roller { get; set; } = [];

    /// <summary>
    /// Cihaz bağlı mı — jetonun KENDİSİ asla dışarı verilmez.
    /// </summary>
    /// <remarks>
    /// v1'in yönetim formu <c>FcmToken</c>'ı düz metin olarak forma basıyordu.
    /// Jeton, sahibinin cihazına bildirim göndermeye yeter; yönetim ekranında
    /// görünmesi için hiçbir sebep yok.
    /// </remarks>
    [JsonPropertyName("mobilBagli")]
    public bool MobilBagli { get; set; }

    [JsonPropertyName("webBagli")]
    public bool WebBagli { get; set; }

    /// <summary>
    /// Gizli etkinlik OLUŞTURABİLİR mi.
    /// </summary>
    /// <remarks>
    /// Görünürlükle ilgisi YOK: bu bayrak başkalarının gizli etkinliklerini
    /// göstermez, yalnızca oluşturma yetkisi verir.
    /// </remarks>
    [JsonPropertyName("gizliEtkinlikEkleyebilir")]
    public bool GizliEtkinlikEkleyebilir { get; set; }

    /// <summary>Dosya gönderimi BAŞLATABİLİR mi (almak yetki istemez).</summary>
    [JsonPropertyName("dosyaGonderebilir")]
    public bool DosyaGonderebilir { get; set; }

    /// <summary>
    /// Saha personeli mi — giriş sonrası varsayılan ekranı belirler.
    /// </summary>
    /// <remarks>
    /// Yanındaki iki alan birer yetki, bu değil: neyi yapabileceğini izinleri
    /// söylüyor, bu alan yalnızca nereye ineceğini.
    /// </remarks>
    [JsonPropertyName("sahaPersoneli")]
    public bool SahaPersoneli { get; set; }
}

/// <summary>Kullanıcı oluşturma isteği.</summary>
public class KullaniciOlusturIstegi
{
    [JsonPropertyName("kullaniciAdi")]
    public string KullaniciAdi { get; set; } = string.Empty;

    [JsonPropertyName("parola")]
    public string Parola { get; set; } = string.Empty;

    [JsonPropertyName("ad")]
    public string? Ad { get; set; }

    [JsonPropertyName("soyad")]
    public string? Soyad { get; set; }

    [JsonPropertyName("unvan")]
    public string? Unvan { get; set; }

    [JsonPropertyName("eposta")]
    public string? Eposta { get; set; }

    [JsonPropertyName("telefon")]
    public string? Telefon
    {
        get => _telefon;
        // Kullanıcı nasıl yazarsa yazsın tek biçime iner; veritabanındaki
        // boşluklu eski numaralar da böylece doğrulamayı geçer.
        set => _telefon = Ortak.Telefon.Duzelt(value);
    }

    private string? _telefon;

    [JsonPropertyName("birimId")]
    public long? BirimId { get; set; }

    [JsonPropertyName("roller")]
    public List<string> Roller { get; set; } = [];

    [JsonPropertyName("gizliEtkinlikEkleyebilir")]
    public bool GizliEtkinlikEkleyebilir { get; set; }

    [JsonPropertyName("dosyaGonderebilir")]
    public bool DosyaGonderebilir { get; set; }

    /// <summary>Giriş sonrası saha ekranına insin mi.</summary>
    [JsonPropertyName("sahaPersoneli")]
    public bool SahaPersoneli { get; set; }

    /// <summary>Kullanıcıya SMS ile giriş bilgisi gönderilsin mi?</summary>
    [JsonPropertyName("smsGonder")]
    public bool SmsGonder { get; set; } = true;
}

/// <summary>Kullanıcı güncelleme isteği.</summary>
/// <remarks>
/// Parola bu uçtan DEĞİŞMEZ; ayrı uç nokta var. Bir formun yanlışlıkla boş
/// parola göndermesi kullanıcının parolasını sıfırlamamalı.
/// </remarks>
public class KullaniciGuncelleIstegi
{
    [JsonPropertyName("kullaniciAdi")]
    public string KullaniciAdi { get; set; } = string.Empty;

    [JsonPropertyName("ad")]
    public string? Ad { get; set; }

    [JsonPropertyName("soyad")]
    public string? Soyad { get; set; }

    [JsonPropertyName("unvan")]
    public string? Unvan { get; set; }

    [JsonPropertyName("eposta")]
    public string? Eposta { get; set; }

    [JsonPropertyName("telefon")]
    public string? Telefon
    {
        get => _telefon;
        // Kullanıcı nasıl yazarsa yazsın tek biçime iner; veritabanındaki
        // boşluklu eski numaralar da böylece doğrulamayı geçer.
        set => _telefon = Ortak.Telefon.Duzelt(value);
    }

    private string? _telefon;

    [JsonPropertyName("birimId")]
    public long? BirimId { get; set; }

    [JsonPropertyName("roller")]
    public List<string> Roller { get; set; } = [];

    [JsonPropertyName("gizliEtkinlikEkleyebilir")]
    public bool GizliEtkinlikEkleyebilir { get; set; }

    [JsonPropertyName("dosyaGonderebilir")]
    public bool DosyaGonderebilir { get; set; }

    /// <summary>Giriş sonrası saha ekranına insin mi.</summary>
    [JsonPropertyName("sahaPersoneli")]
    public bool SahaPersoneli { get; set; }
}

/// <summary>Yönetici tarafından parola sıfırlama.</summary>
public class ParolaSifirlaIstegi
{
    [JsonPropertyName("yeniParola")]
    public string YeniParola { get; set; } = string.Empty;

    /// <summary>Yeni parola kullanıcıya SMS ile bildirilsin mi?</summary>
    [JsonPropertyName("smsGonder")]
    public bool SmsGonder { get; set; } = true;
}

/// <summary>Birim ağacında bir düğüm.</summary>
public class BirimDugumDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("ad")]
    public string Ad { get; set; } = string.Empty;

    [JsonPropertyName("yetkili")]
    public string? Yetkili { get; set; }

    [JsonPropertyName("unvan")]
    public string? Unvan { get; set; }

    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }

    [JsonPropertyName("eposta")]
    public string? Eposta { get; set; }

    [JsonPropertyName("adres")]
    public string? Adres { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }

    [JsonPropertyName("ustBirimId")]
    public long? UstBirimId { get; set; }

    /// <summary>Bu birime bağlı kullanıcı sayısı — silmeden önce uyarı için.</summary>
    [JsonPropertyName("kullaniciSayisi")]
    public int KullaniciSayisi { get; set; }

    [JsonPropertyName("altBirimler")]
    public List<BirimDugumDto> AltBirimler { get; set; } = [];
}

/// <summary>Birim oluşturma/güncelleme isteği.</summary>
public class BirimIstegi
{
    [JsonPropertyName("ad")]
    public string Ad { get; set; } = string.Empty;

    [JsonPropertyName("yetkili")]
    public string Yetkili { get; set; } = string.Empty;

    [JsonPropertyName("unvan")]
    public string? Unvan { get; set; }

    [JsonPropertyName("telefon")]
    public string? Telefon
    {
        get => _telefon;
        // Kullanıcı nasıl yazarsa yazsın tek biçime iner; veritabanındaki
        // boşluklu eski numaralar da böylece doğrulamayı geçer.
        set => _telefon = Ortak.Telefon.Duzelt(value);
    }

    private string? _telefon;

    [JsonPropertyName("eposta")]
    public string? Eposta { get; set; }

    [JsonPropertyName("adres")]
    public string? Adres { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }

    [JsonPropertyName("ustBirimId")]
    public long? UstBirimId { get; set; }
}

/// <summary>Roller listesi öğesi.</summary>
public class RolDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("ad")]
    public string Ad { get; set; } = string.Empty;

    /// <summary>
    /// Rolün ne işe yaradığı.
    /// </summary>
    /// <remarks>
    /// Rol adları teknik ("Sekreter", "BaskanOzel") ve tek başına ne
    /// yapabildiklerini söylemiyor. Kullanıcıya rol atayan kişi listeye
    /// bakarak karar veremiyordu.
    /// </remarks>
    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }

    [JsonPropertyName("kullaniciSayisi")]
    public int KullaniciSayisi { get; set; }

    /// <summary>Role bağlı izin sayısı — listede bir bakışta görünsün.</summary>
    [JsonPropertyName("izinSayisi")]
    public int IzinSayisi { get; set; }

    /// <summary>Yalnızca <c>Sistem</c> rolündeki kullanıcılar atayabilir.</summary>
    [JsonPropertyName("korumali")]
    public bool Korumali { get; set; }
}

/// <summary>Rol oluşturma/güncelleme isteği.</summary>
public class RolIstegi
{
    [JsonPropertyName("ad")]
    [Required(ErrorMessage = "Rol adı zorunludur.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Rol adı 2-60 karakter olmalıdır.")]
    public string Ad { get; set; } = string.Empty;

    [JsonPropertyName("aciklama")]
    [StringLength(300)]
    public string? Aciklama { get; set; }
}

/// <summary>İzin kataloğu kaydı — yönetim ekranındaki seçim listesi.</summary>
public class IzinDto
{
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("grup")] public string Grup { get; set; } = string.Empty;
    [JsonPropertyName("baslik")] public string Baslik { get; set; } = string.Empty;

    /// <summary>
    /// Ne işe yaradığı — rol kuran kişi izin ADINA bakarak karar veremiyor.
    /// </summary>
    [JsonPropertyName("aciklama")] public string Aciklama { get; set; } = string.Empty;

    /// <summary>Kodda hâlâ tanımlı mı; false ise yönetimde soluk gösterilir.</summary>
    [JsonPropertyName("kullanimda")] public bool Kullanimda { get; set; } = true;
}

/// <summary>Bir rolün izinlerini topluca yazma isteği.</summary>
public class RolIzinIstegi
{
    /// <summary>
    /// Rolün SAHİP OLACAĞI izinlerin tamamı — listede olmayan izin kaldırılır.
    /// </summary>
    /// <remarks>
    /// Tek tek ekle/çıkar uçları yerine tam liste: yönetim ekranı onay
    /// kutularıyla çalışıyor ve "kaydet" dendiğinde ekrandaki durum ne ise o
    /// yazılmalı. Aradaki fark, iki sekmede açık iki yöneticinin birbirinin
    /// değişikliğini sessizce geri almasıydı.
    /// </remarks>
    [JsonPropertyName("izinler")]
    public List<string> Izinler { get; set; } = [];
}

/// <summary>Birim detayı — istatistikler ve birimdeki kullanıcılar.</summary>
/// <remarks>
/// Ağaçtaki bir düğüme tıklanınca açılır. Eski sistemde birim yalnızca
/// listeleniyordu; "bu birimde kim var, kaç iş var" sorusuna cevap yoktu.
/// </remarks>
public class BirimDetayDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("yetkili")] public string? Yetkili { get; set; }
    [JsonPropertyName("unvan")] public string? Unvan { get; set; }
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("eposta")] public string? Eposta { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }

    [JsonPropertyName("ustBirimId")] public long? UstBirimId { get; set; }
    [JsonPropertyName("ustBirimAd")] public string? UstBirimAd { get; set; }

    [JsonPropertyName("kullaniciSayisi")] public int KullaniciSayisi { get; set; }
    [JsonPropertyName("altBirimSayisi")] public int AltBirimSayisi { get; set; }
    [JsonPropertyName("etkinlikSayisi")] public int EtkinlikSayisi { get; set; }
    [JsonPropertyName("talepSayisi")] public int TalepSayisi { get; set; }

    [JsonPropertyName("kullanicilar")] public List<KullaniciOzetDto> Kullanicilar { get; set; } = [];
}
