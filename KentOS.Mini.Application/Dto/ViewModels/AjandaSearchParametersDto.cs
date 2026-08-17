using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using KentOS.Mini.Application.Enums;

namespace KentOS.Mini.Application.Dto.ViewModels
{
    public class AjandaSearchParametersDto
    {
        [JsonPropertyName("startDate")]
        public DateTime? StartDate { get; set; }
        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }
        [JsonPropertyName("searchString")]
        public string? SearchString { get; set; }
        [JsonPropertyName("tipId")]
        public int? TipId { get; set; }

        /// Silinmiş (soft-delete) kayıtların dahil edilme biçimi.
        /// Varsayılan: yalnızca aktif (silinmemiş).
        [JsonPropertyName("silinmisFiltre")]
        public SilinmisFiltre SilinmisFiltre { get; set; } = SilinmisFiltre.Aktif;

        /// Gizli etkinliklere göre süzme. Varsayılan Tumu — kullanıcının GÖREBİLDİĞİ
        /// gizli etkinlikler de sonuçta yer alır (görünürlük kapısı ayrıca işler).
        [JsonPropertyName("gizliFiltre")]
        public OzellikFiltre GizliFiltre { get; set; } = OzellikFiltre.Tumu;

        /// Tekrarlanan etkinliklere göre süzme. Varsayılan Tumu.
        [JsonPropertyName("tekrarFiltre")]
        public OzellikFiltre TekrarFiltre { get; set; } = OzellikFiltre.Tumu;

    }
}
