using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>Protokol listesi kaydı.</summary>
public class ProtokolDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("kategoriId")] public long KategoriId { get; set; }
    [JsonPropertyName("kategori")] public string Kategori { get; set; } = string.Empty;
    [JsonPropertyName("kurum")] public string? Kurum { get; set; }
    [JsonPropertyName("adSoyad")] public string AdSoyad { get; set; } = string.Empty;
    [JsonPropertyName("unvan")] public string? Unvan { get; set; }
    [JsonPropertyName("siraNo")] public int SiraNo { get; set; }
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("cepTelefon")] public string? CepTelefon { get; set; }
    [JsonPropertyName("eposta")] public string? Eposta { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("aktif")] public bool Aktif { get; set; }
}

/// <summary>
/// Bir protokol kişisinin DAVET GEÇMİŞİ — hangi törene çağrıldı, ne cevap
/// verdi, ne not düşüldü.
/// </summary>
/// <remarks>
/// Protokol listesi "kim nerede durur"u söyler; bu ise "bu kişiyle daha önce
/// ne yaşadık" sorusunun cevabı. Telefonu elinde tutan kişi arama yapmadan
/// önce geçen sefer ne olduğunu bilmek istiyor — "geçen tören için de
/// aramıştık, gelemedi" bilgisi konuşmanın tonunu belirliyor.
/// </remarks>
public class ProtokolDavetGecmisiDto
{
    [JsonPropertyName("davetId")] public long DavetId { get; set; }
    [JsonPropertyName("baslik")] public string Baslik { get; set; } = string.Empty;
    [JsonPropertyName("tarih")] public DateTime? Tarih { get; set; }
    [JsonPropertyName("yer")] public string? Yer { get; set; }

    /// <summary>Cevap durumu (<c>DavetDurumu</c>).</summary>
    [JsonPropertyName("durum")] public int Durum { get; set; }
    [JsonPropertyName("durumAd")] public string DurumAd { get; set; } = string.Empty;

    [JsonPropertyName("arandi")] public bool Arandi { get; set; }
    [JsonPropertyName("arandiTarihi")] public DateTime? ArandiTarihi { get; set; }
    [JsonPropertyName("mesajGonderildi")] public bool MesajGonderildi { get; set; }
    [JsonPropertyName("mesajTarihi")] public DateTime? MesajTarihi { get; set; }

    [JsonPropertyName("not")] public string? Not { get; set; }
}

/// <summary>Protokol oluşturma/güncelleme isteği.</summary>
public class ProtokolIstegi
{
    [JsonPropertyName("kategoriId")] public long KategoriId { get; set; }
    [JsonPropertyName("kategori")] public string Kategori { get; set; } = string.Empty;
    [JsonPropertyName("kurum")] public string? Kurum { get; set; }
    [JsonPropertyName("adSoyad")] public string AdSoyad { get; set; } = string.Empty;
    [JsonPropertyName("unvan")] public string? Unvan { get; set; }
    [JsonPropertyName("siraNo")] public int SiraNo { get; set; }
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("cepTelefon")] public string? CepTelefon { get; set; }
    [JsonPropertyName("eposta")] public string? Eposta { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("aktif")] public bool Aktif { get; set; } = true;
}

/// <summary>Protokol listesi süzgeci.</summary>
public class ProtokolSuzgeci : SayfaIstegi
{
    [JsonPropertyName("kategoriId")] public long? KategoriId { get; set; }
    [JsonPropertyName("kurum")] public string? Kurum { get; set; }
    /// <summary>Pasif kayıtlar da gelsin mi (görevden ayrılanlar).</summary>
    [JsonPropertyName("pasifDahil")] public bool PasifDahil { get; set; }
}

/// <summary>Protokol kategorisi.</summary>
public class ProtokolKategoriDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("siraNo")] public int SiraNo { get; set; }
    [JsonPropertyName("aktif")] public bool Aktif { get; set; }
    [JsonPropertyName("adet")] public int Adet { get; set; }
}

