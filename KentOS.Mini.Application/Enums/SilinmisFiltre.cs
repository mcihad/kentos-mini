namespace KentOS.Mini.Application.Enums
{
    /// <summary>
    /// Arama sonuçlarında silinmiş (soft-delete) kayıtların nasıl ele alınacağı.
    /// </summary>
    public enum SilinmisFiltre
    {
        /// Yalnızca silinmemiş kayıtlar (varsayılan davranış).
        Aktif = 0,

        /// Yalnızca silinmiş kayıtlar.
        Silinmis = 1,

        /// Silinmiş + silinmemiş tüm kayıtlar.
        Tumu = 2
    }
}
