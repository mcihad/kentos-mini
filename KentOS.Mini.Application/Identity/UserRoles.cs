using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Identity
{
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Sekreter = "Sekreter";
        public const string Yonetici = "Yonetici";
        public const string Kullanici = "Kullanici";
        public const string Basin = "Basin";
        public const string Sibeski = "Sibeski";
        public const string Baskan = "Baskan";
        public const string Cicek = "Cicek";
        public const string Medya = "Medya";
        public const string Sistem = "Sistem";
        public const string BaskanOzel = "BaskanOzel";
        public static List<string> GetRoles()
        {
            return [
                Admin,
                Sekreter,
                Yonetici,
                Kullanici,
                Basin,
                Sibeski,
                Baskan,
                Cicek,
                Medya,
                Sistem,
                BaskanOzel
            ];
        }
    }
}
