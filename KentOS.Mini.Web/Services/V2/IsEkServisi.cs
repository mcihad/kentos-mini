using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.V2.IsTakip;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Storage;

namespace KentOS.Mini.Web.Services.V2;

// ══════════════════════════════════════════════════════════════════ DTO'lar

// `IsEkDto` KentOS.Mini.Application/Dto/V2/IsTakip içine taşındı: aşama ve
// vatandaş bildirimi DTO'ları ek listesi taşıyor ve o proje Web'i göremiyor.

/// <summary>Yüklenen dosyanın ham hâli — controller'dan servise.</summary>
public record IsYuklenenDosya(string Ad, string? IcerikTuru, byte[] Icerik);

// ═══════════════════════════════════════════════════════════════════ arayüz

/// <summary>
/// İş takip varlıklarının ORTAK ek servisi.
/// </summary>
/// <remarks>
/// <para>
/// <b>Görünürlük kapısı burada DEĞİL.</b> Ek servisi "şu varlığın ekleri"
/// sorusunu cevaplıyor; o varlığı görme hakkını çağıran servis denetler ve
/// denetledikten sonra buraya gelir. İki yerde süzmek, birinin unutulduğunda
/// sessizce sızmasına yol açardı — kapı tek yerde, varlığın sahibinde.
/// </para>
/// </remarks>
public interface IIsEkServisi
{
    Task<List<IsEkDto>> ListeAsync(IsVarligi tur, long varlikId, CancellationToken iptal = default);

    Task<IsEkDto> EkleAsync(IsVarligi tur, long varlikId, IsYuklenenDosya dosya,
                            string? aciklama, CancellationToken iptal = default);

    /// <summary>İçeriği indirir. Ek yoksa <see cref="EntityNotFoundException"/>.</summary>
    Task<(byte[] Icerik, string Ad, string Tur)> IcerikAsync(long ekId, CancellationToken iptal = default);

    Task SilAsync(long ekId, CancellationToken iptal = default);

    /// <summary>
    /// Varlık silinirken ONUN bütün eklerini temizler.
    /// </summary>
    /// <remarks>
    /// Yabancı anahtar olmadığı için <c>CASCADE</c> yok; temizlik ELLE
    /// yapılmak zorunda. Çağrılmadığı takdirde yetim ek kaydı ve depoda
    /// yetim dosya kalır.
    /// </remarks>
    Task VarligaAitleriSilAsync(IsVarligi tur, long varlikId, CancellationToken iptal = default);
}

// ══════════════════════════════════════════════════════════════════ uygulama

