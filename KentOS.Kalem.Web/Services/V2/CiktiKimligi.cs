namespace KentOS.Kalem.Web.Services.V2;

/// <summary>
/// ÇIKTILARDA KULLANILAN KURUM KİMLİĞİ — ad, renkler, amblem.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden ortak bir tip:</b> kurum adı beş ayrı PDF üreticisinin dördünde
/// <c>const string Kurum = "SİVAS BELEDİYESİ"</c> olarak <b>koda yazılıydı</b>.
/// Uygulama başka belediyelere de verilecek; kurum ayarlarında adını değiştiren
/// bir müdürlük halk günü listesini, davetiye dökümünü, isim kartını ve çiçek
/// talimatını hâlâ başka bir belediyenin adıyla basıyordu.
/// </para>
/// <para>
/// Doğru kalıp zaten vardı ve tek bir yerde uygulanmıştı
/// (<c>DisaAktarmaServisi.KurumsalKimlikAsync</c>); üstelik
/// <c>GunlukProgramHtml</c> parametresinin belgesi bunu açıkça söylüyor:
/// "KODA YAZILMAZ — kurum kaydından gelir". Kalıp buraya taşındı ki beş
/// üretici de aynı kaynaktan okusun ve altıncısı yazıldığında kopyalanacak
/// bir sabit bulamasın.
/// </para>
/// <para>
/// <b>Ad BÜYÜK HARFE ÇEVRİLMİYOR.</b> Eski sabitler büyük harfliydi
/// ("SİVAS BELEDİYESİ") ama yöneticinin kurum ayarlarına yazdığı ad olduğu
/// gibi basılıyor — hâlihazırda çalışan tek doğru üretici (günlük program)
/// da böyle yapıyor. Zorla büyütmek ayrıca Türkçe'de tuzak: <c>i</c> harfi
/// yalnızca <c>tr-TR</c> kültüründe <c>İ</c> oluyor, değişmez kültürde
/// "İstanbul" → "ISTANBUL" çıkardı.
/// </para>
/// </remarks>
public sealed record CiktiKimligi(string Ad, string AnaRenk, string VurguRenk, string? Amblem)
{
    /// <summary>Kurum kaydı boşken kullanılan kurumsal lacivert.</summary>
    public const string VarsayilanAnaRenk = "#002E6D";

    /// <summary>Kurum kaydı boşken kullanılan vurgu altını.</summary>
    public const string VarsayilanVurguRenk = "#A78952";
}

public static class CiktiKimligiUzantilari
{
    /// <summary>
    /// Kurum kaydını çıktı kimliğine çevirir.
    /// </summary>
    /// <remarks>
    /// Renkler kurum kaydı boş bırakıldığında eski sabit değerlere düşüyor —
    /// mevcut çıktıların görünümü değişmesin diye. Ad için böyle bir düşüş
    /// yok: <c>Institution.Name</c> zorunlu bir alan.
    /// </remarks>
    public static async Task<CiktiKimligi> CiktiKimligiAsync(
        this IInstitutionService kurumServisi, CancellationToken iptal = default)
    {
        var kurum = await kurumServisi.GetAsync(iptal);

        return new CiktiKimligi(
            kurum.ResolvedDisplayName,
            string.IsNullOrWhiteSpace(kurum.BrandPrimary)
                ? CiktiKimligi.VarsayilanAnaRenk
                : kurum.BrandPrimary,
            string.IsNullOrWhiteSpace(kurum.BrandAccent)
                ? CiktiKimligi.VarsayilanVurguRenk
                : kurum.BrandAccent,
            kurum.ResolvedPrintLogo);
    }
}
