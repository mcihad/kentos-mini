using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Models
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
    }
}
