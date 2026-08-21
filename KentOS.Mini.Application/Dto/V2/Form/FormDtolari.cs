using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;

namespace KentOS.Mini.Application.Dto.V2.Form;

// ═══════════════════════════════════════════════════════ yetkili yüzeyi

/// <summary>Form listesi satırı — tanım TAŞIMAZ.</summary>
/// <remarks>
/// Liste ucu tanımı da döndürseydi, yirmi formluk bir sayfa yirmi ağacı
/// birden indirirdi. Tanım yalnızca detayda ve vatandaş yüzeyinde geliyor.
/// </remarks>
public class FormOzetDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("erisimAnahtari")] public string ErisimAnahtari { get; set; } = string.Empty;
    [JsonPropertyName("baslik")] public string Baslik { get; set; } = string.Empty;
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("durum")] public FormDurumu Durum { get; set; }
    [JsonPropertyName("durumAd")] public string DurumAd { get; set; } = string.Empty;
    [JsonPropertyName("erisim")] public FormErisimi Erisim { get; set; }
    [JsonPropertyName("erisimAd")] public string ErisimAd { get; set; } = string.Empty;

    [JsonPropertyName("yanitSayisi")] public int YanitSayisi { get; set; }
    [JsonPropertyName("yanitSiniri")] public int? YanitSiniri { get; set; }
    [JsonPropertyName("baslangicTarihi")] public DateTime? BaslangicTarihi { get; set; }
    [JsonPropertyName("bitisTarihi")] public DateTime? BitisTarihi { get; set; }

    /// <summary>
    /// ŞU AN yanıt kabul ediyor mu — durum, tarih ve sayı birlikte.
    /// </summary>
    /// <remarks>
    /// Sunucuda hesaplanıyor. İstemcide kurulsaydı üç kural iki ayrı
    /// istemcide (web + mobil) ayrı ayrı yazılır ve biri unutulduğunda
    /// ekran "açık" derken sunucu reddederdi.
    /// </remarks>
    [JsonPropertyName("yanitAliyor")] public bool YanitAliyor { get; set; }

    /// <summary>Neden kapalı — kullanıcıya gösterilecek cümle.</summary>
    [JsonPropertyName("kapaliSebebi")] public string? KapaliSebebi { get; set; }

    [JsonPropertyName("surumNo")] public int? SurumNo { get; set; }
    [JsonPropertyName("birimId")] public long? BirimId { get; set; }
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }
    [JsonPropertyName("olusturmaTarihi")] public DateTime OlusturmaTarihi { get; set; }
    [JsonPropertyName("yayinTarihi")] public DateTime? YayinTarihi { get; set; }

    /// <summary>Vatandaşa verilecek tam adres.</summary>
    [JsonPropertyName("paylasimAdresi")] public string? PaylasimAdresi { get; set; }

    /// <summary>
    /// Kurum genelindeki form portalı açık mı.
    /// </summary>
    /// <remarks>
    /// Kapalıyken HİÇBİR form yanıt almıyor; ekranın bunu tek seferde
    /// söyleyebilmesi için form başına değil ama her yanıtta taşınıyor.
    /// </remarks>
    [JsonPropertyName("portalAcik")] public bool PortalAcik { get; set; }
}

/// <summary>Form detayı — tanımıyla birlikte.</summary>
public sealed class FormDetayDto : FormOzetDto
{
    /// <summary>
    /// ÇALIŞILAN tanım (taslak). Yayındaki sürümden farklı olabilir.
    /// </summary>
    [JsonPropertyName("tanim")] public FormTanimiDto Tanim { get; set; } = new();

    /// <summary>Yayındaki sürümde bekleyen bir değişiklik var mı?</summary>
    [JsonPropertyName("yayinlanmamisDegisiklik")] public bool YayinlanmamisDegisiklik { get; set; }

    [JsonPropertyName("tesekkurMetni")] public string? TesekkurMetni { get; set; }
    [JsonPropertyName("tesekkurAdresi")] public string? TesekkurAdresi { get; set; }
    [JsonPropertyName("yanitOzetiGorunur")] public bool YanitOzetiGorunur { get; set; }
    [JsonPropertyName("sonuclarHerkeseAcik")] public bool SonuclarHerkeseAcik { get; set; }
    [JsonPropertyName("tekYanit")] public bool TekYanit { get; set; }
}

