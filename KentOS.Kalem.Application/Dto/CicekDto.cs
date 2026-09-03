using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Dto
{
    /// <summary>Çiçek siparişi taşıma nesnesi — mobil `CicekModel` ile birebir.</summary>
    /// <remarks>
    /// Eskiden `Ajanda` (entity) alanı vardı; Ajanda ⇄ Cicek döngüsü Mapster'da
    /// sonsuz özyinelemeye yol açıyordu (bkz. MapsterConfig). Alan kaldırıldı;
    /// ilişkiyi `AjandaId` taşıyor.
    /// </remarks>
    public class CicekDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("guid")]
        public string? Guid { get; set; }

        [JsonPropertyName("ajandaId")]
        public long? AjandaId { get; set; }

        [JsonPropertyName("ad")]
        public string? Ad { get; set; }

        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }

        [JsonPropertyName("adres")]
        public string? Adres { get; set; }

        [JsonPropertyName("resim")]
        public string? Resim { get; set; }

        [JsonPropertyName("dogrulamaKodu")]
        public int DogrulamaKodu { get; set; } = 0;

        [JsonPropertyName("olusturan")]
        public string? Olusturan { get; set; }

        [JsonPropertyName("gonderildi")]
        public bool Gonderildi { get; set; }

        [JsonPropertyName("gonderilmeTarihi")]
        public DateTime GonderilmeTarihi { get; set; }

        [JsonPropertyName("olusturulmaTarihi")]
        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;

        [JsonPropertyName("guncellemeTarihi")]
        public DateTime? GuncellemeTarihi { get; set; }
        [JsonPropertyName("cicekciId")]
        public long CicekciId { get; set; }
    }
}
