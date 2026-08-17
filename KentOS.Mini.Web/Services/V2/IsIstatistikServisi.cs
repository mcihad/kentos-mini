using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.Analiz;
using KentOS.Mini.Application.Dto.V2.IsTakip;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Web.Data;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>
/// GECİKME PANOSU ve BİRİM KARNESİ.
/// </summary>
/// <remarks>
/// <para>
/// <b>Mevcut <c>TalepIstatistikDto</c> genişletilmedi</b> ve bu bilinçli:
/// tek DTO'ya sıkıştırmak, iki ekranın da ötekinin alanlarını taşıması
/// demekti. Dilim ve seri tipleri ise <b>yeniden kullanılıyor</b> — ön yüz
/// grafikleri zaten onlara göre yazılı.
/// </para>
/// <para>
/// <b>Bu bir kişi karnesi değil.</b> Ölçüm birim düzeyinde: kim kaç iş
/// bitirdi sorusunu cevaplayan bir tablo, kurumda ölçmek istediğimiz şeyi
/// (hizmetin süresinde verilip verilmediğini) değil personel kıyaslamasını
/// üretirdi.
/// </para>
/// </remarks>
public interface IIsIstatistikServisi
{
    Task<IsIstatistikDto> PanoAsync(bool altBirimlerDahil, CancellationToken iptal = default);
}

