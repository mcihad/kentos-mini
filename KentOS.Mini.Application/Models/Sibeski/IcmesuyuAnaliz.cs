using KentOS.Mini.Application.Enums.Sibeski;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Models.Sibeski
{
    [Table("icmesuyu_analizleri")]
    public class IcmesuyuAnaliz
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("tarih")]
        public DateOnly Tarih { get; set; }
        [Column("analiz_no")]
        public Guid AnalizNo { get; set; }
        [Column("analiz_nokta")]
        public AnalizNokta AnalizNokta { get; set; }
        [Column("sicaklik")]
        public float? Sicaklik { get; set; }
        [Column("ph")]
        public float? Ph { get; set; }
        [Column("phs")]
        public float? Phs { get; set; }
        [Column("bulaniklik")]
        public float? Bulaniklik { get; set; }
        [Column("renk")]
        public float? Renk { get; set; }
        [Column("serbest_klor")]
        public float? SerbestKlor { get; set; }
        [Column("alkalinite")]
        public float? Alkalinite { get; set; }
        [Column("tsertlik")]
        public float? Tsertlik { get; set; }
        [Column("casertlik")]
        public float? Casertlik { get; set; }
        [Column("mgsertlik")]
        public float? Mgsertlik { get; set; }
        [Column("tds")]
        public float? Tds { get; set; }
        [Column("iletkenlik")]
        public float? Iletkenlik { get; set; }
        [Column("demir")]
        public float? Demir { get; set; }
        [Column("mangan")]
        public float? Mangan { get; set; }
        [Column("nitrat")]
        public float? Nitrat { get; set; }
        [Column("nitrit")]
        public float? Nitrit { get; set; }
        [Column("amonyak")]
        public float? Amonyak { get; set; }
        [Column("permanganat")]
        public float? Permanganat { get; set; }
        [Column("oksijen")]
        public float? Oksijen { get; set; }
        [Column("bakteriyolojik")]
        public float? Bakteriyolojik { get; set; }
        [Column("aciklama")]
        public string? Aciklama { get; set; }
        [Column("created")]
        public DateTime Created { get; set; }
        [Column("updated")]
        public DateTime? Updated { get; set; }
        [Column("created_by")]
        public string? CreatedBy { get; set; }
        [Column("last_modified_by")]
        public string? LastModifiedBy { get; set; }

    }
}
