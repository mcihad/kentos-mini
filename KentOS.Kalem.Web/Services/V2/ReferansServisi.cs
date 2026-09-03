using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using KentOS.Kalem.Application.Dto.V2.Ortak;
using KentOS.Kalem.Application.Dto.V2.Referans;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Exceptions;
using KentOS.Kalem.Web.Models;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>Hangi tanım tablosuyla çalışıldığı.</summary>
public enum TanimTuru
{
    /// <summary>`RandevuTip` — hem etkinlik hem talep tipi olarak kullanılıyor.</summary>
    EtkinlikTipi,
    /// <summary>`AjandaDurum` — etkinliğin kullanıcı tanımlı durumu.</summary>
    EtkinlikDurumu,
    /// <summary>`RandevuDurum` — talebin iş akışı durumu.</summary>
    TalepDurumu,
}

public interface IReferansServisi
{
    Task<SayfaliSonuc<TanimDto>> TanimlarAsync(TanimTuru tur, SayfaIstegi istek);
    Task<TanimDto> TanimAsync(TanimTuru tur, long id);
    Task<TanimDto> TanimOlusturAsync(TanimTuru tur, TanimIstegi istek);
    Task<TanimDto> TanimGuncelleAsync(TanimTuru tur, long id, TanimIstegi istek);
    Task TanimSilAsync(TanimTuru tur, long id);

    Task<SayfaliSonuc<AdKaydiDto>> MahallelerAsync(SayfaIstegi istek);
    Task<AdKaydiDto> MahalleOlusturAsync(AdKaydiIstegi istek);
    Task<AdKaydiDto> MahalleGuncelleAsync(long id, AdKaydiIstegi istek);
    Task MahalleSilAsync(long id);
    Task<TopluIceAktarmaSonucu> MahalleIceAktarAsync(TopluIceAktarmaIstegi istek);
    Task<int> MahalleTumunuSilAsync();

    Task<SayfaliSonuc<AdKaydiDto>> MesleklerAsync(SayfaIstegi istek);
    Task<AdKaydiDto> MeslekOlusturAsync(AdKaydiIstegi istek);
    Task<AdKaydiDto> MeslekGuncelleAsync(long id, AdKaydiIstegi istek);
    Task MeslekSilAsync(long id);
    Task<TopluIceAktarmaSonucu> MeslekIceAktarAsync(TopluIceAktarmaIstegi istek);
    Task<int> MeslekTumunuSilAsync();
}

