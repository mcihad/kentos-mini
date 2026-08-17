using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using KentOS.Mini.Application.Enums;

namespace KentOS.Mini.Application.Dto
{
    /// <summary>Tek bir alan değişikliği — eski ve yeni değeriyle.</summary>
    public class AjandaAlanDegisikligiDto
    {
        [JsonPropertyName("alan")]
        public string Alan { get; set; } = string.Empty;

        [JsonPropertyName("eski")]
        public string Eski { get; set; } = string.Empty;

        [JsonPropertyName("yeni")]
        public string Yeni { get; set; } = string.Empty;

        public AjandaAlanDegisikligiDto() { }

        public AjandaAlanDegisikligiDto(string alan, string eski, string yeni)
        {
            Alan = alan;
            Eski = eski;
            Yeni = yeni;
        }
    }

    /// <summary>Zaman çizelgesinde gösterilen tek olay.</summary>
    public class AjandaOlayDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("ajandaId")]
        public long AjandaId { get; set; }

        /// <summary>Sayısal değil, metin olarak gider ("Guncellendi").</summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("tip")]
        public AjandaOlayTip Tip { get; set; }

        [JsonPropertyName("kullanici")]
        public string Kullanici { get; set; } = string.Empty;
        [JsonPropertyName("tarih")]
        public DateTime Tarih { get; set; }
        [JsonPropertyName("aciklama")]
        public string Aciklama { get; set; } = string.Empty;

        /// <summary>Alan değişiklikleri; yoksa boş liste.</summary>
        [JsonPropertyName("degisiklikler")]
        public List<AjandaAlanDegisikligiDto> Degisiklikler { get; set; } = new();
    }
}
