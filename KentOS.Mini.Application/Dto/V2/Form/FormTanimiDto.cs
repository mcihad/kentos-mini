using System.Text.Json.Serialization;
using KentOS.Mini.Application.Enums;

namespace KentOS.Mini.Application.Dto.V2.Form;

/// <summary>
/// FORM TANIMI — tasarımcının ürettiği ağacın tamamı.
/// </summary>
/// <remarks>
/// <para>
/// Bu sınıf <c>form_surumleri.tanim</c> (jsonb) kolonunun şeklidir. Hem
/// tasarımcı hem oynatıcı hem de sunucu doğrulaması AYNI şemayı okuyor —
/// istemcide ve sunucuda iki ayrı model olsaydı, biri diğerinden habersiz
/// değiştiğinde doğrulama sessizce ayrışırdı.
/// </para>
/// <para>
/// <b>Ağaç üç katmanlı: adım → grup → alan.</b> Tek adımlı bir tanım klasik
/// form, çok adımlı olan stepper. Ayrı bir "stepper mi" bayrağı yok: adım
/// sayısı zaten söylüyor ve iki kavramı ayırmak, tek adımlı formu bir
/// istisna hâline getirirdi.
/// </para>
/// </remarks>
public sealed class FormTanimiDto
{
    /// <summary>
    /// Şema sürümü — tanımın kendi biçimi değişirse artar.
    /// </summary>
    /// <remarks>
    /// Formun yayın sürümüyle karıştırılmamalı: bu, JSONB'nin <b>şeklinin</b>
    /// sürümü. İleride alan yapısı değişirse eski kayıtları okurken hangi
    /// çözümleyicinin kullanılacağını bu sayı söyler.
    /// </remarks>
    [JsonPropertyName("semaSurumu")]
    public int SemaSurumu { get; set; } = 1;

    [JsonPropertyName("adimlar")]
    public List<FormAdimiDto> Adimlar { get; set; } = [];

    [JsonPropertyName("ayarlar")]
    public FormAyarlariDto Ayarlar { get; set; } = new();
}

/// <summary>Form geneli görünüm ayarları.</summary>
public sealed class FormAyarlariDto
{
    /// <summary>
    /// Form geneli kolon sayısı — grup kendi değerini vermezse bu geçerli.
    /// </summary>
    /// <remarks>
    /// <b>Mobilde her zaman tek kolon.</b> Bu sayı yalnızca masaüstünde
    /// uygulanıyor; 390px'te iki kolon, her alanı okunamayacak kadar
    /// daraltıyor.
    /// </remarks>
    [JsonPropertyName("kolonSayisi")]
    public int KolonSayisi { get; set; } = 1;

    /// <summary>Çok adımlı formda ilerleme çubuğu gösterilsin mi?</summary>
    [JsonPropertyName("ilerlemeCubugu")]
    public bool IlerlemeCubugu { get; set; } = true;

    /// <summary>Soruları numaralandır.</summary>
    [JsonPropertyName("numaralandir")]
    public bool Numaralandir { get; set; }

    /// <summary>Yarım kalan yanıt sürdürülebilsin mi?</summary>
    /// <remarks>
    /// Uzun anketlerde şart; kısa bir geri bildirim formunda gereksiz bir
    /// belirteç üretmek demek. Varsayılan kapalı.
    /// </remarks>
    [JsonPropertyName("kaydetDevamEt")]
    public bool KaydetDevamEt { get; set; }
}

/// <summary>Stepper adımı. Tek adımlı tanım klasik formdur.</summary>
public sealed class FormAdimiDto
{
    [JsonPropertyName("kimlik")]
    public string Kimlik { get; set; } = string.Empty;

    [JsonPropertyName("baslik")]
    public string? Baslik { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }

    [JsonPropertyName("gruplar")]
    public List<FormGrubuDto> Gruplar { get; set; } = [];
}

/// <summary>
/// Alan grubu — başlıklı bölüm.
/// </summary>
/// <remarks>
/// Kolon sayısı GRUP BAZINDA verilebiliyor: bir formda "Kimlik bilgileri"
/// iki kolon, altındaki "Şikâyetiniz" tek kolon olabilmeli. Tek bir form
/// geneli değer, uzun metin alanlarını da ikiye böler ve okunmaz kılardı.
/// </remarks>
public sealed class FormGrubuDto
{
    [JsonPropertyName("kimlik")]
    public string Kimlik { get; set; } = string.Empty;

    [JsonPropertyName("baslik")]
    public string? Baslik { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }

    /// <summary>Boşsa form geneli değeri kullanılır.</summary>
    [JsonPropertyName("kolonSayisi")]
    public int? KolonSayisi { get; set; }

    /// <summary>Grubun tamamı koşula bağlanabilir.</summary>
    [JsonPropertyName("kosul")]
    public FormKosuluDto? Kosul { get; set; }

    [JsonPropertyName("alanlar")]
    public List<FormAlaniDto> Alanlar { get; set; } = [];
}

