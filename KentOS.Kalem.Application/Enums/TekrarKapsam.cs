namespace KentOS.Kalem.Application.Enums
{
    /// <summary>
    /// Tekrarlanan bir etkinlik düzenlenirken/silinirken işlemin KAPSAMI.
    /// Standart takvim davranışı (Google/Outlook) ile aynı üç seçenek.
    ///
    /// VARSAYILAN <see cref="Yalnizca"/>'dır — kapsam göndermeyen eski istemciler
    /// bugünkü davranışı (tek kaydı düzenle/sil) aynen sürdürür.
    /// </summary>
    public enum TekrarKapsam
    {
        /// <summary>Yalnızca bu tekrar. Diğer tekrarlara dokunulmaz.</summary>
        Yalnizca = 0,

        /// <summary>Bu tekrar ve sonrakiler. Geçmiş tekrarlar korunur.</summary>
        BundanSonrakiler = 1,

        /// <summary>Serinin tümü. Bireysel düzenlenmiş (ayrık) tekrarlar atlanır.</summary>
        Tumu = 2
    }
}
