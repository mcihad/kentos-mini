using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Models
{
    [Table("meslekler")]
    public class Meslek
    {
        [Column("id")]
        public long Id { get; set; }
        [Display(Name ="Meslek Adı")]
        [Required(ErrorMessage ="Meslek adı zorunludur")]
        [Column("ad")]
        public string Ad { get; set; }


    }
}
