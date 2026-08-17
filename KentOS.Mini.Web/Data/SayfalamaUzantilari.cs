using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.V2.Ortak;

namespace KentOS.Mini.Web.Data;

/// <summary>
/// <see cref="IQueryable{T}"/> için sayfalama.
/// </summary>
public static class SayfalamaUzantilari
{
    /// <summary>
    /// Sorguyu veritabanı düzeyinde sayfalar.
    ///
    /// <para>
    /// Sayım ve sayfa ayrı iki sorgu olarak gider. Tek sorguda pencere
    /// fonksiyonuyla yapmak mümkün ama EF Core bunu her sağlayıcıda aynı
    /// üretmiyor; iki sorgu okunur ve öngörülebilir.
    /// </para>
    ///
    /// <para>
    /// <b>Sıralama şart:</b> Postgres <c>OFFSET</c>'i sırasız bir sorguda
    /// deterministik değildir — aynı kayıt iki sayfada görünebilir veya hiç
    /// görünmez. Çağıran <c>OrderBy</c> vermeyi unutmuşsa bu sessizce olur,
    /// bu yüzden burada kontrol edilir.
    /// </para>
    /// </summary>
    public static async Task<SayfaliSonuc<TCikti>> SayfalaAsync<TGirdi, TCikti>(
        this IQueryable<TGirdi> sorgu,
        SayfaIstegi istek,
        Func<TGirdi, TCikti> donustur,
        CancellationToken iptal = default)
    {
        var toplam = await sorgu.LongCountAsync(iptal);

        var sayfa = await sorgu
            .Skip(istek.Atla)
            .Take(istek.Boyut)
            .ToListAsync(iptal);

        return SayfaliSonuc<TCikti>.Olustur(
            sayfa.Select(donustur).ToList(), toplam, istek);
    }

    /// <summary>Dönüşüm gerekmediğinde.</summary>
    public static Task<SayfaliSonuc<T>> SayfalaAsync<T>(
        this IQueryable<T> sorgu,
        SayfaIstegi istek,
        CancellationToken iptal = default)
        => sorgu.SayfalaAsync(istek, x => x, iptal);

    /// <summary>
    /// İzin verilen alanlar arasından sıralar; tanınmayan alan varsayılana düşer.
    /// </summary>
    /// <remarks>
    /// İstemcinin gönderdiği alan adı doğrudan bir ifadeye çevrilmez —
    /// beyaz liste dışında bir değer sessizce yok sayılır. Böylece
    /// <c>sirala=Parola</c> gibi bir istek ne hata verir ne de işe yarar.
    /// </remarks>
    public static IOrderedQueryable<T> Sirala<T>(
        this IQueryable<T> sorgu,
        SayfaIstegi istek,
        IReadOnlyDictionary<string, System.Linq.Expressions.Expression<Func<T, object?>>> izinliAlanlar,
        System.Linq.Expressions.Expression<Func<T, object?>> varsayilan,
        bool varsayilanAzalan = false)
    {
        var alan = istek.Sirala;

        if (alan is not null &&
            izinliAlanlar.TryGetValue(alan, out var ifade))
        {
            return istek.Azalan ? sorgu.OrderByDescending(ifade) : sorgu.OrderBy(ifade);
        }

        // Sıralama alanı verilmemişse `azalan` bayrağı da anlamsızdır;
        // varsayılanın kendi yönü kullanılır.
        return varsayilanAzalan ? sorgu.OrderByDescending(varsayilan) : sorgu.OrderBy(varsayilan);
    }
}
