using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto
{
    public enum NotificationEntity
    {
        Ajanda,
        Talep,
        Oneri,

        /// <summary>
        /// Kullanıcıdan kullanıcıya dosya gönderimi (yalnızca web).
        /// </summary>
        /// <remarks>
        /// <para>
        /// SONA eklendi: enum JSON'a AD olarak yazılıyor
        /// (<c>JsonStringEnumConverter</c>), dolayısıyla sıra sözleşmeyi
        /// etkilemez. Yine de araya sokmak, sayısal değere bakan olası bir
        /// istemcide kaymaya yol açardı.
        /// </para>
        /// <para>
        /// <b>Yayındaki mobil sürümler bu değeri TANIMIYOR</b> ve
        /// <c>NotificationEntity.fromString</c> içindeki <c>orElse</c>
        /// yüzünden sessizce <c>talep</c>'e düşüyorlar. Bu yüzden dosya
        /// gönderimi bildirimleri <c>NotificationAction.None</c> ile
        /// gönderilir: eski istemci hiçbir yere gitmez, web istemcisi
        /// varlık adına bakarak doğru yere yönlendirir.
        /// </para>
        /// </remarks>
        Dosya,

        /// <summary>
        /// Özgeçmiş havuzundan yönlendirilen CV.
        /// </summary>
        /// <remarks>
        /// <c>Dosya</c> ile aynı gerekçe: sondadır ve bildirim
        /// <c>NotificationAction.None</c> ile gider — bu değeri tanımayan eski
        /// mobil sürümler varlığı sessizce <c>talep</c>'e düşürüyor ve var
        /// olmayan bir talebin detayını açmaya çalışırdı.
        /// </remarks>
        Ozgecmis,

        /// <summary>
        /// İş takip görevi — atama, aşama, onay ve süre aşımı bildirimleri.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>Dosya</c> ve <c>Ozgecmis</c> ile aynı gerekçe: SONA eklendi ve
        /// bildirim <c>NotificationAction.None</c> ile gider. Bu değeri
        /// tanımayan eski mobil sürümler varlığı sessizce <c>talep</c>'e
        /// düşürüyor ve var olmayan bir talebin detayını açardı.
        /// </para>
        /// <para>
        /// İş takip modülü zaten <b>yalnızca web</b> — mobil uygulamada görev
        /// ekranı yok. Bildirimin mobilde bir yere gitmemesi doğru davranış;
        /// yanlış bir yere gitmesi değil.
        /// </para>
        /// </remarks>
        Gorev,

        /// <summary>
        /// Birimler arası devir — gelen kutusuna düşen kayıt.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Ayrı bir varlık, çünkü devir kaydının <b>kendi detay sayfası
        /// yok</b>: karar gelen kutusu listesinin içinde veriliyor. Önceden
        /// bu bildirimler <c>Gorev</c> varlığıyla ve kimlik olarak
        /// <c>0</c> ile gönderiliyordu; tıklayan kullanıcı hiçbir yere
        /// gitmiyordu, çünkü gidilecek bir görev kimliği yoktu — devri
        /// kabul etmeden görev zaten oluşmuyor.
        /// </para>
        /// <para>
        /// <c>Dosya</c>, <c>Ozgecmis</c> ve <c>Gorev</c> ile aynı gerekçeyle
        /// SONA eklendi ve bildirim <c>NotificationAction.None</c> ile
        /// gidiyor: bu değeri tanımayan eski mobil sürümler varlığı sessizce
        /// <c>talep</c>'e düşürüp var olmayan bir talebi açardı.
        /// </para>
        /// </remarks>
        GelenKutusu,

        /// <summary>
        /// Proje — ekibe alınma, yöneticiliğe atanma, kilometre taşı.
        /// </summary>
        /// <remarks>
        /// <c>Gorev</c> ile aynı gerekçe: sondadır ve
        /// <c>NotificationAction.None</c> ile gider. Proje modülü de
        /// yalnızca web.
        /// </remarks>
        Proje
    }

    public enum NotificationAction
    {
        OpenImages,
        OpenNotes,
        OpenDetails,
        /// <summary>
        /// Tıklanınca detay ekranı AÇILMAZ (yalnızca uygulama öne gelir).
        /// Silme bildirimleri için kullanılır — silinen kayda gitmek anlamsız.
        /// </summary>
        None
    }

    public class TokenDataDto
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("entity")]
        public NotificationEntity Entity { get; set; }
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("action")]
        public NotificationAction Action { get; set; }

        public TokenDataDto(NotificationEntity entity, int id, NotificationAction action)
        {
            Entity = entity;
            Id = id;
            Action = action;
        }

        public static TokenDataDto FromJson(string json)
        {
            return JsonSerializer.Deserialize<TokenDataDto>(json);
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public override string ToString()
        {
            return $"TokenDataDto(entity: {Entity}, id: {Id}, action: {Action})";
        }
    }
}

