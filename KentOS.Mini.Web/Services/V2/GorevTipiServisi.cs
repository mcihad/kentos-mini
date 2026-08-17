using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.V2.IsTakip;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>
/// GÖREV TİPİ TANIMI — hizmet standardının kurulduğu yer.
/// </summary>
/// <remarks>
/// <para>
/// Tip bir etiket değil bir <b>sözleşme</b>: kaç aşamadan geçileceğini, her
/// aşamada ne kanıt isteneceğini ve işin kaç saatte bitmesi gerektiğini
/// söylüyor. Görev açılırken bunların hepsi kopyalanıyor.
/// </para>
/// <para>
/// <b>Tipi hangi birimlerin kullanabileceği</b> ayrı bir listede. Boş liste
/// "herkes kullanabilir" demek — kurum geneli tipler (şikayet, talep) için
/// her birimi tek tek işaretlemek zorunda kalmamak adına.
/// </para>
/// </remarks>
public interface IGorevTipiServisi
{
    Task<SayfaliSonuc<GorevTipiDto>> ListeAsync(SayfaIstegi istek, bool yalnizKullanimda,
        CancellationToken iptal = default);

    /// <summary>Etkin birimin KULLANABİLECEĞİ tipler — görev açma ekranı için.</summary>
    Task<List<GorevTipiDto>> KullanilabilirlerAsync(CancellationToken iptal = default);

    Task<GorevTipiDto> GetirAsync(long id, CancellationToken iptal = default);
    Task<GorevTipiDto> OlusturAsync(GorevTipiKayitDto istek, CancellationToken iptal = default);
    Task<GorevTipiDto> GuncelleAsync(long id, GorevTipiKayitDto istek, CancellationToken iptal = default);
    Task SilAsync(long id, CancellationToken iptal = default);
}

