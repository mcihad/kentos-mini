namespace KentOS.Kalem.Application.Enums;

/// <summary>
/// PROJE DURUMU — kodda sabit.
/// </summary>
/// <remarks>
/// <para>
/// Görev durumlarıyla aynı gerekçe: yönetici bir tablo satırı ekleyerek yeni
/// durum uyduramamalı. Ama liste GÖREVDEN DAHA KISA ve bu bilinçli — proje
/// bir iş değil, işlerin çatısı. Onay kapısı, iade, ret gibi kavramlar
/// projenin değil görevin akışına ait; projede bunlar olsaydı iki ayrı onay
/// mekanizması doğar ve hangisinin bağlayıcı olduğu belirsizleşirdi.
/// </para>
/// <para>
/// Yeni değerler SONA eklenir.
/// </para>
/// </remarks>
public enum ProjeDurumu
{
    /// <summary>Henüz başlamamış — planlama aşamasında.</summary>
    Planlaniyor = 0,

    /// <summary>Yürüyor.</summary>
    Devam = 1,

    /// <summary>Geçici olarak durduruldu; kaldığı yerden sürebilir.</summary>
    Durduruldu = 2,

    /// <summary>Bitti. SON DURAK.</summary>
    Tamamlandi = 3,

    /// <summary>Vazgeçildi. SON DURAK.</summary>
    Iptal = 4,
}

/// <summary>
/// PROJE ÜYE ROLÜ.
/// </summary>
/// <remarks>
/// <c>GorevAtamaRolu</c>'ndan AYRI bir liste. Benziyorlar ama farklı şeyi
/// anlatıyorlar: görev ataması "bu işi kim yapacak", proje üyeliği "bu işin
/// çatısında kim var". Tek enum'a indirseydik, projeye "sorumlu" eklemek bir
/// göreve atamak gibi okunurdu.
/// </remarks>
public enum ProjeUyeRolu
{
    /// <summary>Projeyi yürüten. Bildirimler öncelikle buna gider.</summary>
    Yonetici = 0,

    /// <summary>Çalışan üye.</summary>
    Uye = 1,

    /// <summary>Yalnızca görür; iş almaz.</summary>
    Izleyici = 2,
}
