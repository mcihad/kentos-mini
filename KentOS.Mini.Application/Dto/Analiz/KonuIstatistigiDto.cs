using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.Analiz
{
    /// <summary>
    /// Bir konunun istatistik panosu — GENEL ŞEKİL.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Halk günü, form, protokol, çiçek, özgeçmiş ve sistem panoları aynı
    /// şekli döndürüyor; böylece istemci tarafında <b>tek bir çizici</b>
    /// var. Her konuya özel DTO yazılsaydı yanına da neredeyse birebir aynı
    /// altı React ekranı gerekirdi ve zamanla ayrışırlardı — bu depoda
    /// aynı hata etiket çevirisinde üç kopya olarak yaşandı.
    /// </para>
    /// <para>
    /// <b>Etkinlik ve talep panoları bu şekle TAŞINMADI.</b> İkisi çok daha
    /// zengin (ortalama süre, tamamlanma oranı seyri, katılımcı kırılımı) ve
    /// çalışan iki ekranı yeniden yazmanın karşılığı yok. Genel şekil, yeni
    /// konular için.
    /// </para>
    /// </remarks>
    public class KonuIstatistigiDto
    {
        /// <summary>Konunun makine adı — istemcideki rota parçasıyla aynı.</summary>
        [JsonPropertyName("konu")]
        public string Konu { get; set; } = string.Empty;

        [JsonPropertyName("baslik")]
        public string Baslik { get; set; } = string.Empty;

        /// <summary>Üstteki sayı karoları.</summary>
        [JsonPropertyName("karolar")]
        public List<IstatistikKarosuDto> Karolar { get; set; } = [];

        /// <summary>Dağılım bölümleri — her biri bir grafik.</summary>
        [JsonPropertyName("bolumler")]
        public List<IstatistikBolumuDto> Bolumler { get; set; } = [];

        /// <summary>Aylık seyir; boşsa çizilmez.</summary>
        [JsonPropertyName("seyir")]
        public List<IstatistikSeriNoktasiDto> Seyir { get; set; } = [];

        /// <summary>Seyir grafiğinin ne saydığını söyleyen etiket.</summary>
        [JsonPropertyName("seyirEtiketi")]
        public string? SeyirEtiketi { get; set; }

        /// <summary>
        /// Panoda gösterilecek uyarı — veri yoksa ya da kapsam daraltıldıysa.
        /// </summary>
        /// <remarks>
        /// Boş bir pano "sistem bozuk" gibi okunuyor. Sebebini yazmak,
        /// kullanıcıyı destek hattına gitmekten kurtarıyor.
        /// </remarks>
        [JsonPropertyName("not")]
        public string? Not { get; set; }
    }

    /// <summary>
    /// Tek sayı karosu.
    /// </summary>
    /// <remarks>
    /// <b>Değer METİN.</b> Sayı olarak dönseydi "%73", "4,2 gün" ve "1.240"
    /// biçimlerinin her biri için istemciye ayrı bir biçimlendirme kuralı
    /// göndermek gerekirdi; kural sunucuda kalsın diye metin dönüyor.
    /// Sıralama ya da grafik gerekmiyor — karo yalnızca okunuyor.
    /// </remarks>
    public class IstatistikKarosuDto
    {
        [JsonPropertyName("etiket")]
        public string Etiket { get; set; } = string.Empty;

        [JsonPropertyName("deger")]
        public string Deger { get; set; } = string.Empty;

        [JsonPropertyName("altMetin")]
        public string? AltMetin { get; set; }

        /// <summary>
        /// Karonun tonu: <c>iyi</c> · <c>uyari</c> · <c>kotu</c> · boş.
        /// </summary>
        /// <remarks>
        /// Renk KODU değil ADI dönüyor: renk kurum kaydından geliyor ve
        /// sunucudan <c>#RRGGBB</c> göndermek beyaz etiket sözleşmesini
        /// bozardı (bkz. KURUM BİLGİSİ KODA YAZILMAZ).
        /// </remarks>
        [JsonPropertyName("ton")]
        public string? Ton { get; set; }
    }

    /// <summary>Bir dağılım bölümü — başlık + dilimler.</summary>
    public class IstatistikBolumuDto
    {
        [JsonPropertyName("baslik")]
        public string Baslik { get; set; } = string.Empty;

        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }

        /// <summary><c>cubuk</c> (varsayılan) ya da <c>halka</c>.</summary>
        [JsonPropertyName("gorunum")]
        public string Gorunum { get; set; } = "cubuk";

        [JsonPropertyName("dilimler")]
        public List<IstatistikDilimDto> Dilimler { get; set; } = [];
    }
}