public class GorevTipiServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici,
    IEtkinBirim _etkinBirim) : IGorevTipiServisi
{
    public async Task<SayfaliSonuc<GorevTipiDto>> ListeAsync(
        SayfaIstegi istek, bool yalnizKullanimda, CancellationToken iptal = default)
    {
        var sorgu = _context.GorevTipleri.AsNoTracking();

        if (yalnizKullanimda)
            sorgu = sorgu.Where(t => t.Kullanimda);

        if (istek.TemizArama is { } ara)
            sorgu = sorgu.Where(t => EF.Functions.ILike(t.Ad, $"%{ara}%"));

        var toplam = await sorgu.LongCountAsync(iptal);

        var idler = await sorgu
            .OrderBy(t => t.Ad)
            .Skip(istek.Atla)
            .Take(istek.Boyut)
            .Select(t => t.Id)
            .ToListAsync(iptal);

        var veriler = await YukleAsync(idler, iptal);

        return SayfaliSonuc<GorevTipiDto>.Olustur(veriler, toplam, istek);
    }

    public async Task<List<GorevTipiDto>> KullanilabilirlerAsync(CancellationToken iptal = default)
    {
        var birim = await _etkinBirim.IdAsync(iptal);

        // Birim listesi BOŞ olan tip herkese açık. "Kurum geneli" için ayrı
        // bir bayrak açmak yerine boş liste bu anlama geliyor: yeni bir tip
        // varsayılan olarak herkesin kullanabileceği tip olsun diye.
        var idler = await _context.GorevTipleri
            .AsNoTracking()
            .Where(t => t.Kullanimda)
            .Where(t => !t.Birimler.Any() || t.Birimler.Any(b => b.BirimId == birim))
            .OrderBy(t => t.Ad)
            .Select(t => t.Id)
            .ToListAsync(iptal);

        return await YukleAsync(idler, iptal);
    }

    public async Task<GorevTipiDto> GetirAsync(long id, CancellationToken iptal = default)
    {
        var liste = await YukleAsync([id], iptal);
        return liste.FirstOrDefault()
            ?? throw new EntityNotFoundException("Görev tipi bulunamadı.");
    }

    public async Task<GorevTipiDto> OlusturAsync(
        GorevTipiKayitDto istek, CancellationToken iptal = default)
    {
        await AdTekilMiAsync(istek.Ad, null, iptal);

        var tip = new TaskType
        {
            Ad = istek.Ad.Trim(),
            Aciklama = istek.Aciklama,
            Renk = istek.Renk,
            HizmetStandardiGun = istek.HizmetStandardiGun,
            SlaSaat = istek.SlaSaat,
            VarsayilanOncelik = istek.VarsayilanOncelik,
            KonumZorunlu = istek.KonumZorunlu,
            Kullanimda = istek.Kullanimda,
            BirimId = await _etkinBirim.IdAsync(iptal) is var b && b > 0 ? b : null,
            Olusturan = await _kullanici.GetFullNameAsync(),
            OlusturmaTarihi = DateTime.Now,
        };

        _context.GorevTipleri.Add(tip);
        await _context.SaveChangesAsync(iptal);

        await AltKayitlariYazAsync(tip.Id, istek, iptal);

        return await GetirAsync(tip.Id, iptal);
    }

    public async Task<GorevTipiDto> GuncelleAsync(
        long id, GorevTipiKayitDto istek, CancellationToken iptal = default)
    {
        var tip = await _context.GorevTipleri.FirstOrDefaultAsync(t => t.Id == id, iptal)
            ?? throw new EntityNotFoundException("Görev tipi bulunamadı.");

        await AdTekilMiAsync(istek.Ad, id, iptal);

        tip.Ad = istek.Ad.Trim();
        tip.Aciklama = istek.Aciklama;
        tip.Renk = istek.Renk;
        tip.HizmetStandardiGun = istek.HizmetStandardiGun;
        tip.SlaSaat = istek.SlaSaat;
        tip.VarsayilanOncelik = istek.VarsayilanOncelik;
        tip.KonumZorunlu = istek.KonumZorunlu;
        tip.Kullanimda = istek.Kullanimda;
        tip.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync(iptal);
        await AltKayitlariYazAsync(id, istek, iptal);

        return await GetirAsync(id, iptal);
    }

    /// <summary>
    /// Tipi siler — YALNIZCA hiç görev açılmamışsa.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Kullanılmış bir tipi silmek, o tiple açılmış görevlerin tip adını
    /// kaybettirirdi. Aşamalar zaten kopyalandığı için görev çalışmaya devam
    /// ederdi ama "bu iş hangi hizmet standardına göre ölçüldü?" sorusunun
    /// cevabı kaybolurdu.
    /// </para>
    /// <para>
    /// Kullanımdan kaldırmak için <c>Kullanimda = false</c> — tip yeni
    /// görevlerde seçilemez, eskiler olduğu gibi kalır.
    /// </para>
    /// </remarks>
    public async Task SilAsync(long id, CancellationToken iptal = default)
    {
        var tip = await _context.GorevTipleri.FirstOrDefaultAsync(t => t.Id == id, iptal)
            ?? throw new EntityNotFoundException("Görev tipi bulunamadı.");

        var gorevSayisi = await _context.Gorevler.CountAsync(g => g.GorevTipiId == id, iptal);
        if (gorevSayisi > 0)
        {
            throw new BusinessRuleException(
                $"Bu tiple açılmış {gorevSayisi} görev var; tip silinemez. " +
                "Yeni görevlerde seçilmesini istemiyorsanız KULLANIMDAN KALDIRIN.");
        }

        // Alt kayıtlarda cascade var (aynı tabloya bağlılar); yine de açıkça
        // siliniyor ki davranış yapılandırmaya bağlı kalmasın.
        await _context.GorevTipiAsamalari.Where(a => a.GorevTipiId == id).ExecuteDeleteAsync(iptal);
        await _context.GorevTipiBirimleri.Where(b => b.GorevTipiId == id).ExecuteDeleteAsync(iptal);
        await _context.GorevTipiDevirleri.Where(d => d.GorevTipiId == id).ExecuteDeleteAsync(iptal);

        // Tip de `ExecuteDelete` ile gidiyor, `Remove` + `SaveChanges` ile
        // DEĞİL. Sebebi ölçüldü: aynı bağlamda daha önce aşamalar izlenmişse
        // `Remove` onlar için ikinci bir DELETE üretiyor, satırlar yukarıda
        // zaten silindiği için 0 satır etkileniyor ve EF bunu eşzamanlılık
        // çakışması sanıp istisna atıyor.
        _context.Entry(tip).State = EntityState.Detached;
        await _context.GorevTipleri.Where(t => t.Id == id).ExecuteDeleteAsync(iptal);
    }

    // ── iç ─────────────────────────────────────────────────────────────

    private async Task AdTekilMiAsync(string ad, long? haricId, CancellationToken iptal)
    {
        var temiz = ad.Trim();
        var carpisma = await _context.GorevTipleri
            .AnyAsync(t => t.Ad.ToLower() == temiz.ToLower() && (haricId == null || t.Id != haricId), iptal);

        if (carpisma)
            throw new BusinessRuleException($"\"{temiz}\" adında bir görev tipi zaten var.");
    }

    /// <summary>
    /// Aşama, birim ve devir listelerini TAM LİSTE olarak yazar.
    /// </summary>
    /// <remarks>
    /// Sil-ve-yeniden-yaz. Tek tek eşleştirmek, arayüzün gönderdiği sırayı
    /// korumak için yine de her satıra dokunmayı gerektirirdi; kazanç
    /// yalnızca kimliklerin korunması olurdu ve bu kimlikler hiçbir yerde
    /// referans edilmiyor (görev aşamaları KOPYA taşıyor, bağ değil).
    /// </remarks>
    private async Task AltKayitlariYazAsync(
        long tipId, GorevTipiKayitDto istek, CancellationToken iptal)
    {
        await _context.GorevTipiAsamalari.Where(a => a.GorevTipiId == tipId).ExecuteDeleteAsync(iptal);
        await _context.GorevTipiBirimleri.Where(b => b.GorevTipiId == tipId).ExecuteDeleteAsync(iptal);
        await _context.GorevTipiDevirleri.Where(d => d.GorevTipiId == tipId).ExecuteDeleteAsync(iptal);

        // Sıra numarası istemciden GELDİĞİ GİBİ alınmıyor: arayüz sürükle-bırak
        // sonrası 1,2,5 gibi boşluklu ya da çakışan değerler gönderebilir.
        // Sunucu listedeki sıraya göre yeniden numaralandırıyor.
        var sira = 1;
        foreach (var a in istek.Asamalar)
        {
            _context.GorevTipiAsamalari.Add(new TaskTypeStage
            {
                GorevTipiId = tipId,
                SiraNo = sira++,
                Ad = a.Ad.Trim(),
                Aciklama = a.Aciklama,
                Zorunlu = a.Zorunlu,
                AciklamaZorunlu = a.AciklamaZorunlu,
                FotografZorunlu = a.FotografZorunlu,
                TahminiSaat = a.TahminiSaat,
            });
        }

        foreach (var birimId in istek.BirimIdler.Distinct())
            _context.GorevTipiBirimleri.Add(new TaskTypeUnit { GorevTipiId = tipId, BirimId = birimId });

        foreach (var d in istek.Devirler)
        {
            _context.GorevTipiDevirleri.Add(new TaskTypeHandoff
            {
                GorevTipiId = tipId,
                HedefBirimId = d.HedefBirimId,
                IsTalebi = d.IsTalebi,
                Not = d.Not,
                HedefGorevTipiId = d.HedefGorevTipiId,
            });
        }

        await _context.SaveChangesAsync(iptal);
    }

    /// <summary>
    /// Tipleri alt kayıtlarıyla yükler — sayfa başına SABİT sayıda sorgu.
    /// </summary>
    /// <remarks>
    /// Alt listeler `Include` ile değil ayrı sorgularla çekiliyor: üç ayrı
    /// koleksiyonu tek sorguda birleştirmek kartezyen çarpım üretir ve 20
    /// tiplik bir sayfa yüzlerce satıra şişerdi.
    /// </remarks>
    private async Task<List<GorevTipiDto>> YukleAsync(
        List<long> idler, CancellationToken iptal)
    {
        if (idler.Count == 0) return [];

        var tipler = await _context.GorevTipleri
            .AsNoTracking()
            .Where(t => idler.Contains(t.Id))
            .Select(t => new
            {
                t.Id, t.Ad, t.Aciklama, t.Renk, t.HizmetStandardiGun, t.SlaSaat,
                t.VarsayilanOncelik, t.KonumZorunlu, t.Kullanimda, t.BirimId,
                BirimAd = t.Birim != null ? t.Birim.Ad : null,
            })
            .ToListAsync(iptal);

        var asamalar = await _context.GorevTipiAsamalari
            .AsNoTracking()
            .Where(a => idler.Contains(a.GorevTipiId))
            .OrderBy(a => a.SiraNo)
            .ToListAsync(iptal);

        var birimler = await _context.GorevTipiBirimleri
            .AsNoTracking()
            .Where(b => idler.Contains(b.GorevTipiId))
            .Select(b => new { b.GorevTipiId, b.BirimId })
            .ToListAsync(iptal);

        var devirler = await _context.GorevTipiDevirleri
            .AsNoTracking()
            .Where(d => idler.Contains(d.GorevTipiId))
            .Select(d => new
            {
                d.Id, d.GorevTipiId, d.HedefBirimId, d.IsTalebi, d.Not, d.HedefGorevTipiId,
                HedefBirimAd = d.HedefBirim != null ? d.HedefBirim.Ad : null,
            })
            .ToListAsync(iptal);

        var sayilar = await _context.Gorevler
            .AsNoTracking()
            .Where(g => g.GorevTipiId != null && idler.Contains(g.GorevTipiId.Value))
            .GroupBy(g => g.GorevTipiId!.Value)
            .Select(g => new { TipId = g.Key, Sayi = g.Count() })
            .ToDictionaryAsync(x => x.TipId, x => x.Sayi, iptal);

        // Sıra `idler`'den geliyor: çağıran zaten sıralamıştı, sözlükten
        // okumak o sırayı kaybettirirdi.
        return [.. idler
            .Select(id => tipler.FirstOrDefault(t => t.Id == id))
            .Where(t => t is not null)
            .Select(t => new GorevTipiDto
            {
                Id = t!.Id,
                Ad = t.Ad,
                Aciklama = t.Aciklama,
                Renk = t.Renk,
                HizmetStandardiGun = t.HizmetStandardiGun,
                SlaSaat = t.SlaSaat,
                VarsayilanOncelik = t.VarsayilanOncelik,
                VarsayilanOncelikAd = GorevDurumAkisi.OncelikAdi(t.VarsayilanOncelik),
                KonumZorunlu = t.KonumZorunlu,
                Kullanimda = t.Kullanimda,
                BirimId = t.BirimId,
                BirimAd = t.BirimAd,
                GorevSayisi = sayilar.TryGetValue(t.Id, out var s) ? s : 0,
                Asamalar = [.. asamalar
                    .Where(a => a.GorevTipiId == t.Id)
                    .Select(a => new GorevTipiAsamaDto
                    {
                        Id = a.Id,
                        SiraNo = a.SiraNo,
                        Ad = a.Ad,
                        Aciklama = a.Aciklama,
                        Zorunlu = a.Zorunlu,
                        AciklamaZorunlu = a.AciklamaZorunlu,
                        FotografZorunlu = a.FotografZorunlu,
                        TahminiSaat = a.TahminiSaat,
                    })],
                BirimIdler = [.. birimler.Where(b => b.GorevTipiId == t.Id).Select(b => b.BirimId)],
                Devirler = [.. devirler
                    .Where(d => d.GorevTipiId == t.Id)
                    .Select(d => new GorevTipiDevirDto
                    {
                        Id = d.Id,
                        HedefBirimId = d.HedefBirimId,
                        HedefBirimAd = d.HedefBirimAd,
                        IsTalebi = d.IsTalebi,
                        Not = d.Not,
                        HedefGorevTipiId = d.HedefGorevTipiId,
                    })],
            })];
    }
}
