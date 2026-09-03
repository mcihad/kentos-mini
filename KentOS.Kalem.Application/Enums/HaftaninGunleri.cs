using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Enums
{
    [Flags]
    public enum HaftaninGunleri
    {
        Pazartesi = 1,
        Sali = 2,
        Carsamba = 4,
        Persembe = 8,
        Cuma = 16,
        Cumartesi = 32,
        Pazar = 64
    }
}
