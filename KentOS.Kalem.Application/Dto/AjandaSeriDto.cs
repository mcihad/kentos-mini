using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using KentOS.Kalem.Application.Enums;

namespace KentOS.Kalem.Application.Dto
{
    /// <summary>
    /// Tekrar kuralı OLUŞTURMA/GÜNCELLEME girdisi.
    ///
    /// İstemci ham RRULE üretir (RFC 5545), sunucu genişletmeyi yapar. Böylece
    /// mobil ve web aynı kuralı aynı biçimde ifade eder ve sunucu tek doğruluk
    /// kaynağı olur.
    /// </summary>
    public class AjandaSeriOlusturDto
    {
        /// <summary>
        /// Tekrar kuralı, "RRULE:" öneki OLMADAN.
        /// Örnekler: <c>FREQ=DAILY</c>, <c>FREQ=WEEKLY;BYDAY=MO,WE;INTERVAL=2</c>,
        /// <c>FREQ=MONTHLY;BYMONTHDAY=15;COUNT=10</c>, <c>FREQ=YEARLY;UNTIL=20271231T000000</c>.
        /// </summary>
        [JsonPropertyName("rrule")]
        [Required(ErrorMessage = "Tekrar kuralı zorunludur.")]
        [MaxLength(500, ErrorMessage = "Tekrar kuralı en fazla 500 karakter olabilir.")]
        public string Rrule { get; set; } = string.Empty;

        /// <summary>
        /// Tekrar süresi (dakika). Gönderilmezse etkinliğin başlangıç/bitiş
        /// farkından hesaplanır, o da yoksa 30 dakika varsayılır.
        /// </summary>
        [JsonPropertyName("sureDakika")]
        [Range(1, 60 * 24 * 30, ErrorMessage = "Süre 1 dakika ile 30 gün arasında olmalıdır.")]
        public int? SureDakika { get; set; }
    }

    /// <summary>
    /// Tekrar serisinin OKUMA gösterimi — kural, sınırlar ve üretilen tekrar sayısı.
    /// </summary>
    public class AjandaSeriDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("rrule")]
        public string Rrule { get; set; } = string.Empty;

        [JsonPropertyName("ozet")]
        public string? Ozet { get; set; }

        [JsonPropertyName("dtstart")]
        public DateTime Dtstart { get; set; }

        [JsonPropertyName("sureDakika")]
        public int SureDakika { get; set; }

        [JsonPropertyName("bitisTarihi")]
        public DateTime? BitisTarihi { get; set; }

        [JsonPropertyName("tekrarSayisi")]
        public int? TekrarSayisi { get; set; }

        /// <summary>Bu tarihe kadar olan tekrarlar üretildi (ufuk imi).</summary>
        [JsonPropertyName("uretilenSonTarih")]
        public DateTime? UretilenSonTarih { get; set; }

        [JsonPropertyName("iptal")]
        public bool Iptal { get; set; }

        /// <summary>Veritabanında hâlihazırda bulunan (silinmemiş) tekrar sayısı.</summary>
        [JsonPropertyName("uretilenAdet")]
        public int UretilenAdet { get; set; }

        /// <summary>Serideki ilk tekrarın etkinlik kimliği.</summary>
        [JsonPropertyName("ilkAjandaId")]
        public long? IlkAjandaId { get; set; }
    }

    /// <summary>
    /// Tekrarlanan etkinlik SİLME girdisi — hangi kapsamda silinecek.
    /// Kapsam gönderilmezse <see cref="TekrarKapsam.Yalnizca"/> uygulanır ve
    /// davranış bugünküyle (tek kaydı sil) aynı olur.
    /// </summary>
    public class AjandaSilDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("kapsam")]
        public TekrarKapsam Kapsam { get; set; } = TekrarKapsam.Yalnizca;
    }
}
