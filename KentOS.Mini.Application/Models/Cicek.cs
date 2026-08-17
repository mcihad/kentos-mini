using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Models
{
    [Table("cicekler")]
    public class Cicek
    {
        [Column("id")]
        public long Id { get; set; }
        //guid
        [Column("cicekci_id")]
        public long CicekciId { get; set; }
        [Column("guid")]
        public string? Guid { get; set; } = System.Guid.NewGuid().ToString();
        public Ajanda? Ajanda { get; set; }
        [Column("ajanda_id")]
        public long? AjandaId { get; set; }
        [Column("ad")]
        public string? Ad { get; set; }
        [Column("aciklama")]
        public string? Aciklama { get; set; }
        [Column("adres")]
        public string? Adres { get; set; }
        [Column("resim")]
        public string? Resim { get; set; }
        [Column("dogrulama_kodu")]
        public int DogrulamaKodu { get; set; }= 0;
        [Column("olusturan")]
        public string? Olusturan { get; set; }
        [Column("gonderildi")]
        public bool Gonderildi { get; set; } = false;
        [Column("gonderilme_tarihi")]
        public DateTime GonderilmeTarihi { get; set; }
        [Column("olusturulma_tarihi")]
        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;
        [Column("guncelleme_tarihi")]
        public DateTime? GuncellemeTarihi { get; set; }
    }
}
