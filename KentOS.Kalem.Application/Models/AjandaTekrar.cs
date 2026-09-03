using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models
{
    [Table("ajanda_tekrarlar")]
    public class AjandaTekrar
    {
        [Column("id")]
        public long Id { get; set; }
        [Display(Name = "Tekrar Sıklığı")]
        [Required(ErrorMessage = "Tekrar sıklığı gereklidir.")]
        [Column("tekrar_sikligi")]
        public TekrarSikligi TekrarSikligi { get; set; }

        [Display(Name = "Başlangıç Tarihi")]
        [Required(ErrorMessage = "Başlangıç tarihi gereklidir.")]
        [Column("baslangic_tarihi")]
        public DateTime BaslangicTarihi { get; set; }

        [Display(Name = "Bitiş Tarihi")]
        [Column("bitis_tarihi")]
        public DateTime? BitisTarihi { get; set; }

        [Display(Name = "Tekrar Sayısı")]
        [Column("tekrar_sayisi")]
        public int? TekrarSayisi { get; set; } // Number of occurrences

        [Display(Name = "Haftanın Günleri")]
        [Column("haftanin_gunleri")]
        public HaftaninGunleri? HaftaninGunleri { get; set; }

        [Display(Name = "Ayın Günleri")]
        [Column("ayin_gunleri")]
        public string? AyinGunleri { get; set; } // For monthly recurrence (e.g., "1,15,30")

        [Display(Name = "Oluşturma Tarihi")]
        [Column("olusturma_tarihi")]
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

        [Display(Name = "Güncelleme Tarihi")]
        [Column("guncelleme_tarihi")]
        public DateTime? GuncellemeTarihi { get; set; }

        [Display(Name = "Ajanda")]
        public Ajanda Ajanda { get; set; }
    }
}
