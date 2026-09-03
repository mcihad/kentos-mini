using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Models
{
    [Table("ajanda_durumlar")]
    public class AjandaDurum
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("ad", TypeName = "varchar(100)")]
        [Required(ErrorMessage ="Durum Adı Zorunludur")]
        [Display(Name = "Durum Adı")]
        public string Ad { get; set; }
        [Column("renk", TypeName = "varchar(20)")]
        [Display(Name = "Renk")]
        [Required(ErrorMessage = "Renk Zorunludur")]
        public string Renk { get; set; }

        [Display(Name = "Açıklama")]
        [Column("aciklama", TypeName = "varchar(500)")]
        public string? Aciklama { get; set; }
        [Column("icon", TypeName = "varchar(100)")]
        public string? Icon { get; set; }
        public ICollection<Ajanda>? Ajandalar { get; set; } = new List<Ajanda>();
    }
}
