using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto
{
    /// <summary>
    /// Birimlere SMS gönderiminin SONUCU.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uç eskiden yalnızca <c>true</c> dönüyordu ve arayüz "SMS gönderildi"
    /// yazıyordu — hiç mesaj yazılmamış olsa bile. Telefon numarası olmayan
    /// kullanıcı sessizce atlanıyor, kullanıcıysa "gönderdim ama gelmedi"
    /// diyordu; sebebi görmenin tek yolu veritabanına bakmaktı.
    /// </para>
    /// <para>
    /// Artık sonuç sayılarla dönüyor: kaç kişiye yazıldı, kimin telefonu
    /// eksik, hangi birimde hiç kullanıcı yok. Üçü de düzeltilebilir
    /// şeyler — görünmedikleri sürece düzeltilmiyorlardı.
    /// </para>
    /// </remarks>
    public class SmsGonderimSonucuDto
    {
        /// <summary>Kuyruğa yazılan mesaj sayısı.</summary>
        [JsonPropertyName("gonderilen")] public int Gonderilen { get; set; }

        /// <summary>Telefon numarası olmadığı için atlanan kişiler.</summary>
        [JsonPropertyName("telefonsuzKisiler")]
        public List<string> TelefonsuzKisiler { get; set; } = new();

        /// <summary>Hiç kullanıcısı olmayan birimler.</summary>
        [JsonPropertyName("bosBirimler")]
        public List<string> BosBirimler { get; set; } = new();

        /// <summary>Gönderilen birim adları — özet metninde kullanılır.</summary>
        [JsonPropertyName("birimler")]
        public List<string> Birimler { get; set; } = new();

        /// <summary>Tek satırlık özet; istemciler bunu olduğu gibi gösterebilir.</summary>
        [JsonPropertyName("ozet")]
        public string Ozet =>
            Gonderilen == 0
                ? "Hiç kimseye SMS gönderilemedi."
                : $"{Gonderilen} kişiye SMS kuyruğa alındı." +
                  (TelefonsuzKisiler.Count > 0
                      ? $" {TelefonsuzKisiler.Count} kişide telefon numarası yok."
                      : string.Empty);
    }
}
