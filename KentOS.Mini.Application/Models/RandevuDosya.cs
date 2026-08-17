using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Models
{
    [Table("dosyalar")]
    public class RandevuDosya
    {
        [Column("id")]
        public long Id { get; set; }
        [Display(Name = "Dosya Adı")]
        [Required(ErrorMessage = "Dosya adı boş bırakılamaz")]
        [Column("ad")]
        public string Ad { get; set; }
        [Display(Name = "Açıklama")]
        [Column("aciklama")]

        public string? Aciklama { get; set; }
        [Display(Name = "Dosya Yolu")]
        [Column("path")]
        public string? Path { get; set; }
        [Display(Name = "Dosya Türü")]
        [Column("content_type")]
        public string? ContentType { get; set; }
        [Display(Name = "Dosya Boyutu")]
        [Column("size")]
        public long? Size { get; set; }
        [Display(Name = "Oluşturulma Tarihi")]
        [Column("olusturma_tarih")]
        public DateTime? OlusturmaTarih { get; set; }
        [Display(Name = "Randevu")]
        public Randevu? Randevu { get; set; }
        [Display(Name = "Randevu Id")]
        [Column("randevu_id")]
        public long? RandevuId { get; set; }

    }
}
