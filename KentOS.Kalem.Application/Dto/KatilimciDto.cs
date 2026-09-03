using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto
{
    /// <summary>
    /// Katılımcı seçimi ve gösterimi için SADE kullanıcı bilgisi.
    ///
    /// <see cref="UserDto"/> bilinçli olarak kullanılmadı: o DTO FcmToken, e-posta
    /// ve telefon taşıyor; katılımcı listesi birimdeki herkese görünür olduğu için
    /// oraya iletişim/cihaz bilgisi sızmamalı.
    /// </summary>
    public class KatilimciDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("ad")]
        public string? Ad { get; set; }

        [JsonPropertyName("soyad")]
        public string? Soyad { get; set; }

        [JsonPropertyName("unvan")]
        public string? Unvan { get; set; }

        [JsonPropertyName("birimAd")]
        public string? BirimAd { get; set; }

        /// <summary>Katılımcı bir BİRİM ise birimin kimliği.</summary>
        /// <remarks>
        /// <c>null</c> ise kayıt, katılımcıların kişi olduğu döneme ait eski
        /// bir satırdır ve <see cref="Id"/> kullanıcı kimliğidir.
        /// </remarks>
        [JsonPropertyName("birimId")]
        public long? BirimId { get; set; }

        /// <summary>Katılımcı birim mi (yeni), kişi mi (eski)?</summary>
        [JsonPropertyName("birimMi")]
        public bool BirimMi => BirimId != null;

        /// <summary>
        /// Listelerde gösterilecek ad.
        ///
        /// Birim katılımcıda birimin adı ve yetkilisi, kişi katılımcıda kişinin
        /// adı. Arayüzün iki durumu ayrı ayrı biçimlendirmesi gerekmiyor.
        /// </summary>
        [JsonPropertyName("tamAd")]
        public string TamAd => BirimMi
            ? (BirimAd ?? string.Empty)
            : $"{Ad} {Soyad}".Trim();
    }
}
