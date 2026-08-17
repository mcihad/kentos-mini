using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KentOS.Mini.Application.Models
{
    public class AppUser: IdentityUser<long>
    {
        [Column("ad")]
        public string? Ad { get; set; }
        [Column("soyad")]
        public string? Soyad { get; set; }
        [Column("unvan")]
        public string? Unvan { get; set; }
        public Birim? Birim { get; set; }
        [Column("birim_id")]
        public long? BirimId { get; set; }

        /// <summary>MOBİL uygulamanın push jetonu. v1 sözleşmesi — dokunulmaz.</summary>
        [Column("fcm_token")]
        public string? FcmToken { get; set; }

        /// <summary>
        /// TARAYICI (web SPA) push jetonu.
        ///
        /// <para>
        /// Ayrı sütun olmasının sebebi: tek sütun olsaydı web'den giriş yapmak
        /// mobil jetonun üzerine yazar ve telefona bildirim gelmeyi keserdi.
        /// Bir kullanıcının dolu her jetonu için ayrı bir <c>Messages</c> satırı
        /// üretilir (bkz. <c>MessageService.PushHedefleri</c>).
        /// </para>
        /// </summary>
        [Column("web_fcm_token")]
        public string? WebFcmToken { get; set; }

        /// <summary>
        /// Bu kullanıcı GİZLİ etkinlik oluşturabilir mi?
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Bu bayrak başkalarının gizli etkinliklerini görmeyi SAĞLAMAZ.</b>
        /// Görünürlük kuralı değişmedi: gizli bir etkinliği yalnızca onu
        /// oluşturan ve katılımcıları görür — bu bayrağa sahip bir kullanıcı
        /// bile başkasının gizli kaydını göremez.
        /// </para>
        /// <para>
        /// Yaptığı tek şey, <b>oluşturma</b> yetkisi vermek ve arayüzde gizlilik
        /// alanlarını göstermek. Yetkisi olmayan kullanıcı o alanları hiç
        /// görmez; sunucu da isteği reddeder.
        /// </para>
        /// </remarks>
        [Column("gizli_etkinlik_ekleyebilir")]
        public bool GizliEtkinlikEkleyebilir { get; set; }

        /// <summary>
        /// Bu kullanıcı dosya gönderimi başlatabilir mi?
        /// </summary>
        /// <remarks>
        /// Yetkisi olmayan kullanıcı gönderim ekranını ve menü öğesini görmez.
        /// <b>Ama kendisine gönderilen dosyaları her zaman görebilir</b> —
        /// alıcı olmak yetki gerektirmez, yoksa gönderilen dosya kimseye
        /// ulaşmazdı.
        /// </remarks>
        [Column("dosya_gonderebilir")]
        public bool DosyaGonderebilir { get; set; }

        /// <summary>
        /// Bu kullanıcı SAHA personeli mi?
        /// </summary>
        /// <remarks>
        /// <para>
        /// İşaretliyse giriş sonrası panele değil <c>/saha</c>'ya iniyor. Saha
        /// personeli günde onlarca kez giriş yapıyor ve her seferinde masaüstü
        /// için tasarlanmış bir panelden geçip aradığını bulmak zorundaydı.
        /// </para>
        /// <para>
        /// <b>Bu bir YETKİ DEĞİL, bir varsayılan.</b> Ne yapabileceğini
        /// izinleri belirliyor; bu alan yalnızca nereye ineceğini söylüyor ve
        /// izinleri olan biri panele geçebiliyor. Yetki olarak kurulsaydı izin
        /// kataloğuna girmesi ve her uçta denetlenmesi gerekirdi; oysa hiçbir
        /// uç bu alana bakmıyor — bakmamalı da.
        /// </para>
        /// </remarks>
        [Column("saha_personeli")]
        public bool SahaPersoneli { get; set; }

        /**
        [NotMapped]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; }

        [NotMapped]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre Onayı")]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; }
        */
    }
}