/// <summary>Form kaydetme isteği.</summary>
public sealed class FormKayitDto
{
    [Required(ErrorMessage = "Form başlığı zorunlu.")]
    [MaxLength(300)]
    [JsonPropertyName("baslik")] public string Baslik { get; set; } = string.Empty;

    [MaxLength(2000)]
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }

    [JsonPropertyName("erisim")] public FormErisimi Erisim { get; set; } = FormErisimi.Anonim;
    [JsonPropertyName("baslangicTarihi")] public DateTime? BaslangicTarihi { get; set; }
    [JsonPropertyName("bitisTarihi")] public DateTime? BitisTarihi { get; set; }
    [JsonPropertyName("yanitSiniri")] public int? YanitSiniri { get; set; }
    [JsonPropertyName("tekYanit")] public bool TekYanit { get; set; }

    [MaxLength(2000)]
    [JsonPropertyName("tesekkurMetni")] public string? TesekkurMetni { get; set; }

    [MaxLength(500)]
    [JsonPropertyName("tesekkurAdresi")] public string? TesekkurAdresi { get; set; }

    [JsonPropertyName("yanitOzetiGorunur")] public bool YanitOzetiGorunur { get; set; }
    [JsonPropertyName("sonuclarHerkeseAcik")] public bool SonuclarHerkeseAcik { get; set; }

    /// <summary>Tasarımcının ürettiği ağaç.</summary>
    [JsonPropertyName("tanim")] public FormTanimiDto Tanim { get; set; } = new();
}

/// <summary>Form listesi süzgeci.</summary>
public sealed class FormSuzgecDto : SayfaIstegi
{
    [JsonPropertyName("arama")] public string? Arama { get; set; }
    [JsonPropertyName("durum")] public FormDurumu? Durum { get; set; }
    [JsonPropertyName("birimId")] public long? BirimId { get; set; }
}

// ═══════════════════════════════════════════════════════ yanıtlar

/// <summary>Yanıt listesi satırı.</summary>
public class FormYanitOzetDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("takipNo")] public string TakipNo { get; set; } = string.Empty;
    [JsonPropertyName("durum")] public FormYanitDurumu Durum { get; set; }
    [JsonPropertyName("adSoyad")] public string? AdSoyad { get; set; }
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("eposta")] public string? Eposta { get; set; }
    [JsonPropertyName("gonderimTarihi")] public DateTime? GonderimTarihi { get; set; }
    [JsonPropertyName("surumNo")] public int SurumNo { get; set; }

    /// <summary>Listede gösterilecek ilk birkaç cevabın özeti.</summary>
    [JsonPropertyName("onizleme")] public string? Onizleme { get; set; }
}

/// <summary>Yanıt detayı — soru/cevap çiftleriyle.</summary>
public sealed class FormYanitDetayDto : FormYanitOzetDto
{
    /// <summary>
    /// Yanıtın verildiği SÜRÜMÜN tanımı.
    /// </summary>
    /// <remarks>
    /// Güncel tanım değil: soru metni sonradan değişmiş olabilir ve
    /// vatandaşın gördüğü metin neyse cevabı onun altında okunmalı.
    /// </remarks>
    [JsonPropertyName("tanim")] public FormTanimiDto Tanim { get; set; } = new();

    /// <summary>Ham cevaplar — <c>{ alanKimligi: deger }</c>.</summary>
    [JsonPropertyName("cevaplar")] public Dictionary<string, object?> Cevaplar { get; set; } = [];

    [JsonPropertyName("dosyalar")] public List<FormYanitDosyasiDto> Dosyalar { get; set; } = [];
}

/// <summary>Yanıta eklenen dosya.</summary>
public sealed class FormYanitDosyasiDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("alanKimligi")] public string AlanKimligi { get; set; } = string.Empty;
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("boyut")] public long Boyut { get; set; }
    [JsonPropertyName("icerikTipi")] public string? IcerikTipi { get; set; }
}

/// <summary>Yanıt listesi süzgeci.</summary>
public sealed class FormYanitSuzgecDto : SayfaIstegi
{
    [JsonPropertyName("arama")] public string? Arama { get; set; }
    [JsonPropertyName("durum")] public FormYanitDurumu? Durum { get; set; }
    [JsonPropertyName("baslangic")] public DateTime? Baslangic { get; set; }
    [JsonPropertyName("bitis")] public DateTime? Bitis { get; set; }

