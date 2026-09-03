using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Enums
{
    public enum NotifikasyonTip
    {
        Always,
        HideOldAgendas,
        AgendaOnCreated,
        AgendaOnOrganized,
        AgendaOnDeleted,
        AgendaOnUpdated,
        AgendaOnStatusChange,
        AgendaOnImageUpload,
        AgendaOnNoteAdded,
        AgendaOnPostponed,
        AgendaOnFlowerSent,
        AgendaOnFlowerDeleted,
        RequestOnCreated,
        RequestOnOrganized,
        RequestOnDeleted,
        RequestOnUpdated,
        RequestOnFileAttached,
        RequestOnStatusChange,
        RequestOnNoteAdded,
        RequestOnRemittance,
        RequestOnAddedToAgenda,

        // ── İŞ TAKİP (görev, proje) ─────────────────────────────────────
        // Bu olaylar önce `Always` ile gönderiliyordu: kullanıcı kapatamıyor,
        // ayar ekranında da görünmüyorlardı. Her birine kolon ve `switch`
        // kolu eşlik eder (bkz. UserService.HasReceiveNotification).

        /// <summary>Size yeni bir görev atandı.</summary>
        TaskOnAssigned,

        /// <summary>Görevin durumu değişti (onaylandı, iade edildi, reddedildi).</summary>
        TaskOnStatusChange,

        /// <summary>Bir görev sizin onayınızı bekliyor.</summary>
        TaskOnApprovalNeeded,

        /// <summary>Görevin süre hedefi aşıldı.</summary>
        TaskOnOverdue,

        /// <summary>Bir projenin ekibine eklendiniz ya da yöneticisi oldunuz.</summary>
        ProjectOnTeamChange,

        // ── HALK GÜNÜ ───────────────────────────────────────────────────

        /// <summary>Bir halk gününde göreviniz var (atama, salon).</summary>
        PublicDayOnAssigned,

        /// <summary>Halk günü görüşmesi sonuçlandı ya da takibe alındı.</summary>
        PublicDayOnResult,

        // ── DAVET VE PROTOKOL ───────────────────────────────────────────

        /// <summary>Bir davet listesi oluşturuldu ya da size atandı.</summary>
        InvitationOnAssigned,

        /// <summary>Davet listesinde cevap değişti.</summary>
        InvitationOnResponse,

        // ── BELGE VE KUTULAR ────────────────────────────────────────────

        /// <summary>Size dosya gönderildi.</summary>
        FileOnReceived,

        /// <summary>Size özgeçmiş paylaşıldı.</summary>
        ResumeOnShared,

        /// <summary>Gelen kutunuza yeni kayıt düştü.</summary>
        InboxOnReceived,

        /// <summary>Vatandaş bildirimi kaydınızla ilgili gelişme.</summary>
        CitizenReportOnUpdate
    }
}
