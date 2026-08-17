using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Dto
{
    public class UserSettingDto
    {

        [JsonPropertyName("hideOldAgendas")]
        public bool HideOldAgendas { get; set; }

        // Agenda Notifications
        [JsonPropertyName("agendaOnCreated")]
        public bool AgendaOnCreated { get; set; }

        [JsonPropertyName("agendaOnOrganized")]
        public bool AgendaOnOrganized { get; set; }

        [JsonPropertyName("agendaOnDeleted")]
        public bool AgendaOnDeleted { get; set; }
        [JsonPropertyName("agendaOnUpdated")]
        public bool AgendaOnUpdated { get; set; }

        [JsonPropertyName("agendaOnStatusChange")]
        public bool AgendaOnStatusChange { get; set; }

        [JsonPropertyName("agendaOnImageUpload")]
        public bool AgendaOnImageUpload { get; set; }

        [JsonPropertyName("agendaOnNoteAdded")]
        public bool AgendaOnNoteAdded { get; set; }

        [JsonPropertyName("agendaOnPostponed")]
        public bool AgendaOnPostponed { get; set; }

        [JsonPropertyName("agendaOnFlowerSent")]
        public bool AgendaOnFlowerSent { get; set; }

        [JsonPropertyName("agendaOnFlowerDeleted")]
        public bool AgendaOnFlowerDeleted { get; set; }

        // Request Notifications
        [JsonPropertyName("requestOnCreated")]
        public bool RequestOnCreated { get; set; }

        [JsonPropertyName("requestOnOrganized")]
        public bool RequestOnOrganized { get; set; }

        [JsonPropertyName("requestOnDeleted")]
        public bool RequestOnDeleted { get; set; }
        [JsonPropertyName("requestOnUpdated")]
        public bool RequestOnUpdated { get; set; }

        [JsonPropertyName("requestOnFileAttached")]
        public bool RequestOnFileAttached { get; set; }

        [JsonPropertyName("requestOnStatusChange")]
        public bool RequestOnStatusChange { get; set; }

        [JsonPropertyName("requestOnNoteAdded")]
        public bool RequestOnNoteAdded { get; set; }

        [JsonPropertyName("requestOnRemittance")]
        public bool RequestOnRemittance { get; set; }

        [JsonPropertyName("requestOnAddedToAgenda")]
        public bool RequestOnAddedToAgenda { get; set; }
    }
}
