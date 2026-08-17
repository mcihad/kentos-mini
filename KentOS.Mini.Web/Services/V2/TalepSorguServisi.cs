using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.V2.Talep;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Extensions;
using KentOS.Mini.Application.Services;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>Talep listesi süzgeçleri.</summary>
public class TalepSuzgeci : SayfaIstegi
{
    /// <summary>Alt birimlerin talepleri de gelsin mi.</summary>
    [JsonPropertyName("altBirimlerDahil")]
    public bool AltBirimlerDahil { get; set; }

    /// <summary>Arşivlenmişleri getir (varsayılan: yalnızca aktifler).</summary>
    [JsonPropertyName("arsiv")]
    public bool Arsiv { get; set; }

    [JsonPropertyName("durumId")]
    public long? DurumId { get; set; }
    [JsonPropertyName("tipId")]
    public long? TipId { get; set; }
    [JsonPropertyName("mahalleId")]
    public long? MahalleId { get; set; }

    /// <summary>Ajandaya eklenmiş / eklenmemiş süzgeci.</summary>
    [JsonPropertyName("ajandayaEklendi")]
    public bool? AjandayaEklendi { get; set; }

    [JsonPropertyName("baslangic")]
    public DateTime? Baslangic { get; set; }
    [JsonPropertyName("bitis")]
    public DateTime? Bitis { get; set; }
}

public interface ITalepSorguServisi
{
    Task<SayfaliSonuc<TalepOzetDto>> ListeAsync(TalepSuzgeci suzgec);

    /// <summary>Durum başına sayaç — ana sayfa kartları ve filtre çipleri için.</summary>
    /// <param name="ajandayaEklendi">
    /// Verilirse sayaçlar YALNIZCA o alt kümeyi sayar. "Onaylandı ama ajandaya
    /// eklenmemiş" gibi bir görünümde çipler tüm kümeyi saymaya devam ederse
    /// rakamlar listeyle çelişiyor ve kullanıcı hangisine güveneceğini
    /// bilemiyordu.
    /// </param>
    Task<List<DurumSayaciDto>> DurumSayaclariAsync(
        bool altBirimlerDahil, bool arsiv = false, bool? ajandayaEklendi = null);

    /// <summary>Tip başına sayaç — v1'deki <c>RandevuApi/CountByTip</c> karşılığı.</summary>
    Task<List<DurumSayaciDto>> TipSayaclariAsync(bool altBirimlerDahil, bool arsiv = false);
}

/// <summary>Durum başına talep sayısı.</summary>
public class DurumSayaciDto
{
    [JsonPropertyName("durumId")]
    public long DurumId { get; set; }
    [JsonPropertyName("durumAd")]
    public string DurumAd { get; set; } = string.Empty;
    [JsonPropertyName("renk")]
    public string? Renk { get; set; }
    [JsonPropertyName("adet")]
    public int Adet { get; set; }
}