    /// <summary>
    /// "Şu alana şu cevabı verenler" süzgeci.
    /// </summary>
    /// <remarks>
    /// JSONB üzerinde <c>@&gt;</c> ile çalışıyor ve <c>cevaplar</c>
    /// kolonundaki GIN indeksini kullanıyor — tam tarama değil.
    /// </remarks>
    [JsonPropertyName("alanKimligi")] public string? AlanKimligi { get; set; }
    [JsonPropertyName("alanDegeri")] public string? AlanDegeri { get; set; }
}

// ═══════════════════════════════════════════════════════ özet / istatistik

/// <summary>Bir alanın yanıt dağılımı.</summary>
public sealed class FormAlanOzetiDto
{
    [JsonPropertyName("alanKimligi")] public string AlanKimligi { get; set; } = string.Empty;
    [JsonPropertyName("etiket")] public string Etiket { get; set; } = string.Empty;
    [JsonPropertyName("tip")] public FormAlanTipi Tip { get; set; }
    [JsonPropertyName("yanitSayisi")] public int YanitSayisi { get; set; }

    /// <summary>Seçim tiplerinde seçenek dağılımı.</summary>
    [JsonPropertyName("dagilim")] public List<FormDagilimDto>? Dagilim { get; set; }

    /// <summary>Sayı/ölçek tiplerinde ortalama.</summary>
    [JsonPropertyName("ortalama")] public double? Ortalama { get; set; }

    /// <summary>Metin tiplerinde son birkaç cevap.</summary>
    [JsonPropertyName("ornekler")] public List<string>? Ornekler { get; set; }
}

/// <summary>Tek bir seçeneğin payı.</summary>
public sealed class FormDagilimDto
{
    /// <summary>
    /// MATRİSTE satır etiketi; diğer tiplerde <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Matrisin dağılımı satır satır üretiliyor ve hepsi tek listede
    /// dönüyor. Satır adı etikete gömülseydi ("Temizlik → İyi") istemci
    /// gruplayamaz, uzun satır adları da her çubukta tekrarlanırdı.
    /// </remarks>
    [JsonPropertyName("satir")] public string? Satir { get; set; }

    [JsonPropertyName("etiket")] public string Etiket { get; set; } = string.Empty;
    [JsonPropertyName("adet")] public int Adet { get; set; }
    [JsonPropertyName("yuzde")] public double Yuzde { get; set; }
}

/// <summary>Formun yanıt özeti.</summary>
public sealed class FormOzetRaporuDto
{
    [JsonPropertyName("formId")] public long FormId { get; set; }
    [JsonPropertyName("baslik")] public string Baslik { get; set; } = string.Empty;
    [JsonPropertyName("toplamYanit")] public int ToplamYanit { get; set; }
    [JsonPropertyName("ilkYanit")] public DateTime? IlkYanit { get; set; }
    [JsonPropertyName("sonYanit")] public DateTime? SonYanit { get; set; }
    [JsonPropertyName("alanlar")] public List<FormAlanOzetiDto> Alanlar { get; set; } = [];
}

// ═══════════════════════════════════════════════════════ vatandaş yüzeyi

/// <summary>
/// VATANDAŞIN GÖRDÜĞÜ FORM — anonim uçtan döner.
/// </summary>
/// <remarks>
/// <b>Yetkili yüzeyinin DTO'su kullanılmıyor ve bu bilinçli.</b> Orada
/// birim kimliği, yanıt sayısı, oluşturan ve iç durum alanları var; hiçbiri
/// vatandaşı ilgilendirmiyor ve bir kısmı kurumun iç yapısını ele veriyor.
/// Ayrı DTO, "yanlışlıkla bir alan daha eklendi" hatasını baştan imkânsız
/// kılıyor.
/// </remarks>
public sealed class FormPortalDto
{
    [JsonPropertyName("baslik")] public string Baslik { get; set; } = string.Empty;
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("kurumAdi")] public string? KurumAdi { get; set; }
    [JsonPropertyName("erisim")] public FormErisimi Erisim { get; set; }

    /// <summary>Yayındaki tanım.</summary>
    [JsonPropertyName("tanim")] public FormTanimiDto Tanim { get; set; } = new();

    /// <summary>Yanıtın hangi sürüme verildiği — gönderimde geri gelir.</summary>
    [JsonPropertyName("surumNo")] public int SurumNo { get; set; }

    [JsonPropertyName("yanitAliyor")] public bool YanitAliyor { get; set; }
    [JsonPropertyName("kapaliSebebi")] public string? KapaliSebebi { get; set; }
    [JsonPropertyName("kaydetDevamEt")] public bool KaydetDevamEt { get; set; }
}

