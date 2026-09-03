using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Dto.V2.IsTakip;
using KentOS.Kalem.Application.Dto.V2.Ortak;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Exceptions;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>
/// BİRİM GELEN KUTUSU — birimden birime iş devri.
/// </summary>
/// <remarks>
/// <para>
/// Görev tamamlandığında tipinde tanımlı devir kuralı tetikleniyor ve hedef
/// birimin gelen kutusuna kayıt düşüyor. <b>Doğrudan görev açılmıyor:</b>
/// otomatik açsaydık bir birim, başka bir birimin iş listesine sınırsız iş
/// yazabilirdi ve kimse "bunu kim uygun gördü?" sorusunu soramazdı.
/// </para>
/// </remarks>
public interface IGelenKutusuServisi
{
    /// <summary>
    /// Görev tamamlandığında devir kurallarını uygular.
    /// </summary>
    /// <remarks>
    /// İstisna YUTAR: devir yardımcı bir akış ve görevin onaylanmasını
    /// düşürmesine izin verilmez.
    /// </remarks>
    Task DevirleriUygulaAsync(long gorevId, CancellationToken iptal = default);

    Task<SayfaliSonuc<GelenKutusuDto>> ListeAsync(SayfaIstegi istek, GelenKutusuDurumu? durum,
        bool altBirimlerDahil, CancellationToken iptal = default);

    Task<int> BekleyenSayisiAsync(CancellationToken iptal = default);

    Task<GelenKutusuDto> KabulAsync(long id, GelenKutusuKabulDto istek, CancellationToken iptal = default);
    Task<GelenKutusuDto> ReddetAsync(long id, string gerekce, CancellationToken iptal = default);
    Task<GelenKutusuDto> OkunduAsync(long id, CancellationToken iptal = default);
}

