using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using KentOS.Kalem.Application.Enums;

namespace KentOS.Kalem.Application.Dto
{
    /// <summary>
    /// Etkinlik (ajanda) taşıma nesnesi — mobil `AjandaModel` ile birebir.
    /// </summary>
    /// <remarks>
    /// DOĞRULAMA: Sınırlar veritabanı kolon tipleriyle AYNI tutulur
    /// (Konum/Koordinat → varchar(100); Baslik/Aciklama/BilgiNotu/KonusmaMetni
    /// → text, yani SINIRSIZ). Böylece aşırı uzun metin `DbUpdateException`
    /// ile HTTP 500 yerine anlaşılır bir 400 döner ve istemci formu aynı
    /// sınırı uygular.
    ///
    /// `Baslik` bir dönem `varchar(100)` idi ve sahada yetmiyordu; üretimde
    /// `text`e çevrildi. Sınırı tek katmanda kaldırmak yetmez — form, DTO ve
    /// kolon aynı anda gevşemeli, yoksa kullanıcı yine ilk duvara çarpar.
    ///
    /// ENTITY TAŞIMAZ: Eskiden `Cicek` (entity) ve `ICollection&lt;AjandaNot&gt;`
    /// alanları vardı. EF ilişki düzeltmesi geri referansları doldurduğunda
    /// Ajanda ⇄ AjandaNot / Ajanda ⇄ Cicek döngüsü oluşuyor, Mapster bu döngüyü
    /// klonlarken StackOverflow ile TÜM API sürecini çökertiyordu. Notlar zaten
    /// `GET /api/AjandaApi/{id}/Notes` ucundan alınır.
    /// </remarks>
    public class AjandaDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>Etkinlik başlığı — <b>uzunluk sınırı yok</b> (kolon <c>text</c>).</summary>
        [JsonPropertyName("baslik")]
        [Display(Name = "Başlık")]
        [Required(ErrorMessage = "Başlık zorunludur.")]
        public string Baslik { get; set; }

