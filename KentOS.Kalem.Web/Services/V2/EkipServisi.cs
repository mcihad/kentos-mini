using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto.V2.IsTakip;
using KentOS.Kalem.Application.Dto.V2.Ortak;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Exceptions;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>
/// EKİP — birime bağlı kalıcı çalışma grubu.
/// </summary>
/// <remarks>
/// <para>
/// Ekip <b>birimin</b> yapısı, projenin değil: park bahçelerin budama ekibi
/// her projede aynı ekip. Bu yüzden görünürlük kapısı da birim — kullanıcı
/// yalnızca etkin biriminin (ve istenirse alt birimlerinin) ekiplerini görür.
/// </para>
/// <para>
/// Göreve ekip atandığında bildirim ÖNCE <b>lidere</b> gider; iş dağıtımını
/// lider yapar. Kullanıcının tarifi buydu.
/// </para>
/// </remarks>
public interface IEkipServisi
{
    Task<SayfaliSonuc<EkipDto>> ListeAsync(SayfaIstegi istek, bool altBirimlerDahil,
        bool yalnizKullanimda, CancellationToken iptal = default);

    Task<EkipDto> GetirAsync(long id, CancellationToken iptal = default);
    Task<EkipDto> OlusturAsync(EkipKayitDto istek, CancellationToken iptal = default);
    Task<EkipDto> GuncelleAsync(long id, EkipKayitDto istek, CancellationToken iptal = default);
    Task SilAsync(long id, CancellationToken iptal = default);

    /// <summary>
    /// Ekibe atanan bir görevin bildirimi KİMLERE gider?
    /// </summary>
    /// <remarks>
    /// Lider varsa yalnızca lider; yoksa ekibin tamamı. Lidersiz bir ekipte
    /// kimseye bildirmemek, atamayı görünmez kılardı.
    /// </remarks>
    Task<List<long>> BildirimHedefleriAsync(long ekipId, CancellationToken iptal = default);
}