public class GelenKutusuServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici,
    IEtkinBirim _etkinBirim,
    IIsOlayServisi _olaylar,
    IGorevServisi _gorevler,
    IMessageService _mesajlar,
    ILogger<GelenKutusuServisi> _kayit) : IGelenKutusuServisi
{
    // ── devir tetikleyici ──────────────────────────────────────────────

    public async Task DevirleriUygulaAsync(long gorevId, CancellationToken iptal = default)
    {
        try
        {
            var gorev = await _context.Gorevler
                .AsNoTracking()
                .Where(g => g.Id == gorevId)
                .Select(g => new
                {
                    g.Id, g.BirimId, g.GorevTipiId, g.TakipNo, g.Baslik, g.Aciklama,
                    g.Enlem, g.Boylam, g.Adres,
                })
                .FirstOrDefaultAsync(iptal);

            if (gorev?.GorevTipiId is not { } tipId) return;

            var devirler = await _context.GorevTipiDevirleri
                .AsNoTracking()
                .Where(d => d.GorevTipiId == tipId)
                .ToListAsync(iptal);

            if (devirler.Count == 0) return;

            // Zaten düşmüş kayıtlar ATLANIR: görev iade edilip yeniden
            // tamamlanırsa ikinci bir kayıt doğar ve hedef birim aynı işi
            // iki kez karara bağlardı.
            var mevcutlar = await _context.BirimGelenKutusu
                .AsNoTracking()
                .Where(k => k.KaynakGorevId == gorev.Id)
                .Select(k => k.HedefBirimId)
                .ToListAsync(iptal);

            var yeniler = new List<UnitInbox>();

            foreach (var d in devirler)
            {
                if (mevcutlar.Contains(d.HedefBirimId)) continue;

                // KENDİ BİRİMİNE devir anlamsız: iş zaten orada bitti.
                if (d.HedefBirimId == gorev.BirimId) continue;

                yeniler.Add(new UnitInbox
                {
                    HedefBirimId = d.HedefBirimId,
                    KaynakGorevId = gorev.Id,
                    KaynakBirimId = gorev.BirimId,
                    GorevTipiDevirId = d.Id,
                    HedefGorevTipiId = d.HedefGorevTipiId,
                    Konu = gorev.Baslik,
                    Aciklama = DevirMetni(gorev.TakipNo, gorev.Aciklama, d.Not),
                    IsTalebi = d.IsTalebi,
                    Durum = GelenKutusuDurumu.Bekliyor,
                    Enlem = gorev.Enlem,
                    Boylam = gorev.Boylam,
                    Adres = gorev.Adres,
                    OlusturmaTarihi = DateTime.Now,
                });
            }

            if (yeniler.Count == 0) return;

            _context.BirimGelenKutusu.AddRange(yeniler);
            await _context.SaveChangesAsync(iptal);

            foreach (var k in yeniler)
            {
                await _olaylar.YazAsync(IsVarligi.Gorev, gorev.Id, GorevOlayTipi.BirimAdinaIslem,
                    $"Gelen kutusuna düştü: birim {k.HedefBirimId}.", iptal: iptal);

                await BirimeBildirAsync(k.HedefBirimId, "Gelen kutunuzda yeni kayıt",
                    $"{k.Konu}{(k.IsTalebi ? " — iş talebi" : " — bilgilendirme")}");
            }
        }
        catch (Exception hata)
        {
            // YUTULUYOR: devir yardımcı bir akış ve görevin onaylanmasını
            // düşürmesine izin verilmez. Yine de loglanıyor, yoksa devrin
            // sessizce çalışmaması fark edilmez.
            _kayit.LogWarning(hata, "Devir kuralları uygulanamadı: görev {GorevId}", gorevId);
        }
    }

    // ── liste ──────────────────────────────────────────────────────────

    public async Task<SayfaliSonuc<GelenKutusuDto>> ListeAsync(
        SayfaIstegi istek, GelenKutusuDurumu? durum, bool altBirimlerDahil,
        CancellationToken iptal = default)
    {
        var kapsam = await _etkinBirim.KapsamAsync(altBirimlerDahil, iptal);

        var sorgu = _context.BirimGelenKutusu
            .AsNoTracking()
            .Where(k => kapsam.Contains(k.HedefBirimId));

        if (durum is { } d) sorgu = sorgu.Where(k => k.Durum == d);

        if (istek.TemizArama is { } ara)
            sorgu = sorgu.Where(k => EF.Functions.ILike(k.Konu, $"%{ara}%"));

        var toplam = await sorgu.LongCountAsync(iptal);

        var kayitlar = await sorgu
            .OrderByDescending(k => k.OlusturmaTarihi)
            .Skip(istek.Atla)
            .Take(istek.Boyut)
            .Select(k => new
            {
                k.Id, k.HedefBirimId, k.KaynakGorevId, k.KaynakBirimId, k.Konu, k.Aciklama,
                k.IsTalebi, k.Durum, k.GorevId, k.Gerekce, k.Isleyen, k.IslemTarihi,
                k.Enlem, k.Boylam, k.Adres, k.OlusturmaTarihi, k.HedefGorevTipiId,
                HedefBirimAd = k.HedefBirim != null ? k.HedefBirim.Ad : null,
                KaynakBirimAd = _context.Birimler
                    .Where(b => b.Id == k.KaynakBirimId).Select(b => b.Ad).FirstOrDefault(),
                KaynakTakipNo = _context.Gorevler
                    .Where(g => g.Id == k.KaynakGorevId).Select(g => g.TakipNo).FirstOrDefault(),
                GorevTakipNo = _context.Gorevler
                    .Where(g => g.Id == k.GorevId).Select(g => g.TakipNo).FirstOrDefault(),
            })
            .ToListAsync(iptal);

        var veriler = kayitlar.Select(k => new GelenKutusuDto
        {
            Id = k.Id,
            HedefBirimId = k.HedefBirimId,
            HedefBirimAd = k.HedefBirimAd,
            KaynakGorevId = k.KaynakGorevId,
            KaynakTakipNo = k.KaynakTakipNo,
            KaynakBirimId = k.KaynakBirimId,
            KaynakBirimAd = k.KaynakBirimAd,
            HedefGorevTipiId = k.HedefGorevTipiId,
            Konu = k.Konu,
            Aciklama = k.Aciklama,
            IsTalebi = k.IsTalebi,
            Durum = k.Durum,
            DurumAd = DurumAdi(k.Durum),
            DurumRenk = DurumRengi(k.Durum),
            GorevId = k.GorevId,
            GorevTakipNo = k.GorevTakipNo,
            Gerekce = k.Gerekce,
            Isleyen = k.Isleyen,
            IslemTarihi = k.IslemTarihi,
            Enlem = k.Enlem,
            Boylam = k.Boylam,
            Adres = k.Adres,
            OlusturmaTarihi = k.OlusturmaTarihi,
        }).ToList();

        return SayfaliSonuc<GelenKutusuDto>.Olustur(veriler, toplam, istek);
    }

    /// <summary>Bekleyen kayıt sayısı — menüdeki rozet.</summary>
    public async Task<int> BekleyenSayisiAsync(CancellationToken iptal = default)
    {
        var kapsam = await _etkinBirim.KapsamAsync(altBirimlerDahil: false, iptal);

        return await _context.BirimGelenKutusu
            .CountAsync(k => kapsam.Contains(k.HedefBirimId)
                          && k.Durum == GelenKutusuDurumu.Bekliyor, iptal);
    }

    // ── karar ──────────────────────────────────────────────────────────

    /// <summary>
    /// Kaydı kabul eder ve HEDEF BİRİMDE görev açar.
    /// </summary>
    public async Task<GelenKutusuDto> KabulAsync(
        long id, GelenKutusuKabulDto istek, CancellationToken iptal = default)
    {
        var kayit = await ErisebilirMiAsync(id, iptal);

        if (kayit.Durum != GelenKutusuDurumu.Bekliyor)
            throw new BusinessRuleException($"Bu kayıt zaten işlenmiş ({DurumAdi(kayit.Durum)}).");

        if (!kayit.IsTalebi)
        {
            throw new BusinessRuleException(
                "Bu bir bilgilendirme kaydı; görev açılmaz. OKUNDU olarak işaretleyin.");
        }

        var gorev = await _gorevler.OlusturAsync(new GorevKayitDto
        {
            Baslik = kayit.Konu,
            Aciklama = kayit.Aciklama,
            GorevTipiId = istek.GorevTipiId ?? kayit.HedefGorevTipiId,
            Oncelik = istek.Oncelik,
            Kaynak = GorevKaynagi.BirimDevri,
            KaynakId = kayit.KaynakGorevId,
            Enlem = kayit.Enlem,
            Boylam = kayit.Boylam,
            Adres = kayit.Adres,
        }, kayit.HedefBirimId, iptal);

        kayit.Durum = GelenKutusuDurumu.Kabul;
        kayit.GorevId = gorev.Id;
        kayit.Isleyen = await _kullanici.GetFullNameAsync();
        kayit.IslemTarihi = DateTime.Now;

        await _context.SaveChangesAsync(iptal);

        await _olaylar.YazAsync(IsVarligi.Gorev, kayit.KaynakGorevId, GorevOlayTipi.Onaylandi,
            $"Devir kabul edildi; {gorev.TakipNo} açıldı.", iptal: iptal);

        await BirimeBildirAsync(kayit.KaynakBirimId, "Devir kabul edildi",
            $"{kayit.Konu} — {gorev.TakipNo}");

        return await TekAsync(kayit.Id, iptal);
    }

    /// <summary>
    /// Kaydı reddeder — KAYNAK BİRİME gerekçeli bildirim gider.
    /// </summary>
    /// <remarks>
    /// Gerekçe zorunlu: gerekçesiz bir ret, kaynağın neyi düzelteceğini
    /// bilmemesi demek ve aynı devir bir daha denenirdi.
    /// </remarks>
    public async Task<GelenKutusuDto> ReddetAsync(
        long id, string gerekce, CancellationToken iptal = default)
    {
        var kayit = await ErisebilirMiAsync(id, iptal);

        if (kayit.Durum != GelenKutusuDurumu.Bekliyor)
            throw new BusinessRuleException($"Bu kayıt zaten işlenmiş ({DurumAdi(kayit.Durum)}).");

        if (string.IsNullOrWhiteSpace(gerekce))
            throw new BusinessRuleException("Ret gerekçesi zorunlu.");

        kayit.Durum = GelenKutusuDurumu.Ret;
        kayit.Gerekce = gerekce.Trim();
        kayit.Isleyen = await _kullanici.GetFullNameAsync();
        kayit.IslemTarihi = DateTime.Now;

        await _context.SaveChangesAsync(iptal);

        await _olaylar.YazAsync(IsVarligi.Gorev, kayit.KaynakGorevId, GorevOlayTipi.Reddedildi,
            $"Devir reddedildi: {gerekce.Trim()}", iptal: iptal);

        await BirimeBildirAsync(kayit.KaynakBirimId, "Devir reddedildi",
            $"{kayit.Konu} — {gerekce.Trim()}");

        return await TekAsync(kayit.Id, iptal);
    }

    /// <summary>Bilgilendirme kaydını okundu işaretler.</summary>
    public async Task<GelenKutusuDto> OkunduAsync(long id, CancellationToken iptal = default)
    {
        var kayit = await ErisebilirMiAsync(id, iptal);

        if (kayit.Durum != GelenKutusuDurumu.Bekliyor)
            throw new BusinessRuleException($"Bu kayıt zaten işlenmiş ({DurumAdi(kayit.Durum)}).");

        if (kayit.IsTalebi)
        {
            throw new BusinessRuleException(
                "Bu bir iş talebi; kabul ya da ret ile karara bağlanmalı.");
        }

        kayit.Durum = GelenKutusuDurumu.Okundu;
        kayit.Isleyen = await _kullanici.GetFullNameAsync();
        kayit.IslemTarihi = DateTime.Now;

        await _context.SaveChangesAsync(iptal);
        return await TekAsync(kayit.Id, iptal);
    }

    // ── iç ─────────────────────────────────────────────────────────────

    private async Task<UnitInbox> ErisebilirMiAsync(long id, CancellationToken iptal)
    {
        var kayit = await _context.BirimGelenKutusu.FirstOrDefaultAsync(k => k.Id == id, iptal)
            ?? throw new EntityNotFoundException("Gelen kutusu kaydı bulunamadı.");

        var kapsam = await _etkinBirim.KapsamAsync(altBirimlerDahil: true, iptal);
        if (!kapsam.Contains(kayit.HedefBirimId))
            throw new EntityNotFoundException("Gelen kutusu kaydı bulunamadı.");

        return kayit;
    }

    private async Task<GelenKutusuDto> TekAsync(long id, CancellationToken iptal)
    {
        var sonuc = await ListeAsync(
            new SayfaIstegi { Boyut = 1 }, null, altBirimlerDahil: true, iptal);

        return sonuc.Veriler.FirstOrDefault(k => k.Id == id)
            ?? (await ListeAsync(new SayfaIstegi { Boyut = 200 }, null, true, iptal))
                .Veriler.First(k => k.Id == id);
    }

    private static string DevirMetni(string takipNo, string? aciklama, string? not)
    {
        var satirlar = new List<string>();
        if (!string.IsNullOrWhiteSpace(not)) satirlar.Add(not.Trim());
        if (!string.IsNullOrWhiteSpace(aciklama)) satirlar.Add(aciklama.Trim());
        satirlar.Add($"\nKaynak görev: {takipNo}");
        return string.Join('\n', satirlar);
    }

    /// <summary>
    /// Birimdeki karar verebilecek kullanıcılara bildirir.
    /// </summary>
    /// <remarks>
    /// Hedef <c>gelenKutusu.karar</c> iznine sahip olanlar. Birimin
    /// <c>Yetkili</c> alanı bir METİN, kullanıcı kimliği değil — onunla
    /// bildirim gönderilemez.
    /// </remarks>
    private async Task BirimeBildirAsync(long birimId, string baslik, string icerik)
    {
        try
        {
            var hedefler = await (
                from ur in _context.UserRoles
                join ri in _context.RolIzinleri on ur.RoleId equals ri.RolId
                join iz in _context.Izinler on ri.IzinAd equals iz.Ad
                join k in _context.Users on ur.UserId equals k.Id
                where iz.Ad == Izinler.GelenKutusuKarar && iz.Kullanimda && k.BirimId == birimId
                select ur.UserId
            ).Distinct().ToListAsync();

            if (hedefler.Count == 0) return;

            /*
              VARLIK `GelenKutusu`, `Gorev` DEĞİL.

              Burada kimlik olarak `0` yazılıyordu ve bildirime dokunan
              kullanıcı hiçbir yere gitmiyordu: gidilecek bir görev kimliği
              yok, çünkü devir KABUL EDİLMEDEN görev oluşmuyor. Doğru hedef
              kaydın kendisi değil, kararın verildiği ekran — gelen kutusu
              listesi. Kimlik yine 0: liste ekranının kimliği olmaz.
            */
            var veri = new TokenDataDto(NotificationEntity.GelenKutusu, 0, NotificationAction.None);

            await _mesajlar.CreateForUsersAsync(
                hedefler, baslik, icerik,
                SendMessageType.PushNotification, NotifikasyonTip.InboxOnReceived, veri.ToJson());
        }
        catch (Exception hata)
        {
            _kayit.LogWarning(hata, "Gelen kutusu bildirimi yazılamadı: birim {BirimId}", birimId);
        }
    }

    public static string DurumAdi(GelenKutusuDurumu durum) => durum switch
    {
        GelenKutusuDurumu.Bekliyor => "Bekliyor",
        GelenKutusuDurumu.Kabul => "Kabul edildi",
        GelenKutusuDurumu.Ret => "Reddedildi",
        GelenKutusuDurumu.Okundu => "Okundu",
        _ => durum.ToString(),
    };

    public static string DurumRengi(GelenKutusuDurumu durum) => durum switch
    {
        GelenKutusuDurumu.Bekliyor => "#A78952",
        GelenKutusuDurumu.Kabul => "#4A7A2B",
        GelenKutusuDurumu.Ret => "#7A1F2B",
        GelenKutusuDurumu.Okundu => "#7C8592",
        _ => "#7C8592",
    };
}