/// <summary>Tek bir soru ya da içerik bloğu.</summary>
public sealed class FormAlaniDto
{
    /// <summary>
    /// KALICI KİMLİK — yanıtın JSONB anahtarı.
    /// </summary>
    /// <remarks>
    /// Etiket değişince kimlik DEĞİŞMEZ; değişseydi eski yanıtlar sahipsiz
    /// kalırdı. Tasarımcı bir alan eklerken üretir ve bir daha dokunmaz.
    /// </remarks>
    [JsonPropertyName("kimlik")]
    public string Kimlik { get; set; } = string.Empty;

    [JsonPropertyName("tip")]
    public FormAlanTipi Tip { get; set; }

    [JsonPropertyName("etiket")]
    public string Etiket { get; set; } = string.Empty;

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }

    [JsonPropertyName("yerTutucu")]
    public string? YerTutucu { get; set; }

    /// <summary>
    /// Grup ızgarasında kaç birim yer kaplar (1–12).
    /// </summary>
    /// <remarks>
    /// 12'lik ızgara: iki kolonlu bir grupta alan 6, tam genişlik isteyen
    /// uzun metin 12 alır. Kolon sayısıyla birlikte çalışıyor ve mobilde
    /// hepsi 12'ye iniyor.
    /// </remarks>
    [JsonPropertyName("genislik")]
    public int Genislik { get; set; } = 12;

    [JsonPropertyName("zorunlu")]
    public bool Zorunlu { get; set; }

    [JsonPropertyName("kosul")]
    public FormKosuluDto? Kosul { get; set; }

    /// <summary>Seçim tiplerinin seçenekleri.</summary>
    [JsonPropertyName("secenekler")]
    public List<FormSecenegiDto>? Secenekler { get; set; }

    /// <summary>Matris satırları (soru başına bir satır).</summary>
    [JsonPropertyName("satirlar")]
    public List<FormSecenegiDto>? Satirlar { get; set; }

    /// <summary>Matris sütunları (ortak seçenek kümesi).</summary>
    [JsonPropertyName("sutunlar")]
    public List<FormSecenegiDto>? Sutunlar { get; set; }

    [JsonPropertyName("dogrulama")]
    public FormDogrulamaDto? Dogrulama { get; set; }

    /// <summary>Tipe özel ayarlar (ölçek uçları, yıldız sayısı, adım…).</summary>
    [JsonPropertyName("ayarlar")]
    public FormAlanAyarlariDto? Ayarlar { get; set; }
}

/// <summary>Seçenek / matris satır-sütun başlığı.</summary>
public sealed class FormSecenegiDto
{
    [JsonPropertyName("kimlik")]
    public string Kimlik { get; set; } = string.Empty;

    [JsonPropertyName("etiket")]
    public string Etiket { get; set; } = string.Empty;

    /// <summary>
    /// "Diğer" seçeneği — işaretlenince serbest metin ister.
    /// </summary>
    [JsonPropertyName("digerMi")]
    public bool DigerMi { get; set; }
}

/// <summary>
/// KOŞULLU GÖRÜNÜRLÜK — bağlaçlı kural listesi.
/// </summary>
/// <remarks>
/// <para>
/// <b>Tek koşul yetmiyor.</b> "Başvuru türü işyeri VEYA inşaat ise" ilk
/// hafta gelecek bir istek; tek koşullu bir modelde ifade edilemiyor ve
/// sonradan eklemek yayınlanmış formların JSONB belgelerinde şema geçişi
/// demek.
/// </para>
/// <para>
/// <b>İç içe ifade ağacı ELENDİ</b>: ayrıştırıcı, öncelik kuralı, derinlik
/// sınırı ve bir ağaç editörü gerektiriyor. <c>A ve (B veya C)</c> zaten
/// <b>grup koşulu ∧ alan koşulu</b> ile yazılabiliyor — iç içelik iki
/// seviyeyle sınırlı ve grup içinde grup yok.
/// </para>
/// <para>
/// <b>KOŞUL YALNIZCA GERİYE BAKAR.</b> Hedef alan, koşulu taşıyan alandan
/// önce gelmek zorunda (<c>FormServisi</c> bunu zorluyor). İki kazancı
/// var: döngü tespiti hiç yazılmıyor — Mapster döngüsünün bir kez bütün
/// API sürecini düşürdüğü bu depoda, hatayı yapı gereği imkânsız kılmak
/// testle yakalamaktan ucuz — ve sunucu doğrulaması formu tek geçişte
/// yeniden oynatabiliyor.
/// </para>
/// <para>
/// <b>Sunucu doğrulaması koşulu HESAPLAR.</b> Görünmeyen bir alanın
/// "zorunlu" olması hata vermemeli; istemci alanı hiç göstermediği için
/// değer de göndermiyor.
/// </para>
/// </remarks>
public sealed class FormKosuluDto
{
    /// <summary>Kurallar arasındaki bağlaç.</summary>
    [JsonPropertyName("baglac")]
    public FormKosulBaglaci Baglac { get; set; } = FormKosulBaglaci.Ve;

