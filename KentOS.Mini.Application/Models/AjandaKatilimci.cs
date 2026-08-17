using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Mini.Application.Models
{
    /// <summary>
    /// Etkinliğe bağlı kişi/birim satırı — <b>İKİ AYRI KAVRAM</b>.
    ///
    /// <para>
    /// Bu tablo iki farklı soruyu cevaplıyor ve ikisi birbirine karıştırılmamalı:
    /// </para>
    ///
    /// <list type="bullet">
    /// <item>
    /// <b><see cref="BirimId"/> — KATILIMCI BİRİM.</b> Etkinliğe katılacak
    /// <i>diğer departmanlar</i>: "Fen İşleri Müdürlüğü", "Başkan Yardımcısı".
    /// Kullanıcının kendi seviyesindeki ve altındaki birimlerden seçilir; bir
    /// müdürlük başkan yardımcısını kendi toplantısına çağıramaz. Katılan taraf
    /// kişi değil birimdir, çünkü o birimden kimin geleceği toplantı gününe
    /// kadar değişebiliyor.
    /// </item>
    /// <item>
    /// <b><see cref="KullaniciId"/> — GÖREBİLECEK KİŞİ.</b> Yalnızca <b>gizli</b>
    /// etkinliklerde anlamlıdır ve etkinliği kimin görebileceğini belirler.
    /// <b>Kullanıcının KENDİ BİRİMİNDEKİ</b> kullanıcılardan seçilir.
    /// </item>
    /// </list>
    ///
    /// <para>
    /// <b>Birbirinin yerine geçmezler.</b> Katılımcı birim eklemek gizli bir
    /// etkinliği o birime AÇMAZ; görebilecek kişi eklemek de o kişiyi
    /// toplantıya davet etmez. Bir dönem <see cref="KullaniciId"/> "eski alan"
    /// sanılıp görünürlük katılımcı birimlere bağlanmıştı: gizli bir toplantıya
    /// bir müdürlüğü davet etmek, o müdürlükteki herkesi toplantının içeriğine
    /// ortak ediyordu.
    /// </para>
    ///
    /// <para>
    /// Tekrarlanan etkinliklerde her iki liste de HER TEKRARA kopyalanır; böylece
    /// tek bir tekrar ayrı düzenlendiğinde (istisna) kendi listesini taşır.
    /// </para>
    /// </summary>
    [Table("ajanda_katilimcilar")]
    public class AjandaKatilimci
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("ajanda_id")]
        public long AjandaId { get; set; }
        public Ajanda? Ajanda { get; set; }

        /// <summary>
        /// KATILIMCI BİRİM — etkinliğe katılacak departman.
        /// </summary>
        /// <remarks>
        /// Gizlilikten bağımsızdır: açık bir toplantının davetlilerini kaydetmek
        /// de anlamlı. Gizli etkinliğe erişim <b>vermez</b> — o iş
        /// <see cref="KullaniciId"/>'nin.
        /// </remarks>
        [Column("birim_id")]
        public long? BirimId { get; set; }
        public Birim? Birim { get; set; }

        /// <summary>
        /// GÖREBİLECEK KİŞİ — gizli etkinliği görmeye yetkili kullanıcının
        /// <c>AspNetUsers.Id</c> değeri.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Yalnızca gizli etkinliklerde dolar; etkinlik gizlilikten çıkarsa
        /// liste temizlenir (bkz. <c>KatilimcilariEsitleAsync</c>). Seçilebilecek
        /// kişiler <b>ekleyenin kendi birimiyle</b> sınırlıdır.
        /// </para>
        /// <para>
        /// Gezinme özelliği (navigation) BİLİNÇLİ olarak yok: <c>AppUser</c>
        /// sınıfı Web derlemesinde duruyor, bu proje ona başvuramıyor.
        /// </para>
        /// </remarks>
        [Column("kullanici_id")]
        public long? KullaniciId { get; set; }

        [Column("olusturma_tarihi")]
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    }
}
