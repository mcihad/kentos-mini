using KentOS.Kalem.Web.Exceptions;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>
/// Liste çıktılarının ORTAK sınırı.
/// </summary>
/// <remarks>
/// <para>
/// Dışa aktarma sayfalama yapmıyor — amacı tam listeyi tek dosyada vermek —
/// ama bu, süzgeçsiz bir isteğin iki yıllık veriyi belleğe alması demek.
/// Sınır o yüzden var.
/// </para>
/// <para>
/// <b>Aşıldığında SESSİZCE KIRPILMAZ.</b> Kırpmak, "her şeyi indirdim" sanan
/// bir kullanıcı bırakırdı ve eksik bir raporun yanlış olduğu ancak başka bir
/// yerden sayılınca anlaşılırdı. Bunun yerine anlaşılır bir iş kuralı hatası
/// dönüyor ve kullanıcıdan süzgeç daraltması isteniyor. Depodaki kural:
/// <i>sessiz kırpma yok</i>.
/// </para>
/// </remarks>
internal static class ListeCikti
{
    /// <summary>Bir çıktıya girebilecek en fazla satır.</summary>
    /// <remarks>
    /// 20.000 satır ClosedXML'de ~10-15 MB'lık bir çalışma kitabı demek;
    /// Excel'in kendi sınırı (1.048.576) çok daha yüksek ama o boyutta bir
    /// dosyayı kimse süzgeç olarak kullanmıyor, rapor için de bölmek gerekiyor.
    /// </remarks>
    public const int UstSinir = 20_000;

    /// <summary>
    /// Sorgu <c>UstSinir + 1</c> satır çekiyor; fazlası varsa sınır aşılmıştır.
    /// </summary>
    public static void SiniriDenetle(int cekilen, string kayitAdi)
    {
        if (cekilen <= UstSinir) return;

        throw new BusinessRuleException(
            $"Sonuç çok büyük ({UstSinir:N0}+ {kayitAdi} kaydı). "
            + "Lütfen tarih aralığı ya da süzgeç ekleyip yeniden deneyin.");
    }
}