/// <summary>Kategori oluşturma/güncelleme isteği.</summary>
public class ProtokolKategoriIstegi
{
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("siraNo")] public int SiraNo { get; set; }
    [JsonPropertyName("aktif")] public bool Aktif { get; set; } = true;
}

/// <summary>Kategori ya da kurum adı ve içindeki kayıt sayısı.</summary>
public class ProtokolGrubuDto
{
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("adet")] public int Adet { get; set; }
}

public interface IProtokolServisi
{
    Task<SayfaliSonuc<ProtokolDto>> ListeAsync(ProtokolSuzgeci suzgec);
    Task<ProtokolDto> GetirAsync(long id);
    Task<ProtokolDto> OlusturAsync(ProtokolIstegi istek);
    Task<ProtokolDto> GuncelleAsync(long id, ProtokolIstegi istek);
    Task SilAsync(long id);

    /// <summary>Var olan kategoriler — form önerileri ve süzgeç çipleri için.</summary>
    Task<List<ProtokolKategoriDto>> KategorilerAsync();
    Task<ProtokolKategoriDto> KategoriOlusturAsync(ProtokolKategoriIstegi istek);
    Task<ProtokolKategoriDto> KategoriGuncelleAsync(long id, ProtokolKategoriIstegi istek);
    Task KategoriSilAsync(long id);
    Task<List<ProtokolGrubuDto>> KurumlarAsync();

    /// <summary>Sıra numaralarını topluca yeniden yazar (sürükle-bırak sonrası).</summary>
    Task SiralamaGuncelleAsync(IReadOnlyList<(long Id, int SiraNo)> sira);

    /// <summary>Kişinin davet geçmişi — en yeni önce.</summary>
    Task<List<ProtokolDavetGecmisiDto>> DavetGecmisiAsync(long protokolId);
}

/// <summary>
/// İl protokol listesi.
///
/// <para>
/// Resmî törenlerde karşılama ve oturma düzeni protokol sırasına göre
/// kurulur; liste bu sırayı ve iletişim bilgilerini tutar. Yılda birkaç kez
/// elle güncellenen ~100 satırlık bir liste olduğu için kategori ve kurum
/// ayrı tablolar değil, metin alanlar.
/// </para>
/// </summary>
public class ProtokolServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici) : IProtokolServisi
{
    public Task<SayfaliSonuc<ProtokolDto>> ListeAsync(ProtokolSuzgeci suzgec)
    {
        var sorgu = _context.Protokoller.AsNoTracking().AsQueryable();

        if (!suzgec.PasifDahil) sorgu = sorgu.Where(p => p.Aktif);
        if (suzgec.KategoriId is { } kat) sorgu = sorgu.Where(p => p.KategoriId == kat);
        if (!string.IsNullOrWhiteSpace(suzgec.Kurum))
            sorgu = sorgu.Where(p => p.Kurum == suzgec.Kurum);

        if (suzgec.TemizArama is { } ara)
        {
            var k = $"%{ara}%";
            sorgu = sorgu.Where(p =>
                EF.Functions.ILike(p.AdSoyad, k) ||
                (p.Unvan != null && EF.Functions.ILike(p.Unvan, k)) ||
                (p.Kurum != null && EF.Functions.ILike(p.Kurum, k)) ||
                (p.Kategori != null && EF.Functions.ILike(p.Kategori.Ad, k)) ||
                (p.Telefon != null && EF.Functions.ILike(p.Telefon, k)) ||
                (p.CepTelefon != null && EF.Functions.ILike(p.CepTelefon, k)));
        }

        // Protokol SIRASI birincil ölçüt. Aynı numara birden fazla kayıtta
        // olabilir (eş seviyeli makamlar); eşitlikte kategori ve ad ayırır.
        // Kategori SIRASI birincil: protokolde mülki idare yerel yönetimden
        // önce gelir ve bu alfabetik değil, teamüle bağlı bir sıradır.
        return sorgu
            .OrderBy(p => p.Kategori!.SiraNo)
            .ThenBy(p => p.SiraNo)
            .ThenBy(p => p.AdSoyad)
            .Select(p => new ProtokolDto
            {
                Id = p.Id,
                KategoriId = p.KategoriId,
                Kategori = p.Kategori!.Ad,
                Kurum = p.Kurum,
                AdSoyad = p.AdSoyad,
                Unvan = p.Unvan,
                SiraNo = p.SiraNo,
                Telefon = p.Telefon,
                CepTelefon = p.CepTelefon,
                Eposta = p.Eposta,
                Adres = p.Adres,
                Aciklama = p.Aciklama,
                Aktif = p.Aktif,
            })
            .SayfalaAsync(suzgec);
    }

