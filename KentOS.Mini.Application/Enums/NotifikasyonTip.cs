using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Enums
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
        RequestOnAddedToAgenda
    }
}
