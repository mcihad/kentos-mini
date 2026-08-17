using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Models
{
    [Table("ajanda_notlar")]
    public class AjandaNot
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("not")]
        public string Not { get; set; }
        [Column("ajanda_id")]
        public long? AjandaId { get; set; }
        public Ajanda? Ajanda { get; set; }
        [Column("olusturan")]
        public string? Olusturan { get; set; }
        [Column("olusturulma_tarihi")]
        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;
    }
}
