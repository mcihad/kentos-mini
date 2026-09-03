using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Models
{
    [Table("randevu_tipleri")]
    public class RandevuTip
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("ad", TypeName = "varchar(50)")]
        [Display(Name = "Randevu Tipi")]
        [Required(ErrorMessage = "Randevu tipi adı boş bırakılamaz")]
        public string Ad { get; set; }
        [Column("renk", TypeName = "varchar(50)")]
        [Display(Name = "Renk")]
        public string? Renk { get; set; }
        [Column("aciklama", TypeName = "varchar(255)")]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }

        public ICollection<Randevu>? Randevular { get; set; } = new List<Randevu>();
        public ICollection<Ajanda> Ajandalar { get; set; } = new List<Ajanda>();

    }
}
