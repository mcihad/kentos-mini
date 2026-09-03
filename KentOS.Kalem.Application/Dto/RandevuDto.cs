using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Dto
{
    /// <summary>
    /// Talep (randevu) taşıma nesnesi — mobil `RandevuModel` ile birebir.
    /// </summary>
    /// <remarks>
    /// DOĞRULAMA: Uzunluk sınırları veritabanı kolonlarıyla aynıdır
    /// (Konu/Ad/Soyad/Meslek/Yer/Koordinat → varchar(100), Telefon → varchar(50),
    /// Email → varchar(100), Adres/OzgecmisDosya → varchar(255)). Aksi halde
    /// uzun metin `DbUpdateException` ile HTTP 500 üretiyordu; artık 400 döner
    /// ve mobil form aynı sınırı uygular.
    /// </remarks>
    public class RandevuDto
    {
        [Display(Name = "Id")]
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [Display(Name = "Konu")]
        [Required(ErrorMessage = "Konu alanı boş bırakılamaz.")]
        [MaxLength(100, ErrorMessage = "Konu en fazla 100 karakter olabilir.")]
        [JsonPropertyName("konu")]
        public string Konu { get; set; }

        [Display(Name = "Ad")]
        [Required(ErrorMessage = "Ad alanı boş bırakılamaz.")]
        [MaxLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
        [JsonPropertyName("ad")]
        public string Ad { get; set; }

        [Display(Name = "Soyad")]
        [Required(ErrorMessage = "Soyad alanı boş bırakılamaz.")]
        [MaxLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir.")]
        [JsonPropertyName("soyad")]
        public string? Soyad { get; set; }

        [Display(Name = "Meslek")]
        [MaxLength(100, ErrorMessage = "Meslek en fazla 100 karakter olabilir.")]
        [JsonPropertyName("meslek")]
        public string? Meslek { get; set; }

        [Display(Name = "Telefon")]
        [MaxLength(50, ErrorMessage = "Telefon en fazla 50 karakter olabilir.")]
        [JsonPropertyName("telefon")]
        public string? Telefon { get; set; }

        [Display(Name = "Email")]
        [MaxLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [Display(Name = "Adres")]
        [MaxLength(255, ErrorMessage = "Adres en fazla 255 karakter olabilir.")]
        [JsonPropertyName("adres")]
        public string? Adres { get; set; }


        [Display(Name = "Randevu Yeri")]
        [MaxLength(100, ErrorMessage = "Randevu yeri en fazla 100 karakter olabilir.")]
        [JsonPropertyName("yer")]
        public string? Yer { get; set; }


        [Display(Name = "Harita")]
        [MaxLength(100, ErrorMessage = "Koordinat en fazla 100 karakter olabilir.")]
        [JsonPropertyName("koordinat")]
        public string? Koordinat { get; set; }

        [Display(Name = "Başlangıç Tarihi")]
        [Required(ErrorMessage = "Başlangıç tarihi boş bırakılamaz.")]
        [JsonPropertyName("baslangicTarih")]
        public DateTime? BaslangicTarih { get; set; }

        [Display(Name = "Bitiş Tarihi")]
        [Required(ErrorMessage = "Bitiş tarihi boş bırakılamaz.")]
        [JsonPropertyName("bitisTarih")]
        public DateTime? BitisTarih { get; set; }


        [Display(Name = "Açıklama")]
        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }

        [Display(Name = "Özgeçmiş?")]
        [JsonPropertyName("ozgecmisDurum")]
        public bool OzgecmisDurum { get; set; } = false;

        [Display(Name = "Özgeçmiş Dosya")]
        [MaxLength(255, ErrorMessage = "Dosya adı en fazla 255 karakter olabilir.")]
        [JsonPropertyName("ozgecmisDosya")]
        public string? OzgecmisDosya { get; set; }

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

        [Display(Name = "Tamamlanma Tarihi")]
        [JsonPropertyName("tamamlanmaTarih")]
        public DateTime? TamamlanmaTarih { get; set; }

        [Display(Name = "Ajanda Durumu")]
        [JsonPropertyName("ajandaDurum")]
        public bool AjandaDurum { get; set; } = false;

        [Display(Name = "Ajanda Id")]
        [JsonPropertyName("ajandaId")]
        public long AjandaId { get; set; } = 0;
        [JsonPropertyName("arsivlendi")]
        public bool Arsivlendi { get; set; } = false;

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
