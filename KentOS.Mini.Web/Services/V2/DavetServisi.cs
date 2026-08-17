using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>Davet listesi özeti.</summary>
public class DavetOzetDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("baslik")] public string Baslik { get; set; } = string.Empty;
    [JsonPropertyName("tarih")] public DateTime? Tarih { get; set; }
    [JsonPropertyName("yer")] public string? Yer { get; set; }
    [JsonPropertyName("ajandaId")] public long? AjandaId { get; set; }

    [JsonPropertyName("kisiSayisi")] public int KisiSayisi { get; set; }
    [JsonPropertyName("katilacak")] public int Katilacak { get; set; }
    [JsonPropertyName("katilmayacak")] public int Katilmayacak { get; set; }
    [JsonPropertyName("beklemede")] public int Beklemede { get; set; }
    [JsonPropertyName("arandi")] public int Arandi { get; set; }
}

/// <summary>Davet listesindeki kişi.</summary>
public class DavetKisiDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("protokolId")] public long ProtokolId { get; set; }
    [JsonPropertyName("adSoyad")] public string AdSoyad { get; set; } = string.Empty;
    [JsonPropertyName("unvan")] public string? Unvan { get; set; }
    [JsonPropertyName("kurum")] public string? Kurum { get; set; }
    [JsonPropertyName("kategori")] public string Kategori { get; set; } = string.Empty;
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("cepTelefon")] public string? CepTelefon { get; set; }

    [JsonPropertyName("durum")] public DavetDurumu Durum { get; set; }
    [JsonPropertyName("arandi")] public bool Arandi { get; set; }
    [JsonPropertyName("mesajGonderildi")] public bool MesajGonderildi { get; set; }
    [JsonPropertyName("not")] public string? Not { get; set; }
    [JsonPropertyName("siraNo")] public int SiraNo { get; set; }
}

/// <summary>Davet detayı.</summary>
public class DavetDetayDto : DavetOzetDto
{
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("kisiler")] public List<DavetKisiDto> Kisiler { get; set; } = [];
}

/// <summary>Davet oluşturma/güncelleme.</summary>
public class DavetIstegi
{
    [JsonPropertyName("baslik")] public string Baslik { get; set; } = string.Empty;
    [JsonPropertyName("tarih")] public DateTime? Tarih { get; set; }
    [JsonPropertyName("yer")] public string? Yer { get; set; }
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("ajandaId")] public long? AjandaId { get; set; }
}

/// <summary>Davete kişi ekleme — protokol kimlikleriyle.</summary>
public class DavetKisiEkleIstegi
{
    [JsonPropertyName("protokolIdler")] public List<long> ProtokolIdler { get; set; } = [];

    /// <summary>
    /// Verilirse o kategorideki TÜM aktif protokol kayıtları eklenir.
    /// </summary>
    /// <remarks>
    /// Davetler çoğu zaman "mülki idarenin tamamı" gibi kurulur; tek tek
    /// seçtirmek yüz satırlık listede kullanılmaz bir akış olurdu.
    /// </remarks>
    [JsonPropertyName("kategoriId")] public long? KategoriId { get; set; }
}

/// <summary>Kişi takip bilgisini günceller.</summary>
public class DavetKisiGuncelleIstegi
{
    [JsonPropertyName("durum")] public DavetDurumu? Durum { get; set; }
    [JsonPropertyName("arandi")] public bool? Arandi { get; set; }
    [JsonPropertyName("mesajGonderildi")] public bool? MesajGonderildi { get; set; }
    [JsonPropertyName("not")] public string? Not { get; set; }
}

/// <summary>Davet listesi süzgeci.</summary>
public class DavetSuzgeci : SayfaIstegi
{
    [JsonPropertyName("gecmisDahil")] public bool GecmisDahil { get; set; }
}

public interface IDavetServisi
{
    Task<SayfaliSonuc<DavetOzetDto>> ListeAsync(DavetSuzgeci suzgec);
    Task<DavetDetayDto> DetayAsync(long id);
    Task<DavetDetayDto> OlusturAsync(DavetIstegi istek);
    Task<DavetDetayDto> GuncelleAsync(long id, DavetIstegi istek);
    Task SilAsync(long id);
    Task<DavetDetayDto> KisiEkleAsync(long davetId, DavetKisiEkleIstegi istek);
    Task<DavetKisiDto> KisiGuncelleAsync(long davetId, long kisiId, DavetKisiGuncelleIstegi istek);
    Task KisiCikarAsync(long davetId, long kisiId);
}

