using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Models
{
    [Table("cicekciler")]
    public class Cicekci
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("ad_soyad")]
        public string AdSoyad { get; set; }
        [Column("telefon")]
        public string Telefon { get; set; }
        [Column("adres")]
        public string Adres { get; set; }
        [Column("aktif")]
        public bool Aktif { get; set; }
    }
}
