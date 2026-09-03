using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Dto
{
    public class UserDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("ad")]
        public string Ad { get; set; }
        [JsonPropertyName("soyad")]
        public string Soyad { get; set; }
        [JsonPropertyName("unvan")]
        public string? Unvan { get; set; }
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        [JsonPropertyName("telefon")]
        public string? Telefon { get; set; }
        [JsonPropertyName("birimId")]
        public long BirimId { get; set; }
        [JsonPropertyName("ustBirimId")]
        public long? UstBirimId { get; set; }
        [JsonPropertyName("birimAd")]
        public string? BirimAd { get; set; }
        [JsonPropertyName("fcmToken")]
        public string? FcmToken { get; set; }
        [JsonPropertyName("roles")]
        public string[] Roles { get; set; }

        /// <summary>
        /// Gizli etkinlik OLUŞTURABİLİR mi.
        /// </summary>
        /// <remarks>
        /// Görünürlükle ilgisi yok. Mobil, gizlilik anahtarını bu bayrağa göre
        /// gösterir; yoksa kullanıcı işaretleyip kaydediyor ve sunucudan
        /// "yetkiniz yok" hatası alıyordu. Alan EKLEMEK v1 sözleşmesini
        /// bozmaz — eski sürümler fazladan alanı yok sayar.
        /// </remarks>
        [JsonPropertyName("gizliEtkinlikEkleyebilir")]
        public bool GizliEtkinlikEkleyebilir { get; set; }

        /// <summary>Dosya gönderimi başlatabilir mi (almak yetki istemez).</summary>
        [JsonPropertyName("dosyaGonderebilir")]
        public bool DosyaGonderebilir { get; set; }
    }
}
