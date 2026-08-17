using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Services.V2;

// ══════════════════════════════════════════════════════════════════ DTO'lar

/// <summary>
/// Bir yorum ve altındaki yanıtlar.
/// </summary>
/// <remarks>
/// Ağaç SUNUCUDA kuruluyor. İstemciye düz liste verip orada kurdurmak,
/// aynı ağaç kurma kodunu her istemcide tekrarlamak demekti — ve sıralama
/// iki istemcide ayrışabilirdi.
/// </remarks>
public class IsYorumDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("metin")] public string Metin { get; set; } = string.Empty;
    [JsonPropertyName("yazanId")] public long? YazanId { get; set; }
    [JsonPropertyName("yazan")] public string? Yazan { get; set; }
    [JsonPropertyName("benimMi")] public bool BenimMi { get; set; }
    [JsonPropertyName("silindi")] public bool Silindi { get; set; }
    [JsonPropertyName("tarih")] public DateTime Tarih { get; set; }
    [JsonPropertyName("duzenlendi")] public bool Duzenlendi { get; set; }
    [JsonPropertyName("yanitlar")] public List<IsYorumDto> Yanitlar { get; set; } = [];
}

// ═══════════════════════════════════════════════════════════════════ arayüz

/// <summary>
/// İş takip varlıklarının ORTAK yorum servisi — iç içe.
/// </summary>
/// <remarks>
/// <see cref="IIsEkServisi"/> ile aynı kural: <b>görünürlük kapısı burada
/// değil</b>. Varlığı görme hakkını çağıran servis denetler.
/// </remarks>
public interface IIsYorumServisi
{
    Task<List<IsYorumDto>> AgacAsync(IsVarligi tur, long varlikId, CancellationToken iptal = default);

    Task<IsYorumDto> EkleAsync(IsVarligi tur, long varlikId, string metin,
                               long? ustYorumId, CancellationToken iptal = default);

    /// <summary>Yalnızca YAZANI düzenleyebilir.</summary>
    Task<IsYorumDto> DuzenleAsync(long yorumId, string metin, CancellationToken iptal = default);

    /// <summary>Yumuşak siler — yanıtlar yetim kalmasın.</summary>
    Task SilAsync(long yorumId, CancellationToken iptal = default);

    Task<int> SayiAsync(IsVarligi tur, long varlikId, CancellationToken iptal = default);

    /// <summary>Varlık silinirken onun bütün yorumlarını temizler.</summary>
    Task VarligaAitleriSilAsync(IsVarligi tur, long varlikId, CancellationToken iptal = default);
}

// ══════════════════════════════════════════════════════════════════ uygulama

public class IsYorumServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici) : IIsYorumServisi
{
    private const int EnUzunMetin = 4000;

    public async Task<List<IsYorumDto>> AgacAsync(
        IsVarligi tur, long varlikId, CancellationToken iptal = default)
    {
        var benimId = await _kullanici.GetUserIdAsync();

        // TEK SORGU, ağaç bellekte kuruluyor. Yorum sayısı bir kayıtta
        // yüzlerle ölçülüyor; her seviye için ayrı sorgu atmak (N+1) burada
        // tamamen gereksiz.
        var duz = await _context.IsYorumlari
            .AsNoTracking()
            .Where(y => y.VarlikTuru == tur && y.VarlikId == varlikId)
            .OrderBy(y => y.OlusturmaTarihi)
            .Select(y => new
            {
                y.Id, y.UstYorumId, y.Metin, y.YazanId, y.Yazan,
                y.Silindi, y.OlusturmaTarihi, y.GuncellemeTarihi,
            })
            .ToListAsync(iptal);

        var dugumler = duz.ToDictionary(
            y => y.Id,
            y => new IsYorumDto
            {
                Id = y.Id,
                // Silinen yorumun METNİ VERİLMEZ. İskeleti kalıyor ki altındaki
                // yanıtlar neye cevap verdiğini kaybetmesin; içeriği değil.
                Metin = y.Silindi ? string.Empty : y.Metin,
                YazanId = y.Silindi ? null : y.YazanId,
                Yazan = y.Silindi ? null : y.Yazan,
                BenimMi = !y.Silindi && benimId is not null && y.YazanId == benimId,
                Silindi = y.Silindi,
                Tarih = y.OlusturmaTarihi,
                Duzenlendi = y.GuncellemeTarihi is not null,
            });

        var kokler = new List<IsYorumDto>();

        foreach (var ham in duz)
        {
            var dugum = dugumler[ham.Id];

            if (ham.UstYorumId is { } ustId && dugumler.TryGetValue(ustId, out var ust))
            {
                ust.Yanitlar.Add(dugum);
            }
            else
            {
                // Üstü silinmiş ya da başka bir varlığa ait bir yanıt köke
                // çıkar — kaybolmaktansa görünsün.
                kokler.Add(dugum);
            }
        }

        return kokler;
    }