/// <summary>
/// Davet listeleri.
///
/// <para>
/// Protokol listesi <b>kimin nerede durduğunu</b>, davet listesi <b>kimin
/// çağrıldığını ve ne cevap verdiğini</b> tutar. Kişi bilgisi protokol
/// kaydından OKUNUR, kopyalanmaz: unvan değişirse davet listesi de güncel
/// kalır.
/// </para>
///
/// <para>
/// <b>Birim izolasyonu:</b> davetler oluşturan kullanıcının birimine ait ve
/// yalnızca o birim görür — etkinliklerdeki kuralla aynı.
/// </para>
/// </summary>
public class DavetServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici) : IDavetServisi
{
    /// <summary>GÖRÜNÜRLÜK KAPISI — her sorgu buradan başlamalı.</summary>
    private IQueryable<Davet> GorunurOlanlar() =>
        _context.Davetler
            .AsNoTracking()
            .Where(d => d.BirimId == _kullanici.GetCurrentBirimId());

    public Task<SayfaliSonuc<DavetOzetDto>> ListeAsync(DavetSuzgeci suzgec)
    {
        var sorgu = GorunurOlanlar();

        if (!suzgec.GecmisDahil)
        {
            // Tarihi geçmiş davetler varsayılan olarak gizli: liste her yıl
            // birikiyor ve kullanıcının işi güncel olanla.
            var sinir = DateTime.Now.Date;
            sorgu = sorgu.Where(d => d.Tarih == null || d.Tarih >= sinir);
        }

        if (suzgec.TemizArama is { } ara)
        {
            var k = $"%{ara}%";
            sorgu = sorgu.Where(d =>
                EF.Functions.ILike(d.Baslik, k) ||
                (d.Yer != null && EF.Functions.ILike(d.Yer, k)));
        }

        return sorgu
            .OrderByDescending(d => d.Tarih ?? d.OlusturmaTarihi)
            .Select(d => new DavetOzetDto
            {
                Id = d.Id,
                Baslik = d.Baslik,
                Tarih = d.Tarih,
                Yer = d.Yer,
                AjandaId = d.AjandaId,
                KisiSayisi = d.Kisiler.Count,
                Katilacak = d.Kisiler.Count(k => k.Durum == DavetDurumu.Katilacak),
                Katilmayacak = d.Kisiler.Count(k => k.Durum == DavetDurumu.Katilmayacak),
                Beklemede = d.Kisiler.Count(k => k.Durum == DavetDurumu.Beklemede),
                Arandi = d.Kisiler.Count(k => k.Arandi),
            })
            .SayfalaAsync(suzgec);
    }

    public async Task<DavetDetayDto> DetayAsync(long id)
    {
        var d = await GorunurOlanlar()
            .Include(x => x.Kisiler).ThenInclude(k => k.Protokol).ThenInclude(p => p!.Kategori)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli davet bulunamadı.");

        return Cevir(d);
    }

    public async Task<DavetDetayDto> OlusturAsync(DavetIstegi istek)
    {
        if (string.IsNullOrWhiteSpace(istek.Baslik))
        {
            throw new BusinessRuleException("Davet başlığı boş olamaz.");
        }

        var d = new Davet
        {
            Baslik = istek.Baslik.Trim(),
            Tarih = istek.Tarih,
            Yer = Bosalt(istek.Yer),
            Aciklama = Bosalt(istek.Aciklama),
            AjandaId = istek.AjandaId,
            BirimId = _kullanici.GetCurrentBirimId(),
            KullaniciId = _kullanici.GetUsername(),
        };

        _context.Davetler.Add(d);
        await _context.SaveChangesAsync();
        return await DetayAsync(d.Id);
    }

    public async Task<DavetDetayDto> GuncelleAsync(long id, DavetIstegi istek)
    {
        var d = await BulAsync(id);

        d.Baslik = istek.Baslik.Trim();
        d.Tarih = istek.Tarih;
        d.Yer = Bosalt(istek.Yer);
        d.Aciklama = Bosalt(istek.Aciklama);
        d.AjandaId = istek.AjandaId;
        d.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();
        return await DetayAsync(id);
    }

    public async Task SilAsync(long id)
    {
        var d = await BulAsync(id);
        _context.Davetler.Remove(d);
        await _context.SaveChangesAsync();
    }

    public async Task<DavetDetayDto> KisiEkleAsync(long davetId, DavetKisiEkleIstegi istek)
    {
        await BulAsync(davetId);

        var idler = istek.ProtokolIdler.ToList();

        if (istek.KategoriId is { } kategori)
        {
            var kategoridekiler = await _context.Protokoller
                .Where(p => p.KategoriId == kategori && p.Aktif)
                .Select(p => p.Id)
                .ToListAsync();
            idler.AddRange(kategoridekiler);
        }

        idler = idler.Distinct().ToList();
        if (idler.Count == 0) return await DetayAsync(davetId);

        // Zaten ekli olanlar atlanır: benzersizlik kısıtı hata fırlatırdı ve
        // "tümünü ekle" ikinci kez basıldığında işlem düşerdi.
        var mevcut = await _context.DavetKisileri
            .Where(k => k.DavetId == davetId)
            .Select(k => k.ProtokolId)
            .ToListAsync();

        var eklenecek = idler.Except(mevcut).ToList();
        if (eklenecek.Count == 0) return await DetayAsync(davetId);

        var protokoller = await _context.Protokoller
            .Where(p => eklenecek.Contains(p.Id))
            .Select(p => new { p.Id, p.SiraNo })
            .ToListAsync();

        foreach (var p in protokoller)
        {
            _context.DavetKisileri.Add(new DavetKisi
            {
                DavetId = davetId,
                ProtokolId = p.Id,
                SiraNo = p.SiraNo,
            });
        }

        await _context.SaveChangesAsync();
        return await DetayAsync(davetId);
    }

