using KentOS.Kalem.Application.Enums.Sibeski;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Dto.Sibeski
{
    public class IcmesuyuAnalizDto
    {
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long Id { get; set; }
        [JsonPropertyName("tarih")]
        [Display(Name = "Tarih")]
        [Required(ErrorMessage = "Tarih seçimi zorunludur.")]
        public DateOnly Tarih { get; set; }
        [JsonPropertyName("analizNo")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Guid AnalizNo { get; set; }
        [JsonPropertyName("analizNokta")]
        [Display(Name = "Analiz Nokta")]
        [Required(ErrorMessage = "Analiz nokta seçimi zorunludur.")]
        public AnalizNokta AnalizNokta { get; set; }
        [JsonPropertyName("sicaklik")]
        [Display(Name = "Sıcaklık")]
        public float? Sicaklik { get; set; }
        [JsonPropertyName("ph")]
        [Display(Name = "Ph Değeri")]
        public float? Ph { get; set; }
        [JsonPropertyName("phs")]
        [Display(Name = "Phs Değeri")]
        public float? Phs { get; set; }
        [JsonPropertyName("bulaniklik")]
        [Display(Name = "Bulanıklık(NTU)")]
        public float? Bulaniklik { get; set; }
        [JsonPropertyName("renk")]
        [Display(Name = "Renk(Pt-Co)")]
        public float? Renk { get; set; }
        [JsonPropertyName("serbestKlor")]
        [Display(Name = "Serbest Klor(mg/L)")]
        public float? SerbestKlor { get; set; }
        [JsonPropertyName("alkalinite")]
        [Display(Name = "Alkalinite(mg CaCO3/L)")]
        public float? Alkalinite { get; set; }
        [JsonPropertyName("tsertlik")]
        [Display(Name = "Toplam Sertlik(mg CaCO3/L)")]
        public float? Tsertlik { get; set; }
        [JsonPropertyName("casertlik")]
        [Display(Name = "Ca Sertlik(mg CaCO3/L)")]
        public float? Casertlik { get; set; }
        [JsonPropertyName("mgsertlik")]
        [Display(Name = "Mg Sertlik(mg CaCO3/L)")]
        public float? Mgsertlik { get; set; }
        [JsonPropertyName("tds")]
        [Display(Name = "TDS(ppm)")]
        public float? Tds { get; set; }
        [JsonPropertyName("iletkenlik")]
        [Display(Name = "İletkenlik(µS/cm)")]
        public float? Iletkenlik { get; set; }
        [JsonPropertyName("demir")]
        [Display(Name = "Demir(mg/L)")]
        public float? Demir { get; set; }
        [JsonPropertyName("mangan")]
        [Display(Name = "Mangan(mg/L)")]
        public float? Mangan { get; set; }
        [JsonPropertyName("nitrat")]
        [Display(Name = "Nitrat(mg NO3/L)")]
        public float? Nitrat { get; set; }
        [JsonPropertyName("nitrit")]
        [Display(Name = "Nitrit(mg NO2/L)")]
        public float? Nitrit { get; set; }
        [JsonPropertyName("amonyak")]
        [Display(Name = "Amonyak(mg NH3/L)")]
        public float? Amonyak { get; set; }
        [JsonPropertyName("permanganat")]
        [Display(Name = "Permanganat(mg KMnO4/L)")]

        public float? Permanganat { get; set; }
        [JsonPropertyName("oksijen")]
        [Display(Name = "Oksijen(mg/L)")]
        public float? Oksijen { get; set; }
        [JsonPropertyName("bakteriyolojik")]
        [Display(Name = "Bakteriyolojik")]
        public float? Bakteriyolojik { get; set; }
        [JsonPropertyName("aciklama")]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }
        [JsonPropertyName("created")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public DateTime Created { get; set; }
        [JsonPropertyName("updated")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public DateTime? Updated { get; set; }
        [JsonPropertyName("createdBy")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? CreatedBy { get; set; }
        [JsonPropertyName("lastModifiedBy")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? LastModifiedBy { get; set; }

        [NotMapped]
        [JsonPropertyName("analizNoktaName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string AnalizNoktaName => AnalizNokta.GetDisplayName();
    }
}