public class IsIstatistikServisi(
    AppDbContext _context,
    IEtkinBirim _etkinBirim,
    IGorevServisi _gorevler) : IIsIstatistikServisi
{
    /// <summary>Panoda gösterilen geciken iş sayısı.</summary>
    private const int GecikenListesi = 20;

    public async Task<IsIstatistikDto> PanoAsync(
        bool altBirimlerDahil, CancellationToken iptal = default)
    {
        var kapsam = await _etkinBirim.KapsamAsync(altBirimlerDahil, iptal);
        var simdi = DateTime.Now;
        var bugun = simdi.Date;

        var gorevler = _context.Gorevler.AsNoTracking().Where(g => kapsam.Contains(g.BirimId));

        var pano = new IsIstatistikDto
        {
            Acik = await gorevler.CountAsync(
                g => g.Durum != GorevDurumu.Tamamlandi && g.Durum != GorevDurumu.Iptal, iptal),

            Geciken = await gorevler.CountAsync(
                g => g.SlaBitis != null && g.SlaBitis < simdi
                  && g.Durum != GorevDurumu.Tamamlandi && g.Durum != GorevDurumu.Iptal, iptal),

            OnayBekleyen = await gorevler.CountAsync(
                g => g.Durum == GorevDurumu.TamamlanmaBekliyor, iptal),

            // ATANMAMIŞ: kimseye verilmemiş iş, gecikmenin en sık sebebi.
            Atanmamis = await gorevler.CountAsync(
                g => g.Durum == GorevDurumu.Yeni
                  && !_context.GorevAtamalari.Any(a => a.GorevId == g.Id), iptal),

            BugunTamamlanan = await gorevler.CountAsync(
                g => g.TamamlanmaTarihi != null && g.TamamlanmaTarihi >= bugun, iptal),

            BekleyenBildirim = await _context.VatandasBildirimleri
                .CountAsync(b => b.Durum == VatandasBildirimDurumu.Yeni, iptal),

            BekleyenDevir = await _context.BirimGelenKutusu
                .CountAsync(k => kapsam.Contains(k.HedefBirimId)
                              && k.Durum == GelenKutusuDurumu.Bekliyor, iptal),
        };

        // ── durum dağılımı ─────────────────────────────────────────────
        var dagilim = await gorevler
            .GroupBy(g => g.Durum)
            .Select(g => new { Durum = g.Key, Adet = g.Count() })
            .ToListAsync(iptal);

        pano.DurumDagilimi = [.. dagilim
            .OrderBy(d => d.Durum)
            .Select(d => new IstatistikDilimDto
            {
                Etiket = GorevDurumAkisi.Ad(d.Durum),
                Deger = d.Adet,
                Renk = GorevDurumAkisi.Renk(d.Durum),
            })];

        // ── birim karnesi ──────────────────────────────────────────────
        var birimKayitlari = await gorevler
            .GroupBy(g => g.BirimId)
            .Select(g => new
            {
                BirimId = g.Key,
                Acik = g.Count(x => x.Durum != GorevDurumu.Tamamlandi && x.Durum != GorevDurumu.Iptal),
                Tamamlanan = g.Count(x => x.Durum == GorevDurumu.Tamamlandi),
                Geciken = g.Count(x =>
                    x.SlaBitis != null && x.SlaBitis < simdi &&
                    x.Durum != GorevDurumu.Tamamlandi && x.Durum != GorevDurumu.Iptal),

                // SÜRESİNDE TAMAMLANAN: SLA damgası olan ve o damgadan önce
                // bitmiş işler. SLA'sı olmayan iş bu orana HİÇ girmiyor —
                // ölçülmemiş bir şeyi "zamanında" saymak sayıyı şişirirdi.
                SlaliTamamlanan = g.Count(x =>
                    x.Durum == GorevDurumu.Tamamlandi && x.SlaBitis != null),
                Zamaninda = g.Count(x =>
                    x.Durum == GorevDurumu.Tamamlandi && x.SlaBitis != null
                    && x.TamamlanmaTarihi != null && x.TamamlanmaTarihi <= x.SlaBitis),

            })
            .ToListAsync(iptal);

        /*
          ORTALAMA SÜRE AYRI SORGUDA ve SON 90 GÜNLE SINIRLI.

          İki nokta arasındaki farkı veritabanında ortalamak sağlayıcıya
          özel bir işlev gerektiriyordu (`DateDiffHour` SQL Server'a ait);
          onun yerine iki damga çekilip bellekte ortalanıyor.

          90 gün sınırı yalnızca satır sayısını değil ANLAMI da kısıtlıyor:
          iki yıllık ortalama, birimin bugünkü durumu hakkında bir şey
          söylemiyor.
        */
        var doksanGun = simdi.AddDays(-90);

        var sureler = await gorevler
            .Where(g => g.Durum == GorevDurumu.Tamamlandi
                     && g.BaslamaTarihi != null
                     && g.TamamlanmaTarihi != null
                     && g.TamamlanmaTarihi >= doksanGun)
            .Select(g => new
            {
                g.BirimId,
                Baslangic = g.BaslamaTarihi!.Value,
                Bitis = g.TamamlanmaTarihi!.Value,
            })
            .ToListAsync(iptal);

        var ortalamalar = sureler
            .GroupBy(x => x.BirimId)
            .ToDictionary(
                g => g.Key,
                g => Math.Round(g.Average(x => (x.Bitis - x.Baslangic).TotalHours), 1));

        var adlar = await _context.Birimler
            .AsNoTracking()
            .Where(b => kapsam.Contains(b.Id))
            .Select(b => new { b.Id, b.Ad })
            .ToDictionaryAsync(b => b.Id, b => b.Ad, iptal);

        pano.Birimler = [.. birimKayitlari
            .Select(b => new BirimKarnesiDto
            {
                BirimId = b.BirimId,
                BirimAd = adlar.GetValueOrDefault(b.BirimId) ?? "—",
                Acik = b.Acik,
                Tamamlanan = b.Tamamlanan,
                Geciken = b.Geciken,
                ZamanindaOran = b.SlaliTamamlanan == 0
                    ? null
                    : b.Zamaninda * 100 / b.SlaliTamamlanan,
                OrtalamaSaat = ortalamalar.TryGetValue(b.BirimId, out var o) ? o : null,
            })
            // En çok geciken ÜSTTE: pano bir sıralama değil bir uyarı ekranı.
            .OrderByDescending(b => b.Geciken)
            .ThenByDescending(b => b.Acik)];

        // ── geciken işler ──────────────────────────────────────────────
        var gecikenIdler = await gorevler
            .Where(g => g.SlaBitis != null && g.SlaBitis < simdi)
            .Where(g => g.Durum != GorevDurumu.Tamamlandi && g.Durum != GorevDurumu.Iptal)
            .OrderBy(g => g.SlaBitis)
            .Take(GecikenListesi)
            .Select(g => g.Id)
            .ToListAsync(iptal);

        pano.Gecikenler = await _gorevler.OzetleAsync(gecikenIdler, iptal);

        return pano;
    }
}
