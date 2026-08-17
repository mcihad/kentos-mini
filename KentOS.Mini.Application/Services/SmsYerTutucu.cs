using System.Globalization;

namespace KentOS.Mini.Application.Services;

/// <summary>
/// SMS metninde kullanılabilen yer tutucular — <b>tek doğruluk kaynağı</b>.
/// </summary>
/// <remarks>
/// <para>
/// Sunucu bir dönem yalnızca <c>{gonderici}</c> ve <c>{alici}</c> değiştiriyordu
/// ve bunu ARAYÜZDE söyleyen hiçbir şey yoktu: özellik vardı ama kimse
/// bilmiyordu. Şimdi katalog burada duruyor, istemciler
/// <c>GET /api/v2/ayar/sms-yer-tutucular</c> ile okuyor — üç yerde ayrı liste
/// tutmak, birine yeni bir alan eklendiğinde ötekilerin sessizce eksik
/// kalması demekti.
/// </para>
/// <para>
/// <b>Bilinmeyen yer tutucu OLDUĞU GİBİ kalır.</b> Boşa çevirmek, kullanıcının
/// yazım hatasını (ör. <c>{tarıh}</c>) sessizce yutup mesajı eksik
/// gönderirdi; metinde görünmesi hatayı hemen belli ediyor.
/// </para>
/// </remarks>
public static class SmsYerTutucu
{
    private static readonly CultureInfo Tr = new("tr-TR");

    /// <param name="Ad">Süslü parantezsiz ad, ör. <c>alici</c>.</param>
    /// <param name="Baslik">Seçicide görünen etiket.</param>
    /// <param name="Aciklama">Ne yazacağını anlatan tek cümle.</param>
    public record Kayit(string Ad, string Baslik, string Aciklama);

    public static readonly IReadOnlyList<Kayit> Katalog =
    [
        new("alici", "Alıcı adı", "Mesajın gittiği kişinin adı soyadı."),
        new("gonderici", "Gönderen", "Gönderen birimin yetkilisi ve unvanı."),
        new("baslik", "Etkinlik başlığı", "Etkinliğin adı."),
        new("tarih", "Tarih", "Etkinlik tarihi (gg.aa.yyyy)."),
        new("saat", "Saat", "Etkinliğin başlangıç saati (ss:dd)."),
        new("gun", "Gün", "Haftanın günü, ör. Pazartesi."),
        new("konum", "Konum", "Etkinliğin yapılacağı yer."),
        new("birim", "Birim", "Etkinliğin sahibi birim."),
    ];

    /// <summary>Metindeki yer tutucuları değerleriyle değiştirir.</summary>
    /// <remarks>
    /// Değeri OLMAYAN (ör. konumu girilmemiş etkinlik) yer tutucu boş metne
    /// çevrilir: mesajda "Konum: {konum}" yazması, boş bırakmaktan daha kötü.
    /// </remarks>
    public static string Uygula(string? metin, IReadOnlyDictionary<string, string?> degerler)
    {
        if (string.IsNullOrEmpty(metin)) return string.Empty;

        var sonuc = metin;
        foreach (var (ad, deger) in degerler)
        {
            sonuc = sonuc.Replace($"{{{ad}}}", deger ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }
        return sonuc;
    }

    /// <summary>
    /// HALK GÜNÜ yer tutucuları.
    /// </summary>
    /// <remarks>
    /// Etkinlik katalogundan ayrı: halk günü SMS'i vatandaşa gidiyor ve orada
    /// "gönderen birim yetkilisi" gibi iç alanların karşılığı yok; buna
    /// karşılık <c>{sira}</c> (kaçıncı sırada) yalnızca burada anlamlı. Tek
    /// katalog kullansaydık her iki ekran da ötekinin doldurmadığı alanları
    /// listeliyor olurdu.
    /// </remarks>
    public static readonly IReadOnlyList<Kayit> HalkGunuKatalog =
    [
        new("ad", "Ad", "Vatandaşın adı."),
        new("soyad", "Soyad", "Vatandaşın soyadı."),
        new("adSoyad", "Ad Soyad", "Adı ve soyadı birlikte."),
        new("tarih", "Halk günü tarihi", "Görüşmenin yapılacağı gün (gg.aa.yyyy)."),
        new("saat", "Randevu saati", "Atandığı zaman diliminin başlangıcı (ss:dd)."),
        new("sira", "Sıra numarası", "O saatteki görüşme sırası."),
        new("konum", "Konum", "Görüşmenin yapılacağı yer."),
    ];

    /// <summary>Halk günü katılımından türeyen değerler.</summary>
    public static Dictionary<string, string?> HalkGunuDegerleri(
        string? ad, string? soyad, DateTime tarih, DateTime? saat, int sira, string? konum) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ad"] = ad,
            ["soyad"] = soyad,
            ["adSoyad"] = $"{ad} {soyad}".Trim(),
            ["tarih"] = tarih.ToString("dd.MM.yyyy", Tr),
            // Dilime atanmamış kişide saat YOK; boş bırakmak "14:00" uydurmaktan
            // iyi — vatandaşa yanlış saat göndermek geri alınamaz.
            ["saat"] = saat?.ToString("HH:mm", Tr),
            ["sira"] = sira > 0 ? sira.ToString() : null,
            ["konum"] = konum,
        };

    /// <summary>Etkinlikten türeyen ortak değerler.</summary>
    public static Dictionary<string, string?> EtkinlikDegerleri(
        string? baslik, DateTime? baslangic, string? konum, string? birimAd) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["baslik"] = baslik,
            ["tarih"] = baslangic?.ToString("dd.MM.yyyy", Tr),
            ["saat"] = baslangic?.ToString("HH:mm", Tr),
            ["gun"] = baslangic is null ? null : Tr.DateTimeFormat.GetDayName(baslangic.Value.DayOfWeek),
            ["konum"] = konum,
            ["birim"] = birimAd,
        };
}
