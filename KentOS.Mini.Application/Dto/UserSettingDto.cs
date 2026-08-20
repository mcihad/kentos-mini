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
    
        // İş takip, halk günü, davet ve kutu bildirimleri.
        // Varsayılan AÇIK — kullanıcı kapatana kadar gelir.

        [JsonPropertyName("taskOnAssigned")]
        public bool TaskOnAssigned { get; set; } = true;

        [JsonPropertyName("taskOnStatusChange")]
        public bool TaskOnStatusChange { get; set; } = true;

        [JsonPropertyName("taskOnApprovalNeeded")]
        public bool TaskOnApprovalNeeded { get; set; } = true;

        [JsonPropertyName("taskOnOverdue")]
        public bool TaskOnOverdue { get; set; } = true;

        [JsonPropertyName("projectOnTeamChange")]
        public bool ProjectOnTeamChange { get; set; } = true;

        [JsonPropertyName("publicDayOnAssigned")]
        public bool PublicDayOnAssigned { get; set; } = true;

        [JsonPropertyName("publicDayOnResult")]
        public bool PublicDayOnResult { get; set; } = true;

        [JsonPropertyName("invitationOnAssigned")]
        public bool InvitationOnAssigned { get; set; } = true;

        [JsonPropertyName("invitationOnResponse")]
        public bool InvitationOnResponse { get; set; } = true;

        [JsonPropertyName("fileOnReceived")]
        public bool FileOnReceived { get; set; } = true;

        [JsonPropertyName("resumeOnShared")]
        public bool ResumeOnShared { get; set; } = true;

        [JsonPropertyName("inboxOnReceived")]
        public bool InboxOnReceived { get; set; } = true;

        [JsonPropertyName("citizenReportOnUpdate")]
        public bool CitizenReportOnUpdate { get; set; } = true;
}
}
