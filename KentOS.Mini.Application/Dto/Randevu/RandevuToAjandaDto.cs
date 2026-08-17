using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Dto.Randevu
{
    public class RandevuToAjandaDto
    {
        [JsonPropertyName("randevuId")]
        public long RandevuId { get; set; }
        [JsonPropertyName("baslangicTarih")]
        [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
        public DateTime BaslangicTarih { get; set; }
        [JsonPropertyName("resimEklensin")]
        public bool ResimEklensin { get; set; }
        [JsonPropertyName("bilgiNotuEklensin")]
        public bool BilgiNotuEklensin { get; set; }
        [JsonPropertyName("konusmaMetniEklensin")]
        public bool KonusmaMetniEklensin { get; set; }
        [JsonPropertyName("basinKatilsin")]
        public bool BasinKatilsin { get; set; }
        [JsonPropertyName("ajandaDurumId")]
        [Required(ErrorMessage = "Etkinlik durumu zorunludur.")]
        public long AjandaDurumId { get; set; }
    }
}