    public async Task<IsYorumDto> EkleAsync(
        IsVarligi tur, long varlikId, string metin, long? ustYorumId,
        CancellationToken iptal = default)
    {
        var temiz = (metin ?? string.Empty).Trim();
        if (temiz.Length == 0)
        {
            throw new BusinessRuleException("Yorum boş olamaz.");
        }

        if (temiz.Length > EnUzunMetin)
        {
            throw new BusinessRuleException($"Yorum {EnUzunMetin} karakteri aşamaz.");
        }

        if (ustYorumId is { } ust)
        {
            // Yanıt verilen yorum AYNI kayda ait olmalı. Aksi hâlde başka bir
            // görevin yorumuna yanıt yazılabilir ve iki kayıt birbirine
            // karışırdı.
            var uygun = await _context.IsYorumlari.AnyAsync(
                y => y.Id == ust && y.VarlikTuru == tur && y.VarlikId == varlikId, iptal);

            if (!uygun)
            {
                throw new BusinessRuleException("Yanıtlanan yorum bu kayda ait değil.");
            }
        }

        var kayit = new WorkComment
        {
            VarlikTuru = tur,
            VarlikId = varlikId,
            UstYorumId = ustYorumId,
            Metin = temiz,
            YazanId = await _kullanici.GetUserIdAsync(),
            Yazan = await _kullanici.GetFullNameAsync(),
        };

        _context.IsYorumlari.Add(kayit);
        await _context.SaveChangesAsync(iptal);

        return new IsYorumDto
        {
            Id = kayit.Id,
            Metin = kayit.Metin,
            YazanId = kayit.YazanId,
            Yazan = kayit.Yazan,
            BenimMi = true,
            Tarih = kayit.OlusturmaTarihi,
        };
    }

    public async Task<IsYorumDto> DuzenleAsync(
        long yorumId, string metin, CancellationToken iptal = default)
    {
        var kayit = await BulAsync(yorumId, iptal);

        var temiz = (metin ?? string.Empty).Trim();
        if (temiz.Length == 0)
        {
            throw new BusinessRuleException("Yorum boş olamaz.");
        }

        kayit.Metin = temiz;
        kayit.GuncellemeTarihi = DateTime.Now;
        await _context.SaveChangesAsync(iptal);

        return new IsYorumDto
        {
            Id = kayit.Id,
            Metin = kayit.Metin,
            YazanId = kayit.YazanId,
            Yazan = kayit.Yazan,
            BenimMi = true,
            Tarih = kayit.OlusturmaTarihi,
            Duzenlendi = true,
        };
    }

    public async Task SilAsync(long yorumId, CancellationToken iptal = default)
    {
        var kayit = await BulAsync(yorumId, iptal);

        // YUMUŞAK silme: sert silme altındaki yanıtları yetim bırakır ve
        // konuşmanın ortasında boşluk açar.
        kayit.Silindi = true;
        kayit.Metin = string.Empty;
        kayit.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync(iptal);
    }

    public Task<int> SayiAsync(IsVarligi tur, long varlikId, CancellationToken iptal = default) =>
        _context.IsYorumlari.CountAsync(
            y => y.VarlikTuru == tur && y.VarlikId == varlikId && !y.Silindi, iptal);

    public async Task VarligaAitleriSilAsync(
        IsVarligi tur, long varlikId, CancellationToken iptal = default)
    {
        await _context.IsYorumlari
            .Where(y => y.VarlikTuru == tur && y.VarlikId == varlikId)
            .ExecuteDeleteAsync(iptal);
    }

    /// <summary>
    /// Yazma yolu — yalnızca YAZANI bulur.
    /// </summary>
    /// <remarks>
    /// "Yetkin yok" yerine "bulunamadı": başkasının yorumunun VAR olduğunu
    /// doğrulamak bile bilgi sızdırır.
    /// </remarks>
    private async Task<WorkComment> BulAsync(long yorumId, CancellationToken iptal)
    {
        var benimId = await _kullanici.GetUserIdAsync();

        return await _context.IsYorumlari
            .FirstOrDefaultAsync(y => y.Id == yorumId && !y.Silindi && y.YazanId == benimId, iptal)
            ?? throw new EntityNotFoundException(
                $"{yorumId} kimlikli yorum bulunamadı ya da düzenleme yetkiniz yok.");
    }
}
