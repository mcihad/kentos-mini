using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Models
{
    [Table("notlar")]
    public class RandevuNot
    {
        [Column("id")]
        public long Id { get; set; }

        [Display(Name = "Not Tipi")]
        [Column("tip")]
        public string? Tip { get; set; }
        [Display(Name = "Not")]
        [Column("not")]
        public string Not { get; set; }
        [Display(Name = "Randevu")]
        [Column("randevu_id")]
        public long? RandevuId { get; set; }
        [Display(Name = "Randevu")]
        public Randevu? Randevu { get; set; }
        [Display(Name = "Tarih")]
        [Column("tarih")]
        public DateTime? Tarih { get; set; } = DateTime.Now;
        [Display(Name = "Oluşturan")]
        [Column("olusturan")]
        public string? Olusturan { get; set; }


    }
}
