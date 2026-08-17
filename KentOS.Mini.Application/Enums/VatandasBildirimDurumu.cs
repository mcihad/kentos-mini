namespace KentOS.Mini.Application.Enums;

/// <summary>
/// VATANDAŞ BİLDİRİMİ DURUMU.
/// </summary>
/// <remarks>
/// <para>
/// Görev durumundan AYRI ve çok daha kısa. Bildirimin kendi hayatı yalnızca
/// "geldi → yönlendirildi ya da reddedildi" kadar; yönlendirildikten sonra
/// işin takibi <b>görevde</b> sürüyor. İki yerde birden durum tutsaydık
/// hangisinin bağlayıcı olduğu belirsizleşirdi.
/// </para>
/// <para>
/// Yeni değerler SONA eklenir.
/// </para>
/// </remarks>
public enum VatandasBildirimDurumu
{
    /// <summary>Karşılama ekranında bekliyor.</summary>
    Yeni = 0,

    /// <summary>Bir birime yönlendirildi ve görev açıldı.</summary>
    Yonlendirildi = 1,

    /// <summary>İşleme alınmadı — konusuz, mükerrer ya da kurumun görevi değil.</summary>
    Reddedildi = 2,
}
