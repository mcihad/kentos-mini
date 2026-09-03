using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Models
{
    [Table("randevular")]
    public class Randevu
    {
        [Display(Name = "Id")]
        [Column("id")]
        public long Id { get; set; }
        [Column("konu", TypeName = "varchar(100)")]
        [Display(Name = "Konu")]
        [Required(ErrorMessage = "Konu alanı boş bırakılamaz.")]
        public string Konu { get; set; }
        [Column("ad", TypeName = "varchar(100)")]
        [Display(Name = "Ad")]
        [Required(ErrorMessage = "Ad alanı boş bırakılamaz.")]
        public string Ad { get; set; }
        [Column("soyad", TypeName = "varchar(100)")]
        [Display(Name = "Soyad")]
        [Required(ErrorMessage = "Soyad alanı boş bırakılamaz.")]
        public string? Soyad { get; set; }
        [Column("meslek", TypeName = "varchar(100)")]
        [Display(Name = "Meslek")]
        public string? Meslek { get; set; }
        [Column("telefon", TypeName = "varchar(50)")]
        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }
        [Column("email", TypeName = "varchar(100)")]
        [Display(Name = "Email")]
        public string? Email { get; set; }
        [Column("adres", TypeName = "varchar(255)")]
        [Display(Name = "Adres")]
        public string? Adres { get; set; }
        [Column("yer", TypeName = "varchar(100)")]
        [Display(Name = "Randevu Yeri")]
        public string? Yer { get; set; }
        [Column("koordinat", TypeName = "varchar(100)")]
        [Display(Name = "Harita")]
        public string? Koordinat { get; set; }
        [Display(Name = "Başlangıç Tarihi")]
        [Required(ErrorMessage = "Başlangıç tarihi boş bırakılamaz.")]
        [Column("baslangic_tarih")]
        public DateTime? BaslangicTarih { get; set; }

        [Display(Name = "Bitiş Tarihi")]
        [Required(ErrorMessage = "Bitiş tarihi boş bırakılamaz.")]
        [Column("bitis_tarih")]
        public DateTime? BitisTarih { get; set; }
        [Column("aciklama", TypeName = "text")]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }
        [Display(Name = "Özgeçmiş?")]
        [Column("ozgecmis_durum")]
        public bool OzgecmisDurum { get; set; } = false;
        [Column("ozgecmis_dosya", TypeName = "varchar(255)")]
        [Display(Name = "Özgeçmiş Dosya")]
        public string? OzgecmisDosya { get; set; }

        [Display(Name = "Birim")]
        public Birim? Birim { get; set; }
        [Display(Name ="Birim")]
        [Column("birim_id")]
        public long? BirimId { get; set; }
        [Display(Name = "Randevu Tipi")]
        public RandevuTip? RandevuTip { get; set; }
        [Display(Name = "Randevu Tipi")]
        [Column("randevu_tip_id")]
        public long? RandevuTipId { get; set; }
        [Display(Name = "Mahalle")]

        public Mahalle? Mahalle { get; set; }
        [Display(Name = "Mahalle")]
        [Required(ErrorMessage = "Mahalle boş bırakılamaz.")]
        [Column("mahalle_id")]
        public long? MahalleId { get; set; }
        [Display(Name = "Randevu Durumu")]
        public RandevuDurum? RandevuDurum { get; set; }
        [Display(Name = "Randevu Durumu")]
        [Required(ErrorMessage = "Randevu durumu boş bırakılamaz.")]
        [Column("randevu_durum_id")]
        public long? RandevuDurumId { get; set; }
        [Display(Name = "Kayıt Tarihi")]
        [Column("olusturma_tarih")]
        public DateTime? OlusturmaTarih { get; set; }
        [Display(Name = "Güncelleme Tarihi")]
        [Column("guncelleme_tarih")]
        public DateTime? GuncellemeTarih { get; set; }
        [Column("olusturan", TypeName = "varchar(100)")]
        [Display(Name = "Oluşturan")]
        public string? Olusturan { get; set; }

        [Display(Name = "Tamamlanma Tarihi")]
        [Column("tamamlanma_tarih")]
        public DateTime? TamamlanmaTarih { get; set; }

        [Column("guncelleyen", TypeName = "varchar(100)")]
        [Display(Name = "Güncelleyen")]

        public string? Guncelleyen { get; set; }
        [Display(Name = "Ajanda Durumu")]
        [Column("ajanda_durum")]
        public bool AjandaDurum { get; set; } = false;
        [Display(Name = "Ajanda Id")]
        [Column("ajanda_id")]
        public long AjandaId { get; set; } = 0;
        [Column("arsivlendi")]
        public bool Arsivlendi { get; set; } = false;

        public ICollection<RandevuNot>? Notlar { get; set; } = new List<RandevuNot>();
        public ICollection<RandevuDosya>? Dosyalar { get; set; } = new List<RandevuDosya>();
        public ICollection<RandevuHareket>? Hareketler { get; set; } = new List<RandevuHareket>();
    }
}