    public async Task<ProtokolDto> GetirAsync(long id)
    {
        var p = await _context.Protokoller
            .AsNoTracking()
            .Include(x => x.Kategori)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli protokol kaydı bulunamadı.");
        return Cevir(p);
    }

    public async Task<ProtokolDto> OlusturAsync(ProtokolIstegi istek)
    {
        await KategoriVarMiAsync(istek.KategoriId);

        var p = new Protokol
        {
            KategoriId = istek.KategoriId,
            Kurum = Bosalt(istek.Kurum),
            AdSoyad = istek.AdSoyad.Trim(),
            Unvan = Bosalt(istek.Unvan),
            SiraNo = istek.SiraNo > 0 ? istek.SiraNo : await SonrakiSiraAsync(),
            Telefon = Bosalt(istek.Telefon),
            CepTelefon = Bosalt(istek.CepTelefon),
            Eposta = Bosalt(istek.Eposta),
            Adres = Bosalt(istek.Adres),
            Aciklama = Bosalt(istek.Aciklama),
            Aktif = istek.Aktif,
        };

        _context.Protokoller.Add(p);
        await _context.SaveChangesAsync();
        return await GetirAsync(p.Id);
    }

    public async Task<ProtokolDto> GuncelleAsync(long id, ProtokolIstegi istek)
    {
        var p = await _context.Protokoller.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli protokol kaydı bulunamadı.");

        await KategoriVarMiAsync(istek.KategoriId);
        p.KategoriId = istek.KategoriId;
        p.Kurum = Bosalt(istek.Kurum);
        p.AdSoyad = istek.AdSoyad.Trim();
        p.Unvan = Bosalt(istek.Unvan);
        p.SiraNo = istek.SiraNo;
        p.Telefon = Bosalt(istek.Telefon);
        p.CepTelefon = Bosalt(istek.CepTelefon);
        p.Eposta = Bosalt(istek.Eposta);
        p.Adres = Bosalt(istek.Adres);
        p.Aciklama = Bosalt(istek.Aciklama);
        p.Aktif = istek.Aktif;
        p.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();
        return await GetirAsync(p.Id);
    }

    public async Task SilAsync(long id)
    {
        var silinen = await _context.Protokoller.Where(p => p.Id == id).ExecuteDeleteAsync();
        if (silinen == 0)
        {
            throw new EntityNotFoundException($"{id} kimlikli protokol kaydı bulunamadı.");
        }
    }

    /// <summary>
    /// Kategoriler — kayıt sayılarıyla.
    /// </summary>
    /// <remarks>
    /// Kayıt SAYISI SIFIR olan kategoriler de döner: yeni açılmış bir
    /// kategori listede görünmezse kullanıcı onu tekrar açmaya kalkıyor ve
    /// benzersizlik kısıtına takılıyordu.
    /// </remarks>
    public Task<List<ProtokolKategoriDto>> KategorilerAsync() =>
        _context.ProtokolKategorileri
            .AsNoTracking()
            .OrderBy(k => k.SiraNo)
            .ThenBy(k => k.Ad)
            .Select(k => new ProtokolKategoriDto
            {
                Id = k.Id,
                Ad = k.Ad,
                SiraNo = k.SiraNo,
                Aktif = k.Aktif,
                Adet = k.Protokoller.Count,
            })
            .ToListAsync();

