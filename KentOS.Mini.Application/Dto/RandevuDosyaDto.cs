using KentOS.Mini.Application.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto
{
    public class RandevuDosyaDto
    {
        /// <summary>
        /// Kayıt kimliği.
        /// </summary>
        /// <remarks>
        /// EKLENDİ: silme ucu (<c>DELETE /api/v2/talep/dosya/{dosyaId}</c>)
        /// kimlik istiyor ama liste yanıtı taşımıyordu; arayüz bir dosyayı
        /// listeleyip silemiyordu. Alan EKLEMEK v1 sözleşmesini bozmaz —
        /// mobil istemci JSON'u anahtara göre okuyor, fazladan alanı yok
        /// sayıyor.
        /// </remarks>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [Display(Name = "Dosya Adı")]
        [Required(ErrorMessage = "Dosya adı boş bırakılamaz")]
        [JsonPropertyName("ad")]
        public string Ad { get; set; }
        [Display(Name = "Açıklama")]
        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }
        [Display(Name = "Dosya Yolu")]
        [JsonPropertyName("path")]
        public string? Path { get; set; }
        [Display(Name = "Dosya Türü")]
        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }
        [Display(Name = "Dosya Boyutu")]
        [JsonPropertyName("size")]
        public long? Size { get; set; }
        [Display(Name = "Oluşturulma Tarihi")]
        [JsonPropertyName("olusturmaTarih")]
        public DateTime? OlusturmaTarih { get; set; }
    }
}
