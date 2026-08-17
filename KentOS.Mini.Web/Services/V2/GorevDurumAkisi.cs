using KentOS.Mini.Application.Enums;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>
/// GÖREV DURUM AKIŞI — hangi durumdan hangisine geçilebilir.
/// </summary>
/// <remarks>
/// <para>
/// Akış tek yerde tanımlı ve <b>veri değil kod</b>. Yönetici bir tablo
/// satırı ekleyerek yeni bir durum uyduramaz: her durumun ne tetiklediği
/// (SLA damgası, bildirim, onay kapısı) burada yazılı ve tanımsız bir
/// durum o zincirin dışında kalırdı.
/// </para>
/// <para>
/// <b>Geçiş listesi izin verilenleri sayar, yasakları değil.</b> Yeni bir
/// durum eklendiğinde hiçbir yere bağlanmazsa erişilemez kalır — sessizce
/// her yerden ulaşılabilir olmaktansa.
/// </para>
/// </remarks>
public static class GorevDurumAkisi
{
    /// <summary>Bir durumdan gidilebilecek durumlar.</summary>
    private static readonly Dictionary<GorevDurumu, GorevDurumu[]> Gecisler = new()
    {
        // Atanmamış görev: ya atanır, ya iptal edilir.
        [GorevDurumu.Yeni] =
            [GorevDurumu.Atandi, GorevDurumu.Iptal],

        // Atanan kişi işi kabul eder (başlar) ya da reddeder.
        [GorevDurumu.Atandi] =
            [GorevDurumu.Basladi, GorevDurumu.Reddedildi, GorevDurumu.Iptal,
             // Yeniden atama: sorumlu değişince görev "atandı"da kalır.
             GorevDurumu.Atandi],

        [GorevDurumu.Basladi] =
            [GorevDurumu.DevamEdiyor, GorevDurumu.Beklemede,
             // Aşaması olmayan görev doğrudan tamamlanmaya gidebilir.
             GorevDurumu.TamamlanmaBekliyor, GorevDurumu.Iptal],

        [GorevDurumu.DevamEdiyor] =
            [GorevDurumu.Beklemede, GorevDurumu.TamamlanmaBekliyor, GorevDurumu.Iptal],

        // Bekleyen iş kaldığı yerden sürer.
        [GorevDurumu.Beklemede] =
            [GorevDurumu.DevamEdiyor, GorevDurumu.Basladi, GorevDurumu.Iptal],

        // ONAY KAPISI: yalnızca yönetici geçirir.
        [GorevDurumu.TamamlanmaBekliyor] =
            [GorevDurumu.Tamamlandi, GorevDurumu.IadeEdildi],

        // İade edilen iş personele geri döner.
        [GorevDurumu.IadeEdildi] =
            [GorevDurumu.DevamEdiyor, GorevDurumu.Basladi, GorevDurumu.Iptal],

        // Reddedilen görev başkasına atanabilir.
        [GorevDurumu.Reddedildi] =
            [GorevDurumu.Atandi, GorevDurumu.Iptal],

        // TAMAMLANDI ve İPTAL SON DURAK.
        //
        // Tamamlanmış bir işi yeniden açmak, ölçümü (SLA, hizmet standardı)
        // geçmişe dönük değiştirmek demek. Yeniden yapılması gerekiyorsa
        // YENİ görev açılır ve ikisi ayrı ayrı ölçülür.
        [GorevDurumu.Tamamlandi] = [],
        [GorevDurumu.Iptal] = [],
    };

    /// <summary>Bu geçiş serbest mi?</summary>
    public static bool Gecerli(GorevDurumu mevcut, GorevDurumu hedef) =>
        Gecisler.TryGetValue(mevcut, out var izinli) && izinli.Contains(hedef);

    /// <summary>Mevcut durumdan gidilebilecekler — arayüz düğmeleri buradan.</summary>
    public static IReadOnlyList<GorevDurumu> Sonraki(GorevDurumu mevcut) =>
        Gecisler.TryGetValue(mevcut, out var izinli) ? izinli.Distinct().ToArray() : [];

    /// <summary>Görev kapandı mı? Kapalı görevde aşama ilerletilemez.</summary>
    public static bool Kapali(GorevDurumu durum) =>
        durum is GorevDurumu.Tamamlandi or GorevDurumu.Iptal;

    /// <summary>SLA sayacı bu durumda işliyor mu?</summary>
    /// <remarks>
    /// Beklemede DURUR: malzeme bekleyen bir işi geciktirdi diye personele
    /// yazmak ölçümü anlamsız kılar. Atanmamış görevde de işlemez —
    /// kimseye verilmemiş iş geciktirilmiş sayılmaz.
    /// </remarks>
    public static bool SlaIsliyor(GorevDurumu durum) =>
        durum is GorevDurumu.Basladi or GorevDurumu.DevamEdiyor
              or GorevDurumu.TamamlanmaBekliyor or GorevDurumu.IadeEdildi;

    /// <summary>Kullanıcıya gösterilen ad — SUNUCUDA üretilir.</summary>
    /// <remarks>
    /// İki istemcinin aynı duruma farklı ad vermesi imkânsız olsun diye.
    /// </remarks>
    public static string Ad(GorevDurumu durum) => durum switch
    {
        GorevDurumu.Yeni => "Yeni",
        GorevDurumu.Atandi => "Atandı",
        GorevDurumu.Basladi => "Başladı",
        GorevDurumu.DevamEdiyor => "Devam ediyor",
        GorevDurumu.Beklemede => "Beklemede",
        GorevDurumu.TamamlanmaBekliyor => "Onay bekliyor",
        GorevDurumu.Tamamlandi => "Tamamlandı",
        GorevDurumu.IadeEdildi => "İade edildi",
        GorevDurumu.Reddedildi => "Reddedildi",
        GorevDurumu.Iptal => "İptal edildi",
        _ => durum.ToString(),
    };

    /// <summary>Durum rengi — listede ve haritada işaretçi rengi.</summary>
    public static string Renk(GorevDurumu durum) => durum switch
    {
        GorevDurumu.Yeni => "#7C8592",
        GorevDurumu.Atandi => "#0E4C5C",
        GorevDurumu.Basladi => "#157F7F",
        GorevDurumu.DevamEdiyor => "#1E5FBF",
        GorevDurumu.Beklemede => "#A65A2E",
        GorevDurumu.TamamlanmaBekliyor => "#A78952",
        GorevDurumu.Tamamlandi => "#4A7A2B",
        GorevDurumu.IadeEdildi => "#A8324A",
        GorevDurumu.Reddedildi => "#7A1F2B",
        GorevDurumu.Iptal => "#4D4D4F",
        _ => "#7C8592",
    };

    public static string OncelikAdi(GorevOnceligi oncelik) => oncelik switch
    {
        GorevOnceligi.Dusuk => "Düşük",
        GorevOnceligi.Normal => "Normal",
        GorevOnceligi.Yuksek => "Yüksek",
        GorevOnceligi.Acil => "Acil",
        _ => oncelik.ToString(),
    };

    public static string KaynakAdi(GorevKaynagi kaynak) => kaynak switch
    {
        GorevKaynagi.Manuel => "Elle açıldı",
        GorevKaynagi.Saha => "Saha tespiti",
        GorevKaynagi.Vatandas => "Vatandaş bildirimi",
        GorevKaynagi.Talep => "Talepten",
        GorevKaynagi.Etkinlik => "Etkinlikten",
        GorevKaynagi.Proje => "Projeden",
        GorevKaynagi.BirimDevri => "Birim devri",
        _ => kaynak.ToString(),
    };
}
