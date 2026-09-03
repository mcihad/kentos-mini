using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto.Randevu
{
    public class RandevuListDto
    {
        [Display(Name = "Id")]
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [Display(Name = "Konu")]
        [Required(ErrorMessage = "Konu alanı boş bırakılamaz.")]
        [JsonPropertyName("konu")]
        public string Konu { get; set; }

        [Display(Name = "Ad")]
        [Required(ErrorMessage = "Ad alanı boş bırakılamaz.")]
        [JsonPropertyName("ad")]
        public string Ad { get; set; }

        [Display(Name = "Soyad")]
        [Required(ErrorMessage = "Soyad alanı boş bırakılamaz.")]
        [JsonPropertyName("soyad")]
        public string? Soyad { get; set; }

        [Display(Name = "Meslek")]
        [JsonPropertyName("meslek")]
        public string? Meslek { get; set; }

        [Display(Name = "Başlangıç Tarihi")]
        [Required(ErrorMessage = "Başlangıç tarihi boş bırakılamaz.")]
        [JsonPropertyName("baslangicTarih")]
        public DateTime? BaslangicTarih { get; set; }

        [Display(Name = "Bitiş Tarihi")]
        [Required(ErrorMessage = "Bitiş tarihi boş bırakılamaz.")]
        [JsonPropertyName("bitisTarih")]
        public DateTime? BitisTarih { get; set; }

        [Display(Name = "Özgeçmiş?")]
        [JsonPropertyName("ozgecmisDurum")]
        public bool OzgecmisDurum { get; set; } = false;

        [Display(Name = "Birim")]
        [JsonPropertyName("birimId")]
        public long? BirimId { get; set; }

        [Display(Name = "Randevu Tipi")]
        [JsonPropertyName("randevuTipId")]
        public long? RandevuTipId { get; set; }

        [Display(Name = "Mahalle")]
        [Required(ErrorMessage = "Mahalle boş bırakılamaz.")]
        [JsonPropertyName("mahalleId")]
        public long? MahalleId { get; set; }

        [Display(Name = "Randevu Durumu")]
        [Required(ErrorMessage = "Randevu durumu boş bırakılamaz.")]
        [JsonPropertyName("randevuDurumId")]
        public long? RandevuDurumId { get; set; }

        [Display(Name = "Ajanda Durumu")]
        [JsonPropertyName("ajandaDurum")]
        public bool AjandaDurum { get; set; } = false;


        //AdSoyad
        [NotMapped]
        [Display(Name = "Ad Soyad")]
        [JsonPropertyName("adSoyad")]
        public string AdSoyad
        {
            get
            {
                return Ad + " " + Soyad;
            }
        }
    }
}
