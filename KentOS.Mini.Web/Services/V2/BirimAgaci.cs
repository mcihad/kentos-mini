using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using KentOS.Mini.Web.Data;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>
/// Birim ağacı sorguları — <b>tek sorguda</b>.
/// </summary>
/// <remarks>
/// <para>
/// Var olan <see cref="Extensions.BirimExtensions.GetDescendants"/> adı
/// <c>IQueryable</c> döndürse de içi bir genişlik-öncelikli arama: ağacın
/// HER SEVİYESİ için ayrı bir veritabanı gidiş-dönüşü yapıyor ve sonucu
/// belleğe alıp <c>AsQueryable()</c> ile geri veriyor. Otuz yerde çağrılıyor;
/// derin bir ağaçta her liste sorgusu birkaç ek sorgu demek.
/// </para>
/// <para>
/// İş takip modülü birim ağacını <b>her liste isteğinde</b> kullanacak
/// (görev listesi, pano, harita, istatistik). Bu yüzden burada özyinelemeli
/// CTE ile tek sorguya indiriliyor ve sonuç önbelleğe alınıyor.
/// </para>
/// <para>
/// <b>Eski yardımcı yerinde bırakıldı.</b> Otuz çağrı yerini değiştirmek bu
/// modülün işi değil; ikisi bir süre yan yana yaşayacak. Yeni kod bunu
/// kullanır.
/// </para>
/// </remarks>
public interface IBirimAgaci
{
    /// <summary>
    /// <paramref name="kok"/> ve ONUN ALTINDAKİ bütün birimlerin kimlikleri.
    /// Kökün kendisi de kümededir.
    /// </summary>
    Task<IReadOnlySet<long>> AltAgacAsync(long kok, CancellationToken iptal = default);

    /// <summary>
    /// <paramref name="aday"/>, <paramref name="kok"/>'ün alt ağacında mı?
    /// Kökün kendisi için de <c>true</c>.
    /// </summary>
    Task<bool> AltAgactaMiAsync(long kok, long aday, CancellationToken iptal = default);

    /// <summary>Önbelleği düşürür — birim ağacı değiştiğinde çağrılır.</summary>
    void Dusur();
}

public class BirimAgaci(AppDbContext _context, IMemoryCache _onbellek) : IBirimAgaci
{
    /// <summary>
    /// Önbellek ömrü izin çözümüyle AYNI (5 dk).
    /// </summary>
    /// <remarks>
    /// İkisi de aynı soruya hizmet ediyor: "bu kullanıcı neyi görebilir?".
    /// Farklı ömürler, yetki değişiminden sonra bir kapının açılıp diğerinin
    /// kapalı kalmasına yol açardı.
    /// </remarks>
    private static readonly TimeSpan Omur = TimeSpan.FromMinutes(5);

    private const string OnbellekOneki = "birim-alt-agac:";

    /// <summary>Toplu düşürme için ortak jeton.</summary>
    private static CancellationTokenSource _surum = new();

    public async Task<IReadOnlySet<long>> AltAgacAsync(long kok, CancellationToken iptal = default)
    {
        if (kok <= 0) return new HashSet<long>();

        var anahtar = OnbellekOneki + kok;
        if (_onbellek.TryGetValue<IReadOnlySet<long>>(anahtar, out var hazir) && hazir is not null)
        {
            return hazir;
        }

        var kumeler = await OkuAsync(kok, iptal);

        _onbellek.Set(anahtar, kumeler, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Omur,
        }.AddExpirationToken(new Microsoft.Extensions.Primitives.CancellationChangeToken(_surum.Token)));

        return kumeler;
    }

    public async Task<bool> AltAgactaMiAsync(long kok, long aday, CancellationToken iptal = default)
    {
        if (kok <= 0 || aday <= 0) return false;
        if (kok == aday) return true;

        return (await AltAgacAsync(kok, iptal)).Contains(aday);
    }

    public void Dusur()
    {
        var eski = Interlocked.Exchange(ref _surum, new CancellationTokenSource());
        eski.Cancel();
        eski.Dispose();
    }

    /// <summary>
    /// Özyinelemeli CTE — ağacın tamamı TEK sorguda.
    /// </summary>
    /// <remarks>
    /// <para>
    /// EF Core özyinelemeli CTE üretemiyor, bu yüzden ham SQL. Parametre
    /// <c>FromSqlInterpolated</c> ile geçiyor — dize birleştirme değil, yani
    /// SQL enjeksiyonuna kapalı.
    /// </para>
    /// <para>
    /// <b>Döngü koruması:</b> birim ağacı bir çevrim içerirse (veri hatası)
    /// CTE sonsuza kadar dönerdi. <c>UNION</c> (<c>UNION ALL</c> değil)
    /// tekrarlanan kimliği eleyerek özyinelemeyi durduruyor.
    /// </para>
    /// </remarks>
    private async Task<IReadOnlySet<long>> OkuAsync(long kok, CancellationToken iptal)
    {
        var idler = await _context.Database
            .SqlQuery<long>($"""
                WITH RECURSIVE alt_agac(id) AS (
                    SELECT id FROM birimler WHERE id = {kok}
                    UNION
                    SELECT b.id FROM birimler b
                    JOIN alt_agac a ON b.ust_birim_id = a.id
                )
                SELECT id AS "Value" FROM alt_agac
                """)
            .ToListAsync(iptal);

        return idler.ToHashSet();
    }
}
