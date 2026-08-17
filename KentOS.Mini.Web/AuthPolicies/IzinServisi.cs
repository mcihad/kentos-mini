using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using KentOS.Mini.Web.Data;

namespace KentOS.Mini.Web.AuthPolicies;

/// <summary>
/// Bir kullanıcının izinlerini çözer.
/// </summary>
/// <remarks>
/// <para>
/// <b>İZİNLER JETONA GİRMEZ.</b> JWT 900 dakika geçerli ve iptal listesi yok;
/// izne jetonda taşımak, geri alınan bir yetkinin 15 saat daha çalışması
/// demekti. Sistem bunu bir kez öğretti — <c>DosyaGonderebilir</c> bayrağı
/// tam da bu yüzden her istekte veritabanından okunuyor.
/// </para>
/// <para>
/// Bedeli istek başına bir sorgu; <see cref="IMemoryCache"/> ile kullanıcı
/// başına 5 dakika tutuluyor ve yetki değişince <see cref="Dusur"/> ile
/// düşürülüyor. Önbelleği hiç kullanmamak her istekte iki JOIN demekti;
/// süresiz tutmak ise jetonun sorununu önbelleğe taşımak.
/// </para>
/// </remarks>
public interface IIzinServisi
{
    /// <summary>Kullanıcının sahip olduğu izin adları.</summary>
    Task<IReadOnlySet<string>> IzinleriAsync(long kullaniciId);

    Task<bool> VarMiAsync(long kullaniciId, string izin);

    /// <summary>Rol ya da izin değişince çağrılır.</summary>
    void Dusur(long kullaniciId);

    /// <summary>Bir rolün izinleri değişti — o roldeki HERKESİ düşür.</summary>
    Task RolDegistiAsync(long rolId);
}

public class IzinServisi(AppDbContext _context, IMemoryCache _onbellek) : IIzinServisi
{
    private static readonly TimeSpan Sure = TimeSpan.FromMinutes(5);

    private static string Anahtar(long kullaniciId) => $"izin:{kullaniciId}";

    public async Task<IReadOnlySet<string>> IzinleriAsync(long kullaniciId)
    {
        if (_onbellek.TryGetValue<IReadOnlySet<string>>(Anahtar(kullaniciId), out var hazir)
            && hazir is not null)
        {
            return hazir;
        }

        var izinler = await (
            from ur in _context.UserRoles
            join ri in _context.RolIzinleri on ur.RoleId equals ri.RolId
            join i in _context.Izinler on ri.IzinAd equals i.Ad
            where ur.UserId == kullaniciId && i.Kullanimda
            select ri.IzinAd).Distinct().ToListAsync();

        var kume = izinler.ToHashSet();
        _onbellek.Set(Anahtar(kullaniciId), (IReadOnlySet<string>)kume, Sure);
        return kume;
    }

    public async Task<bool> VarMiAsync(long kullaniciId, string izin)
        => (await IzinleriAsync(kullaniciId)).Contains(izin);

    public void Dusur(long kullaniciId) => _onbellek.Remove(Anahtar(kullaniciId));

    public async Task RolDegistiAsync(long rolId)
    {
        var kullanicilar = await _context.UserRoles
            .Where(ur => ur.RoleId == rolId)
            .Select(ur => ur.UserId)
            .ToListAsync();

        foreach (var k in kullanicilar) Dusur(k);
    }
}
