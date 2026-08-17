using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Dto.V2.IsTakip;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>
/// VATANDAŞ BİLDİRİMİ — dışarıdan gelen ilk kayıt.
/// </summary>
/// <remarks>
/// <para>
/// Uygulamanın <b>ilk anonim yazma yolu</b>. Bu yüzden buradaki her kural
/// ötekilerden daha dikkatli: kimliği doğrulanmamış bir kaynak veritabanına
/// satır yazıyor.
/// </para>
/// <para>
/// <b>Bildirim görev değildir.</b> Kayıt karşılama ekranında bekliyor; bir
/// personel okuyup birime yönlendiriyor ve ancak o zaman görev doğuyor.
/// Doğrudan görev açsaydık mükerrer ve konusuz bildirimler birimlerin iş
/// listesini kullanılamaz hâle getirirdi.
/// </para>
/// </remarks>
public interface IVatandasBildirimServisi
{
    /// <summary>Doğrulama kodu üretir ve SMS kuyruğuna yazar.</summary>
    Task KodGonderAsync(string telefon, string? ip, CancellationToken iptal = default);

    /// <summary>Kodu doğrular ve kısa ömürlü bilet döner.</summary>
    Task<DogrulamaSonucuDto> KodDogrulaAsync(string telefon, string kod, string? ip,
        CancellationToken iptal = default);

    /// <summary>Bildirimi kaydeder. Yalnızca takip numarası döner.</summary>
    Task<VatandasBildirimiSonucuDto> BildirAsync(VatandasBildirimiIstegiDto istek, string? ip,
        CancellationToken iptal = default);

    /// <summary>Bildirime fotoğraf ekler — kısa ömürlü yükleme anahtarıyla.</summary>
    Task FotografEkleAsync(string yuklemeAnahtari, IsYuklenenDosya dosya,
        CancellationToken iptal = default);

    // ── personel tarafı ────────────────────────────────────────────────

    Task<SayfaliSonuc<VatandasBildirimiDto>> ListeAsync(SayfaIstegi istek,
        VatandasBildirimDurumu? durum, CancellationToken iptal = default);

    Task<VatandasBildirimiDto> GetirAsync(long id, CancellationToken iptal = default);

    Task<VatandasBildirimiDto> YonlendirAsync(long id, BildirimYonlendirmeDto istek,
        CancellationToken iptal = default);

    Task<VatandasBildirimiDto> ReddetAsync(long id, string not, CancellationToken iptal = default);
}

