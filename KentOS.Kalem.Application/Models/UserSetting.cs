using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Models
{
    [Table("user_settings")]
    public class UserSetting
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("user_id")]
        public long UserId { get; set; }
        [Column("hide_old_agendas")]
        public bool HideOldAgendas { get; set; } = true;

        // Agenda Notifications
        [Column("agenda_on_created")]
        public bool AgendaOnCreated { get; set; } = true;
        [Column("agenda_on_organized")]
        public bool AgendaOnOrganized { get; set; } = true;
        [Column("agenda_on_deleted")]
        public bool AgendaOnDeleted { get; set; } = true;
        [Column("agenda_on_updated")]
        public bool AgendaOnUpdated { get; set; } = true;
        [Column("agenda_on_status_change")]
        public bool AgendaOnStatusChange { get; set; } = true;
        [Column("agenda_on_image_upload")]
        public bool AgendaOnImageUpload { get; set; } = true;
        [Column("agenda_on_note_added")]
        public bool AgendaOnNoteAdded { get; set; } = true;
        [Column("agenda_on_postponed")]
        public bool AgendaOnPostponed { get; set; } = true;
        [Column("agenda_on_flower_sent")]
        public bool AgendaOnFlowerSent { get; set; } = true;
        [Column("agenda_on_flower_deleted")]
        public bool AgendaOnFlowerDeleted { get; set; } = true;

        // Request Notifications
        [Column("request_on_created")]
        public bool RequestOnCreated { get; set; } = true;
        [Column("request_on_organized")]
        public bool RequestOnOrganized { get; set; } = true;
        [Column("request_on_deleted")]
        public bool RequestOnDeleted { get; set; } = true;
        [Column("request_on_updated")]
        public bool RequestOnUpdated { get; set; } = true;
        [Column("request_on_file_attached")]
        public bool RequestOnFileAttached { get; set; } = true;
        [Column("request_on_status_change")]
        public bool RequestOnStatusChange { get; set; } = true;
        [Column("request_on_note_added")]
        public bool RequestOnNoteAdded { get; set; } = true;
        [Column("request_on_remittance")]
        public bool RequestOnRemittance { get; set; } = true;
        [Column("request_on_added_to_agenda")]
        public bool RequestOnAddedToAgenda { get; set; } = true;
    
        // ── YENİ MODÜLLER ───────────────────────────────────────────
        // Hepsi VARSAYILAN AÇIK. Bildirim, kullanıcının istemediğini
        // söylemesine kadar gelir; kapalı başlayan bir bildirim, ayarı hiç
        // açmayan kullanıcı için hiç var olmamış demektir.

        /// <summary>Size yeni bir görev atandı.</summary>
        [Column("task_on_assigned")]
        public bool TaskOnAssigned { get; set; } = true;

        /// <summary>Görevin durumu değişti.</summary>
        [Column("task_on_status_change")]
        public bool TaskOnStatusChange { get; set; } = true;

        /// <summary>Bir görev onayınızı bekliyor.</summary>
        [Column("task_on_approval_needed")]
        public bool TaskOnApprovalNeeded { get; set; } = true;

        /// <summary>Görevin süre hedefi aşıldı.</summary>
        [Column("task_on_overdue")]
        public bool TaskOnOverdue { get; set; } = true;

        /// <summary>Proje ekibine eklendiniz.</summary>
        [Column("project_on_team_change")]
        public bool ProjectOnTeamChange { get; set; } = true;

        /// <summary>Halk gününde göreviniz var.</summary>
        [Column("public_day_on_assigned")]
        public bool PublicDayOnAssigned { get; set; } = true;

        /// <summary>Halk günü görüşmesi sonuçlandı.</summary>
        [Column("public_day_on_result")]
        public bool PublicDayOnResult { get; set; } = true;

        /// <summary>Davet listesi size atandı.</summary>
        [Column("invitation_on_assigned")]
        public bool InvitationOnAssigned { get; set; } = true;

        /// <summary>Davette cevap değişti.</summary>
        [Column("invitation_on_response")]
        public bool InvitationOnResponse { get; set; } = true;

        /// <summary>Size dosya gönderildi.</summary>
        [Column("file_on_received")]
        public bool FileOnReceived { get; set; } = true;

        /// <summary>Size özgeçmiş paylaşıldı.</summary>
        [Column("resume_on_shared")]
        public bool ResumeOnShared { get; set; } = true;

        /// <summary>Gelen kutunuza yeni kayıt düştü.</summary>
        [Column("inbox_on_received")]
        public bool InboxOnReceived { get; set; } = true;

        /// <summary>Vatandaş bildiriminizde gelişme.</summary>
        [Column("citizen_report_on_update")]
        public bool CitizenReportOnUpdate { get; set; } = true;
}
}
