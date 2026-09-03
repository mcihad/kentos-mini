using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Models
{
    [Table("mahalleler")]
    public class Mahalle
    {
        [Column("id")]
        public long Id { get; set; }
        [Display(Name = "Mahalle Adı")]
        [Required(ErrorMessage = "Mahalle adı boş bırakılamaz.")]
        [Column("ad")]
        public string Ad { get; set; }

        public ICollection<Randevu> Randevular { get; set; } = new List<Randevu>();

    }
}
