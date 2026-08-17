namespace KentOS.Mini.Web.Options;

/// <summary>Veritabanı davranışı.</summary>
public sealed class DatabaseOptions
{
    /// <summary>Yapılandırma bölümü adı: <c>DATABASE__AUTOMIGRATE</c> → <c>Database:AutoMigrate</c>.</summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Açılışta bekleyen migration'lar otomatik uygulansın mı?
    ///
    /// <para>
    /// Varsayılan <c>true</c>: tek sunuculu kurulumlarda yayın adımını
    /// sadeleştiriyor. Çok örnekli (birden fazla sunucu aynı veritabanına)
    /// kurulumlarda <c>false</c> yapıp migration'ı yayın hattında tek seferde
    /// çalıştırın — aksi hâlde iki örnek aynı anda şema değiştirmeye kalkar.
    /// </para>
    /// </summary>
    public bool AutoMigrate { get; set; } = true;
}

/// <summary>
/// Talep modülüne ait kuruma göre değişen kimlikler.
/// </summary>
public sealed class RequestOptions
{
    /// <summary>Yapılandırma bölümü adı: <c>REQUESTS__PUBLICDAYTYPEID</c>.</summary>
    public const string SectionName = "Requests";

    /// <summary>
    /// "Halk günü" talep tipinin veritabanı kimliği.
    ///
    /// <para>
    /// Bu kimlik <c>randevu_tipleri</c> tablosunun tohum verisinden geliyor ve
    /// kuruma göre değişebilir. Kodda sabit <c>1</c> olarak duruyordu; başka
    /// bir kurumda o kimlik başka bir tipe denk gelir ve harita ekranı sessizce
    /// yanlış veri gösterirdi.
    /// </para>
    /// <para>
    /// ESKİ ANAHTAR: <c>Randevu:HalkGunuTipId</c> — hâlâ geri düşüş olarak
    /// okunuyor.
    /// </para>
    /// <para>
    /// <b>Varsayılan 0 (tanımsız) BİLİNÇLİ:</b> ayar verilmediğinde servis tipi
    /// ADA göre arar ("halk gün..."), bulamazsa 1'e düşer. Buraya 1 yazmak o
    /// zinciri kısa devre yapar ve ada göre arama hiç çalışmazdı.
    /// </para>
    /// </summary>
    public long PublicDayTypeId { get; set; }
}