public class VatandasBildirimServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici,
    IIsEkServisi _ekler,
    IIsOlayServisi _olaylar,
    IGorevServisi _gorevler,
    IMessageService _mesajlar,
    ILogger<VatandasBildirimServisi> _kayit) : IVatandasBildirimServisi
{
    /// <summary>Kodun ömrü. Kısa: SMS gelmesi saniyeler sürüyor.</summary>
    private static readonly TimeSpan KodOmru = TimeSpan.FromMinutes(5);

    /// <summary>Biletin ömrü — kullanıcının formu doldurma süresi.</summary>
    private static readonly TimeSpan BiletOmru = TimeSpan.FromMinutes(30);

    /// <summary>Yükleme anahtarının ömrü — fotoğraflar hemen sonra geliyor.</summary>
    private static readonly TimeSpan YuklemeOmru = TimeSpan.FromMinutes(15);

    /// <summary>Bir kod için en fazla deneme. Dört hane sınırsız denemede saniyede bulunur.</summary>
    private const int EnFazlaDeneme = 5;

    /// <summary>Aynı numaradan bir saatte açılabilecek bildirim.</summary>
    private const int SaatlikBildirimSiniri = 3;

    /// <summary>Bir bildirime yüklenebilecek fotoğraf.</summary>
    private const int EnFazlaFotograf = 5;

    // ── doğrulama ──────────────────────────────────────────────────────

    public async Task KodGonderAsync(string telefon, string? ip, CancellationToken iptal = default)
    {
        var sade = TelefonSadelestir(telefon);

        /*
          NUMARA BAŞINA SINIR — hız sınırlayıcıya EK olarak.

          `AddRateLimiter` IP başına çalışıyor; bir saldırgan farklı IP'lerden
          aynı numaraya SMS yağdırabilir. Bu, kuruma para ve numara sahibine
          rahatsızlık olarak yansır. Sınır burada numaranın kendisine bağlı.
        */
        var sonDakika = DateTime.Now.AddMinutes(-1);
        var sonKod = await _context.TelefonDogrulamalari
            .AsNoTracking()
            .Where(d => d.TelefonSade == sade && d.OlusturmaTarihi > sonDakika)
            .AnyAsync(iptal);

        if (sonKod)
            throw new BusinessRuleException("Az önce kod gönderildi. Bir dakika sonra tekrar deneyin.");

        // Kod 6 HANE: dört hane 10 000 ihtimal demek ve beş denemelik sınıra
        // rağmen çok sayıda paralel oturumla anlamlı bir başarı şansı bırakır.
        var kod = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();

        _context.TelefonDogrulamalari.Add(new PhoneVerification
        {
            TelefonSade = sade,
            KodKarmasi = Karma(sade, kod),
            Gecerlilik = DateTime.Now.Add(KodOmru),
            Ip = ip,
            OlusturmaTarihi = DateTime.Now,
        });

        await _context.SaveChangesAsync(iptal);

        // SMS mevcut kuyruktan gidiyor — ikinci bir gönderim yolu açmak,
        // kimlik bilgilerinin iki yerde tutulması demekti.
        await _mesajlar.CreateAsync(
            0, sade, "Doğrulama",
            $"Bildirim doğrulama kodunuz: {kod}. {(int)KodOmru.TotalMinutes} dakika geçerlidir.",
            SendMessageType.SMS, NotifikasyonTip.Always, null);
    }

    public async Task<DogrulamaSonucuDto> KodDogrulaAsync(
        string telefon, string kod, string? ip, CancellationToken iptal = default)
    {
        var sade = TelefonSadelestir(telefon);
        var simdi = DateTime.Now;

        var kayit = await _context.TelefonDogrulamalari
            .Where(d => d.TelefonSade == sade && !d.Dogrulandi)
            .OrderByDescending(d => d.OlusturmaTarihi)
            .FirstOrDefaultAsync(iptal);

        // Aynı mesaj: "kod yok" ile "kod yanlış" ayrımı, hangi numaraların
        // sistemde kod beklediğini dışarıya söylerdi.
        const string Hata = "Doğrulama kodu geçersiz ya da süresi dolmuş.";

        if (kayit is null || kayit.Gecerlilik < simdi) throw new BusinessRuleException(Hata);

        if (kayit.Deneme >= EnFazlaDeneme)
        {
            throw new BusinessRuleException(
                "Çok fazla hatalı deneme yapıldı. Yeni kod isteyin.");
        }

        // Deneme sayacı DOĞRULAMADAN ÖNCE artıyor: sonra artırsaydık, isteği
        // yarıda kesen bir saldırgan sayacı hiç ilerletmeden deneyebilirdi.
        kayit.Deneme++;
        await _context.SaveChangesAsync(iptal);

        if (!SabitZamanliEsit(kayit.KodKarmasi, Karma(sade, kod.Trim())))
            throw new BusinessRuleException(Hata);

        kayit.Dogrulandi = true;
        await _context.SaveChangesAsync(iptal);

        var gecerlilik = simdi.Add(BiletOmru);
        return new DogrulamaSonucuDto
        {
            Bilet = BiletUret(sade, gecerlilik),
            Gecerlilik = gecerlilik,
        };
    }

    // ── bildirim ───────────────────────────────────────────────────────

    public async Task<VatandasBildirimiSonucuDto> BildirAsync(
        VatandasBildirimiIstegiDto istek, string? ip, CancellationToken iptal = default)
    {
        var sade = TelefonSadelestir(istek.Telefon);

        if (!BiletGecerliMi(istek.Bilet, sade))
            throw new BusinessRuleException("Telefon doğrulaması geçersiz ya da süresi dolmuş.");

        // NUMARA BAŞINA BİLDİRİM SINIRI. Doğrulanmış bir numara bile sınırsız
        // kayıt açamaz: tek bir doğrulamayla yüzlerce bildirim yazmak,
        // karşılama ekranını kullanılamaz hâle getirirdi.
        var birSaatOnce = DateTime.Now.AddHours(-1);
        var sonBirSaat = await _context.VatandasBildirimleri
            .CountAsync(b => b.TelefonSade == sade && b.OlusturmaTarihi > birSaatOnce, iptal);

        if (sonBirSaat >= SaatlikBildirimSiniri)
        {
            throw new BusinessRuleException(
                "Bu numaradan son bir saatte çok sayıda bildirim alındı. Lütfen sonra tekrar deneyin.");
        }

        var bildirim = new CitizenReport
        {
            TakipNo = await TakipNoUretAsync(iptal),
            AdSoyad = istek.AdSoyad.Trim(),
            Telefon = istek.Telefon.Trim(),
            TelefonSade = sade,
            Konu = istek.Konu.Trim(),
            Aciklama = istek.Aciklama.Trim(),
            Enlem = istek.Enlem,
            Boylam = istek.Boylam,
            Adres = istek.Adres,
            MahalleId = istek.MahalleId,
            Durum = VatandasBildirimDurumu.Yeni,
            Ip = ip,
            OlusturmaTarihi = DateTime.Now,
        };

        _context.VatandasBildirimleri.Add(bildirim);
        await _context.SaveChangesAsync(iptal);

        await _olaylar.YazAsync(IsVarligi.VatandasBildirimi, bildirim.Id,
            GorevOlayTipi.Olusturuldu, $"{bildirim.TakipNo} alındı.", iptal: iptal);

        // Takip numarası SMS ile de gidiyor: portalı kapatan vatandaşın elinde
        // kaydın alındığına dair başka bir kanıt kalmıyor.
        try
        {
            await _mesajlar.CreateAsync(
                0, sade, "Bildiriminiz alındı",
                $"Bildiriminiz alınmıştır. Takip numaranız: {bildirim.TakipNo}",
                SendMessageType.SMS, NotifikasyonTip.Always, null);
        }
        catch (Exception hata)
        {
            // SMS gitmedi diye kayıt geri alınmaz: bildirim veritabanında ve
            // takip numarası ekranda da gösteriliyor.
            _kayit.LogWarning(hata, "Bildirim SMS'i kuyruğa yazılamadı: {TakipNo}", bildirim.TakipNo);
        }

        return new VatandasBildirimiSonucuDto
        {
            TakipNo = bildirim.TakipNo,
            YuklemeAnahtari = YuklemeAnahtariUret(bildirim.Id, DateTime.Now.Add(YuklemeOmru)),
        };
    }

    /// <summary>
    /// Bildirime fotoğraf ekler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Anahtar bildirimin KİMLİĞİNİ taşıyor ve imzalı: kimlik doğrudan
    /// alınsaydı herkes sıralı bir sayı deneyerek başkasının bildirimine
    /// fotoğraf ekleyebilirdi.
    /// </para>
    /// <para>
    /// Fotoğraflar <c>StorageArea.Private</c>'a yazılıyor — vatandaşın evinin
    /// önünü gösteren bir görüntünün bağlantısı tahmin edilebilir olmamalı.
    /// </para>
    /// </remarks>
    public async Task FotografEkleAsync(
        string yuklemeAnahtari, IsYuklenenDosya dosya, CancellationToken iptal = default)
    {
        var bildirimId = YuklemeAnahtariCoz(yuklemeAnahtari)
            ?? throw new BusinessRuleException("Yükleme anahtarı geçersiz ya da süresi dolmuş.");

        var varMi = await _context.VatandasBildirimleri
            .AnyAsync(b => b.Id == bildirimId, iptal);

        if (!varMi) throw new EntityNotFoundException("Bildirim bulunamadı.");

        var mevcut = await _context.IsEkleri
            .CountAsync(e => e.VarlikTuru == IsVarligi.VatandasBildirimi && e.VarlikId == bildirimId, iptal);

        if (mevcut >= EnFazlaFotograf)
            throw new BusinessRuleException($"En fazla {EnFazlaFotograf} fotoğraf yüklenebilir.");

        await _ekler.EkleAsync(IsVarligi.VatandasBildirimi, bildirimId, dosya, null, iptal);
    }

    // ── personel tarafı ────────────────────────────────────────────────

    public async Task<SayfaliSonuc<VatandasBildirimiDto>> ListeAsync(
        SayfaIstegi istek, VatandasBildirimDurumu? durum, CancellationToken iptal = default)
    {
        var sorgu = _context.VatandasBildirimleri.AsNoTracking();

        if (durum is { } d) sorgu = sorgu.Where(b => b.Durum == d);

        if (istek.TemizArama is { } ara)
        {
            sorgu = sorgu.Where(b =>
                EF.Functions.ILike(b.Konu, $"%{ara}%") ||
                EF.Functions.ILike(b.AdSoyad, $"%{ara}%") ||
                EF.Functions.ILike(b.TakipNo, $"%{ara}%") ||
                EF.Functions.ILike(b.TelefonSade, $"%{ara}%"));
        }

        var toplam = await sorgu.LongCountAsync(iptal);

        var idler = await sorgu
            .OrderByDescending(b => b.OlusturmaTarihi)
            .Skip(istek.Atla)
            .Take(istek.Boyut)
            .Select(b => b.Id)
            .ToListAsync(iptal);

        return SayfaliSonuc<VatandasBildirimiDto>.Olustur(
            await YukleAsync(idler, iptal), toplam, istek);
    }

    public async Task<VatandasBildirimiDto> GetirAsync(long id, CancellationToken iptal = default)
    {
        var liste = await YukleAsync([id], iptal);
        return liste.FirstOrDefault() ?? throw new EntityNotFoundException("Bildirim bulunamadı.");
    }

    /// <summary>
    /// Bildirimi bir birime yönlendirir ve GÖREV AÇAR.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Görev, <see cref="IGorevServisi.OlusturAsync"/> üzerinden açılıyor:
    /// aşama kopyalama, SLA damgası ve bildirimler orada. Görevi doğrudan
    /// yazsaydık bu akış hiçbirini tetiklemezdi.
    /// </para>
    /// <para>
    /// <b>Bir bildirim bir kez yönlendirilir.</b> İkinci çağrı reddediliyor,
    /// yoksa aynı şikayet için birden çok görev açılırdı.
    /// </para>
    /// </remarks>
    public async Task<VatandasBildirimiDto> YonlendirAsync(
        long id, BildirimYonlendirmeDto istek, CancellationToken iptal = default)
    {
        var bildirim = await _context.VatandasBildirimleri
            .FirstOrDefaultAsync(b => b.Id == id, iptal)
            ?? throw new EntityNotFoundException("Bildirim bulunamadı.");

        if (bildirim.Durum != VatandasBildirimDurumu.Yeni)
        {
            throw new BusinessRuleException(
                $"Bu bildirim zaten işlenmiş ({DurumAdi(bildirim.Durum)}).");
        }

        var birimVarMi = await _context.Birimler.AnyAsync(b => b.Id == istek.BirimId, iptal);
        if (!birimVarMi) throw new EntityNotFoundException("Birim bulunamadı.");

        var gorev = await _gorevler.OlusturAsync(new GorevKayitDto
        {
            Baslik = bildirim.Konu,
            Aciklama = VatandasMetni(bildirim, istek.Not),
            GorevTipiId = istek.GorevTipiId,
            Oncelik = istek.Oncelik,
            Kaynak = GorevKaynagi.Vatandas,
            KaynakId = bildirim.Id,
            Enlem = bildirim.Enlem,
            Boylam = bildirim.Boylam,
            Adres = bildirim.Adres,
            MahalleId = bildirim.MahalleId,
        }, istek.BirimId, iptal);

        bildirim.Durum = VatandasBildirimDurumu.Yonlendirildi;
        bildirim.BirimId = istek.BirimId;
        bildirim.GorevId = gorev.Id;
        bildirim.IslemNotu = istek.Not;
        bildirim.Isleyen = await _kullanici.GetFullNameAsync();
        bildirim.IslemTarihi = DateTime.Now;

        await _context.SaveChangesAsync(iptal);

        // FOTOĞRAFLAR GÖREVE KOPYALANMIYOR, PAYLAŞILIYOR.
        //
        // Ek kayıtları bildirime bağlı kalıyor; görev ekranı onları
        // bildirimden okuyor. Kopyalasaydık aynı dosya iki kez depolanır ve
        // biri silindiğinde ötekinin ne olacağı belirsiz kalırdı.
        await _olaylar.YazAsync(IsVarligi.VatandasBildirimi, bildirim.Id,
            GorevOlayTipi.Atandi, $"{gorev.TakipNo} görevi açıldı.", iptal: iptal);

        await VatandasaHaberVerAsync(bildirim,
            $"Bildiriminiz ({bildirim.TakipNo}) ilgili birime iletilmiştir.");

        return await GetirAsync(bildirim.Id, iptal);
    }

    public async Task<VatandasBildirimiDto> ReddetAsync(
        long id, string not, CancellationToken iptal = default)
    {
        var bildirim = await _context.VatandasBildirimleri
            .FirstOrDefaultAsync(b => b.Id == id, iptal)
            ?? throw new EntityNotFoundException("Bildirim bulunamadı.");

        if (bildirim.Durum != VatandasBildirimDurumu.Yeni)
        {
            throw new BusinessRuleException(
                $"Bu bildirim zaten işlenmiş ({DurumAdi(bildirim.Durum)}).");
        }

        if (string.IsNullOrWhiteSpace(not))
            throw new BusinessRuleException("Ret gerekçesi zorunlu.");

        bildirim.Durum = VatandasBildirimDurumu.Reddedildi;
        bildirim.IslemNotu = not.Trim();
        bildirim.Isleyen = await _kullanici.GetFullNameAsync();
        bildirim.IslemTarihi = DateTime.Now;

        await _context.SaveChangesAsync(iptal);

        await _olaylar.YazAsync(IsVarligi.VatandasBildirimi, bildirim.Id,
            GorevOlayTipi.Reddedildi, not.Trim(), iptal: iptal);

        // Ret gerekçesi vatandaşa AYNEN gitmiyor: iç not ile vatandaşa
        // söylenen ayrı şeyler olabilir ("mükerrer kayıt" iç bilgi).
        await VatandasaHaberVerAsync(bildirim,
            $"Bildiriminiz ({bildirim.TakipNo}) değerlendirilmiş olup işleme alınmamıştır.");

        return await GetirAsync(bildirim.Id, iptal);
    }

    // ── iç ─────────────────────────────────────────────────────────────

    private async Task VatandasaHaberVerAsync(CitizenReport bildirim, string metin)
    {
        try
        {
            await _mesajlar.CreateAsync(0, bildirim.TelefonSade, "Bildiriminiz", metin,
                SendMessageType.SMS, NotifikasyonTip.Always, null);
        }
        catch (Exception hata)
        {
            _kayit.LogWarning(hata, "Vatandaş SMS'i yazılamadı: {TakipNo}", bildirim.TakipNo);
        }
    }

    /// <summary>Görev açıklamasına vatandaşın anlattığını ve iletişimini koyar.</summary>
    private static string VatandasMetni(CitizenReport b, string? not)
    {
        var satirlar = new List<string> { b.Aciklama };

        if (!string.IsNullOrWhiteSpace(not)) satirlar.Add($"\nKarşılama notu: {not.Trim()}");

        satirlar.Add($"\nBildiren: {b.AdSoyad} · {b.Telefon}");
        satirlar.Add($"Takip no: {b.TakipNo}");

        return string.Join('\n', satirlar);
    }

    private async Task<List<VatandasBildirimiDto>> YukleAsync(
        List<long> idler, CancellationToken iptal)
    {
        if (idler.Count == 0) return [];

        var kayitlar = await _context.VatandasBildirimleri
            .AsNoTracking()
            .Where(b => idler.Contains(b.Id))
            .Select(b => new
            {
                b.Id, b.TakipNo, b.AdSoyad, b.Telefon, b.TelefonSade, b.Konu, b.Aciklama,
                b.Enlem, b.Boylam, b.Adres, b.MahalleId, b.Durum, b.BirimId, b.GorevId,
                b.IslemNotu, b.Isleyen, b.IslemTarihi, b.OlusturmaTarihi,
                MahalleAd = b.Mahalle != null ? b.Mahalle.Ad : null,
                BirimAd = b.Birim != null ? b.Birim.Ad : null,
                GorevTakipNo = _context.Gorevler
                    .Where(g => g.Id == b.GorevId)
                    .Select(g => g.TakipNo)
                    .FirstOrDefault(),
            })
            .ToListAsync(iptal);

        var ekSayilari = await _context.IsEkleri
            .AsNoTracking()
            .Where(e => e.VarlikTuru == IsVarligi.VatandasBildirimi && idler.Contains(e.VarlikId))
            .GroupBy(e => e.VarlikId)
            .Select(g => new { BildirimId = g.Key, Sayi = g.Count() })
            .ToDictionaryAsync(x => x.BildirimId, x => x.Sayi, iptal);

        // MÜKERRER SAYACI: aynı numaradan gelmiş ÖNCEKİ kayıtlar. Karşılama
        // ekranının en sık işi mükerrer ayıklamak; sayı görünmeseydi personel
        // her kaydı sıfırdan değerlendirir ve aynı çukur için beş görev açardı.
        var numaralar = kayitlar.Select(k => k.TelefonSade).Distinct().ToList();
        var numaraSayilari = await _context.VatandasBildirimleri
            .AsNoTracking()
            .Where(b => numaralar.Contains(b.TelefonSade))
            .GroupBy(b => b.TelefonSade)
            .Select(g => new { Telefon = g.Key, Sayi = g.Count() })
            .ToDictionaryAsync(x => x.Telefon, x => x.Sayi, iptal);

        return [.. idler
            .Select(id => kayitlar.FirstOrDefault(k => k.Id == id))
            .Where(k => k is not null)
            .Select(k => new VatandasBildirimiDto
            {
                Id = k!.Id,
                TakipNo = k.TakipNo,
                AdSoyad = k.AdSoyad,
                Telefon = k.Telefon,
                Konu = k.Konu,
                Aciklama = k.Aciklama,
                Enlem = k.Enlem,
                Boylam = k.Boylam,
                Adres = k.Adres,
                MahalleId = k.MahalleId,
                MahalleAd = k.MahalleAd,
                Durum = k.Durum,
                DurumAd = DurumAdi(k.Durum),
                DurumRenk = DurumRengi(k.Durum),
                BirimId = k.BirimId,
                BirimAd = k.BirimAd,
                GorevId = k.GorevId,
                GorevTakipNo = k.GorevTakipNo,
                IslemNotu = k.IslemNotu,
                Isleyen = k.Isleyen,
                IslemTarihi = k.IslemTarihi,
                OlusturmaTarihi = k.OlusturmaTarihi,
                EkSayisi = ekSayilari.GetValueOrDefault(k.Id),
                AyniNumaradanOnceki = Math.Max(0, numaraSayilari.GetValueOrDefault(k.TelefonSade) - 1),
            })];
    }

    private async Task<string> TakipNoUretAsync(CancellationToken iptal)
    {
        var onEk = $"VB-{DateTime.Now.Year}-";

        var sonuncu = await _context.VatandasBildirimleri
            .Where(b => b.TakipNo.StartsWith(onEk))
            .OrderByDescending(b => b.TakipNo)
            .Select(b => b.TakipNo)
            .FirstOrDefaultAsync(iptal);

        var sira = 1;
        if (sonuncu is not null && int.TryParse(sonuncu[onEk.Length..], out var son))
            sira = son + 1;

        return $"{onEk}{sira:D6}";
    }

    /// <summary>
    /// Telefonu yalnızca rakama indirger ve ülke kodunu ayıklar.
    /// </summary>
    /// <remarks>
    /// Vatandaş numarayı beş farklı biçimde yazıyor. Ham metinle eşleştirme
    /// yapılsaydı aynı kişi her biçimde yeniden hız sınırı hakkı kazanırdı.
    /// </remarks>
    public static string TelefonSadelestir(string telefon)
    {
        var rakamlar = new string([.. (telefon ?? "").Where(char.IsDigit)]);

        // `+90 532…`, `0532…` ve `532…` aynı numara.
        if (rakamlar.StartsWith("90") && rakamlar.Length > 10) rakamlar = rakamlar[2..];
        if (rakamlar.StartsWith('0') && rakamlar.Length > 10) rakamlar = rakamlar[1..];

        return rakamlar;
    }

    private static string Karma(string telefon, string kod) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{telefon}:{kod}")));

    /// <summary>Karşılaştırma SABİT ZAMANLI — süre farkı kodu sızdırabilir.</summary>
    private static bool SabitZamanliEsit(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    // ── bilet ve yükleme anahtarı ──────────────────────────────────────
    //
    // İkisi de KISA ÖMÜRLÜ, İMZALI metinler; veritabanında satır tutmuyorlar.
    // Sunucunun JWT anahtarıyla imzalanıyorlar: ayrı bir sır tanımlamak,
    // kurulumda unutulabilecek bir ayar daha demekti.

    private static byte[]? _imzaAnahtari;

    /// <summary>İmza anahtarını bir kez kurar (Program.cs'ten çağrılır).</summary>
    public static void ImzaAnahtariniKur(string sir) =>
        _imzaAnahtari = Encoding.UTF8.GetBytes(sir);

    private static string Imzala(string govde)
    {
        var anahtar = _imzaAnahtari
            ?? throw new InvalidOperationException("Bilet imza anahtarı kurulmadı.");

        var imza = HMACSHA256.HashData(anahtar, Encoding.UTF8.GetBytes(govde));
        return $"{govde}.{Convert.ToHexString(imza)}";
    }

    private static string? ImzayiCoz(string? jeton)
    {
        if (string.IsNullOrWhiteSpace(jeton)) return null;

        var ayrac = jeton.LastIndexOf('.');
        if (ayrac <= 0) return null;

        var govde = jeton[..ayrac];
        var imza = jeton[(ayrac + 1)..];

        return SabitZamanliEsit(Imzala(govde)[(govde.Length + 1)..], imza) ? govde : null;
    }

    private static string BiletUret(string telefon, DateTime gecerlilik) =>
        Imzala($"{telefon}|{gecerlilik.Ticks}");

    private static bool BiletGecerliMi(string? bilet, string telefon)
    {
        if (ImzayiCoz(bilet) is not { } govde) return false;

        var parcalar = govde.Split('|');
        if (parcalar.Length != 2) return false;

        // Telefon biletin İÇİNDE: başka bir numarayla alınmış geçerli bir
        // bileti bu numara için kullanmak mümkün olmamalı.
        if (parcalar[0] != telefon) return false;

        return long.TryParse(parcalar[1], out var tik) && new DateTime(tik) > DateTime.Now;
    }

    private static string YuklemeAnahtariUret(long bildirimId, DateTime gecerlilik) =>
        Imzala($"y{bildirimId}|{gecerlilik.Ticks}");

    private static long? YuklemeAnahtariCoz(string? anahtar)
    {
        if (ImzayiCoz(anahtar) is not { } govde) return null;

        var parcalar = govde.Split('|');
        if (parcalar.Length != 2 || !parcalar[0].StartsWith('y')) return null;

        if (!long.TryParse(parcalar[0][1..], out var id)) return null;
        if (!long.TryParse(parcalar[1], out var tik) || new DateTime(tik) <= DateTime.Now) return null;

        return id;
    }

    // ── etiketler ──────────────────────────────────────────────────────

    public static string DurumAdi(VatandasBildirimDurumu durum) => durum switch
    {
        VatandasBildirimDurumu.Yeni => "Bekliyor",
        VatandasBildirimDurumu.Yonlendirildi => "Yönlendirildi",
        VatandasBildirimDurumu.Reddedildi => "İşleme alınmadı",
        _ => durum.ToString(),
    };

    public static string DurumRengi(VatandasBildirimDurumu durum) => durum switch
    {
        VatandasBildirimDurumu.Yeni => "#A78952",
        VatandasBildirimDurumu.Yonlendirildi => "#4A7A2B",
        VatandasBildirimDurumu.Reddedildi => "#7A1F2B",
        _ => "#7C8592",
    };
}
