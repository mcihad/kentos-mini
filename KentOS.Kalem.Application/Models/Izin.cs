using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models
{
    /// <summary>
    /// İzin kataloğunun veritabanı kopyası.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Doğruluk kaynağı <c>Identity.Izinler</c> sınıfıdır; bu tablo açılışta
    /// oradan tohumlanır. Veritabanında da tutulmasının iki sebebi var:
    /// <c>RolIzin</c> için yabancı anahtar ve yönetim ekranının izin listesini
    /// tek sorguda okuyabilmesi.
    /// </para>
    /// <para>
    /// <b>Koddan kaldırılan izin SİLİNMEZ</b>, <see cref="Kullanimda"/> false
    /// olur. Silmek, o izne bağlı rol kayıtlarını da götürür ve yetkinin fark
    /// edilmeden değişmesi demektir; işaretlemek ise yönetim ekranında
    /// "artık kullanılmıyor" diye gösterilmesine izin verir.
    /// </para>
    /// </remarks>
    [Table("izinler")]
    public class Izin
    {
        /// <summary>Kod sabitiyle aynı metin: <c>ajanda.ekle</c>.</summary>
        [Column("ad")]
        public string Ad { get; set; } = string.Empty;

        /// <summary>Yönetim ekranındaki başlık: "Ajanda", "Talep"…</summary>
        [Column("grup")]
        public string Grup { get; set; } = string.Empty;

        [Column("baslik")]
        public string Baslik { get; set; } = string.Empty;

        /// <summary>
        /// Ne işe yaradığı. Rol kuran kişi izin ADINA bakarak karar veremiyor.
        /// </summary>
        [Column("aciklama")]
        public string Aciklama { get; set; } = string.Empty;

        /// <summary>Kodda hâlâ tanımlı mı.</summary>
        [Column("kullanimda")]
        public bool Kullanimda { get; set; } = true;

        /// <summary>Katalogdaki sıra — yönetim ekranı bunu kullanır.</summary>
        [Column("sira_no")]
        public int SiraNo { get; set; }

        [Column("olusturma_tarihi")]
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    }

    /// <summary>Bir role bağlanmış izin.</summary>
    /// <remarks>
    /// Rol ↔ izin ilişkisi <b>veritabanında</b>: yeni bir yetki dağılımı için
    /// kod değiştirip yayın yapmak gerekmiyor. Önceden politikalar
    /// <c>PolicyRegistrar.cs</c> içinde sabit rol listeleriydi ve her yetki
    /// değişikliği bir sürüm demekti.
    /// </remarks>
    [Table("rol_izinleri")]
    public class RolIzin
    {
        [Column("rol_id")]
        public long RolId { get; set; }
        [Column("izin_ad")]
        public string IzinAd { get; set; } = string.Empty;
        public Izin? Izin { get; set; }

        [Column("olusturma_tarihi")]
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    }
}
