namespace KentOS.Kalem.Application.Enums;

/// <summary>
/// Ek ve yorum eklenebilen iş takip varlıkları.
/// </summary>
/// <remarks>
/// <para>
/// <b>YENİ DEĞER EKLERKEN SONA EKLEYİN</b> — sayısal karşılıklar
/// veritabanında saklanıyor. Araya sokmak, var olan bütün eklerin ve
/// yorumların hangi kayda ait olduğunu sessizce değiştirirdi.
/// </para>
/// <para>
/// Liste <b>bilinçli olarak kapalı</b>. `is_ekleri` ve `is_yorumlari`
/// tabloları çok biçimli (polymorphic): yabancı anahtar yok, bağ
/// <c>(varlik_turu, varlik_id)</c> ikilisiyle kuruluyor. Serbest bir metin
/// ayrımı kullansaydık yazım hatası sessizce yetim kayıt üretirdi; enum
/// hem yazımı hem de kümeyi sabitliyor.
/// </para>
/// <para>
/// Kapsam <b>yalnızca iş takip modülü</b>. Etkinlik, talep ve özgeçmiş
/// kendi tablolarında kalıyor — onları buraya taşımak, iki yıldır çalışan
/// yollara dokunmak demekti.
/// </para>
/// </remarks>
public enum IsVarligi
{
    /// <summary>Görev.</summary>
    Gorev = 0,

    /// <summary>Görevin tek bir aşaması — sahada çekilen kanıt fotoğrafı buraya.</summary>
    GorevAsama = 1,

    /// <summary>Proje.</summary>
    Proje = 2,

    /// <summary>Projenin kilometre taşı.</summary>
    KilometreTasi = 3,

    /// <summary>Vatandaşın gönderdiği bildirim.</summary>
    VatandasBildirimi = 4,
}
