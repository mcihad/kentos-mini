using System.ComponentModel.DataAnnotations.Schema;
using KentOS.Mini.Application.Enums;

namespace KentOS.Mini.Application.Models;

/// <summary>
/// DİNAMİK FORM / ANKET — başlık kaydı.
/// </summary>
/// <remarks>
/// <para>
/// <b>Tanımın kendisi burada DEĞİL, sürümde.</b> Bir form yayınlandıktan
/// sonra düzenlenebiliyor; tanım burada dursaydı, düzenleme eski yanıtların
/// şemasını da değiştirir ve "3. soruya ne cevap verilmişti" sorusunun
/// cevabı sessizce bozulurdu. Her yayın yeni bir <see cref="FormVersion"/>
/// üretir; yanıt hangi sürüme verildiyse onu işaret eder.
/// </para>
/// <para>
/// <b>Adres GUID.</b> Artan bir kimlik, yayınlanmamış formların adresini
/// tahmin etmeyi ve kurumun kaç form açtığını saymayı mümkün kılardı.
/// Okunabilir bir kısa ad (slug) da tutuluyor ama <b>yetki belirteci
/// GUID</b>; slug yalnızca insan gözü için.
/// </para>
/// </remarks>
[Table("formlar")]
public class Form
{
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// Vatandaş adresindeki tahmin edilemez belirteç.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>BAŞLATICI YOK ve bu hayati.</b> <c>= Guid.NewGuid()</c> yazılsaydı
    /// <c>_mapper.Map(dto, entity)</c> her güncellemede alanı varsayılana
    /// çeker ve <b>vatandaşın adresi sessizce dönerdi</b>: dağıtılmış QR
    /// kodları, SMS bağlantıları ve afişler ölür, hiçbir istisna atılmaz.
    /// Bu, depoda adı konmuş bir hatanın (<c>Ajanda.KullaniciId</c>'nin
    /// güncellemede <c>NULL</c>'a düşmesi) birebir aynısı.
    /// </para>
    /// <para>
    /// Değer servis katmanında, kaydın YARATILDIĞI anda bir kez üretiliyor.
    /// Ad <c>guid</c> değil <c>erisim_anahtari</c>: <c>guid</c> bir tip adı,
    /// anlam değil — ve sözleşme dondurma testi bu adı sonsuza kadar
    /// sabitliyor.
    /// </para>
    /// </remarks>
    [Column("erisim_anahtari")]
    public string ErisimAnahtari { get; set; } = string.Empty;

    /// <summary>
    /// FORMA ÖZEL TUZ — mükerrer yanıt kontrolü için.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tek yanıt kuralı <c>HMAC(tuz, telefon)</c> ile çalışıyor; telefonun
    /// kendisi <b>anonim formda hiç saklanmıyor</b>. Tuz form başına
    /// olduğu için iki farklı formdaki aynı kişi eşleştirilemiyor —
    /// kurum geneli tek tuz, bütün anketleri birbirine bağlayan bir kimlik
    /// üretirdi.
    /// </para>
    /// </remarks>
    [Column("anonim_tuzu")]
    public string AnonimTuzu { get; set; } = string.Empty;

    [Column("baslik")]
    public string Baslik { get; set; } = string.Empty;

    [Column("aciklama")]
    public string? Aciklama { get; set; }

    [Column("durum")]
    public FormDurumu Durum { get; set; } = FormDurumu.Taslak;

    [Column("erisim")]
    public FormErisimi Erisim { get; set; } = FormErisimi.Anonim;

    /// <summary>Yayındaki sürüm. Taslakta null olabilir.</summary>
    [Column("yayin_surum_id")]
    public long? YayinSurumId { get; set; }

    public FormVersion? YayinSurumu { get; set; }

    /// <summary>Formu açan birim — görünürlük kapısı buradan geçer.</summary>
    [Column("birim_id")]
    public long? BirimId { get; set; }

    // ── yanıt kabul kuralları ──────────────────────────────────────────

    /// <summary>Bu tarihten önce yanıt alınmaz.</summary>
    [Column("baslangic_tarihi")]
    public DateTime? BaslangicTarihi { get; set; }

    /// <summary>Bu tarihten sonra yanıt alınmaz.</summary>
    [Column("bitis_tarihi")]
    public DateTime? BitisTarihi { get; set; }

    /// <summary>
    /// Toplam yanıt üst sınırı. Dolunca form kendiliğinden yanıt almaz.
    /// </summary>
    /// <remarks>
    /// Sayaç <b>sorgu anında</b> hesaplanmıyor, <see cref="YanitSayisi"/>
    /// alanında tutuluyor: her gönderimde tabloyu saymak, binlerce yanıtlı
    /// bir ankette gönderim başına tam tarama demekti.
    /// </remarks>
    [Column("yanit_siniri")]
    public int? YanitSiniri { get; set; }

    [Column("yanit_sayisi")]
    public int YanitSayisi { get; set; }

