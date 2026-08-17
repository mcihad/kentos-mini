using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Enums.Sibeski
{
    public enum AnalizNokta
    {
        [Display(Name = "Ham Su")]
        Hamsu = 1,
        [Display(Name = "Tavra Deresi")]
        TavraDeresi = 2,
        [Display(Name = "Durultucu")]
        Durultucu = 3,
        [Display(Name = "Havalandırma")]
        Havalandirma = 4,
        [Display(Name = "Filitre")]
        Filitre = 5,
        [Display(Name = "Arıtılmış")]
        Aritilmis = 6,

    }

    public static class AnalizNoktaExtension
    {
        public static string GetDisplayName(this AnalizNokta analizNokta)
        {
            var type = typeof(AnalizNokta);
            var member = type.GetMember(analizNokta.ToString());
            var displayAttribute = member[0].GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            return displayAttribute?.Name ?? analizNokta.ToString();
        }
    }
}
