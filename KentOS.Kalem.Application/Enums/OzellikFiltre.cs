namespace KentOS.Kalem.Application.Enums
{
    /// <summary>
    /// Arama sonuçlarında bir ÖZELLİĞE göre süzme (gizli etkinlik, tekrarlanan
    /// etkinlik gibi). <see cref="SilinmisFiltre"/> ile aynı üçlü mantık.
    ///
    /// Varsayılan <see cref="Tumu"/>'dür: filtre göndermeyen istemcilerde sonuç
    /// kümesi bugünküyle aynı kalır.
    /// </summary>
    public enum OzellikFiltre
    {
        /// Özelliğe bakılmaz (varsayılan).
        Tumu = 0,

        /// Yalnızca özelliği TAŞIYAN kayıtlar.
        Sadece = 1,

        /// Özelliği taşıyan kayıtlar HARİÇ.
        Haric = 2
    }
}