public class EkipServisi(
    AppDbContext _context,
    IEtkinBirim _etkinBirim) : IEkipServisi
{
    public async Task<SayfaliSonuc<EkipDto>> ListeAsync(
        SayfaIstegi istek, bool altBirimlerDahil, bool yalnizKullanimda,
        CancellationToken iptal = default)
    {
        var kapsam = await _etkinBirim.KapsamAsync(altBirimlerDahil, iptal);

        var sorgu = _context.Ekipler
            .AsNoTracking()
            .Where(e => kapsam.Contains(e.BirimId));

        if (yalnizKullanimda)
            sorgu = sorgu.Where(e => e.Kullanimda);

        if (istek.TemizArama is { } ara)
            sorgu = sorgu.Where(e => EF.Functions.ILike(e.Ad, $"%{ara}%"));

        var toplam = await sorgu.LongCountAsync(iptal);

        var idler = await sorgu
            .OrderBy(e => e.Ad)
            .Skip(istek.Atla)
            .Take(istek.Boyut)
            .Select(e => e.Id)
            .ToListAsync(iptal);

        return SayfaliSonuc<EkipDto>.Olustur(await YukleAsync(idler, iptal), toplam, istek);
    }

    public async Task<EkipDto> GetirAsync(long id, CancellationToken iptal = default)
    {
        await ErisebilirMiAsync(id, iptal);

        var liste = await YukleAsync([id], iptal);
        return liste.FirstOrDefault() ?? throw new EntityNotFoundException("Ekip bulunamadı.");
    }

    public async Task<EkipDto> OlusturAsync(EkipKayitDto istek, CancellationToken iptal = default)
    {
        var birim = await _etkinBirim.IdAsync(iptal);
        if (birim <= 0) throw new BusinessRuleException("Ekip açmak için bir birime bağlı olmalısınız.");

        LiderUyeMi(istek);

        var ekip = new Team
        {
            Ad = istek.Ad.Trim(),
            Aciklama = istek.Aciklama,
            BirimId = birim,
            LiderId = istek.LiderId,
            Kullanimda = istek.Kullanimda,
            OlusturmaTarihi = DateTime.Now,
        };

        _context.Ekipler.Add(ekip);
        await _context.SaveChangesAsync(iptal);

        await UyeleriYazAsync(ekip.Id, istek.UyeIdler, iptal);

        return await GetirAsync(ekip.Id, iptal);
    }

    public async Task<EkipDto> GuncelleAsync(
        long id, EkipKayitDto istek, CancellationToken iptal = default)
    {
        var ekip = await ErisebilirMiAsync(id, iptal);

        LiderUyeMi(istek);

        ekip.Ad = istek.Ad.Trim();
        ekip.Aciklama = istek.Aciklama;
        ekip.LiderId = istek.LiderId;
        ekip.Kullanimda = istek.Kullanimda;
        ekip.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync(iptal);
        await UyeleriYazAsync(id, istek.UyeIdler, iptal);

        return await GetirAsync(id, iptal);
    }

    /// <summary>
    /// Ekibi siler — YALNIZCA üzerinde açık görev yoksa.
    /// </summary>
    /// <remarks>
    /// Açık görevi olan bir ekibi silmek, o görevleri sahipsiz bırakırdı:
    /// atama satırı bir ekibi gösteriyor ve o ekip artık yok. Kapanmış
    /// görevlerin ataması tarihî kayıt; onlar engel değil.
    /// </remarks>
    public async Task SilAsync(long id, CancellationToken iptal = default)
    {
        var ekip = await ErisebilirMiAsync(id, iptal);

        var acik = await _context.GorevAtamalari
            .Where(a => a.EkipId == id)
            .Join(_context.Gorevler, a => a.GorevId, g => g.Id, (a, g) => g.Durum)
            .CountAsync(d => d != GorevDurumu.Tamamlandi && d != GorevDurumu.Iptal, iptal);

        if (acik > 0)
        {
            throw new BusinessRuleException(
                $"Ekibin üzerinde {acik} açık görev var; ekip silinemez. " +
                "Görevleri devredin ya da ekibi KULLANIMDAN KALDIRIN.");
        }

        await _context.EkipUyeleri.Where(u => u.EkipId == id).ExecuteDeleteAsync(iptal);

        // `Remove` + `SaveChanges` DEĞİL: aynı bağlamda üyeler daha önce
        // izlenmişse `Remove` onlar için ikinci bir DELETE üretiyor, satırlar
        // yukarıda zaten silindiği için 0 satır etkileniyor ve EF bunu
        // eşzamanlılık çakışması sanıyor. Aynı gerekçe `GorevTipiServisi`de de
        // yazılı.
        _context.Entry(ekip).State = EntityState.Detached;
        await _context.Ekipler.Where(e => e.Id == id).ExecuteDeleteAsync(iptal);
    }

    public async Task<List<long>> BildirimHedefleriAsync(
        long ekipId, CancellationToken iptal = default)
    {
        var lider = await _context.Ekipler
            .AsNoTracking()
            .Where(e => e.Id == ekipId)
            .Select(e => e.LiderId)
            .FirstOrDefaultAsync(iptal);

        if (lider is > 0) return [lider.Value];

        return await _context.EkipUyeleri
            .AsNoTracking()
            .Where(u => u.EkipId == ekipId)
            .Select(u => u.KullaniciId)
            .ToListAsync(iptal);
    }

    // ── iç ─────────────────────────────────────────────────────────────

    /// <summary>Lider ekibin üyesi olmalı — dışarıdan biri ekibi yönetemez.</summary>
    private static void LiderUyeMi(EkipKayitDto istek)
    {
        if (istek.LiderId is { } lider && lider > 0 && !istek.UyeIdler.Contains(lider))
            throw new BusinessRuleException("Ekip lideri, ekibin üyesi olmalı.");
    }

    /// <summary>Ekip etkin birimin kapsamında mı? Değilse 404 — varlığı bile sızmasın.</summary>
    private async Task<Team> ErisebilirMiAsync(long id, CancellationToken iptal)
    {
        var ekip = await _context.Ekipler.FirstOrDefaultAsync(e => e.Id == id, iptal)
            ?? throw new EntityNotFoundException("Ekip bulunamadı.");

        var kapsam = await _etkinBirim.KapsamAsync(altBirimlerDahil: true, iptal);
        if (!kapsam.Contains(ekip.BirimId))
            throw new EntityNotFoundException("Ekip bulunamadı.");

        return ekip;
    }

    private async Task UyeleriYazAsync(long ekipId, List<long> uyeIdler, CancellationToken iptal)
    {
        await _context.EkipUyeleri.Where(u => u.EkipId == ekipId).ExecuteDeleteAsync(iptal);

        foreach (var kullaniciId in uyeIdler.Distinct())
            _context.EkipUyeleri.Add(new TeamMember { EkipId = ekipId, KullaniciId = kullaniciId });

        await _context.SaveChangesAsync(iptal);
    }

    private async Task<List<EkipDto>> YukleAsync(List<long> idler, CancellationToken iptal)
    {
        if (idler.Count == 0) return [];

        var ekipler = await _context.Ekipler
            .AsNoTracking()
            .Where(e => idler.Contains(e.Id))
            .Select(e => new
            {
                e.Id, e.Ad, e.Aciklama, e.BirimId, e.LiderId, e.Kullanimda,
                BirimAd = e.Birim != null ? e.Birim.Ad : null,
            })
            .ToListAsync(iptal);

        var uyeler = await _context.EkipUyeleri
            .AsNoTracking()
            .Where(u => idler.Contains(u.EkipId))
            .Join(_context.Users, u => u.KullaniciId, k => k.Id, (u, k) => new
            {
                u.EkipId,
                u.KullaniciId,
                Ad = ((k.Ad ?? "") + " " + (k.Soyad ?? "")).Trim(),
                BirimAd = k.Birim != null ? k.Birim.Ad : null,
            })
            .ToListAsync(iptal);

        var acikSayilar = await _context.GorevAtamalari
            .AsNoTracking()
            .Where(a => a.EkipId != null && idler.Contains(a.EkipId.Value))
            .Join(_context.Gorevler, a => a.GorevId, g => g.Id, (a, g) => new { a.EkipId, g.Durum })
            .Where(x => x.Durum != GorevDurumu.Tamamlandi && x.Durum != GorevDurumu.Iptal)
            .GroupBy(x => x.EkipId!.Value)
            .Select(g => new { EkipId = g.Key, Sayi = g.Count() })
            .ToDictionaryAsync(x => x.EkipId, x => x.Sayi, iptal);

        return [.. idler
            .Select(id => ekipler.FirstOrDefault(e => e.Id == id))
            .Where(e => e is not null)
            .Select(e =>
            {
                var kendiUyeleri = uyeler.Where(u => u.EkipId == e!.Id).ToList();

                return new EkipDto
                {
                    Id = e!.Id,
                    Ad = e.Ad,
                    Aciklama = e.Aciklama,
                    BirimId = e.BirimId,
                    BirimAd = e.BirimAd,
                    LiderId = e.LiderId,
                    LiderAd = kendiUyeleri.FirstOrDefault(u => u.KullaniciId == e.LiderId)?.Ad,
                    Kullanimda = e.Kullanimda,
                    UyeSayisi = kendiUyeleri.Count,
                    AcikGorevSayisi = acikSayilar.TryGetValue(e.Id, out var s) ? s : 0,
                    Uyeler = [.. kendiUyeleri
                        .OrderBy(u => u.Ad, StringComparer.CurrentCulture)
                        .Select(u => new EkipUyeDto
                        {
                            KullaniciId = u.KullaniciId,
                            Ad = u.Ad,
                            BirimAd = u.BirimAd,
                            Lider = u.KullaniciId == e.LiderId,
                        })],
                };
            })];
    }
}
