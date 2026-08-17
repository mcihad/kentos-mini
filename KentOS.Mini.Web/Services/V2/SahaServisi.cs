using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.V2.IsTakip;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>
/// SAHA — tespit ve harita.
/// </summary>
/// <remarks>
/// <para>
/// <b>Saha tespitinde karşılama adımı yok.</b> Vatandaş bildiriminden farkı
/// bu: tespiti yapan zaten kurumun personeli ve hangi birimin işi olduğunu
/// biliyor. Kayıt doğrudan kendi biriminin görevi olarak açılıyor ve
/// varsayılan olarak tespiti yapana atanıyor — sahadaki kişi gördüğü işi
/// çoğu zaman kendisi yapıyor.
/// </para>
/// </remarks>
public interface ISahaServisi
{
    /// <summary>Sahada görülen sorunu doğrudan görev olarak açar.</summary>
    Task<GorevDetayDto> TespitAsync(SahaTespitiDto istek, CancellationToken iptal = default);

    /// <summary>Haritaya basılacak noktalar — görevler ve bekleyen bildirimler.</summary>
    Task<List<IsHaritaNoktasiDto>> NoktalarAsync(
        bool altBirimlerDahil, bool bildirimlerDahil, bool yalnizAcik,
        CancellationToken iptal = default);

    /// <summary>Kullanıcının ÜZERİNDEKİ açık görevler — saha listesi.</summary>
    Task<List<GorevOzetDto>> BenimIslerimAsync(CancellationToken iptal = default);
}

