using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models
{
    /// <summary>
    /// Tekrarlanan etkinlik SERİSİ — RFC 5545 (iCalendar) RRULE kuralını tutar.
    ///
    /// TASARIM: Kural burada durur, tekrarlar ise GERÇEK <see cref="Ajanda"/> satırı
    /// olarak üretilir (somutlaştırma / materialization). Böylece not, fotoğraf,
    /// çiçek, durum, zaman çizelgesi doğal olarak "eklendiği tekrara" ait olur;
    /// listeleme, arama, arşiv, istatistik ve takvim yolları hiç değişmeden çalışır
    /// ve sahadaki eski mobil sürümler tekrarları normal etkinlik gibi görür.
    ///
    /// Eski <c>AjandaTekrar</c> tablosu (ajanda_tekrarlar) bu iş için kullanılmadı:
    /// hiçbir kod tarafından okunmuyor/yazılmıyor (ölü kod), çift yabancı anahtarlı
    /// hatalı bir tasarımı var ve RRULE'un tamamını ifade edemiyor. O tabloya
    /// DOKUNULMADI — mevcut veriyi bozmamak için yerinde bırakıldı.
    /// </summary>
    [Table("ajanda_seriler")]
    public class AjandaSeri
    {
        [Column("id")]
        public long Id { get; set; }

        /// <summary>
        /// RFC 5545 tekrar kuralı, ör. <c>FREQ=WEEKLY;BYDAY=MO;INTERVAL=1</c>.
        /// "RRULE:" öneki OLMADAN saklanır.
        /// </summary>
        [Required]
        [Column("rrule", TypeName = "varchar(500)")]
        public string Rrule { get; set; } = string.Empty;

        /// <summary>Serinin ilk tekrarının başlangıcı (DTSTART).</summary>
        [Column("dtstart")]
        public DateTime Dtstart { get; set; }

        /// <summary>
        /// Tekrar süresi (dakika). Her tekrarın bitişi
        /// <c>başlangıç + SureDakika</c> olarak hesaplanır; böylece kural
        /// değiştiğinde süre korunur.
        /// </summary>
        [Column("sure_dakika")]
        public int SureDakika { get; set; }

        /// <summary>UNTIL önbelleği — kuralda bitiş tarihi varsa buraya kopyalanır.</summary>
        [Column("bitis_tarihi")]
        public DateTime? BitisTarihi { get; set; }

        /// <summary>COUNT önbelleği — kuralda tekrar sayısı varsa buraya kopyalanır.</summary>
        [Column("tekrar_sayisi")]
        public int? TekrarSayisi { get; set; }

        /// <summary>
        /// Ufuk imi: bu tarihe kadar olan tekrarlar üretildi. Sonsuz kurallarda
        /// (UNTIL/COUNT yok) arka plan görevi bu imi ileriye taşır.
        /// </summary>
        [Column("uretilen_son_tarih")]
        public DateTime? UretilenSonTarih { get; set; }

        [Display(Name = "Birim Id")]
        [Column("birim_id")]
        public long? BirimId { get; set; }

        /// <summary>Seriyi oluşturan kullanıcı ADI (Ajanda.KullaniciId ile aynı biçim).</summary>
        [Column("kullanici_id", TypeName = "varchar(150)")]
        public string? KullaniciId { get; set; }

        /// <summary>Seri iptal edildi — yeni tekrar üretilmez.</summary>
        [Column("iptal")]
        public bool Iptal { get; set; }

        [Column("olusturma_tarihi")]
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

        [Column("guncelleme_tarihi")]
        public DateTime? GuncellemeTarihi { get; set; }

        /// <summary>Bu seriden üretilmiş tekrarlar.</summary>
        public virtual ICollection<Ajanda> Tekrarlar { get; set; } = new List<Ajanda>();
    }
}