    /// <summary>
    /// Aynı kişi birden çok kez yanıtlayabilir mi?
    /// </summary>
    /// <remarks>
    /// Yalnızca <see cref="FormErisimi.TelefonDogrulamali"/> ve
    /// <see cref="FormErisimi.Personel"/> kiplerinde ANLAMLI: anonim bir
    /// formda "aynı kişi" diye güvenilir bir kavram yok. IP'ye bağlamak
    /// aynı kurumdaki yüz kişiyi tek kişi sayardı.
    /// </remarks>
    [Column("tek_yanit")]
    public bool TekYanit { get; set; }

    // ── sonuç sayfası ──────────────────────────────────────────────────

    /// <summary>Gönderimden sonra gösterilen metin.</summary>
    [Column("tesekkur_metni")]
    public string? TesekkurMetni { get; set; }

    /// <summary>Gönderimden sonra yönlendirilecek adres (isteğe bağlı).</summary>
    [Column("tesekkur_adresi")]
    public string? TesekkurAdresi { get; set; }

    /// <summary>Vatandaş yanıtının bir özetini görebilsin mi?</summary>
    [Column("yanit_ozeti_gorunur")]
    public bool YanitOzetiGorunur { get; set; }

    /// <summary>Yanıt dağılımları herkese açık olsun mu (anket sonucu)?</summary>
    [Column("sonuclar_herkese_acik")]
    public bool SonuclarHerkeseAcik { get; set; }

    // ── künye ──────────────────────────────────────────────────────────

    [Column("olusturan_id")]
    public long? OlusturanId { get; set; }

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    [Column("guncelleme_tarihi")]
    public DateTime? GuncellemeTarihi { get; set; }

    [Column("yayin_tarihi")]
    public DateTime? YayinTarihi { get; set; }

    [Column("silindi")]
    public bool Silindi { get; set; }

    public ICollection<FormVersion> Surumler { get; set; } = new List<FormVersion>();
    public ICollection<FormResponse> Yanitlar { get; set; } = new List<FormResponse>();
}

/// <summary>
/// FORMUN DONMUŞ TANIMI — bir yayın anının fotoğrafı.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden JSONB.</b> Tanım bir ağaç: adımlar → gruplar → alanlar, üstüne
/// kolon düzeni, koşullu görünürlük ve alan tipine göre değişen ayarlar.
/// İlişkisel modellenseydi en az altı tablo, her sıralamada toplu güncelleme
/// ve tip başına ayrı ayar tablosu gerekirdi. Tanım <b>bir bütün olarak</b>
/// okunuyor ve <b>bir bütün olarak</b> yazılıyor; parçalarına ayrı ayrı
/// sorgu atılmıyor.
/// </para>
/// <para>
/// <b>Sürüm DEĞİŞMEZ.</b> Yayınlanmış bir sürümün tanımı bir daha
/// yazılmaz; düzenleme yeni bir sürüm doğurur. Yanıtlar sürümü işaret
/// ettiği için, soru metni sonradan değişse bile "o kişi neyi okuyup ne
/// cevapladı" sorusu cevaplanabiliyor.
/// </para>
/// </remarks>
[Table("form_surumleri")]
public class FormVersion
{
    [Column("id")]
    public long Id { get; set; }

    [Column("form_id")]
    public long FormId { get; set; }

    public Form? Form { get; set; }

    /// <summary>1'den başlar, her yayında artar.</summary>
    [Column("surum_no")]
    public int SurumNo { get; set; }

    /// <summary>
    /// Tanımın kendisi — <c>jsonb</c>.
    /// </summary>
    /// <remarks>
    /// Şekli <c>FormTanimiDto</c> ile birebir. Metin olarak tutuluyor ve
    /// kolon tipi <c>AppDbContext</c>'te <c>jsonb</c> olarak sabitleniyor:
    /// böylece Postgres tarafında yol sorgusu ve GIN indeksi mümkün, C#
    /// tarafında ise serileştirme tek yerde ve denetimli.
    /// </remarks>
    [Column("tanim")]
    public string Tanim { get; set; } = "{}";

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    [Column("olusturan_id")]
    public long? OlusturanId { get; set; }
}

/// <summary>
/// VATANDAŞIN YANITI.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cevaplar tek JSONB gövdede.</b> Soru başına satır da denenebilirdi
/// ama her okuma bir pivot demek: yanıt detayını göstermek, Excel'e
/// aktarmak ve "şu soruya X diyenler" süzgeci hep aynı tabloyu döndürüp
/// çevirmeyi gerektirirdi. PostgreSQL 18'in <c>JSON_TABLE</c>'ı ve GIN
/// indeksi bu iki işi JSONB üzerinde doğrudan yapıyor.
/// </para>
/// <para>
/// <b>IP saklanmıyor, özeti saklanıyor.</b> Ham IP kişisel veri ve
/// saklanmasının tek gerekçesi kötüye kullanımı ayırt etmek; bunun için
/// tuzlanmış özet yetiyor. Aynı gövdeden gelen yinelenen gönderimler
/// sayılabiliyor, geriye dönük kimse adreslenemiyor.
/// </para>
/// </remarks>
[Table("form_yanitlari")]
public class FormResponse
{
    [Column("id")]
    public long Id { get; set; }