/// <summary>
/// Referans (tanım) verilerinin yönetimi.
///
/// <para>
/// Eski arayüzde her tablo için ayrı bir MVC controller'ı vardı
/// (<c>RandevuTipController</c>, <c>AjandaDurumController</c>,
/// <c>RandevuDurumController</c>, <c>MahalleController</c>,
/// <c>MeslekController</c>) ve mantık view'lara gömülüydü. O controller'lar
/// aynen çalışmaya devam ediyor; burası aynı işi JSON üzerinden yapar.
/// </para>
///
/// <para>
/// <b>Önbellek:</b> <c>SettingsService</c> bu listeleri <see cref="IMemoryCache"/>
/// içinde tutuyor. Yazma işlemlerinden sonra ilgili anahtar temizlenmezse
/// eski uygulama saatlerce bayat liste gösterir — bu, eski MVC controller'ının
/// da yaptığı şey ve atlanması sessiz bir hata olur.
/// </para>
/// </summary>
public class ReferansServisi(
    AppDbContext _context,
    IMemoryCache _onbellek,
    ILogger<ReferansServisi> _logger) : IReferansServisi
{
    // ─────────────────────────────────────────────────────── tanımlar

    public async Task<SayfaliSonuc<TanimDto>> TanimlarAsync(TanimTuru tur, SayfaIstegi istek)
    {
        var ara = istek.TemizArama;

        return tur switch
        {
            TanimTuru.EtkinlikTipi => await _context.RandevuTipleri
                .AsNoTracking()
                .Where(t => ara == null || EF.Functions.ILike(t.Ad, $"%{ara}%"))
                .OrderBy(t => t.Ad)
                .Select(t => new TanimDto
                {
                    Id = t.Id,
                    Ad = t.Ad,
                    Renk = t.Renk,
                    Aciklama = t.Aciklama,
                    // Tip hem etkinlikte hem talepte kullanılıyor; ikisini topla.
                    KullanimSayisi = t.Ajandalar.Count + (t.Randevular == null ? 0 : t.Randevular.Count),
                })
                .SayfalaAsync(istek),

            TanimTuru.EtkinlikDurumu => await _context.AjandaDurumlar
                .AsNoTracking()
                .Where(d => ara == null || EF.Functions.ILike(d.Ad, $"%{ara}%"))
                .OrderBy(d => d.Ad)
                .Select(d => new TanimDto
                {
                    Id = d.Id,
                    Ad = d.Ad,
                    Renk = d.Renk,
                    Simge = d.Icon,
                    Aciklama = d.Aciklama,
                    KullanimSayisi = d.Ajandalar == null ? 0 : d.Ajandalar.Count,
                })
                .SayfalaAsync(istek),

            TanimTuru.TalepDurumu => await _context.RandevuDurumlar
                .AsNoTracking()
                .Where(d => ara == null || EF.Functions.ILike(d.DurumAd, $"%{ara}%"))
                .OrderBy(d => d.DurumAd)
                .Select(d => new TanimDto
                {
                    Id = d.Id,
                    Ad = d.DurumAd,
                    Renk = d.Renk,
                    Simge = d.Simge,
                    Aciklama = d.Aciklama,
                    KullanimSayisi = d.Randevular == null ? 0 : d.Randevular.Count,
                })
                .SayfalaAsync(istek),

            _ => throw new ArgumentOutOfRangeException(nameof(tur)),
        };
    }

    public async Task<TanimDto> TanimAsync(TanimTuru tur, long id)
    {
        var sonuc = await TanimlarAsync(tur, new SayfaIstegi { Boyut = SayfaIstegi.EnBuyukBoyut });
        return sonuc.Veriler.FirstOrDefault(t => t.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli tanım bulunamadı.");
    }

    public async Task<TanimDto> TanimOlusturAsync(TanimTuru tur, TanimIstegi istek)
    {
        await AdCakismasiVarMiAsync(tur, istek.Ad, null);

        long id;
        switch (tur)
        {
            case TanimTuru.EtkinlikTipi:
                var tip = new RandevuTip { Ad = istek.Ad, Renk = istek.Renk, Aciklama = istek.Aciklama };
                _context.RandevuTipleri.Add(tip);
                await _context.SaveChangesAsync();
                id = tip.Id;
                break;

            case TanimTuru.EtkinlikDurumu:
                // `ajanda_durumlar.renk` NOT NULL; boş gelirse kurumsal lacivert.
                var ed = new AjandaDurum
                {
                    Ad = istek.Ad,
                    Renk = istek.Renk ?? "#002E6D",
                    Icon = istek.Simge,
                    Aciklama = istek.Aciklama,
                };
                _context.AjandaDurumlar.Add(ed);
                await _context.SaveChangesAsync();
                id = ed.Id;
                break;

            case TanimTuru.TalepDurumu:
                var td = new RandevuDurum
                {
                    DurumAd = istek.Ad,
                    Renk = istek.Renk ?? "#002E6D",
                    Simge = istek.Simge,
                    Aciklama = istek.Aciklama,
                };
                _context.RandevuDurumlar.Add(td);
                await _context.SaveChangesAsync();
                id = td.Id;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(tur));
        }

        OnbellegiTemizle(tur);
        _logger.LogInformation("Yeni {Tur} tanımı: {Ad} ({Id})", tur, istek.Ad, id);
        return await TanimAsync(tur, id);
    }

    public async Task<TanimDto> TanimGuncelleAsync(TanimTuru tur, long id, TanimIstegi istek)
    {
        await AdCakismasiVarMiAsync(tur, istek.Ad, id);

        switch (tur)
        {
            case TanimTuru.EtkinlikTipi:
                var tip = await _context.RandevuTipleri.FirstOrDefaultAsync(t => t.Id == id)
                    ?? throw new EntityNotFoundException($"{id} kimlikli etkinlik tipi bulunamadı.");
                tip.Ad = istek.Ad;
                tip.Renk = istek.Renk;
                tip.Aciklama = istek.Aciklama;
                break;

            case TanimTuru.EtkinlikDurumu:
                var ed = await _context.AjandaDurumlar.FirstOrDefaultAsync(d => d.Id == id)
                    ?? throw new EntityNotFoundException($"{id} kimlikli etkinlik durumu bulunamadı.");
                ed.Ad = istek.Ad;
                ed.Renk = istek.Renk ?? ed.Renk;
                ed.Icon = istek.Simge;
                ed.Aciklama = istek.Aciklama;
                break;

            case TanimTuru.TalepDurumu:
                var td = await _context.RandevuDurumlar.FirstOrDefaultAsync(d => d.Id == id)
                    ?? throw new EntityNotFoundException($"{id} kimlikli talep durumu bulunamadı.");
                td.DurumAd = istek.Ad;
                td.Renk = istek.Renk ?? td.Renk;
                td.Simge = istek.Simge;
                td.Aciklama = istek.Aciklama;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(tur));
        }

        await _context.SaveChangesAsync();
        OnbellegiTemizle(tur);
        return await TanimAsync(tur, id);
    }

    public async Task TanimSilAsync(TanimTuru tur, long id)
    {
        // Kullanımdaki bir tanımı silmek, ona bağlı kayıtları yabancı anahtar
        // hatasıyla ya da (daha kötüsü) sessiz bir NULL'la bırakır. Sayıyı
        // burada kontrol etmek, veritabanı hatasını kullanıcıya anlaşılır bir
        // mesaja çevirir.
        var kullanim = await KullanimSayisiAsync(tur, id);
        if (kullanim > 0)
        {
            throw new BusinessRuleException(
                $"Bu tanım {kullanim} kayıtta kullanılıyor; silinemez. " +
                "Önce ilgili kayıtları başka bir tanıma taşıyın.");
        }

        switch (tur)
        {
            case TanimTuru.EtkinlikTipi:
                var tip = await _context.RandevuTipleri.FirstOrDefaultAsync(t => t.Id == id)
                    ?? throw new EntityNotFoundException($"{id} kimlikli etkinlik tipi bulunamadı.");
                _context.RandevuTipleri.Remove(tip);
                break;

            case TanimTuru.EtkinlikDurumu:
                var ed = await _context.AjandaDurumlar.FirstOrDefaultAsync(d => d.Id == id)
                    ?? throw new EntityNotFoundException($"{id} kimlikli etkinlik durumu bulunamadı.");
                _context.AjandaDurumlar.Remove(ed);
                break;

            case TanimTuru.TalepDurumu:
                var td = await _context.RandevuDurumlar.FirstOrDefaultAsync(d => d.Id == id)
                    ?? throw new EntityNotFoundException($"{id} kimlikli talep durumu bulunamadı.");
                _context.RandevuDurumlar.Remove(td);
                break;
        }

        await _context.SaveChangesAsync();
        OnbellegiTemizle(tur);
    }

    // ─────────────────────────────────────────────────────── mahalle

    public Task<SayfaliSonuc<AdKaydiDto>> MahallelerAsync(SayfaIstegi istek)
    {
        var ara = istek.TemizArama;
        return _context.Mahalleler
            .AsNoTracking()
            .Where(m => ara == null || EF.Functions.ILike(m.Ad, $"%{ara}%"))
            .OrderBy(m => m.Ad)
            .Select(m => new AdKaydiDto
            {
                Id = m.Id,
                Ad = m.Ad,
                KullanimSayisi = m.Randevular.Count,
            })
            .SayfalaAsync(istek);
    }

    public async Task<AdKaydiDto> MahalleOlusturAsync(AdKaydiIstegi istek)
    {
        if (await _context.Mahalleler.AnyAsync(m => m.Ad.ToLower() == istek.Ad.ToLower()))
        {
            throw new BusinessRuleException($"\"{istek.Ad}\" mahallesi zaten kayıtlı.");
        }

        var m = new Mahalle { Ad = istek.Ad };
        _context.Mahalleler.Add(m);
        await _context.SaveChangesAsync();
        _onbellek.Remove(CacheKeys.Mahalle);

        return new AdKaydiDto { Id = m.Id, Ad = m.Ad };
    }

    public async Task<AdKaydiDto> MahalleGuncelleAsync(long id, AdKaydiIstegi istek)
    {
        var m = await _context.Mahalleler.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli mahalle bulunamadı.");

        if (await _context.Mahalleler.AnyAsync(x => x.Id != id && x.Ad.ToLower() == istek.Ad.ToLower()))
        {
            throw new BusinessRuleException($"\"{istek.Ad}\" mahallesi zaten kayıtlı.");
        }

        m.Ad = istek.Ad;
        await _context.SaveChangesAsync();
        _onbellek.Remove(CacheKeys.Mahalle);

        var sayi = await _context.Randevular.IgnoreQueryFilters().CountAsync(r => r.MahalleId == id);
        return new AdKaydiDto { Id = m.Id, Ad = m.Ad, KullanimSayisi = sayi };
    }

    public async Task MahalleSilAsync(long id)
    {
        var m = await _context.Mahalleler.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli mahalle bulunamadı.");

        var sayi = await _context.Randevular.IgnoreQueryFilters().CountAsync(r => r.MahalleId == id);
        if (sayi > 0)
        {
            throw new BusinessRuleException($"Bu mahalle {sayi} talepte kullanılıyor; silinemez.");
        }

        _context.Mahalleler.Remove(m);
        await _context.SaveChangesAsync();
        _onbellek.Remove(CacheKeys.Mahalle);
    }

    public async Task<TopluIceAktarmaSonucu> MahalleIceAktarAsync(TopluIceAktarmaIstegi istek)
    {
        var adlar = SatirlariTemizle(istek.Satirlar);

        var mevcut = istek.KopyalariAtla
            ? (await _context.Mahalleler.Select(m => m.Ad.ToLower()).ToListAsync()).ToHashSet()
            : [];

        var eklenecek = new List<Mahalle>();
        var atlanan = 0;

        foreach (var ad in adlar)
        {
            if (istek.KopyalariAtla && !mevcut.Add(ad.ToLowerInvariant()))
            {
                atlanan++;
                continue;
            }
            eklenecek.Add(new Mahalle { Ad = ad });
        }

        if (eklenecek.Count > 0)
        {
            _context.Mahalleler.AddRange(eklenecek);
            await _context.SaveChangesAsync();
            _onbellek.Remove(CacheKeys.Mahalle);
        }

        return new TopluIceAktarmaSonucu
        {
            OkunanSatir = istek.Satirlar.Count,
            Eklenen = eklenecek.Count,
            Atlanan = atlanan + (istek.Satirlar.Count - adlar.Count),
            Mesaj = $"{eklenecek.Count} mahalle eklendi, {atlanan} kopya atlandı.",
        };
    }

    public async Task<int> MahalleTumunuSilAsync()
    {
        // Kullanımda olan mahalleleri silmek talepleri sahipsiz bırakır.
        if (await _context.Randevular.IgnoreQueryFilters().AnyAsync(r => r.MahalleId != null))
        {
            throw new BusinessRuleException(
                "Taleplerde kullanılan mahalleler var; tümünü silmek talepleri bozar. " +
                "Kullanılmayan mahalleleri tek tek silin.");
        }

        var silinen = await _context.Mahalleler.ExecuteDeleteAsync();
        _onbellek.Remove(CacheKeys.Mahalle);
        _logger.LogWarning("TÜM mahalleler silindi: {Adet} kayıt", silinen);
        return silinen;
    }

    // ─────────────────────────────────────────────────────── meslek

    public Task<SayfaliSonuc<AdKaydiDto>> MesleklerAsync(SayfaIstegi istek)
    {
        var ara = istek.TemizArama;
        return _context.Meslekler
            .AsNoTracking()
            .Where(m => ara == null || EF.Functions.ILike(m.Ad, $"%{ara}%"))
            .OrderBy(m => m.Ad)
            .Select(m => new AdKaydiDto { Id = m.Id, Ad = m.Ad })
            .SayfalaAsync(istek);
    }

    public async Task<AdKaydiDto> MeslekOlusturAsync(AdKaydiIstegi istek)
    {
        if (await _context.Meslekler.AnyAsync(m => m.Ad.ToLower() == istek.Ad.ToLower()))
        {
            throw new BusinessRuleException($"\"{istek.Ad}\" mesleği zaten kayıtlı.");
        }

        var m = new Meslek { Ad = istek.Ad };
        _context.Meslekler.Add(m);
        await _context.SaveChangesAsync();
        _onbellek.Remove(CacheKeys.Meslek);

        return new AdKaydiDto { Id = m.Id, Ad = m.Ad };
    }

    public async Task<AdKaydiDto> MeslekGuncelleAsync(long id, AdKaydiIstegi istek)
    {
        var m = await _context.Meslekler.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli meslek bulunamadı.");

        if (await _context.Meslekler.AnyAsync(x => x.Id != id && x.Ad.ToLower() == istek.Ad.ToLower()))
        {
            throw new BusinessRuleException($"\"{istek.Ad}\" mesleği zaten kayıtlı.");
        }

        m.Ad = istek.Ad;
        await _context.SaveChangesAsync();
        _onbellek.Remove(CacheKeys.Meslek);

        return new AdKaydiDto { Id = m.Id, Ad = m.Ad };
    }

    public async Task MeslekSilAsync(long id)
    {
        var m = await _context.Meslekler.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli meslek bulunamadı.");

        // `Randevu.Meslek` bir METİN alanı, yabancı anahtar DEĞİL — meslek
        // kaydını silmek eski talepleri bozmaz, yalnızca listeden düşer.
        _context.Meslekler.Remove(m);
        await _context.SaveChangesAsync();
        _onbellek.Remove(CacheKeys.Meslek);
    }

    public async Task<TopluIceAktarmaSonucu> MeslekIceAktarAsync(TopluIceAktarmaIstegi istek)
    {
        var adlar = SatirlariTemizle(istek.Satirlar);

        var mevcut = istek.KopyalariAtla
            ? (await _context.Meslekler.Select(m => m.Ad.ToLower()).ToListAsync()).ToHashSet()
            : [];

        var eklenecek = new List<Meslek>();
        var atlanan = 0;

        foreach (var ad in adlar)
        {
            if (istek.KopyalariAtla && !mevcut.Add(ad.ToLowerInvariant()))
            {
                atlanan++;
                continue;
            }
            eklenecek.Add(new Meslek { Ad = ad });
        }

        if (eklenecek.Count > 0)
        {
            _context.Meslekler.AddRange(eklenecek);
            await _context.SaveChangesAsync();
            _onbellek.Remove(CacheKeys.Meslek);
        }

        return new TopluIceAktarmaSonucu
        {
            OkunanSatir = istek.Satirlar.Count,
            Eklenen = eklenecek.Count,
            Atlanan = atlanan + (istek.Satirlar.Count - adlar.Count),
            Mesaj = $"{eklenecek.Count} meslek eklendi, {atlanan} kopya atlandı.",
        };
    }

    public async Task<int> MeslekTumunuSilAsync()
    {
        var silinen = await _context.Meslekler.ExecuteDeleteAsync();
        _onbellek.Remove(CacheKeys.Meslek);
        _logger.LogWarning("TÜM meslekler silindi: {Adet} kayıt", silinen);
        return silinen;
    }

    // ─────────────────────────────────────────────────────── yardımcı

    /// <summary>Boş satırları atar, kırpar ve aynı istekteki kopyaları teker.</summary>
    private static List<string> SatirlariTemizle(IEnumerable<string> satirlar) =>
        satirlar
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private async Task AdCakismasiVarMiAsync(TanimTuru tur, string ad, long? haricId)
    {
        var carpisti = tur switch
        {
            TanimTuru.EtkinlikTipi => await _context.RandevuTipleri
                .AnyAsync(t => t.Id != haricId && t.Ad.ToLower() == ad.ToLower()),
            TanimTuru.EtkinlikDurumu => await _context.AjandaDurumlar
                .AnyAsync(d => d.Id != haricId && d.Ad.ToLower() == ad.ToLower()),
            TanimTuru.TalepDurumu => await _context.RandevuDurumlar
                .AnyAsync(d => d.Id != haricId && d.DurumAd.ToLower() == ad.ToLower()),
            _ => false,
        };

        if (carpisti)
        {
            throw new BusinessRuleException($"\"{ad}\" adında bir tanım zaten var.");
        }
    }

    private async Task<int> KullanimSayisiAsync(TanimTuru tur, long id) => tur switch
    {
        // `IgnoreQueryFilters`: silinmiş etkinlikler de bu tanıma bağlıdır;
        // tanımı silmek onların satırlarını da bozar.
        TanimTuru.EtkinlikTipi =>
            await _context.Ajandalar.IgnoreQueryFilters().CountAsync(a => a.RandevuTipId == id) +
            await _context.Randevular.IgnoreQueryFilters().CountAsync(r => r.RandevuTipId == id),
        TanimTuru.EtkinlikDurumu =>
            await _context.Ajandalar.IgnoreQueryFilters().CountAsync(a => a.DurumId == id),
        TanimTuru.TalepDurumu =>
            await _context.Randevular.IgnoreQueryFilters().CountAsync(r => r.RandevuDurumId == id),
        _ => 0,
    };

    private void OnbellegiTemizle(TanimTuru tur)
    {
        // SettingsService bu listeleri önbellekte tutuyor; temizlemezsek ESKİ
        // uygulama bayat liste gösterir.
        _onbellek.Remove(tur switch
        {
            TanimTuru.EtkinlikTipi => CacheKeys.RandevuTip,
            TanimTuru.EtkinlikDurumu => CacheKeys.AjandaDurum,
            TanimTuru.TalepDurumu => CacheKeys.RandevuDurum,
            _ => string.Empty,
        });
    }
}
