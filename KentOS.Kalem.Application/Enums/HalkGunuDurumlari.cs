namespace KentOS.Kalem.Application.Enums;

/// <summary>
/// Halk gününün kendi durumu.
/// </summary>
/// <remarks>
/// Sayısal karşılıklar AÇIKÇA yazılı: iki istemci de bu değerleri JSON'da
/// sayı olarak taşıyor ve araya bir değer eklemek sahadaki uygulamalarda
/// durumları kaydırırdı.
/// </remarks>
public enum HalkGunuDurumu
{
    /// <summary>Liste hazırlanıyor; vatandaşa henüz haber verilmedi.</summary>
    Planlaniyor = 0,

    /// <summary>Liste kesinleşti, SMS gönderilebilir.</summary>
    Yayinda = 1,

    /// <summary>Gün bitti, görüşmeler kaydedildi.</summary>
    Tamamlandi = 2,

    Iptal = 3,
}

/// <summary>
/// Bekleyenler havuzundaki bir başvurunun durumu.
/// </summary>
/// <remarks>
/// Havuz, halk gününden BAĞIMSIZ yaşar: vatandaş bugün başvurur, üç hafta
/// sonraki güne atanır. Bu yüzden başvurunun kendi durumu var; atandığı
/// katılımın durumu ayrı (<see cref="KatilimDurumu"/>).
/// </remarks>
public enum BasvuruDurumu
{
    /// <summary>Havuzda, henüz bir güne atanmadı.</summary>
    Bekliyor = 0,

    /// <summary>Bir halk gününe atandı.</summary>
    Atandi = 1,

    /// <summary>Görüşme yapıldı.</summary>
    Gorusuldu = 2,

    /// <summary>Vatandaş vazgeçti ya da ulaşılamadı.</summary>
    Iptal = 3,

    /// <summary>
    /// Halk günü görüşmesi UYGUN GÖRÜLMEDİ.
    /// </summary>
    /// <remarks>
    /// <see cref="Iptal"/>'den ayrı: iptalde vazgeçen vatandaş, burada karar
    /// makamın. Ayrımı kaybetmek "kaç kişi geri çevrildi" sorusunu
    /// cevaplanamaz kılıyordu; üstelik gerekçe de yazılmalı.
    /// </remarks>
    Reddedildi = 4,
}

/// <summary>
/// Bir kişinin BELLİ bir halk günündeki durumu.
/// </summary>
/// <remarks>
/// <b>Geldi</b> ile <b>Görüşüldü</b> ayrı: salonda sırası gelen kişi
/// gelmiş olabilir ama görüşme henüz bitmemiştir. Tek alana sıkıştırmak,
/// "geldi ama sırada bekliyor" ile "görüşmesi bitti"yi ayırt edilemez
/// kılardı — salondaki operatörün ekranı tam olarak bu ayrım üzerine kurulu.
/// </remarks>
public enum KatilimDurumu
{
    Bekliyor = 0,
    Geldi = 1,
    Gelmedi = 2,
    Gorusuldu = 3,
    Iptal = 4,
}
