namespace KentOS.Mini.Application.Enums;

/// <summary>
/// Davet edilen kişinin CEVABI.
/// </summary>
/// <remarks>
/// Arama/mesaj eylemleriyle karıştırılmamalı: bir kişi aranmış ama cevabı
/// belli değilse <see cref="Beklemede"/> kalır. Eylem bilgisi
/// <c>DavetKisi.Arandi</c> ve <c>MesajGonderildi</c> alanlarında.
/// </remarks>
public enum DavetDurumu
{
    /// <summary>Henüz cevap alınmadı.</summary>
    Beklemede = 0,

    /// <summary>Görüşüldü, katılacak.</summary>
    Katilacak = 1,

    /// <summary>Görüşüldü, katılmayacak.</summary>
    Katilmayacak = 2,

    /// <summary>Arandı ama ulaşılamadı.</summary>
    Ulasilamadi = 3,
}

/// <summary>Davet listesi PDF çıktısının türü.</summary>
public enum DavetCiktiTuru
{
    /// <summary>Katılım durumları ve notlarla — takip çıktısı.</summary>
    Durumlu = 1,

    /// <summary>Telefon numaralarıyla — arama listesi.</summary>
    Telefonlu = 2,

    /// <summary>BOŞ katılım listesi — törende elle işaretlenir/imzalanır.</summary>
    BosKatilim = 3,

    /// <summary>BOŞ protokol listesi — yalnızca ad, unvan, kurum.</summary>
    BosProtokol = 4,
}