    public async Task<ProtokolKategoriDto> KategoriOlusturAsync(ProtokolKategoriIstegi istek)
    {
        var ad = istek.Ad.Trim();
        if (string.IsNullOrWhiteSpace(ad))
        {
            throw new BusinessRuleException("Kategori adı boş olamaz.");
        }

        // Büyük/küçük harf duyarsız kontrol: kategoriyi tabloya almanın sebebi
        // "Mülki İdare" / "Mülki idare" ikilemini bitirmekti.
        if (await _context.ProtokolKategorileri.AnyAsync(k => k.Ad.ToLower() == ad.ToLower()))
        {
            throw new BusinessRuleException($"\"{ad}\" kategorisi zaten var.");
        }

        var k = new ProtokolKategori
        {
            Ad = ad,
            SiraNo = istek.SiraNo > 0
                ? istek.SiraNo
                : (await _context.ProtokolKategorileri.MaxAsync(x => (int?)x.SiraNo) ?? 0) + 1,
            Aktif = istek.Aktif,
        };

        _context.ProtokolKategorileri.Add(k);
        await _context.SaveChangesAsync();

        return new ProtokolKategoriDto { Id = k.Id, Ad = k.Ad, SiraNo = k.SiraNo, Aktif = k.Aktif };
    }

    public async Task<ProtokolKategoriDto> KategoriGuncelleAsync(long id, ProtokolKategoriIstegi istek)
    {
        var k = await _context.ProtokolKategorileri.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli kategori bulunamadı.");

        var ad = istek.Ad.Trim();
        if (await _context.ProtokolKategorileri.AnyAsync(x => x.Id != id && x.Ad.ToLower() == ad.ToLower()))
        {
            throw new BusinessRuleException($"\"{ad}\" kategorisi zaten var.");
        }

        k.Ad = ad;
        k.SiraNo = istek.SiraNo;
        k.Aktif = istek.Aktif;
        await _context.SaveChangesAsync();

        return new ProtokolKategoriDto { Id = k.Id, Ad = k.Ad, SiraNo = k.SiraNo, Aktif = k.Aktif };
    }

    public async Task KategoriSilAsync(long id)
    {
        // Kullanımdaki kategori SİLİNMEZ: bağlı kayıtlar kategorisiz kalırdı.
        var kullanim = await _context.Protokoller.CountAsync(p => p.KategoriId == id);
        if (kullanim > 0)
        {
            throw new BusinessRuleException(
                $"Bu kategoride {kullanim} kayıt var. Önce kayıtları başka kategoriye taşıyın.");
        }

        var silinen = await _context.ProtokolKategorileri.Where(k => k.Id == id).ExecuteDeleteAsync();
        if (silinen == 0)
        {
            throw new EntityNotFoundException($"{id} kimlikli kategori bulunamadı.");
        }
    }

    private async Task KategoriVarMiAsync(long kategoriId)
    {
        if (!await _context.ProtokolKategorileri.AnyAsync(k => k.Id == kategoriId))
        {
            throw new BusinessRuleException("Geçerli bir protokol kategorisi seçin.");
        }
    }

    public Task<List<ProtokolGrubuDto>> KurumlarAsync() =>
        _context.Protokoller
            .AsNoTracking()
            .Where(p => p.Kurum != null && p.Kurum != "")
            .GroupBy(p => p.Kurum!)
            .Select(g => new ProtokolGrubuDto { Ad = g.Key, Adet = g.Count() })
            .OrderBy(g => g.Ad)
            .ToListAsync();