public class IsEkServisi(
    AppDbContext _context,
    IFileStorage _depo,
    ICurrentUserService _kullanici,
    ILogger<IsEkServisi> _logger) : IIsEkServisi
{
    /// <summary>Oturum varsa adı, yoksa <c>null</c>.</summary>
    private async Task<string?> YukleyenAdiAsync()
    {
        try
        {
            return await _kullanici.GetFullNameAsync();
        }
        catch
        {
            // Anonim istek — vatandaş portalı. Yutulan tek şey ADIN
            // çözülememesi; yükleme kuralları (boyut, uzantı) yerinde.
            return null;
        }
    }

    /// <summary>Depo anahtarının kökü — veritabanındaki yolla birebir aynı.</summary>
    private const string Kok = "uploads/is";

    private const long EnBuyukDosya = 20 * 1024 * 1024;   // 20 MB

    /// <remarks>
    /// Yürütülebilir dosyalar depoda durup indirilebiliyor; birinin
    /// çalıştırması an meselesi.
    /// </remarks>
    private static readonly string[] YasakUzantilar =
        [".exe", ".dll", ".bat", ".cmd", ".sh", ".ps1", ".js", ".jar", ".msi", ".scr", ".com", ".vbs"];

    private static readonly string[] ResimTurleri =
        ["image/jpeg", "image/png", "image/webp", "image/heic", "image/heif"];

    public Task<List<IsEkDto>> ListeAsync(IsVarligi tur, long varlikId, CancellationToken iptal = default) =>
        _context.IsEkleri
            .AsNoTracking()
            .Where(e => e.VarlikTuru == tur && e.VarlikId == varlikId)
            .OrderBy(e => e.OlusturmaTarihi)
            .Select(e => new IsEkDto
            {
                Id = e.Id,
                Ad = e.Ad,
                IcerikTuru = e.IcerikTuru,
                Boyut = e.Boyut,
                ResimMi = e.ResimMi,
                Aciklama = e.Aciklama,
                Yukleyen = e.Yukleyen,
                Tarih = e.OlusturmaTarihi,
            })
            .ToListAsync(iptal);

    public async Task<IsEkDto> EkleAsync(
        IsVarligi tur, long varlikId, IsYuklenenDosya dosya, string? aciklama,
        CancellationToken iptal = default)
    {
        if (dosya.Icerik.Length == 0)
        {
            throw new BusinessRuleException("Yüklenecek dosya boş.");
        }

        if (dosya.Icerik.LongLength > EnBuyukDosya)
        {
            throw new BusinessRuleException("Dosya 20 MB'tan büyük olamaz.");
        }

        var uzanti = Path.GetExtension(dosya.Ad).ToLowerInvariant();
        if (YasakUzantilar.Contains(uzanti))
        {
            throw new BusinessRuleException($"\"{uzanti}\" uzantılı dosyalar yüklenemez.");
        }

        // Depodaki ad SUNUCUDA üretilir; istemciden gelen ad hiç kullanılmaz,
        // böylece `../` içeren bir ad dizin dışına yazamaz.
        var diskAdi = $"{Guid.NewGuid()}{uzanti}";
        var anahtar = $"{Kok}/{diskAdi}";

        await _depo.SaveAsync(StorageArea.Public, anahtar, dosya.Icerik, dosya.IcerikTuru, iptal);

        var kayit = new WorkAttachment
        {
            VarlikTuru = tur,
            VarlikId = varlikId,
            Ad = Path.GetFileName(dosya.Ad),
            DosyaYolu = anahtar,
            IcerikTuru = dosya.IcerikTuru,
            Boyut = dosya.Icerik.LongLength,
            ResimMi = dosya.IcerikTuru is not null && ResimTurleri.Contains(dosya.IcerikTuru),
            Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim(),
            // YÜKLEYEN ADI ZORUNLU DEĞİL.
            //
            // Vatandaş portalı ANONİM ve `ICurrentUserService` orada
            // "Kullanıcı bulunamadı" fırlatıyor; ölçümde tam olarak bu çıktı:
            // fotoğraf yüklenemiyor, kayıt ekransız kalıyordu. Ad yalnızca
            // bilgilendirme — kimin yüklediğini bilmemek, yüklemeyi hiç
            // kabul etmemekten iyi.
            Yukleyen = await YukleyenAdiAsync(),
        };

        _context.IsEkleri.Add(kayit);
        await _context.SaveChangesAsync(iptal);

        return new IsEkDto
        {
            Id = kayit.Id,
            Ad = kayit.Ad,
            IcerikTuru = kayit.IcerikTuru,
            Boyut = kayit.Boyut,
            ResimMi = kayit.ResimMi,
            Aciklama = kayit.Aciklama,
            Yukleyen = kayit.Yukleyen,
            Tarih = kayit.OlusturmaTarihi,
        };
    }

    public async Task<(byte[] Icerik, string Ad, string Tur)> IcerikAsync(
        long ekId, CancellationToken iptal = default)
    {
        var ek = await _context.IsEkleri.AsNoTracking().FirstOrDefaultAsync(e => e.Id == ekId, iptal)
            ?? throw new EntityNotFoundException($"{ekId} kimlikli ek bulunamadı.");

        var icerik = await _depo.ReadAllBytesAsync(StorageArea.Public, ek.DosyaYolu, iptal)
            ?? throw new EntityNotFoundException("Dosya sunucuda bulunamadı.");

        return (icerik, ek.Ad, ek.IcerikTuru ?? "application/octet-stream");
    }

    public async Task SilAsync(long ekId, CancellationToken iptal = default)
    {
        var ek = await _context.IsEkleri.FirstOrDefaultAsync(e => e.Id == ekId, iptal)
            ?? throw new EntityNotFoundException($"{ekId} kimlikli ek bulunamadı.");

        await DepodanSilAsync(ek.DosyaYolu);

        _context.IsEkleri.Remove(ek);
        await _context.SaveChangesAsync(iptal);
    }

    public async Task VarligaAitleriSilAsync(
        IsVarligi tur, long varlikId, CancellationToken iptal = default)
    {
        var ekler = await _context.IsEkleri
            .Where(e => e.VarlikTuru == tur && e.VarlikId == varlikId)
            .ToListAsync(iptal);

        if (ekler.Count == 0) return;

        foreach (var ek in ekler)
        {
            await DepodanSilAsync(ek.DosyaYolu);
        }

        _context.IsEkleri.RemoveRange(ekler);
        await _context.SaveChangesAsync(iptal);
    }

    /// <summary>
    /// Depodan siler; hata yalnızca günlüğe yazılır.
    /// </summary>
    /// <remarks>
    /// Silme hatası veritabanı kaydının silinmesini ENGELLEMEMELİ: yetim bir
    /// dosya zararsız, ama silinemeyen bir kayıt kullanıcıyı çıkışsız
    /// bırakır.
    /// </remarks>
    private async Task DepodanSilAsync(string yol)
    {
        try
        {
            await _depo.DeleteAsync(StorageArea.Public, yol);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ek depodan silinemedi: {Yol}", yol);
        }
    }
}
