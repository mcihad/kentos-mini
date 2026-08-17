using KentOS.Mini.Application.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Mini.Application.Models;

/// <summary>
/// Kullanıcı oturum açma / kapama denetim kaydı.
///
/// <para>
/// Sistem iki yıldır canlıda ve bugüne kadar <b>kimin ne zaman girdiğine dair
/// hiçbir kayıt tutulmuyordu</b>. Gizli etkinlik taşıyan bir sistemde
/// "bu kayda kim baktı" sorusunun ilk adımı budur.
/// </para>
///
/// <para>
/// Başarısız denemeler de kaydedilir (<see cref="Basarili"/> = false):
/// arka arkaya başarısız giriş, hesap kilitlenmeden önce görülmesi gereken
/// tek sinyaldir.
/// </para>
/// </summary>
[Table("oturum_kayitlari")]
public class KullaniciOturumKaydi
{
    [Column("id")]
    public long Id { get; set; }

    /// <summary>Kullanıcı bulunamadıysa null (hatalı kullanıcı adı denemesi).</summary>
    [Column("kullanici_id")]
    public long? KullaniciId { get; set; }

    /// <summary>Denenen kullanıcı adı — kullanıcı silinse bile kayıtta kalır.</summary>
    [Column("kullanici_adi")]
    public string KullaniciAdi { get; set; } = string.Empty;

    [Column("olay")]
    public OturumOlayi Olay { get; set; }

    [Column("basarili")]
    public bool Basarili { get; set; }

    /// <summary>Başarısızlık nedeni (kimlik hatalı, kilitli, pasif).</summary>
    [Column("aciklama")]
    public string? Aciklama { get; set; }

    /// <summary>İstemci IP adresi. Vekil arkasındaysa `X-Forwarded-For` okunur.</summary>
    [Column("ip_adresi")]
    public string? IpAdresi { get; set; }

    /// <summary>Tarayıcı / uygulama tanıtıcısı — mobil ile web'i ayırt eder.</summary>
    [Column("istemci")]
    public string? Istemci { get; set; }

    [Column("tarih")]
    public DateTime Tarih { get; set; } = DateTime.Now;
}
