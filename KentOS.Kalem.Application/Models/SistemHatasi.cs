using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models
{
    /// <summary>
    /// Sunucuda oluşan beklenmeyen bir hatanın kaydı.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sistem iki yıldır canlıda ve bugüne kadar hatalar YALNIZCA konsol
    /// günlüğüne düşüyordu: sunucu yeniden başlayınca kayboluyor, kullanıcı
    /// "hata aldım" dediğinde geriye bakacak bir şey kalmıyordu.
    /// </para>
    /// <para>
    /// Aynı hata tekrar tekrar oluşuyorsa YENİ SATIR AÇILMAZ; var olan kaydın
    /// <see cref="Adet"/> değeri artar ve <see cref="SonGorulme"/> güncellenir.
    /// Aksi hâlde bir döngüye giren tek bir hata, listeyi binlerce satırla
    /// doldurup diğerlerini görünmez kılardı.
    /// </para>
    /// </remarks>
    [Table("sistem_hatalari")]
    public class SistemHatasi
    {
        [Column("id")]
        public long Id { get; set; }

        /// <summary>
        /// Aynı hatayı tanımanın anahtarı — tür + mesaj + yığın konumundan
        /// üretilen kararlı bir özet.
        /// </summary>
        [Column("parmakizi")]
        public string Parmakizi { get; set; } = string.Empty;

        [Column("ilk_gorulme")]
        public DateTime IlkGorulme { get; set; } = DateTime.Now;
        [Column("son_gorulme")]
        public DateTime SonGorulme { get; set; } = DateTime.Now;

        /// <summary>Kaç kez oluştu.</summary>
        [Column("adet")]
        public int Adet { get; set; } = 1;

        // ── Ne oldu ──
        [Column("tur")]
        public string Tur { get; set; } = string.Empty;
        [Column("mesaj")]
        public string Mesaj { get; set; } = string.Empty;
        [Column("ic_mesaj")]
        public string? IcMesaj { get; set; }
        [Column("yigin_izi")]
        public string? YiginIzi { get; set; }

        /// <summary>Hatanın atıldığı kaynak dosya (varsa).</summary>
        [Column("dosya")]
        public string? Dosya { get; set; }
        [Column("satir")]
        public int? Satir { get; set; }

        /// <summary>HTTP durum kodu (çoğunlukla 500).</summary>
        [Column("durum_kodu")]
        public int DurumKodu { get; set; }

        // ── Nerede oldu ──
        [Column("yol")]
        public string? Yol { get; set; }
        [Column("yontem")]
        public string? Yontem { get; set; }
        [Column("sorgu_dizesi")]
        public string? SorguDizesi { get; set; }

        /// <summary>İstek gövdesi — kırpılmış ve hassas alanları maskelenmiş.</summary>
        [Column("govde")]
        public string? Govde { get; set; }

        /// <summary>Başlıklar — <c>Authorization</c> ve <c>Cookie</c> maskelenir.</summary>
        [Column("basliklar")]
        public string? Basliklar { get; set; }

        // ── Kim ──
        [Column("kullanici_id")]
        public long? KullaniciId { get; set; }
        [Column("kullanici_adi")]
        public string? KullaniciAdi { get; set; }
        [Column("birim_id")]
        public long? BirimId { get; set; }
        [Column("ip_adresi")]
        public string? IpAdresi { get; set; }
        [Column("istemci")]
        public string? Istemci { get; set; }

        /// <summary>Yanıtla birlikte kullanıcıya verilen iz kimliği.</summary>
        [Column("iz_kimligi")]
        public string? IzKimligi { get; set; }

        // ── Çözüm takibi ──
        [Column("cozuldu")]
        public bool Cozuldu { get; set; }
        [Column("cozulme_tarihi")]
        public DateTime? CozulmeTarihi { get; set; }
        [Column("cozen_kullanici")]
        public string? CozenKullanici { get; set; }

        /// <summary>Çözüm notları — serbest metin.</summary>
        [Column("notlar")]
        public string? Notlar { get; set; }
    }
}