    /// <summary>1–8 kural. Boş liste "koşulsuz" demektir.</summary>
    [JsonPropertyName("kurallar")]
    public List<FormKosulKuraliDto> Kurallar { get; set; } = [];
}

/// <summary>Tek bir karşılaştırma.</summary>
public sealed class FormKosulKuraliDto
{
    [JsonPropertyName("alanKimligi")]
    public string AlanKimligi { get; set; } = string.Empty;

    [JsonPropertyName("operator")]
    public FormKosulOperatoru Operator { get; set; }

    /// <summary>Karşılaştırılan değer; <c>Dolu</c>/<c>Bos</c> için boş.</summary>
    [JsonPropertyName("deger")]
    public string? Deger { get; set; }
}

/// <summary>Kurallar arasındaki bağlaç.</summary>
public enum FormKosulBaglaci
{
    Ve = 0,
    Veya = 1,
}

/// <summary>Koşul karşılaştırması.</summary>
public enum FormKosulOperatoru
{
    Esit = 0,
    EsitDegil = 1,
    Icerir = 2,
    IcermeZ = 3,
    Dolu = 4,
    Bos = 5,
    Buyuk = 6,
    Kucuk = 7,
}

/// <summary>
/// Alan doğrulama kuralları.
/// </summary>
/// <remarks>
/// <b>Tek kaynak.</b> Aynı nesne hem istemcide anlık uyarı için hem
/// sunucuda kesin karar için okunuyor. İstemci doğrulaması bir kolaylık;
/// sunucu onu hiç görmemiş gibi yeniden denetliyor.
/// </remarks>
public sealed class FormDogrulamaDto
{
    [JsonPropertyName("enAzUzunluk")] public int? EnAzUzunluk { get; set; }
    [JsonPropertyName("enCokUzunluk")] public int? EnCokUzunluk { get; set; }

    [JsonPropertyName("enAzDeger")] public decimal? EnAzDeger { get; set; }
    [JsonPropertyName("enCokDeger")] public decimal? EnCokDeger { get; set; }

    [JsonPropertyName("enAzTarih")] public DateTime? EnAzTarih { get; set; }
    [JsonPropertyName("enCokTarih")] public DateTime? EnCokTarih { get; set; }

    /// <summary>En az / en çok kaç seçenek işaretlenebilir.</summary>
    [JsonPropertyName("enAzSecim")] public int? EnAzSecim { get; set; }
    [JsonPropertyName("enCokSecim")] public int? EnCokSecim { get; set; }

    /// <summary>
    /// Düzenli ifade — kullanıcıdan gelir, sunucuda ZAMAN AŞIMIYLA çalışır.
    /// </summary>
    /// <remarks>
    /// Form kuran yetkili buraya kendi desenini yazabiliyor. Kötü yazılmış
    /// bir desen (iç içe yıldız) katastrofik geri izlemeye yol açıp isteği
    /// kilitleyebilir; doğrulayıcı bu yüzden desenleri kısa bir zaman
    /// aşımıyla çalıştırıyor.
    /// </remarks>
    [JsonPropertyName("desen")] public string? Desen { get; set; }
    [JsonPropertyName("desenMesaji")] public string? DesenMesaji { get; set; }

    /// <summary>İzinli dosya uzantıları (nokta ile, küçük harf).</summary>
    [JsonPropertyName("dosyaUzantilari")] public List<string>? DosyaUzantilari { get; set; }
    [JsonPropertyName("enCokDosyaMb")] public int? EnCokDosyaMb { get; set; }
    [JsonPropertyName("enCokDosyaSayisi")] public int? EnCokDosyaSayisi { get; set; }
}

/// <summary>Alan tipine özel ayarlar.</summary>
public sealed class FormAlanAyarlariDto
{
    /// <summary>Ölçek/yıldız: alt ve üst sınır.</summary>
    [JsonPropertyName("enAz")] public int? EnAz { get; set; }
    [JsonPropertyName("enCok")] public int? EnCok { get; set; }

    /// <summary>Ölçek uçlarının etiketleri ("Hiç katılmıyorum" / "Tamamen").</summary>
    [JsonPropertyName("altEtiket")] public string? AltEtiket { get; set; }
    [JsonPropertyName("ustEtiket")] public string? UstEtiket { get; set; }

    /// <summary>Sayı alanında ondalık basamak.</summary>
    [JsonPropertyName("ondalik")] public int? Ondalik { get; set; }

    /// <summary>Seçenekler karışık sırayla gösterilsin mi (anket yanlılığı).</summary>
    [JsonPropertyName("karistir")] public bool Karistir { get; set; }

    /// <summary>Uzun metin satır sayısı.</summary>
    [JsonPropertyName("satir")] public int? Satir { get; set; }

    /// <summary>Görsel blok için adres.</summary>
    [JsonPropertyName("gorselAdresi")] public string? GorselAdresi { get; set; }
}