/// <summary>
/// Taleplerin sayfalı, süzgeçli okunması.
///
/// <para>
/// <see cref="Application.Services.IRandevuService"/> listeleri tek seferde
/// belleğe çekiyor — v1'in davranışı bu ve mobil ona bağlı, dolayısıyla
/// değişmiyor. Web için sayfalama <b>veritabanı düzeyinde</b> yapılır:
/// binlerce satırı tarayıcıya göndermek hem yavaş hem gereksiz.
/// </para>
///
/// <para>
/// Birim kapsamı <c>BirimExtensions.GetDescendants</c> ile kurulur —
/// bu bir SORGU yardımcısıdır, iş kuralı değil; kural kopyalanmıyor.
/// </para>
/// </summary>
public class TalepSorguServisi(
    AppDbContext _context,
    ICurrentUserService _mevcutKullanici) : ITalepSorguServisi
{
    /// <summary>Sıralanabilir alanların beyaz listesi.</summary>
    private static readonly Dictionary<string, Expression<Func<Randevu, object?>>> SiralamaAlanlari = new()
    {
        ["konu"] = r => r.Konu,
        ["ad"] = r => r.Ad,
        ["soyad"] = r => r.Soyad,
        ["tarih"] = r => r.BaslangicTarih,
        ["durum"] = r => r.RandevuDurumId,
        ["tip"] = r => r.RandevuTipId,
    };

    public async Task<SayfaliSonuc<TalepOzetDto>> ListeAsync(TalepSuzgeci suzgec)
    {
        var sorgu = TemelSorgu(suzgec.AltBirimlerDahil, suzgec.Arsiv);

        if (suzgec.DurumId is { } durumId) sorgu = sorgu.Where(r => r.RandevuDurumId == durumId);
        if (suzgec.TipId is { } tipId) sorgu = sorgu.Where(r => r.RandevuTipId == tipId);
        if (suzgec.MahalleId is { } mahalleId) sorgu = sorgu.Where(r => r.MahalleId == mahalleId);
        if (suzgec.AjandayaEklendi is { } ajanda) sorgu = sorgu.Where(r => r.AjandaDurum == ajanda);
        if (suzgec.Baslangic is { } bas) sorgu = sorgu.Where(r => r.BaslangicTarih >= bas);
        if (suzgec.Bitis is { } bit) sorgu = sorgu.Where(r => r.BaslangicTarih <= bit);

        if (suzgec.TemizArama is { } ara)
        {
            // `ILike` Postgres'e özgü ve büyük/küçük harf duyarsız; Türkçe
            // karakterlerde `ToLower()` karşılaştırmasından daha güvenilir.
            var kalip = $"%{ara}%";
            sorgu = sorgu.Where(r =>
                EF.Functions.ILike(r.Konu, kalip) ||
                EF.Functions.ILike(r.Ad, kalip) ||
                EF.Functions.ILike(r.Soyad, kalip) ||
                (r.Meslek != null && EF.Functions.ILike(r.Meslek, kalip)) ||
                (r.Telefon != null && EF.Functions.ILike(r.Telefon, kalip)));
        }

        // En yeni önce: talepler kronolojik olarak okunur.
        var sirali = sorgu.Sirala(suzgec, SiralamaAlanlari, r => r.BaslangicTarih, varsayilanAzalan: true);

        // Renkler ve adlar KAYITLA BİRLİKTE gelir: istemcinin durum/tip
        // listelerini ayrıca çekip eşleştirmesi gerekmez ve tanımı silinmiş
        // bir kayıt da doğru renkte kalır.
        return await sirali
            .Select(r => new TalepOzetDto
            {
                Id = r.Id,
                Konu = r.Konu,
                Ad = r.Ad,
                Soyad = r.Soyad,
                Meslek = r.Meslek,
                Telefon = r.Telefon,
                BaslangicTarih = r.BaslangicTarih,
                BitisTarih = r.BitisTarih,
                BirimId = r.BirimId,
                BirimAd = r.Birim == null ? null : r.Birim.Ad,
                TipId = r.RandevuTipId,
                TipAd = r.RandevuTip == null ? null : r.RandevuTip.Ad,
                TipRenk = r.RandevuTip == null ? null : r.RandevuTip.Renk,
                DurumId = r.RandevuDurumId,
                DurumAd = r.RandevuDurum == null ? null : r.RandevuDurum.DurumAd,
                DurumRenk = r.RandevuDurum == null ? null : r.RandevuDurum.Renk,
                MahalleId = r.MahalleId,
                MahalleAd = r.Mahalle == null ? null : r.Mahalle.Ad,
                OzgecmisVar = r.OzgecmisDurum,
                AjandayaEklendi = r.AjandaDurum,
                Arsivlendi = r.Arsivlendi,
                NotSayisi = r.Notlar == null ? 0 : r.Notlar.Count,
                DosyaSayisi = r.Dosyalar == null ? 0 : r.Dosyalar.Count,
            })
            .SayfalaAsync(suzgec);
    }

    public async Task<List<DurumSayaciDto>> DurumSayaclariAsync(
        bool altBirimlerDahil, bool arsiv, bool? ajandayaEklendi = null)
    {
        var sorgu = TemelSorgu(altBirimlerDahil, arsiv);
        if (ajandayaEklendi is { } ajanda) sorgu = sorgu.Where(r => r.AjandaDurum == ajanda);

        var sayimlar = await sorgu
            .Where(r => r.RandevuDurumId != null)
            .GroupBy(r => r.RandevuDurumId!.Value)
            .Select(g => new { DurumId = g.Key, Adet = g.Count() })
            .ToListAsync();

        var durumlar = await _context.RandevuDurumlar
            .AsNoTracking()
            .Select(d => new { d.Id, d.DurumAd, d.Renk })
            .ToListAsync();

        // Sıfır kayıtlı durumlar da listelenir: filtre çipinin kaybolması
        // "böyle bir durum yok" izlenimi verir.
        return durumlar
            .Select(d => new DurumSayaciDto
            {
                DurumId = d.Id,
                DurumAd = d.DurumAd,
                Renk = d.Renk,
                Adet = sayimlar.FirstOrDefault(s => s.DurumId == d.Id)?.Adet ?? 0,
            })
            .OrderByDescending(d => d.Adet)
            .ToList();
    }

    /// <summary>
    /// Tip başına talep sayısı.
    /// </summary>
    /// <remarks>
    /// Durum sayaçlarıyla AYNI kalıp ve aynı DTO: istemci tarafında iki
    /// farklı şekil, aynı çipi çizen iki farklı kod yolu demekti.
    /// Tipin de kendi rengi var, o da döner.
    /// </remarks>
    public async Task<List<DurumSayaciDto>> TipSayaclariAsync(bool altBirimlerDahil, bool arsiv)
    {
        var sorgu = TemelSorgu(altBirimlerDahil, arsiv);

        var sayimlar = await sorgu
            .Where(r => r.RandevuTipId != null)
            .GroupBy(r => r.RandevuTipId!.Value)
            .Select(g => new { TipId = g.Key, Adet = g.Count() })
            .ToListAsync();

        var tipler = await _context.RandevuTipleri
            .AsNoTracking()
            .Select(t => new { t.Id, t.Ad, t.Renk })
            .ToListAsync();

        return tipler
            .Select(t => new DurumSayaciDto
            {
                DurumId = t.Id,
                DurumAd = t.Ad,
                Renk = t.Renk,
                Adet = sayimlar.FirstOrDefault(s => s.TipId == t.Id)?.Adet ?? 0,
            })
            .OrderByDescending(t => t.Adet)
            .ToList();
    }

    /// <summary>Birim kapsamı + arşiv süzgeci.</summary>
    private IQueryable<Randevu> TemelSorgu(bool altBirimlerDahil, bool arsiv)
    {
        var birimId = _mevcutKullanici.GetCurrentBirimId();

        var birimIdleri = altBirimlerDahil
            ? _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList()
            : [birimId];

        // Global sorgu süzgeci `!Arsivlendi` uyguluyor; arşivi görmek için
        // AÇIKÇA devre dışı bırakmak gerekiyor.
        var sorgu = _context.Randevular.AsNoTracking().IgnoreQueryFilters();

        return sorgu.Where(r =>
            birimIdleri.Contains(r.BirimId ?? 0) && r.Arsivlendi == arsiv);
    }
}
