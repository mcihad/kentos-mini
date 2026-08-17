using KentOS.Mini.Application.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Models
{
    [Table("oneriler")]
    public class Oneri
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("baslik")]
        public string Baslik { get; set; }
        [Column("aciklama")]
        public string? Aciklama { get; set; }
        [Column("tip")]
        public  OneriTip Tip { get; set; }
        [Column("tarih")]
        public DateTime? Tarih { get; set; }
        [Column("kullanici_id")]
        public long? KullaniciId { get; set; }
        [Column("kullanici_adi")]
        public string? KullaniciAdi { get; set; }
        [Column("cevap")]
        public string? Cevap { get; set; }
        [Column("cevap_tarih")]
        public DateTime? CevapTarih { get; set; }
    }
}
