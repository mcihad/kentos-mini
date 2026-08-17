using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Extensions;
using KentOS.Mini.Application.Services;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>Haritada gösterilecek talep.</summary>
public class HaritaNoktasiDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("konu")] public string Konu { get; set; } = string.Empty;
    [JsonPropertyName("adSoyad")] public string? AdSoyad { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }

    /// <summary>Enlem — ayrıştırılmış hâli.</summary>
    [JsonPropertyName("enlem")] public double Enlem { get; set; }
    [JsonPropertyName("boylam")] public double Boylam { get; set; }

    [JsonPropertyName("tarih")] public DateTime? Tarih { get; set; }
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }

    [JsonPropertyName("tipAd")] public string? TipAd { get; set; }
    [JsonPropertyName("tipRenk")] public string? TipRenk { get; set; }
    [JsonPropertyName("durumAd")] public string? DurumAd { get; set; }

    /// <summary>Harita işaretçisinin rengi — durumdan gelir.</summary>
    [JsonPropertyName("durumRenk")] public string? DurumRenk { get; set; }

    [JsonPropertyName("arsivlendi")] public bool Arsivlendi { get; set; }
}

/// <summary>Halk günü kaydı.</summary>
public class HalkGunuDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("tarih")] public DateTime? Tarih { get; set; }
    [JsonPropertyName("konu")] public string Konu { get; set; } = string.Empty;
    [JsonPropertyName("adSoyad")] public string? AdSoyad { get; set; }
    [JsonPropertyName("meslek")] public string? Meslek { get; set; }
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("mahalleAd")] public string? MahalleAd { get; set; }
    [JsonPropertyName("durumAd")] public string? DurumAd { get; set; }
    [JsonPropertyName("durumRenk")] public string? DurumRenk { get; set; }
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }
}

public interface IHaritaServisi
{
    Task<List<HaritaNoktasiDto>> NoktalarAsync(bool arsivDahil, DateTime? bas, DateTime? bit);
    Task<SayfaliSonuc<HalkGunuDto>> HalkGunleriAsync(DateTime? bas, DateTime? bit, SayfaIstegi sayfa);
}

