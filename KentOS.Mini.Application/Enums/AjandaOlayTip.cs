namespace KentOS.Mini.Application.Enums
{
    /// <summary>
    /// Bir etkinlik (Ajanda) üzerinde gerçekleşen işlem türü.
    /// Zaman çizelgesinde ikon/renk seçimi bu değere göre yapılır.
    /// YENİ DEĞER EKLERKEN SONA EKLEYİN — sayısal karşılıklar veritabanında saklanır.
    /// </summary>
    public enum AjandaOlayTip
    {
        Olusturuldu = 0,
        Guncellendi = 1,
        Silindi = 2,
        GeriAlindi = 3,
        NotEklendi = 4,
        FotografEklendi = 5,
        Ertelendi = 6,
        HavaleEdildi = 7,
        DurumDegisti = 8,
        TipDegisti = 9,
        StatuDegisti = 10,
        CicekGonderildi = 11,
        CicekIptalEdildi = 12,
        SmsGonderildi = 13,
        UstBirimeGonderildi = 14,

        /// <summary>Tekrar serisi oluşturuldu (ilk tekrarın zaman çizelgesine yazılır).</summary>
        SeriOlusturuldu = 15,

        /// <summary>Seri "bundan sonrakiler" veya "tümü" kapsamıyla güncellendi.</summary>
        SeriGuncellendi = 16,

        /// <summary>Seri "bundan sonrakiler" veya "tümü" kapsamıyla silindi.</summary>
        SeriSilindi = 17,

        /// <summary>Tek bir tekrar seriden ayrıldı (istisna oluştu).</summary>
        TekrarAyrildi = 18
    }
}
