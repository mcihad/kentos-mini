using KentOS.Mini.Application.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.Randevu
{
    public class AjandaHareketDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("kullaniciId")]
        public long KullaniciId { get; set; }
        [Column(TypeName = "varchar(100)")]
        [Display(Name = "Kullanıcı")]
        [JsonPropertyName("kullanici")]
        public string Kullanici { get; set; }
        [JsonPropertyName("eskiBirimId")]
        public long EskiBirimId { get; set; }
        [Column(TypeName = "varchar(100)")]
        [Display(Name = "Gelen Birim")]
        [JsonPropertyName("eskiBirim")]
        public string EskiBirim { get; set; }
        [JsonPropertyName("yeniBirimId")]
        public long YeniBirimId { get; set; }
        [Column(TypeName = "varchar(100)")]
        [Display(Name = "Giden Birim")]
        [JsonPropertyName("yeniBirim")]
        public string YeniBirim { get; set; }
        [Display(Name = "Aşağı Hareket")]
        [JsonPropertyName("asagiHareket")]
        public bool AsagiHareket { get; set; } = true;

        [Column(TypeName = "varchar(100)")]
        [Display(Name = "İşlem Tarihi")]
        [JsonPropertyName("tarih")]

        public DateTime Tarih { get; set; }
        [Display(Name = "Ajanda")]
        [JsonPropertyName("ajandaId")]
        public long AjandaId { get; set; }
        [Display(Name = "Ajanda")]
        [JsonPropertyName("ajanda")]
        public Ajanda Ajanda { get; set; }
    }
}