/// <summary>Vatandaşın gönderdiği yanıt.</summary>
public sealed class FormYanitIstegiDto
{
    /// <summary>
    /// Cevaplar — <c>{ alanKimligi: deger }</c>.
    /// </summary>
    /// <remarks>
    /// <b>Hangi forma gönderildiği GÖVDEDEN alınmıyor</b>, adresteki
    /// GUID'den. Gövdeye güvenilseydi, bir formun adresinden başka bir
    /// forma yanıt yazmak mümkün olurdu.
    /// </remarks>
    [JsonPropertyName("cevaplar")] public Dictionary<string, object?> Cevaplar { get; set; } = [];

    [JsonPropertyName("adSoyad")] public string? AdSoyad { get; set; }
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("eposta")] public string? Eposta { get; set; }

    /// <summary>Yarım kalan yanıtı sürdürmek için; aynı zamanda idempotans anahtarı.</summary>
    [JsonPropertyName("surdurmeAnahtari")] public string? SurdurmeAnahtari { get; set; }

    /// <summary>
    /// TEK YANIT için tarayıcı kimliği — telefon sorulmayan formlarda.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Anonim bir formda "aynı kişi mi" sorusunun başka cevabı yok. Yumuşak
    /// bir kapı: tarayıcı verisi temizlenince ya da başka bir cihazdan
    /// girilince aşılır — ama vatandaşın gönderip sayfayı yenileyerek
    /// yeniden doldurmasını engelliyor, ki şikâyet edilen davranış buydu.
    /// </para>
    /// <para>
    /// Sunucuda ham saklanmaz; <c>HMAC(form tuzu, …)</c> özeti yazılır.
    /// </para>
    /// </remarks>
    [JsonPropertyName("cihazAnahtari")] public string? CihazAnahtari { get; set; }

    /// <summary>
    /// BOT TUZAĞI — insan bunu doldurmaz.
    /// </summary>
    /// <remarks>
    /// Ekranda gizli bir alan; otomatik doldurucu botlar her alanı
    /// doldurduğu için burası doluysa gönderim sessizce başarılı sayılıp
    /// atılıyor. CAPTCHA yerine seçilmesinin sebebi: vatandaşa hiçbir
    /// engel çıkarmıyor ve dış bir servise bağımlılık getirmiyor.
    /// </remarks>
    [JsonPropertyName("website")] public string? Website { get; set; }
}

/// <summary>Gönderim sonucu — vatandaşa dönen tek şey.</summary>
public sealed class FormYanitSonucuDto
{
    [JsonPropertyName("takipNo")] public string TakipNo { get; set; } = string.Empty;
    [JsonPropertyName("tesekkurMetni")] public string? TesekkurMetni { get; set; }
    [JsonPropertyName("tesekkurAdresi")] public string? TesekkurAdresi { get; set; }

    /// <summary>Vatandaş kendi cevaplarının özetini görebilecekse dolu.</summary>
    [JsonPropertyName("ozet")] public List<FormCevapOzetiDto>? Ozet { get; set; }
}

/// <summary>Sonuç sayfasındaki tek satır.</summary>
public sealed class FormCevapOzetiDto
{
    [JsonPropertyName("etiket")] public string Etiket { get; set; } = string.Empty;
    [JsonPropertyName("deger")] public string Deger { get; set; } = string.Empty;
}

/// <summary>Yarım yanıt kaydetme sonucu.</summary>
public sealed class FormTaslakSonucuDto
{
    [JsonPropertyName("surdurmeAnahtari")] public string SurdurmeAnahtari { get; set; } = string.Empty;
}

/// <summary>Dosya yükleme sonucu — vatandaşa dönen.</summary>
public sealed class FormDosyaSonucuDto
{
    [JsonPropertyName("dosyaId")] public long DosyaId { get; set; }
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("boyut")] public long Boyut { get; set; }

    /// <summary>
    /// Dosya bir TASLAK yanıta bağlandı; gönderimde bu anahtar geri gelmeli.
    /// </summary>
    /// <remarks>
    /// Gelmezse gönderim yeni bir yanıt açar ve yüklenen dosya sahipsiz
    /// kalır — kullanıcının gördüğü, "dosyayı ekledim ama kayıtta yok".
    /// </remarks>
    [JsonPropertyName("surdurmeAnahtari")] public string SurdurmeAnahtari { get; set; } = string.Empty;
}
