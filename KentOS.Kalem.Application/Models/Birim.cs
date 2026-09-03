using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Models
{
    [Table("birimler")]
    public class Birim
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("ad", TypeName = "varchar(100)")]
        [Display(Name = "Birim Adı")]
        [Required(ErrorMessage = "Birim adı boş bırakılamaz")]
        public string Ad { get; set; }
        [Column("yetkili", TypeName = "varchar(100)")]
        [Display(Name = "Yetkili")]
        [Required(ErrorMessage = "Yetkili alanı boş bırakılamaz")]
        public string Yetkili { get; set; }
        [Column("unvan", TypeName = "varchar(50)")]
        [Display(Name = "Ünvan")]
        public string? Unvan { get; set; }

        [Column("telefon")]
        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }
        [Column("email")]
        [Display(Name = "Email")]
        public string? Email { get; set; }
        [Column("adres")]
        [Display(Name = "Adres")]
        public string? Adres { get; set; }
        [Column("aciklama")]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }
        [Display(Name = "Üst Birim Id")]

        [Column("ust_birim_id")]
        public long? UstBirimId { get; set; }
        [Display(Name = "Üst Birim")]
        public Birim? UstBirim { get; set; }
        [Column("left_id")]
        public int LeftId { get; set; } = 0;
        [Column("right_id")]
        public int RightId { get; set; } = 0;
        [Display(Name = "Seviye")]
        [Column("level")]
        public int Level { get; set; } = 0;
        public ICollection<Birim>? AltBirimler { get; set; } = new List<Birim>();

        public ICollection<Randevu>? Randevular { get; set; } = new List<Randevu>();
        public ICollection<Ajanda> Ajandalar { get; set; } = new List<Ajanda>();

        public string GetFullName()
        {
            if (UstBirim == null)
            {
                return Ad;
            }
            return UstBirim.GetFullName() + " / " + Ad;
        }
    }
}