/// <summary>
/// Harita noktaları ve halk günü listesi.
/// </summary>
public class HaritaServisi(
    AppDbContext _context,
    ICurrentUserService _mevcutKullanici,
    Options.RequestOptions _talepAyari) : IHaritaServisi
{
    /// <summary>
    /// "Halk Günü" etkinlik tipinin kimliği.
    /// </summary>
    /// <remarks>
    /// Eski controller bunu <c>const long HALK_GUNU_TIP_ID = 1</c> olarak
    /// GÖMÜLÜ tutuyordu; tip tablosunda sıralama değişirse liste sessizce
    /// yanlış kayıtları gösterir. Burada önce yapılandırmadan, yoksa ADA göre
    /// aranır; ikisi de yoksa eski değere düşülür. Ayar anahtarı
    /// <c>REQUESTS__PUBLICDAYTYPEID</c> (eski adı <c>Randevu:HalkGunuTipId</c>).
    /// </remarks>
    private async Task<long> HalkGunuTipIdAsync()
    {
        if (_talepAyari.PublicDayTypeId > 0)
        {
            return _talepAyari.PublicDayTypeId;
        }

        var adaGore = await _context.RandevuTipleri
            .Where(t => EF.Functions.ILike(t.Ad, "%halk gün%"))
            .Select(t => (long?)t.Id)
            .FirstOrDefaultAsync();

        return adaGore ?? 1;
    }

    public async Task<List<HaritaNoktasiDto>> NoktalarAsync(bool arsivDahil, DateTime? bas, DateTime? bit)
    {
        var birimId = _mevcutKullanici.GetCurrentBirimId();
        var birimIdleri = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();

        var sorgu = _context.Randevular
            .AsNoTracking()
            .IgnoreQueryFilters()
            // Eski uç BÜTÜN birimlerin taleplerini haritaya basıyordu; birim
            // kapsamı burada uygulanıyor.
            .Where(r => birimIdleri.Contains(r.BirimId ?? 0))
            .Where(r => r.Koordinat != null && r.Koordinat != "");

        if (!arsivDahil) sorgu = sorgu.Where(r => !r.Arsivlendi);
        if (bas is { } b) sorgu = sorgu.Where(r => r.BaslangicTarih >= b.Date);
        if (bit is { } t) sorgu = sorgu.Where(r => r.BaslangicTarih < t.Date.AddDays(1));

        var ham = await sorgu
            .Select(r => new
            {
                r.Id, r.Konu, r.Ad, r.Soyad, r.Adres, r.Telefon, r.Koordinat,
                r.BaslangicTarih, r.Arsivlendi,
                BirimAd = r.Birim == null ? null : r.Birim.Ad,
                TipAd = r.RandevuTip == null ? null : r.RandevuTip.Ad,
                TipRenk = r.RandevuTip == null ? null : r.RandevuTip.Renk,
                DurumAd = r.RandevuDurum == null ? null : r.RandevuDurum.DurumAd,
                DurumRenk = r.RandevuDurum == null ? null : r.RandevuDurum.Renk,
            })
            .ToListAsync();

        // Koordinat metin olarak saklanıyor ("38.7312,37.0150" gibi). Bozuk
        // kayıtlar istemciye NaN göndermek yerine burada elenir — haritanın
        // ortasına düşen bir (0,0) işaretçisi, hiç işaretçi olmamasından kötü.
        return ham
            .Select(r => new { r, k = KoordinatAyristir(r.Koordinat) })
            .Where(x => x.k is not null)
            .Select(x => new HaritaNoktasiDto
            {
                Id = x.r.Id,
                Konu = x.r.Konu,
                AdSoyad = $"{x.r.Ad} {x.r.Soyad}".Trim(),
                Adres = x.r.Adres,
                Telefon = x.r.Telefon,
                Enlem = x.k!.Value.Enlem,
                Boylam = x.k.Value.Boylam,
                Tarih = x.r.BaslangicTarih,
                BirimAd = x.r.BirimAd,
                TipAd = x.r.TipAd,
                TipRenk = x.r.TipRenk,
                DurumAd = x.r.DurumAd,
                DurumRenk = x.r.DurumRenk,
                Arsivlendi = x.r.Arsivlendi,
            })
            .ToList();
    }

    public async Task<SayfaliSonuc<HalkGunuDto>> HalkGunleriAsync(
        DateTime? bas, DateTime? bit, SayfaIstegi sayfa)
    {
        // Aralık verilmezse son 3 ay — eski davranışla aynı.
        bas ??= DateTime.Now.AddMonths(-3);
        bit ??= DateTime.Now;

        var tipId = await HalkGunuTipIdAsync();
        var birimId = _mevcutKullanici.GetCurrentBirimId();
        var birimIdleri = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();

        var sorgu = _context.Randevular
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => birimIdleri.Contains(r.BirimId ?? 0)
                     && r.RandevuTipId == tipId
                     && r.BaslangicTarih >= bas.Value.Date
                     && r.BaslangicTarih < bit.Value.Date.AddDays(1));

        if (sayfa.TemizArama is { } ara)
        {
            var k = $"%{ara}%";
            sorgu = sorgu.Where(r =>
                EF.Functions.ILike(r.Konu, k) ||
                EF.Functions.ILike(r.Ad, k) ||
                EF.Functions.ILike(r.Soyad, k));
        }

        return await sorgu
            .OrderByDescending(r => r.BaslangicTarih)
            .Select(r => new HalkGunuDto
            {
                Id = r.Id,
                Tarih = r.BaslangicTarih,
                Konu = r.Konu,
                AdSoyad = (r.Ad + " " + r.Soyad).Trim(),
                Meslek = r.Meslek,
                Telefon = r.Telefon,
                MahalleAd = r.Mahalle == null ? null : r.Mahalle.Ad,
                DurumAd = r.RandevuDurum == null ? null : r.RandevuDurum.DurumAd,
                DurumRenk = r.RandevuDurum == null ? null : r.RandevuDurum.Renk,
                BirimAd = r.Birim == null ? null : r.Birim.Ad,
            })
            .SayfalaAsync(sayfa);
    }

    /// <summary>
    /// "38.7312,37.0150" biçimindeki koordinat metnini ayrıştırır.
    /// </summary>
    /// <remarks>
    /// <see cref="CultureInfo.InvariantCulture"/> ZORUNLU: Türkçe kültürde
    /// ondalık ayracı virgüldür ve "38.7312" tam sayı 387312 olarak okunur —
    /// işaretçi haritanın çok uzağına düşer.
    /// </remarks>
    private static (double Enlem, double Boylam)? KoordinatAyristir(string? metin)
    {
        if (string.IsNullOrWhiteSpace(metin)) return null;

        var parcalar = metin.Split(',', StringSplitOptions.TrimEntries);
        if (parcalar.Length != 2) return null;

        if (!double.TryParse(parcalar[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var enlem) ||
            !double.TryParse(parcalar[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var boylam))
        {
            return null;
        }

        // Geçerli aralık dışındaki değerler bozuk kayıttır.
        if (enlem is < -90 or > 90 || boylam is < -180 or > 180) return null;
        if (enlem == 0 && boylam == 0) return null;

        return (enlem, boylam);
    }
}
