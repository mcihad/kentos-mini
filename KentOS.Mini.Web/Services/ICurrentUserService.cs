using KentOS.Mini.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Services
{
    public interface ICurrentUserService
    {
        string GetUsername();
        public long GetCurrentBirimId();

        /// <summary>
        /// Oturum açan kullanıcının SAYISAL kimliği (AspNetUsers.Id).
        ///
        /// Önce <c>UserId</c> claim'inden okur (JWT ve çerezde zaten yazılıyor) —
        /// bu yol veritabanına gitmez. Claim yoksa (ör. eski bir çerez) kullanıcı
        /// adından tek sorguyla düşer. Kimlik yoksa null döner.
        /// </summary>
        Task<long?> GetUserIdAsync();

        Task<AppUser> GetCurrentAsync();

        /// <summary>
        /// Oturum sahibi ajandanın yalnızca <b>basına açık</b> kısmını mı görür?
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>ajanda.basinGoruntule</c> iznine sahip AMA tam görüntüleme izni
        /// (<c>ajanda.goruntule</c>) olmayan kullanıcı için <c>true</c>. İkisi
        /// birden verilmişse GENİŞ olan kazanır: daraltan izin yalnızca tek
        /// başına anlamlı.
        /// </para>
        /// <para>
        /// Kapı burada duruyor çünkü ajandayı okuyan yedi servisin hepsinde
        /// zaten bu arayüz var; ayrı bir bağımlılık eklemek her birine tek tek
        /// dokunmayı ve birini unutmayı davet ederdi. Unutulan tek sorgu,
        /// basın kullanıcısına makamın bütün gününü gösterirdi.
        /// </para>
        /// </remarks>
        Task<bool> YalnizcaBasinMiAsync();
        /// <summary>
        /// Kullanıcının BİRİMİNİN adı — rapor ve çıktı başlıklarında kullanılır.
        /// </summary>
        /// <remarks>
        /// Çıktılarda birim adı sabit "Başkanlık Makamı" yazıyordu; uygulamayı
        /// bütün müdürlükler kullanıyor ve listeler kullanıcının birimine göre
        /// süzülüyor — başka bir birimin adıyla basılan tablo, kimin verisi
        /// olduğunu yanlış söylüyordu. Birim bulunamazsa boş döner; başlıkta
        /// kurum adı tek başına kalır.
        /// </remarks>
        Task<string?> GetCurrentBirimAdiAsync();

        Task<string> GetFullNameAsync();
        Task<string> GetFullNameAndUnvan();
    }
}