    public async Task SiralamaGuncelleAsync(IReadOnlyList<(long Id, int SiraNo)> sira)
    {
        if (sira.Count == 0) return;

        var idler = sira.Select(s => s.Id).ToList();
        var kayitlar = await _context.Protokoller.Where(p => idler.Contains(p.Id)).ToListAsync();

        // Tek `SaveChanges`: yarım kalmış bir sıralama, listeyi rastgele
        // bir düzende bırakırdı.
        foreach (var k in kayitlar)
        {
            k.SiraNo = sira.First(s => s.Id == k.Id).SiraNo;
            k.GuncellemeTarihi = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Birim süzgeci davetten gelir</b> (<c>DavetServisi.GorunurOlanlar</c>
    /// ile aynı kural): protokol kaydı kurum geneli ama davet listeleri
    /// birime ait. Başka birimin kime ne sorduğu burada görünmemeli.
    /// </remarks>
    public async Task<List<ProtokolDavetGecmisiDto>> DavetGecmisiAsync(long protokolId)
    {
        var birimId = _kullanici.GetCurrentBirimId();

        return await _context.DavetKisileri
            .AsNoTracking()
            .Where(k => k.ProtokolId == protokolId
                     && k.Davet != null
                     && k.Davet.BirimId == birimId)
            // En yeni önce: "geçen sefer ne olmuştu" sorusunun cevabı en
            // üstte durmalı. Tarihi olmayan (henüz planlanmamış) davetler
            // kayıt sırasına düşer.
            .OrderByDescending(k => k.Davet!.Tarih ?? k.Davet.OlusturmaTarihi)
            .Select(k => new ProtokolDavetGecmisiDto
            {
                DavetId = k.DavetId,
                Baslik = k.Davet!.Baslik,
                Tarih = k.Davet.Tarih,
                Yer = k.Davet.Yer,
                Durum = (int)k.Durum,
                Arandi = k.Arandi,
                ArandiTarihi = k.ArandiTarihi,
                MesajGonderildi = k.MesajGonderildi,
                MesajTarihi = k.MesajTarihi,
                Not = k.Not,
            })
            .ToListAsync()
            .ContinueWith(t =>
            {
                foreach (var d in t.Result)
                {
                    d.DurumAd = DurumMetni((DavetDurumu)d.Durum);
                }
                return t.Result;
            });
    }

    /// <summary>
    /// Durum etiketi — sunucuda üretilir.
    /// </summary>
    /// <remarks>
    /// Web ve mobilde ayrı ayrı kurulsaydı biri eksik kalırdı; enum'a yeni bir
    /// değer eklendiğinde istemciler "bilinmeyen" gösterirdi.
    /// </remarks>
    private static string DurumMetni(DavetDurumu d) => d switch
    {
        DavetDurumu.Katilacak => "Katılacak",
        DavetDurumu.Katilmayacak => "Katılmayacak",
        DavetDurumu.Ulasilamadi => "Ulaşılamadı",
        _ => "Beklemede",
    };

    /// <summary>Sıra verilmemişse listenin sonuna ekle.</summary>
    private async Task<int> SonrakiSiraAsync() =>
        (await _context.Protokoller.MaxAsync(p => (int?)p.SiraNo) ?? 0) + 1;

    private static string? Bosalt(string? m) => string.IsNullOrWhiteSpace(m) ? null : m.Trim();

    private static ProtokolDto Cevir(Protokol p) => new()
    {
        Id = p.Id,
        KategoriId = p.KategoriId,
        Kategori = p.Kategori?.Ad ?? string.Empty,
        Kurum = p.Kurum,
        AdSoyad = p.AdSoyad,
        Unvan = p.Unvan,
        SiraNo = p.SiraNo,
        Telefon = p.Telefon,
        CepTelefon = p.CepTelefon,
        Eposta = p.Eposta,
        Adres = p.Adres,
        Aciklama = p.Aciklama,
        Aktif = p.Aktif,
    };
}
