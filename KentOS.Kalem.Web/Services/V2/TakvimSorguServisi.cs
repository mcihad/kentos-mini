using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto.V2.Etkinlik;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Data;

namespace KentOS.Kalem.Web.Services.V2;

public interface ITakvimSorguServisi
{
    Task<List<EtkinlikOzetDto>> AralikAsync(AralikIstegi istek, CancellationToken iptal = default);
    /// <summary>
    /// Gün başına etkinlik sayısı. <paramref name="ay"/> verilirse yalnızca o ay.
    /// </summary>
    /// <remarks>
    /// SPA'nın yıl görünümü tüm yılı ister; mobilin ay takvimi tek ay ister
    /// (v1 karşılığı <c>AjandaApi/CountByDay/{ay}/{yil}</c>). Mobil için bir
    /// yıllık sayacı indirip tarayıcı tarafında süzmek, ölçülü mobil veriyle
    /// ödenen gereksiz bir bedeldi.
    /// </remarks>
    Task<List<GunSayaciDto>> GunSayaclariAsync(int yil, int? ay = null, CancellationToken iptal = default);
}

/// <summary>
/// Takvim görünümlerinin OKUMA sorguları.
///
/// <para>
/// Yalnızca okuma yapar; yazma yolları mevcut <c>IAjandaService</c> /
/// <c>IAjandaSeriService</c> üzerinden gider. v2 asla ikinci bir yazma yolu
/// açmaz — iş kuralları tek yerde kalır.
/// </para>
///
/// <para>
/// <b>Her sorgu <see cref="AjandaSorguUzantilari.ErisilebilirOlanlar"/> ile
/// başlamak ZORUNDA.</b> O metot birim izolasyonunu ve gizliliği BİRLİKTE
/// uygular. Yalnızca <c>GorunurOlanlar</c> çağırmak yetmez — bu sorgu bir
/// süre öyleydi ve yeni arayüz başka birimlerin etkinliklerini gösteriyordu.
/// Testlerle bekçileniyor.
/// </para>
///
/// <para>
/// Tekrarlar veritabanında GERÇEK satır olarak durduğu için burada kural
/// genişletmesi yapılmaz; basit bir tarih aralığı sorgusu yeterlidir.
/// </para>
/// </summary>
public class TakvimSorguServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici) : ITakvimSorguServisi
{
    public async Task<List<EtkinlikOzetDto>> AralikAsync(AralikIstegi istek, CancellationToken iptal = default)
    {
        var kullaniciId = await _kullanici.GetUserIdAsync();
        var kullaniciAdi = _kullanici.GetUsername();
        var birimId = _kullanici.GetCurrentBirimId();

        // Basın kullanıcısı ajandanın yalnızca basına açık kısmını görür.
        // Kapı `ICurrentUserService`te: bu sorguların hepsinde zaten var.
        var yalnizcaBasin = await _kullanici.YalnizcaBasinMiAsync();

        var sorgu = _context.Ajandalar
            .AsNoTracking()
            .ErisilebilirOlanlar(kullaniciId, kullaniciAdi, birimId, yalnizcaBasin)
            // Kesişim: etkinlik penceredeyse dahil. Bitişi olmayanlar tek nokta sayılır.
            .Where(a => a.BaslangicTarihi < istek.Bitis
                     && (a.BitisTarihi ?? a.BaslangicTarihi) >= istek.Baslangic);

        if (istek.TipIdler is { Length: > 0 })
        {
            sorgu = sorgu.Where(a => a.RandevuTipId != null && istek.TipIdler.Contains(a.RandevuTipId.Value));
        }

        if (istek.DurumIdler is { Length: > 0 })
        {
            sorgu = sorgu.Where(a => a.DurumId != null && istek.DurumIdler.Contains(a.DurumId.Value));
        }

        if (!string.IsNullOrWhiteSpace(istek.Arama))
        {
            var q = istek.Arama.Trim();
            sorgu = sorgu.Where(a => EF.Functions.ILike(a.Baslik, $"%{q}%")
                                  || (a.Konum != null && EF.Functions.ILike(a.Konum, $"%{q}%")));
        }

        return await sorgu
            .OrderBy(a => a.BaslangicTarihi)
            .Select(a => new EtkinlikOzetDto
            {
                Id = a.Id,
                Baslik = a.Baslik,
                Baslangic = a.BaslangicTarihi,
                Bitis = a.BitisTarihi,
                TumGun = a.TumGun,
                Konum = a.Konum,
                TipId = a.RandevuTipId,
                TipAd = a.RandevuTip == null ? null : a.RandevuTip.Ad,
                TipRenk = a.RandevuTip == null ? null : a.RandevuTip.Renk,
                DurumId = a.DurumId,
                DurumAd = a.Durum == null ? null : a.Durum.Ad,
                DurumRenk = a.Durum == null ? null : a.Durum.Renk,
                Statu = (int)a.Status,
                Gizli = a.Gizli,
                SeriId = a.SeriId,
                SeriAyrik = a.SeriAyrik,
                ResimVar = a.ResimVar,
                // EF bunu alt sorguya çeviriyor (COUNT); `Include` gerekmez
                // ve satır çoğaltmıyor.
                ResimSayisi = a.Photos.Count(),
                BasinKatilsin = a.BasinKatilsin,
                BirimId = a.BirimId,
                BirimAd = a.Birim == null ? null : a.Birim.Ad,
            })
            .ToListAsync(iptal);
    }

    /// <summary>
    /// Yıl görünümü için gün başına etkinlik sayısı.
    ///
    /// 365 günün etkinliklerini çekmek yerine yalnızca sayılar gruplanır —
    /// yıl görünümü zaten yoğunluk noktası gösteriyor.
    /// </summary>
    public async Task<List<GunSayaciDto>> GunSayaclariAsync(
        int yil, int? ay = null, CancellationToken iptal = default)
    {
        var kullaniciId = await _kullanici.GetUserIdAsync();
        var kullaniciAdi = _kullanici.GetUsername();
        var birimId = _kullanici.GetCurrentBirimId();

        var bas = ay is >= 1 and <= 12 ? new DateTime(yil, ay.Value, 1) : new DateTime(yil, 1, 1);
        var bit = ay is >= 1 and <= 12 ? bas.AddMonths(1) : bas.AddYears(1);

        // Basın kullanıcısı ajandanın yalnızca basına açık kısmını görür.
        // Kapı `ICurrentUserService`te: bu sorguların hepsinde zaten var.
        var yalnizcaBasin = await _kullanici.YalnizcaBasinMiAsync();

        return await _context.Ajandalar
            .AsNoTracking()
            .ErisilebilirOlanlar(kullaniciId, kullaniciAdi, birimId, yalnizcaBasin)
            .Where(a => a.BaslangicTarihi >= bas && a.BaslangicTarihi < bit)
            .GroupBy(a => a.BaslangicTarihi.Date)
            .Select(g => new GunSayaciDto { Gun = g.Key, Adet = g.Count() })
            .OrderBy(x => x.Gun)
            .ToListAsync(iptal);
    }
}