        [JsonPropertyName("aciklama")]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }

        [JsonPropertyName("konum")]
        [Display(Name = "Konum")]
        [MaxLength(100, ErrorMessage = "Konum en fazla 100 karakter olabilir.")]
        public string? Konum { get; set; }

        [JsonPropertyName("koordinat")]
        [Display(Name = "Koordinat")]
        [MaxLength(100, ErrorMessage = "Koordinat en fazla 100 karakter olabilir.")]
        public string? Koordinat { get; set; }

        [JsonPropertyName("baslangicTarihi")]
        [Display(Name = "Başlangıç Tarihi")]
        [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
        public DateTime BaslangicTarihi { get; set; } = DateTime.Now;

        [JsonPropertyName("bitisTarihi")]
        [Display(Name = "Bitiş Tarihi")]
        public DateTime? BitisTarihi { get; set; }

        [JsonPropertyName("irtibatKisi")]
        [Display(Name = "İrtibat Kişi")]
        public string? IrtibatKisi { get; set; }

        [JsonPropertyName("irtibatTelefon")]
        [Display(Name = "İrtibat Telefon")]
        public string? IrtibatTelefon { get; set; }

        [JsonPropertyName("tumGun")]
        [Display(Name = "Tüm Gün")]
        public bool TumGun { get; set; }

        [JsonPropertyName("basinKatilsin")]
        [Display(Name = "Basın Katılsın?")]
        public bool BasinKatilsin { get; set; }

        [JsonPropertyName("konusmaMetniDurum")]
        [Display(Name = "Konuşma Metni İstensin")]
        public bool KonusmaMetniDurum { get; set; }

        [JsonPropertyName("bilgiNotuDurum")]
        [Display(Name = "Bilgi Notu İstensin")]
        public bool BilgiNotuDurum { get; set; }

        [JsonPropertyName("resimVar")]
        [Display(Name = "Resim İstensin")]
        public bool ResimVar { get; set; }

        [JsonPropertyName("tekrarEden")]
        [Display(Name = "Tekrar Eden")]
        public bool TekrarEden { get; set; }

        [JsonPropertyName("isDeleted")]
        [Display(Name = "Silinmiş")]
        public bool IsDeleted { get; set; }

        [JsonPropertyName("bilgiNotu")]
        [Display(Name = "Bilgi Notu")]
        public string? BilgiNotu { get; set; }

        [JsonPropertyName("konusmaMetni")]
        [Display(Name = "Konuşma Metni")]
        public string? KonusmaMetni { get; set; }

        [JsonPropertyName("olusturmaTarihi")]
        [Display(Name = "Oluşturma Tarihi")]
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

        [JsonPropertyName("guncellemeTarihi")]
        [Display(Name = "Güncelleme Tarihi")]
        public DateTime? GuncellemeTarihi { get; set; }

        [JsonPropertyName("kullaniciId")]
        [Display(Name = "Kullanıcı Id")]
        public string? KullaniciId { get; set; }

        [JsonPropertyName("birimId")]
        [Display(Name = "Birim Id")]
        public long? BirimId { get; set; }

        /// <summary>
        /// Etkinliğin SAHİBİ olan birimin adı — "bu kayıt kimin ajandasından?"
        /// </summary>
        /// <remarks>
        /// Katılımcı birim, davet edildiği etkinliği kendi ajandasında kendi
        /// kaydıyla yan yana görüyor ve ikisini ayırt edemiyordu. Alan eklemek
        /// v1 sözleşmesini bozmaz; eski istemciler tanımadıkları alanı yok
        /// sayar. Servis dolduruyor (bkz. <c>ZenginlestirAsync</c>), Mapster
        /// değil: her sorgu <c>Include(a => a.Birim)</c> yapmıyor ve unutulan
        /// tek sorguda alan sessizce boş kalırdı.
        /// </remarks>
        [JsonPropertyName("birimAd")]
        public string? BirimAd { get; set; }

        [JsonPropertyName("randevuId")]
        [Display(Name = "Talep Id")]
        public long RandevuId { get; set; } = 0;

        [JsonPropertyName("randevuTipId")]
        [Display(Name = "Etkinlik Tipi")]
        [Required(ErrorMessage = "Etkinlik tipi zorunludur.")]
        public long? RandevuTipId { get; set; }

        /// <summary>
        /// Etkinlik durumu (AjandaDurum kimliği).
        /// </summary>
        [JsonPropertyName("durumId")]
        [Display(Name = "Durum")]
        [Required(ErrorMessage = "Durum zorunludur.")]
        public long? DurumId { get; set; }

        /// <summary>
        /// GERİYE DÖNÜK UYUM: Alan uzun süre `durum` adıyla taşındı; sahadaki
        /// eski mobil sürümler bu adı gönderiyor/okuyor. Aynı değeri her iki
        /// adla da kabul eder ve yanıtta her ikisini de döndürür.
        /// Eski sürümler tamamen kalktığında kaldırılabilir.
        /// </summary>
        /// <remarks>
        /// Özellik adı bilinçli olarak entity'deki `Durum` navigasyonundan
        /// FARKLIDIR: aynı adı kullanmak Mapster'ın AjandaDurum navigasyonunu bu
        /// sayısal alana eşlemesine ve boş bir durum kaydı INSERT etmeye
        /// çalışmasına yol açıyordu.
        /// </remarks>
        [JsonPropertyName("durum")]
        public long? DurumIdEskiAd
        {
            get => DurumId;
            set => DurumId ??= value;
        }

        [JsonPropertyName("cicekId")]
        public long? CicekId { get; set; }

        [JsonPropertyName("photos")]
        public virtual ICollection<AjandaPhotoDto> Photos { get; set; }
            = new List<AjandaPhotoDto>();

        /// <summary>Gönderilen çiçek bilgisi (entity değil, DTO).</summary>
        [JsonPropertyName("cicek")]
        public CicekDto? Cicek { get; set; }

        [JsonPropertyName("status")]
        public AjandaStatus Status { get; set; } = AjandaStatus.Pending;

        // ---------------------------------------------------------------- gizlilik
        /// <summary>
        /// Gizli etkinlik: yalnızca ekleyen ve <see cref="KatilimciIdler"/>
        /// listesindeki kişiler görebilir.
        /// Alan EKLEMELİDİR — göndermeyen eski istemcilerde false kalır ve
        /// davranış bugünküyle aynı olur.
        /// </summary>
        [JsonPropertyName("gizli")]
        [Display(Name = "Gizli Etkinlik")]
        public bool Gizli { get; set; }

        /// <summary>
        /// OKUMA: her iki liste birlikte — katılımcı birimler ve (gizliyse)
        /// görebilecek kişiler.
        /// </summary>
        /// <remarks>
        /// Ayrım <c>KatilimciDto.BirimId</c> ile yapılır: dolu ise KATILIMCI
        /// BİRİM, boş ise GÖREBİLECEK KİŞİ. Yazma sırasında dikkate alınmaz.
        /// </remarks>
        [JsonPropertyName("katilimcilar")]
        public List<KatilimciDto> Katilimcilar { get; set; } = new();

        /// <summary>
        /// YAZMA: gizli etkinliği GÖREBİLECEK KİŞİLERİN kullanıcı kimlikleri.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Yalnızca <see cref="Gizli"/> iken anlamlıdır; etkinlik gizlilikten
        /// çıkarsa liste temizlenir. Seçilebilecek kişiler <b>ekleyenin kendi
        /// birimiyle</b> sınırlıdır ve bu sunucuda zorlanır.
        /// </para>
        /// <para>
        /// <b>Katılımcı birimle karıştırılmamalı.</b> Bu liste "kim görebilir"
        /// sorusunun cevabı; "kim katılacak" sorusununki
        /// <see cref="KatilimciBirimIdler"/>. Bir dönem ikisi birleştirilmişti
        /// ve gizli bir toplantıya bir müdürlüğü davet etmek o müdürlükteki
        /// herkesi içeriğe ortak ediyordu.
        /// </para>
        /// <para>
        /// <c>null</c> = dokunma, boş liste = temizle.
        /// </para>
        /// </remarks>
        [JsonPropertyName("katilimciIdler")]
        public List<long>? KatilimciIdler { get; set; }

        /// <summary>
        /// YAZMA: etkinliğe KATILACAK BİRİMLERİN kimlikleri.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Gizlilikten bağımsızdır — açık toplantının davetlileri de kaydedilir.
        /// Gizli etkinliğe erişim <b>vermez</b>. Yalnızca kullanıcının kendi
        /// seviyesindeki ve altındaki birimler seçilebilir; sunucuda zorlanır.
        /// </para>
        /// <para>
        /// <c>null</c> gönderilirse liste DEĞİŞTİRİLMEZ (kısmi güncelleme);
        /// boş liste gönderilirse tüm birim katılımcıları silinir.
        /// </para>
        /// </remarks>
        [JsonPropertyName("katilimciBirimIdler")]
        public List<long>? KatilimciBirimIdler { get; set; }

        // ------------------------------------------------------------------ tekrar
        /// <summary>Bu kayıt bir tekrar serisinden üretildiyse serinin kimliği.</summary>
        [JsonPropertyName("seriId")]
        public long? SeriId { get; set; }

        /// <summary>Tekrarın kural gereği hesaplanan özgün başlangıcı (RECURRENCE-ID).</summary>
        [JsonPropertyName("seriOrijinalBaslangic")]
        public DateTime? SeriOrijinalBaslangic { get; set; }

        /// <summary>Bu tekrar bireysel düzenlendi (istisna) — seri güncellemeleri atlar.</summary>
        [JsonPropertyName("seriAyrik")]
        public bool SeriAyrik { get; set; }

        /// <summary>
        /// OKUMA: serinin RRULE kuralı (ör. <c>FREQ=WEEKLY;BYDAY=MO</c>).
        /// Yazma sırasında yoksayılır — kural oluşturma/güncelleme
        /// <c>AjandaSeriOlusturDto</c> ile yapılır.
        /// </summary>
        [JsonPropertyName("tekrarKurali")]
        [MaxLength(500)]
        public string? TekrarKurali { get; set; }

        /// <summary>OKUMA: kuralın insan-okur Türkçe özeti ("Her hafta Pazartesi").</summary>
        [JsonPropertyName("tekrarOzeti")]
        public string? TekrarOzeti { get; set; }

        /// <summary>
        /// OKUMA: serinin SON tekrarının tarihi. Sonsuz seride <c>null</c>.
        ///
        /// Özet "10 tekrar" dediğinde kullanıcı serinin ne zaman biteceğini
        /// bilemiyordu; arayüzler bu alanı "… tarihinde bitecek" satırında
        /// gösterir. COUNT'lu kuralda da somut tarih üretilir.
        /// </summary>
        [JsonPropertyName("tekrarBitisi")]
        public DateTime? TekrarBitisi { get; set; }

        /// <summary>
        /// YAZMA: yeni etkinlik oluşturulurken tekrar kuralı verilirse etkinlik
        /// bir SERİ olarak kurulur ve tekrarlar üretilir. Boş bırakılırsa tek
        /// seferlik etkinlik oluşur — göndermeyen eski istemciler etkilenmez.
        /// Güncellemede, kural değiştirilmek istendiğinde
        /// <see cref="Kapsam"/> = BundanSonrakiler ile birlikte kullanılır.
        /// </summary>
        [JsonPropertyName("tekrar")]
        public AjandaSeriOlusturDto? Tekrar { get; set; }

        /// <summary>
        /// YAZMA: tekrarlanan bir etkinliğin tekrarını KALDIRIR.
        ///
        /// `tekrar` alanının boş gelmesi "tekrarı kaldır" anlamına GELMEZ —
        /// alanı hiç göndermeyen eski istemciler tüm serileri bozardı. Kaldırma
        /// bu açık bayrakla istenir ve <see cref="Kapsam"/> ile birlikte çalışır.
        /// </summary>
        [JsonPropertyName("tekrarKaldir")]
        public bool TekrarKaldir { get; set; }

        /// <summary>
        /// YAZMA: tekrarlanan etkinlikte güncelleme/silme KAPSAMI.
        /// Varsayılan <see cref="TekrarKapsam.Yalnizca"/> olduğu için kapsam
        /// göndermeyen istemcilerde davranış bugünküyle birebir aynıdır.
        /// </summary>
        [JsonPropertyName("kapsam")]
        public TekrarKapsam Kapsam { get; set; } = TekrarKapsam.Yalnizca;

        // ------------------------------------------------------------------
        // Türetilmiş alanlar: MVC görünümleri kullanıyor (ör. Views/Medya),
        // ancak JSON gövdesinde yer kaplamalarının anlamı yok — mobil aynı
        // bilgiyi kendi modelinde hesaplıyor.
        // ------------------------------------------------------------------
        [NotMapped, JsonIgnore]
        public bool HasCicek => CicekId != null;

        [NotMapped, JsonIgnore]
        public bool HasNoCicek => CicekId == null && Status != AjandaStatus.Canceled;

        [NotMapped, JsonIgnore]
        public bool HasPhotos => Photos.Count > 0;

        [NotMapped, JsonIgnore]
        public bool HasNoPhotos => Photos.Count == 0;

        [NotMapped, JsonIgnore]
        public bool IsCurrent => BaslangicTarihi <= DateTime.Now && BitisTarihi >= DateTime.Now;
    }
}
