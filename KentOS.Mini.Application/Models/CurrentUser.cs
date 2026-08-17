using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Models
{
    public class CurrentUser
    {
        public long Id { get; set; }
        public string? Ad { get; set; }
        public string? Soyad { get; set; }
        public string KullaniciAdi { get; set; }
        public string? Email{ get; set; }

        public string FullName {
            //getter
            get
            {
                return Ad + " " + Soyad;
            }
        }

        public string FullNameWithUsername
        {
            //getter
            get
            {
                return Ad + " " + Soyad + " (" + KullaniciAdi + ")";
            }
        }

    }
}
