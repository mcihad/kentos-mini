using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Mini.Application.Models
{
    /// <summary>
    /// ÖZGEÇMİŞ HAVUZU — kurum genelinde aranabilir CV kaydı.
    /// </summary>
    /// <remarks>
    /// <para>
    /// İş talebiyle gelen özgeçmiş bugün <see cref="Randevu.OzgecmisDosya"/>
    /// alanında duruyor: talebe bağlı, tek dosya, aranabilir bir bilgi taşımıyor.
    /// "Elimizde kaynak mühendisi var mı?" sorusunun cevabı yoktu — talepleri
    /// tek tek açmak gerekiyordu.
    /// </para>
    /// <para>
    /// Havuz o kaydın YERİNE geçmez, <b>üstüne</b> gelir: talebe özgeçmiş
    /// yüklendiğinde burada da bir satır açılır ve <see cref="RandevuId"/> ile
    /// hangi talepten geldiği belli olur. Doğrudan havuza eklenen kayıtlarda bu
    /// alan boştur. Böylece arama TEK tabloda yapılır; iki kaynağı sorgu anında
    /// birleştirmek sayfalama ve sıralamayı bozuyordu.
    /// </para>
    /// <para>
    /// <b>Görünürlük birime bağlı DEĞİL.</b> Sistemin geri kalanında kayıtlar
    /// birim süzgecinden geçer ama havuzun varlık sebebi tam tersi: bir
    /// müdürlüğün elindeki özgeçmiş, o kişiye iş verebilecek başka bir
    /// müdürlüğe de görünmeli. Kapı <c>ozgecmis.goruntule</c> izni;
    /// <see cref="BirimId"/> yalnızca "kim eklemiş" bilgisi için tutulur.
    /// </para>
    /// </remarks>
    [Table("ozgecmisler")]
    public class Ozgecmis
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("ad_soyad")]
        public string AdSoyad { get; set; } = string.Empty;

        [Column("telefon")]
        public string? Telefon { get; set; }

        /// <summary>
        /// Yalnızca rakamlar — kişiyi bulmanın tek güvenilir yolu.
        /// </summary>
        /// <remarks>
        /// Aynı numara veritabanında <c>0541 298 34 50</c>, <c>05412983450</c>
        /// ve <c>+90 541…</c> diye üç türlü duruyor; ham sütunda arama
        /// numarayı bitişik yazınca bulmuyordu.
        /// </remarks>
        [Column("telefon_sade")]
        public string? TelefonSade { get; set; }

        [Column("eposta")]
        public string? Eposta { get; set; }

        /// <summary>Meslek tanımı (liste). Serbest metin için <see cref="MeslekAd"/>.</summary>
        [Column("meslek_id")]
        public long? MeslekId { get; set; }
        public Meslek? Meslek { get; set; }

        /// <summary>
        /// Listede olmayan meslek. Talepten gelen kayıtlarda meslek zaten
        /// serbest metin olarak tutuluyor; kaybetmemek için buraya kopyalanır.
        /// </summary>
        [Column("meslek_ad")]
        public string? MeslekAd { get; set; }

        [Column("mahalle_id")]
        public long? MahalleId { get; set; }
        public Mahalle? Mahalle { get; set; }

        [Column("adres")]
        public string? Adres { get; set; }

        /// <summary>Aranabilir serbest not: deneyim, referans, ehliyet…</summary>
        [Column("aciklama")]
        public string? Aciklama { get; set; }

        /// <summary>Kullanıcının yüklediği ad — indirirken bu ad kullanılır.</summary>
        [Column("dosya_adi")]
        public string DosyaAdi { get; set; } = string.Empty;

        /// <summary>Diskteki ad (<c>wwwroot/uploads/ozgecmis</c> altında).</summary>
        [Column("dosya_yolu")]
        public string DosyaYolu { get; set; } = string.Empty;

        [Column("boyut")]
        public long Boyut { get; set; }
        [Column("icerik_turu")]
        public string? IcerikTuru { get; set; }

        /// <summary>Doluysa kayıt bir TALEPTEN geldi.</summary>
        [Column("randevu_id")]
        public long? RandevuId { get; set; }
        public Randevu? Randevu { get; set; }

        /// <summary>Ekleyenin birimi — süzgeç için değil, künye için.</summary>
        [Column("birim_id")]
        public long? BirimId { get; set; }

        [Column("olusturan")]
        public string? Olusturan { get; set; }
        [Column("olusturma_tarihi")]
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
        [Column("guncelleyen")]
        public string? Guncelleyen { get; set; }
        [Column("guncelleme_tarihi")]
        public DateTime? GuncellemeTarihi { get; set; }

        /// <summary>
        /// Yumuşak silme: dosyanın kendisi de kayıtla birlikte kaybolur ve
        /// yanlışlıkla silinen bir özgeçmişi geri getirmenin başka yolu yok.
        /// </summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        public ICollection<OzgecmisPaylasim> Paylasimlar { get; set; }
            = new List<OzgecmisPaylasim>();
    }

    /// <summary>
    /// Özgeçmişin başka bir kullanıcıya YÖNLENDİRİLMESİ.
    /// </summary>
    /// <remarks>
    /// "Bu iş için bizde uygun kişi var" demenin yolu: kaydı ilgili birimdeki
    /// kişiye iletmek. Dosyayı e-postayla göndermek kaydı havuzun dışına
    /// çıkarıyor ve kimin kime ne gönderdiği kayboluyordu; burada paylaşım
    /// kaydı kalır ve alıcı bildirim alır.
    /// </remarks>
    [Table("ozgecmis_paylasimlari")]
    public class OzgecmisPaylasim
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("ozgecmis_id")]
        public long OzgecmisId { get; set; }
        public Ozgecmis? Ozgecmis { get; set; }

        /// <summary><c>AspNetUsers.Id</c> — sayısal kimlik.</summary>
        [Column("paylasan_id")]
        public long PaylasanId { get; set; }
        [Column("paylasan_ad")]
        public string? PaylasanAd { get; set; }

        [Column("alici_id")]
        public long AliciId { get; set; }
        [Column("alici_ad")]
        public string? AliciAd { get; set; }

        [Column("not")]
        public string? Not { get; set; }

        [Column("tarih")]
        public DateTime Tarih { get; set; } = DateTime.Now;

        /// <summary>Alıcı kaydı açtı mı — "gönderdim ama bakmadı" ayrımı.</summary>
        [Column("goruntuleme_tarihi")]
        public DateTime? GoruntulemeTarihi { get; set; }
    }
}
