using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Models
{
    [Table("randevu_hareketler")]
    public class RandevuHareket
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("kullanici_id")]
        public long KullaniciId { get; set; }
        [Column("kullanici", TypeName = "varchar(100)")]
        [Display(Name = "Kullanıcı")]
        public string Kullanici { get; set; }
        [Column("eski_birim_id")]
        public long EskiBirimId { get; set; }
        [Column("eski_birim", TypeName = "varchar(100)")]
        [Display(Name = "Gelen Birim")]
        public string EskiBirim { get; set; }
        [Column("yeni_birim_id")]
        public long YeniBirimId { get; set; }
        [Column("yeni_birim", TypeName = "varchar(100)")]
        [Display(Name = "Giden Birim")]
        public string YeniBirim { get; set; }
        [Display(Name = "Aşağı Hareket")]
        [Column("asagi_hareket")]
        public bool AsagiHareket { get; set; } = true;

        [Column("tarih", TypeName = "varchar(100)")]
        [Display(Name = "İşlem Tarihi")]

        public DateTime Tarih { get; set; }
        [Display(Name = "Randevu")]
        [Column("randevu_id")]
        public long RandevuId { get; set; }
        [Display(Name = "Randevu")]
        public Randevu Randevu { get; set; }
    }
}