    [Column("form_id")]
    public long FormId { get; set; }

    public Form? Form { get; set; }

    /// <summary>Hangi tanıma göre yanıtlandı.</summary>
    [Column("surum_id")]
    public long SurumId { get; set; }

    public FormVersion? Surum { get; set; }

    /// <summary>Vatandaşa verilen takip numarası.</summary>
    [Column("takip_no")]
    public string TakipNo { get; set; } = string.Empty;

    /// <summary>
    /// Yarım kalmış yanıtı sürdürmek için gizli belirteç.
    /// </summary>
    /// <remarks>
    /// Takip numarası kısa ve insana okunacak kadar basit; onu sürdürme
    /// anahtarı yapmak, numarayı tahmin eden birinin başkasının yarım
    /// formunu açması demekti.
    /// </remarks>
    [Column("surdurme_anahtari")]
    public string? SurdurmeAnahtari { get; set; }

    [Column("durum")]
    public FormYanitDurumu Durum { get; set; } = FormYanitDurumu.Taslak;

    /// <summary>Cevaplar — <c>jsonb</c>, <c>{ "alanKimligi": deger }</c>.</summary>
    [Column("cevaplar")]
    public string Cevaplar { get; set; } = "{}";

    // ── kimlik (kipe göre dolu) ────────────────────────────────────────

    [Column("ad_soyad")]
    public string? AdSoyad { get; set; }

    [Column("telefon")]
    public string? Telefon { get; set; }

    /// <summary>Sadeleştirilmiş telefon — arama ve raporlama için.</summary>
    [Column("telefon_sade")]
    public string? TelefonSade { get; set; }

    /// <summary>
    /// TEK YANIT ANAHTARI — <c>HMAC(form.AnonimTuzu, telefon|kullaniciId)</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mükerrer kontrolü <c>CountAsync</c> + <c>Insert</c> ile yapmak bir
    /// TOCTOU: iki eşzamanlı gönderim ikisi de "yok" okur ve ikisi de yazar.
    /// Karar veritabanında, <b>kısmi benzersiz indeksle</b> veriliyor —
    /// yarış koşulu diye bir şey kalmıyor.
    /// </para>
    /// <para>
    /// Telefonun kendisi değil özeti: anonim ama mükerrersiz anket
    /// (<i>"kimin oy verdiğini tutma, iki kez oy vermesini engelle"</i>)
    /// ancak böyle mümkün.
    /// </para>
    /// </remarks>
    [Column("kimlik_karmasi")]
    public string? KimlikKarmasi { get; set; }

    [Column("eposta")]
    public string? Eposta { get; set; }

    /// <summary>Personel kipinde dolu.</summary>
    [Column("kullanici_id")]
    public long? KullaniciId { get; set; }

    // ── iz ─────────────────────────────────────────────────────────────

    /// <summary>Tuzlanmış IP özeti — ham adres SAKLANMAZ.</summary>
    [Column("ip_ozeti")]
    public string? IpOzeti { get; set; }

    [Column("tarayici")]
    public string? Tarayici { get; set; }

    [Column("baslama_tarihi")]
    public DateTime BaslamaTarihi { get; set; } = DateTime.Now;

    [Column("gonderim_tarihi")]
    public DateTime? GonderimTarihi { get; set; }

    public ICollection<FormResponseFile> Dosyalar { get; set; } = new List<FormResponseFile>();
}

/// <summary>
/// Yanıta eklenen dosya.
/// </summary>
/// <remarks>
/// <b>GİZLİ ALANDA saklanır.</b> Vatandaşın yüklediği belge kimlik
/// fotokopisi olabiliyor; <c>wwwroot/uploads</c> altındaki her şey kimlik
/// doğrulanmadan servis ediliyor ve orası bu iş için yanlış yer.
/// </remarks>
[Table("form_yanit_dosyalari")]
public class FormResponseFile
{
    [Column("id")]
    public long Id { get; set; }

    [Column("yanit_id")]
    public long YanitId { get; set; }

    public FormResponse? Yanit { get; set; }

    /// <summary>Hangi alana yüklendi.</summary>
    [Column("alan_kimligi")]
    public string AlanKimligi { get; set; } = string.Empty;

    [Column("ad")]
    public string Ad { get; set; } = string.Empty;

    /// <summary>Depodaki anahtar (gizli alan).</summary>
    [Column("anahtar")]
    public string Anahtar { get; set; } = string.Empty;

    [Column("icerik_tipi")]
    public string? IcerikTipi { get; set; }

    [Column("boyut")]
    public long Boyut { get; set; }

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
}