public class SahaServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici,
    IEtkinBirim _etkinBirim,
    IGorevServisi _gorevler) : ISahaServisi
{
    public async Task<GorevDetayDto> TespitAsync(
        SahaTespitiDto istek, CancellationToken iptal = default)
    {
        var kullaniciId = await _kullanici.GetUserIdAsync();

        return await _gorevler.OlusturAsync(new GorevKayitDto
        {
            Baslik = istek.Baslik.Trim(),
            Aciklama = istek.Aciklama,
            GorevTipiId = istek.GorevTipiId,
            Oncelik = istek.Oncelik,
            Kaynak = GorevKaynagi.Saha,
            Enlem = istek.Enlem,
            Boylam = istek.Boylam,
            Adres = istek.Adres,
            MahalleId = istek.MahalleId,

            // Sahada VARSAYILAN kendine atama: gördüğü işi çoğu zaman aynı
            // kişi yapıyor ve ayrı bir atama adımı, telefonu tek elle
            // kullanan personel için fazladan bir engel.
            Atamalar = istek.KendimeAta && kullaniciId is > 0
                ? [new GorevAtamaIstegiDto { KullaniciId = kullaniciId, Rol = GorevAtamaRolu.Sorumlu }]
                : [],
        }, iptal);
    }

    /// <summary>
    /// Harita noktaları.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Üst sınır var.</b> Haritaya sınırsız nokta basmak, iki yıllık veride
    /// tarayıcıyı kilitler. Sınır aşıldığında EN YENİLER dönüyor; kullanıcı
    /// süzgeçle daraltıyor.
    /// </para>
    /// <para>
    /// Konumu olmayan kayıt hiç sorgulanmıyor — haritada yeri yok.
    /// </para>
    /// </remarks>
    private const int EnFazlaNokta = 1000;

    public async Task<List<IsHaritaNoktasiDto>> NoktalarAsync(
        bool altBirimlerDahil, bool bildirimlerDahil, bool yalnizAcik,
        CancellationToken iptal = default)
    {
        var kapsam = await _etkinBirim.KapsamAsync(altBirimlerDahil, iptal);
        var simdi = DateTime.Now;

        var gorevSorgusu = _context.Gorevler
            .AsNoTracking()
            .Where(g => kapsam.Contains(g.BirimId))
            .Where(g => g.Enlem != null && g.Boylam != null);

        if (yalnizAcik)
        {
            gorevSorgusu = gorevSorgusu.Where(g =>
                g.Durum != GorevDurumu.Tamamlandi && g.Durum != GorevDurumu.Iptal);
        }

        var gorevler = await gorevSorgusu
            .OrderByDescending(g => g.OlusturmaTarihi)
            .Take(EnFazlaNokta)
            .Select(g => new
            {
                g.Id, g.TakipNo, g.Baslik, g.Durum, g.Enlem, g.Boylam, g.Adres, g.SlaBitis,
            })
            .ToListAsync(iptal);

        var noktalar = gorevler.Select(g => new IsHaritaNoktasiDto
        {
            Id = g.Id,
            Tur = "gorev",
            TakipNo = g.TakipNo,
            Baslik = g.Baslik,
            Enlem = g.Enlem!.Value,
            Boylam = g.Boylam!.Value,
            Renk = GorevDurumAkisi.Renk(g.Durum),
            DurumAd = GorevDurumAkisi.Ad(g.Durum),
            Gecikti = !GorevDurumAkisi.Kapali(g.Durum) && g.SlaBitis is { } s && s < simdi,
            Adres = g.Adres,
        }).ToList();

        /*
          BEKLEYEN BİLDİRİMLER de haritada — İSTEĞE BAĞLI.

          Karşılama personeli için değerli: aynı sokakta biriken üç bildirim
          haritada tek bakışta görünüyor ve mükerrer olduğu anlaşılıyor.
          Varsayılan olarak KAPALI çünkü saha personelinin işine karışıyor;
          onun listesinde yalnızca kendi görevleri olmalı.
        */
        if (bildirimlerDahil)
        {
            var bildirimler = await _context.VatandasBildirimleri
                .AsNoTracking()
                .Where(b => b.Durum == VatandasBildirimDurumu.Yeni)
                .Where(b => b.Enlem != null && b.Boylam != null)
                .OrderByDescending(b => b.OlusturmaTarihi)
                .Take(EnFazlaNokta)
                .Select(b => new { b.Id, b.TakipNo, b.Konu, b.Enlem, b.Boylam, b.Adres })
                .ToListAsync(iptal);

            noktalar.AddRange(bildirimler.Select(b => new IsHaritaNoktasiDto
            {
                Id = b.Id,
                Tur = "bildirim",
                TakipNo = b.TakipNo,
                Baslik = b.Konu,
                Enlem = b.Enlem!.Value,
                Boylam = b.Boylam!.Value,
                Renk = VatandasBildirimServisi.DurumRengi(VatandasBildirimDurumu.Yeni),
                DurumAd = "Bekliyor",
                Adres = b.Adres,
            }));
        }

        return noktalar;
    }

    /// <summary>
    /// Kullanıcının üzerindeki AÇIK görevler.
    /// </summary>
    /// <remarks>
    /// Ekip ataması da sayılıyor: sahadaki kişi çoğu zaman kişisel olarak
    /// değil ekibiyle atanıyor ve yalnızca kişisel atamalara bakan bir liste
    /// ona boş görünürdü.
    /// </remarks>
    public async Task<List<GorevOzetDto>> BenimIslerimAsync(CancellationToken iptal = default)
    {
        var kullaniciId = await _kullanici.GetUserIdAsync();
        if (kullaniciId is not > 0) return [];

        var ekiplerim = await _context.EkipUyeleri
            .AsNoTracking()
            .Where(u => u.KullaniciId == kullaniciId)
            .Select(u => u.EkipId)
            .ToListAsync(iptal);

        var idler = await _context.Gorevler
            .AsNoTracking()
            .Where(g => g.Durum != GorevDurumu.Tamamlandi && g.Durum != GorevDurumu.Iptal)
            .Where(g => _context.GorevAtamalari.Any(a =>
                a.GorevId == g.Id &&
                (a.KullaniciId == kullaniciId ||
                 (a.EkipId != null && ekiplerim.Contains(a.EkipId.Value)))))
            // En az vakti kalan önce: sahadaki kişi sıradaki işi arıyor.
            .OrderBy(g => g.SlaBitis == null)
            .ThenBy(g => g.SlaBitis)
            .Take(200)
            .Select(g => g.Id)
            .ToListAsync(iptal);

        return await _gorevler.OzetleAsync(idler, iptal);
    }
}