    public async Task<DavetKisiDto> KisiGuncelleAsync(
        long davetId, long kisiId, DavetKisiGuncelleIstegi istek)
    {
        await BulAsync(davetId);

        var k = await _context.DavetKisileri
            .Include(x => x.Protokol).ThenInclude(p => p!.Kategori)
            .FirstOrDefaultAsync(x => x.Id == kisiId && x.DavetId == davetId)
            ?? throw new EntityNotFoundException("Davet kaydı bulunamadı.");

        if (istek.Durum is { } durum) k.Durum = durum;
        if (istek.Not is not null) k.Not = Bosalt(istek.Not);

        // Arama/mesaj TARİHİ, işaret ilk kez konduğunda damgalanır; her
        // güncellemede yenilenirse "ne zaman arandı" bilgisi kaybolur.
        if (istek.Arandi is { } arandi)
        {
            if (arandi && !k.Arandi) k.ArandiTarihi = DateTime.Now;
            if (!arandi) k.ArandiTarihi = null;
            k.Arandi = arandi;
        }

        if (istek.MesajGonderildi is { } mesaj)
        {
            if (mesaj && !k.MesajGonderildi) k.MesajTarihi = DateTime.Now;
            if (!mesaj) k.MesajTarihi = null;
            k.MesajGonderildi = mesaj;
        }

        k.GuncellemeTarihi = DateTime.Now;
        await _context.SaveChangesAsync();

        return KisiCevir(k);
    }

    public async Task KisiCikarAsync(long davetId, long kisiId)
    {
        await BulAsync(davetId);

        var silinen = await _context.DavetKisileri
            .Where(k => k.Id == kisiId && k.DavetId == davetId)
            .ExecuteDeleteAsync();

        if (silinen == 0)
        {
            throw new EntityNotFoundException("Davet kaydı bulunamadı.");
        }
    }

    /// <summary>Yazma yolları için — görünürlük kapısından geçer.</summary>
    private async Task<Davet> BulAsync(long id) =>
        await _context.Davetler
            .Where(d => d.BirimId == _kullanici.GetCurrentBirimId())
            .FirstOrDefaultAsync(d => d.Id == id)
        ?? throw new EntityNotFoundException($"{id} kimlikli davet bulunamadı.");

    private static string? Bosalt(string? m) => string.IsNullOrWhiteSpace(m) ? null : m.Trim();

    private static DavetDetayDto Cevir(Davet d) => new()
    {
        Id = d.Id,
        Baslik = d.Baslik,
        Tarih = d.Tarih,
        Yer = d.Yer,
        Aciklama = d.Aciklama,
        AjandaId = d.AjandaId,
        KisiSayisi = d.Kisiler.Count,
        Katilacak = d.Kisiler.Count(k => k.Durum == DavetDurumu.Katilacak),
        Katilmayacak = d.Kisiler.Count(k => k.Durum == DavetDurumu.Katilmayacak),
        Beklemede = d.Kisiler.Count(k => k.Durum == DavetDurumu.Beklemede),
        Arandi = d.Kisiler.Count(k => k.Arandi),
        Kisiler = d.Kisiler
            .OrderBy(k => k.Protokol!.Kategori!.SiraNo)
            .ThenBy(k => k.SiraNo)
            .ThenBy(k => k.Protokol!.AdSoyad)
            .Select(KisiCevir)
            .ToList(),
    };

    private static DavetKisiDto KisiCevir(DavetKisi k) => new()
    {
        Id = k.Id,
        ProtokolId = k.ProtokolId,
        AdSoyad = k.Protokol?.AdSoyad ?? string.Empty,
        Unvan = k.Protokol?.Unvan,
        Kurum = k.Protokol?.Kurum,
        Kategori = k.Protokol?.Kategori?.Ad ?? string.Empty,
        Telefon = k.Protokol?.Telefon,
        CepTelefon = k.Protokol?.CepTelefon,
        Durum = k.Durum,
        Arandi = k.Arandi,
        MesajGonderildi = k.MesajGonderildi,
        Not = k.Not,
        SiraNo = k.SiraNo,
    };
}
