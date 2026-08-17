using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Dto.V2.IsTakip;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>
/// GÖREV — iş takibinin çekirdeği.
/// </summary>
/// <remarks>
/// <para>
/// <b>Tek oluşturma yolu.</b> <see cref="OlusturAsync"/> <c>kaynak</c> ve
/// <c>kaynakId</c> alıyor; vatandaş bildiriminden, talepten, ajandadan ya da
/// projeden görev açmak için ayrı bir yol yazılmayacak. Aksi hâlde SLA
/// damgası, aşama kopyalama ve bildirim mantığı her akış için yeniden
/// yazılırdı ve biri eksik kalırdı.
/// </para>
/// <para>
/// <b>Görünürlük kapısı birim.</b> Kullanıcı yalnızca etkin biriminin (ve
/// istenirse alt ağacının) görevlerini görür. Kapsam dışı bir görev
/// <c>403</c> değil <c>404</c> döner — varlığının bile sızmaması için.
/// </para>
/// </remarks>
public interface IGorevServisi
{
    Task<SayfaliSonuc<GorevOzetDto>> ListeAsync(GorevSuzgecDto suzgec, CancellationToken iptal = default);
    Task<GorevDetayDto> GetirAsync(long id, CancellationToken iptal = default);
    Task<GorevDetayDto> OlusturAsync(GorevKayitDto istek, CancellationToken iptal = default);

    /// <summary>
    /// Görevi BELİRTİLEN birimde açar — vatandaş karşılama akışı için.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Normal yol görevi <b>etkin birimde</b> açıyor. Karşılama personelinin
    /// işi ise tam tersi: gelen bildirimi kendi birimine değil ilgili
    /// müdürlüğe yönlendirmek. Etkin birime bağlı kalsaydı bütün vatandaş
    /// şikayetleri karşılama biriminin iş listesinde birikirdi.
    /// </para>
    /// <para>
    /// <b>Bu, görünürlük kapısını AŞAR</b> ve bilerek öyle: kullanıcı hedef
    /// birimi görmese de oraya iş yazabiliyor. Kapı bu yüzden ucun kendisinde
    /// (<c>bildirim.yonlendir</c> izni); metot istemciye açık bir uç değil.
    /// </para>
    /// </remarks>
    Task<GorevDetayDto> OlusturAsync(
        GorevKayitDto istek, long? hedefBirimId, CancellationToken iptal = default);
    Task<GorevDetayDto> GuncelleAsync(long id, GorevKayitDto istek, CancellationToken iptal = default);
    Task SilAsync(long id, CancellationToken iptal = default);

    Task<GorevDetayDto> AtaAsync(long id, List<GorevAtamaIstegiDto> atamalar, CancellationToken iptal = default);
    Task<GorevDetayDto> DurumDegistirAsync(long id, GorevDurumIstegiDto istek, CancellationToken iptal = default);
    Task<GorevDetayDto> AsamaTamamlaAsync(long gorevId, long asamaId, GorevAsamaIstegiDto istek, CancellationToken iptal = default);

    Task<List<IsOlayDto>> OlaylarAsync(long id, CancellationToken iptal = default);

    /// <summary>
    /// Verilen kimlikleri liste satırına çevirir — GÖRÜNÜRLÜK DENETLEMEZ.
    /// </summary>
    /// <remarks>
    /// Proje panosu ve gantt'ı kendi kapılarından geçirdikleri görevleri
    /// buradan biçimlendiriyor. Kapıyı ikinci kez uygulamıyor çünkü çağıran
    /// zaten projeye erişebildiğini doğruladı ve projeye bağlı görevler o
    /// projenin birimine ait. Bu yüzden metot İSTEMCİYE AÇIK BİR UÇ DEĞİL;
    /// yalnızca servisler arası paylaşım.
    /// </remarks>
    Task<List<GorevOzetDto>> OzetleAsync(List<long> idler, CancellationToken iptal = default);
}

public class GorevServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici,
    IEtkinBirim _etkinBirim,
    IIsOlayServisi _olaylar,
    IIsEkServisi _ekler,
    IIsYorumServisi _yorumlar,
    IEkipServisi _ekipler,
    IMessageService _mesajlar,
    IServiceProvider _saglayici,
    ILogger<GorevServisi> _kayit) : IGorevServisi
{
    /*
      DEVİR SERVİSİ SAĞLAYICIDAN, KURUCUDAN DEĞİL.

      `GelenKutusuServisi` görev açmak için `IGorevServisi`ye bağlı; onu
      buraya kurucudan almak DAİRESEL BAĞIMLILIK olurdu ve DI kapsayıcısı
      açılışta patlardı. Devir yalnızca görev tamamlandığında, yani nadiren
      gerekiyor — o an çözmek doğru karşılığı.
    */
    private IGelenKutusuServisi GelenKutusu =>
        _saglayici.GetRequiredService<IGelenKutusuServisi>();

    // ── liste ──────────────────────────────────────────────────────────

    public async Task<SayfaliSonuc<GorevOzetDto>> ListeAsync(
        GorevSuzgecDto suzgec, CancellationToken iptal = default)
    {
        var kapsam = await _etkinBirim.KapsamAsync(suzgec.AltBirimlerDahil, iptal);

        var sorgu = _context.Gorevler
            .AsNoTracking()
            .Where(g => kapsam.Contains(g.BirimId));

        if (suzgec.Durumlar is { Count: > 0 })
            sorgu = sorgu.Where(g => suzgec.Durumlar.Contains(g.Durum));

        if (suzgec.Oncelikler is { Count: > 0 })
            sorgu = sorgu.Where(g => suzgec.Oncelikler.Contains(g.Oncelik));

        if (suzgec.Kaynaklar is { Count: > 0 })
            sorgu = sorgu.Where(g => suzgec.Kaynaklar.Contains(g.Kaynak));

        if (suzgec.GorevTipiId is { } tip)
            sorgu = sorgu.Where(g => g.GorevTipiId == tip);

        if (suzgec.ProjeId is { } proje)
            sorgu = sorgu.Where(g => g.ProjeId == proje);

        if (suzgec.YalnizKok)
            sorgu = sorgu.Where(g => g.UstGorevId == null);

        if (suzgec.Baslangic is { } bas)
            sorgu = sorgu.Where(g => g.OlusturmaTarihi >= bas);

        if (suzgec.Bitis is { } bit)
            sorgu = sorgu.Where(g => g.OlusturmaTarihi < bit.Date.AddDays(1));

        if (suzgec.KullaniciId is { } kisi)
        {
            sorgu = sorgu.Where(g => _context.GorevAtamalari
                .Any(a => a.GorevId == g.Id && a.KullaniciId == kisi));
        }

        if (suzgec.EkipId is { } ekip)
        {
            sorgu = sorgu.Where(g => _context.GorevAtamalari
                .Any(a => a.GorevId == g.Id && a.EkipId == ekip));
        }

        if (suzgec.YalnizGeciken)
        {
            // Gecikme SUNUCUDA süzülüyor: istemcinin saati yanlışsa gecikme
            // listesi de yanlış olurdu. Kapanmış görev asla geciken sayılmaz —
            // ölçüm bitti.
            var simdi = DateTime.Now;
            sorgu = sorgu.Where(g =>
                g.SlaBitis != null && g.SlaBitis < simdi &&
                g.Durum != GorevDurumu.Tamamlandi && g.Durum != GorevDurumu.Iptal);
        }

        if (suzgec.TemizArama is { } ara)
        {
            sorgu = sorgu.Where(g =>
                EF.Functions.ILike(g.Baslik, $"%{ara}%") ||
                EF.Functions.ILike(g.TakipNo, $"%{ara}%") ||
                (g.Adres != null && EF.Functions.ILike(g.Adres, $"%{ara}%")));
        }

        var toplam = await sorgu.LongCountAsync(iptal);

        sorgu = suzgec.Sirala?.ToLowerInvariant() switch
        {
            // SLA'sı olmayan görev sıralamanın SONUNA gider: `null` Postgres'te
            // varsayılan olarak en büyük sayılır ve artan sıralamada en acil
            // işler en alta düşerdi.
            "sla" => suzgec.Azalan
                ? sorgu.OrderByDescending(g => g.SlaBitis == null).ThenByDescending(g => g.SlaBitis)
                : sorgu.OrderBy(g => g.SlaBitis == null).ThenBy(g => g.SlaBitis),
            "oncelik" => suzgec.Azalan
                ? sorgu.OrderBy(g => g.Oncelik)
                : sorgu.OrderByDescending(g => g.Oncelik),
            "baslik" => suzgec.Azalan
                ? sorgu.OrderByDescending(g => g.Baslik)
                : sorgu.OrderBy(g => g.Baslik),
            "durum" => suzgec.Azalan
                ? sorgu.OrderByDescending(g => g.Durum)
                : sorgu.OrderBy(g => g.Durum),
            _ => suzgec.Azalan
                ? sorgu.OrderBy(g => g.OlusturmaTarihi)
                : sorgu.OrderByDescending(g => g.OlusturmaTarihi),
        };

        var idler = await sorgu
            .Skip(suzgec.Atla)
            .Take(suzgec.Boyut)
            .Select(g => g.Id)
            .ToListAsync(iptal);

        return SayfaliSonuc<GorevOzetDto>.Olustur(await OzetleAsync(idler, iptal), toplam, suzgec);
    }

    // ── tekil ──────────────────────────────────────────────────────────

    public async Task<GorevDetayDto> GetirAsync(long id, CancellationToken iptal = default)
    {
        var gorev = await ErisebilirMiAsync(id, iptal);
        return await DetayaCevirAsync(gorev, iptal);
    }

    public async Task<List<IsOlayDto>> OlaylarAsync(long id, CancellationToken iptal = default)
    {
        // Çizelge, kaydın kendisinden FAZLASINI taşıyor: eski başlıklar, eski
        // atamalar, gerekçeler. Bu yüzden görünürlük kapısı okumada yeniden
        // uygulanıyor — görevi göremeyen çizelgesini de göremez.
        var gorev = await ErisebilirMiAsync(id, iptal);
        return await _olaylar.ListeAsync(IsVarligi.Gorev, gorev.Id, iptal);
    }

    // ── oluşturma ──────────────────────────────────────────────────────

    public Task<GorevDetayDto> OlusturAsync(
        GorevKayitDto istek, CancellationToken iptal = default) =>
        OlusturAsync(istek, null, iptal);

    public async Task<GorevDetayDto> OlusturAsync(
        GorevKayitDto istek, long? hedefBirimId, CancellationToken iptal = default)
    {
        // Hedef birim verilmişse ONA, yoksa etkin birime açılıyor.
        var birim = hedefBirimId is > 0
            ? hedefBirimId.Value
            : await _etkinBirim.IdAsync(iptal);
        if (birim <= 0) throw new BusinessRuleException("Görev açmak için bir birime bağlı olmalısınız.");

        // Alt görev üst görevin BİRİMİNİ devralır: ekip yöneticisi büyük bir
        // işi parçalayıp personeline dağıtırken parçaların başka bir birime
        // kaçmaması gerekiyor.
        WorkTask? ust = null;
        if (istek.UstGorevId is { } ustId)
        {
            ust = await ErisebilirMiAsync(ustId, iptal);
            if (GorevDurumAkisi.Kapali(ust.Durum))
                throw new BusinessRuleException("Kapanmış bir göreve alt görev eklenemez.");

            birim = ust.BirimId;
        }

        var tip = await TipiCozAsync(istek.GorevTipiId, birim, iptal);

        if (tip?.KonumZorunlu == true && (istek.Enlem is null || istek.Boylam is null))
            throw new BusinessRuleException($"\"{tip.Ad}\" tipinde konum zorunlu.");

        var kullaniciBirimi = _kullanici.GetCurrentBirimId();

        var gorev = new WorkTask
        {
            TakipNo = await TakipNoUretAsync(iptal),
            Baslik = istek.Baslik.Trim(),
            Aciklama = istek.Aciklama,
            GorevTipiId = tip?.Id,
            Durum = GorevDurumu.Yeni,
            Oncelik = istek.Oncelik ?? tip?.VarsayilanOncelik ?? GorevOnceligi.Normal,
            Kaynak = istek.Kaynak,
            KaynakId = istek.KaynakId,
            BirimId = birim,
            UstGorevId = ust?.Id,
            ProjeId = istek.ProjeId ?? ust?.ProjeId,
            KilometreTasiId = istek.KilometreTasiId,
            Enlem = istek.Enlem,
            Boylam = istek.Boylam,
            Adres = istek.Adres,
            MahalleId = istek.MahalleId,
            PlanlananBaslangic = istek.PlanlananBaslangic,
            // Hizmet standardı vatandaşa taahhüt edilen süre: planlanan bitiş
            // verilmemişse ondan türetiliyor ki her görevin bir hedefi olsun.
            PlanlananBitis = istek.PlanlananBitis
                ?? (tip?.HizmetStandardiGun is { } gun ? DateTime.Now.Date.AddDays(gun) : null),
            Olusturan = await _kullanici.GetFullNameAsync(),
            OlusturmaTarihi = DateTime.Now,
            OlusturanBirimId = kullaniciBirimi > 0 ? kullaniciBirimi : null,
        };

        _context.Gorevler.Add(gorev);
        await _context.SaveChangesAsync(iptal);

        await AsamalariKopyalaAsync(gorev.Id, tip, iptal);

        await _olaylar.YazAsync(IsVarligi.Gorev, gorev.Id, GorevOlayTipi.Olusturuldu,
            $"{gorev.TakipNo} açıldı.", iptal: iptal);

        // Vekâlet izi AYRI bir olay: "bu işi bize kim yazdı?" sorusu sonradan
        // sorulacak ve cevabı görevin kendi alanlarında değil çizelgede kalmalı.
        if (kullaniciBirimi > 0 && kullaniciBirimi != birim)
        {
            await _olaylar.YazAsync(IsVarligi.Gorev, gorev.Id, GorevOlayTipi.BirimAdinaIslem,
                "Görev başka bir birim adına açıldı.", iptal: iptal);
        }

        if (ust is not null)
        {
            await _olaylar.YazAsync(IsVarligi.Gorev, ust.Id, GorevOlayTipi.AltGorevAcildi,
                $"{gorev.TakipNo} — {gorev.Baslik}", iptal: iptal);
        }

        if (istek.Atamalar.Count > 0)
            await AtamalariYazAsync(gorev, istek.Atamalar, iptal);

        return await DetayaCevirAsync(gorev, iptal);
    }

    public async Task<GorevDetayDto> GuncelleAsync(
        long id, GorevKayitDto istek, CancellationToken iptal = default)
    {
        var gorev = await ErisebilirMiAsync(id, iptal);

        if (GorevDurumAkisi.Kapali(gorev.Durum))
        {
            throw new BusinessRuleException(
                "Kapanmış görev düzenlenemez. Yeniden yapılması gerekiyorsa YENİ görev açın.");
        }

        var tip = istek.GorevTipiId == gorev.GorevTipiId
            ? null
            : await TipiCozAsync(istek.GorevTipiId, gorev.BirimId, iptal);

        // TİP DEĞİŞTİRİLEMEZ. Aşamalar tipten kopyalandı ve bir kısmı
        // tamamlanmış olabilir; tipi değiştirmek ya yapılmış işin kanıtını
        // silmek ya da yeni tipin aşamalarını hiç uygulamamak olurdu.
        if (istek.GorevTipiId != gorev.GorevTipiId && gorev.GorevTipiId is not null)
        {
            throw new BusinessRuleException(
                "Görev tipi sonradan değiştirilemez — aşamalar tipten kopyalanmış durumda. " +
                "Farklı bir tip gerekiyorsa yeni görev açın.");
        }

        var once = new
        {
            gorev.Baslik, gorev.Aciklama, gorev.Oncelik, gorev.Adres,
            gorev.PlanlananBaslangic, gorev.PlanlananBitis,
        };

        gorev.Baslik = istek.Baslik.Trim();
        gorev.Aciklama = istek.Aciklama;
        gorev.Oncelik = istek.Oncelik ?? gorev.Oncelik;
        gorev.Enlem = istek.Enlem;
        gorev.Boylam = istek.Boylam;
        gorev.Adres = istek.Adres;
        gorev.MahalleId = istek.MahalleId;
        gorev.PlanlananBaslangic = istek.PlanlananBaslangic;
        gorev.PlanlananBitis = istek.PlanlananBitis;
        gorev.GuncellemeTarihi = DateTime.Now;
        gorev.Guncelleyen = await _kullanici.GetFullNameAsync();

        // Tip ilk kez atanıyorsa aşamalar şimdi kopyalanır — tipsiz açılmış bir
        // görev sonradan bir standarda bağlanabilsin diye.
        if (tip is not null && gorev.GorevTipiId is null)
        {
            gorev.GorevTipiId = tip.Id;
            await _context.SaveChangesAsync(iptal);
            await AsamalariKopyalaAsync(gorev.Id, tip, iptal);
        }
        else
        {
            await _context.SaveChangesAsync(iptal);
        }

        var farklar = new List<AjandaAlanDegisikligiDto>();
        void Fark(string alan, object? eski, object? yeni)
        {
            var e = eski?.ToString() ?? "";
            var y = yeni?.ToString() ?? "";
            if (e != y) farklar.Add(new AjandaAlanDegisikligiDto { Alan = alan, Eski = e, Yeni = y });
        }

        Fark("Başlık", once.Baslik, gorev.Baslik);
        Fark("Açıklama", once.Aciklama, gorev.Aciklama);
        Fark("Öncelik", GorevDurumAkisi.OncelikAdi(once.Oncelik), GorevDurumAkisi.OncelikAdi(gorev.Oncelik));
        Fark("Adres", once.Adres, gorev.Adres);
        Fark("Planlanan başlangıç", once.PlanlananBaslangic, gorev.PlanlananBaslangic);
        Fark("Planlanan bitiş", once.PlanlananBitis, gorev.PlanlananBitis);

        if (farklar.Count > 0)
        {
            await _olaylar.YazAsync(IsVarligi.Gorev, gorev.Id, GorevOlayTipi.Guncellendi,
                null, farklar, iptal);
        }

        return await DetayaCevirAsync(gorev, iptal);
    }

    /// <summary>
    /// Görevi ve ona bağlı HER ŞEYİ siler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ekler, yorumlar ve çizelge yabancı anahtarla bağlı DEĞİL (çok biçimli
    /// tablolar), dolayısıyla veritabanı cascade'i onları toplamaz. Temizlik
    /// burada elle yapılıyor — yapılmazsa silinen görevlerin dosyaları diskte
    /// ve satırları tabloda sonsuza kadar kalırdı.
    /// </para>
    /// <para>
    /// Alt görevler de siliniyor: sahipsiz kalan bir alt görev hiçbir listede
    /// görünmez ama tabloda durur.
    /// </para>
    /// </remarks>
    public async Task SilAsync(long id, CancellationToken iptal = default)
    {
        var gorev = await ErisebilirMiAsync(id, iptal);

        // Genişlik öncelikli: sıra kökten yapraklara doğru. Silerken ters
        // çevriliyor, yoksa `ust_gorev_id` yabancı anahtarı çocuğu olan bir
        // satırı silmeye izin vermezdi.
        var silinecekler = new List<long>();
        var gorulen = new HashSet<long>();
        var kuyruk = new Queue<long>([gorev.Id]);

        while (kuyruk.Count > 0)
        {
            var mevcut = kuyruk.Dequeue();
            if (!gorulen.Add(mevcut)) continue;
            silinecekler.Add(mevcut);

            var cocuklar = await _context.Gorevler
                .Where(g => g.UstGorevId == mevcut)
                .Select(g => g.Id)
                .ToListAsync(iptal);

            foreach (var c in cocuklar) kuyruk.Enqueue(c);
        }

        foreach (var gorevId in silinecekler)
        {
            var asamaIdler = await _context.GorevAsamalari
                .Where(a => a.GorevId == gorevId)
                .Select(a => a.Id)
                .ToListAsync(iptal);

            foreach (var asamaId in asamaIdler)
            {
                await _ekler.VarligaAitleriSilAsync(IsVarligi.GorevAsama, asamaId, iptal);
                await _yorumlar.VarligaAitleriSilAsync(IsVarligi.GorevAsama, asamaId, iptal);
            }

            await _ekler.VarligaAitleriSilAsync(IsVarligi.Gorev, gorevId, iptal);
            await _yorumlar.VarligaAitleriSilAsync(IsVarligi.Gorev, gorevId, iptal);
            await _olaylar.VarligaAitleriSilAsync(IsVarligi.Gorev, gorevId, iptal);

            await _context.GorevAsamalari.Where(a => a.GorevId == gorevId).ExecuteDeleteAsync(iptal);
            await _context.GorevAtamalari.Where(a => a.GorevId == gorevId).ExecuteDeleteAsync(iptal);
        }

        // Çocuklar önce: `ust_gorev_id` yabancı anahtarı kendine bakıyor.
        foreach (var gorevId in silinecekler.AsEnumerable().Reverse())
            await _context.Gorevler.Where(g => g.Id == gorevId).ExecuteDeleteAsync(iptal);
    }

    // ── atama ──────────────────────────────────────────────────────────

    /// <summary>
    /// Atamaları TAM LİSTE olarak yazar ve yeni atananlara bildirir.
    /// </summary>
    /// <remarks>
    /// Görev <c>Yeni</c> durumundaysa atama onu <c>Atandi</c>'ya taşır: atanmış
    /// ama hâlâ "yeni" görünen bir görev, hiçbir listede doğru yerde
    /// durmazdı.
    /// </remarks>
    public async Task<GorevDetayDto> AtaAsync(
        long id, List<GorevAtamaIstegiDto> atamalar, CancellationToken iptal = default)
    {
        var gorev = await ErisebilirMiAsync(id, iptal);

        if (GorevDurumAkisi.Kapali(gorev.Durum))
            throw new BusinessRuleException("Kapanmış göreve atama yapılamaz.");

        await AtamalariYazAsync(gorev, atamalar, iptal);

        return await DetayaCevirAsync(gorev, iptal);
    }

    // ── durum akışı ────────────────────────────────────────────────────

    public async Task<GorevDetayDto> DurumDegistirAsync(
        long id, GorevDurumIstegiDto istek, CancellationToken iptal = default)
    {
        var gorev = await ErisebilirMiAsync(id, iptal);
        var eski = gorev.Durum;
        var yeni = istek.Durum;

        if (eski == yeni && yeni != GorevDurumu.Atandi)
            throw new BusinessRuleException($"Görev zaten \"{GorevDurumAkisi.Ad(yeni)}\" durumunda.");

        if (!GorevDurumAkisi.Gecerli(eski, yeni))
        {
            throw new BusinessRuleException(
                $"\"{GorevDurumAkisi.Ad(eski)}\" durumundan \"{GorevDurumAkisi.Ad(yeni)}\" " +
                "durumuna geçilemez.");
        }

        // GEREKÇE ZORUNLU. İade, ret ve iptal birinin işini geri çeviriyor;
        // gerekçesiz bir ret, personelin neyi düzelteceğini bilmemesi demek.
        if (yeni is GorevDurumu.IadeEdildi or GorevDurumu.Reddedildi or GorevDurumu.Iptal
            && string.IsNullOrWhiteSpace(istek.Gerekce))
        {
            throw new BusinessRuleException(
                $"\"{GorevDurumAkisi.Ad(yeni)}\" için gerekçe zorunlu.");
        }

        if (yeni == GorevDurumu.Basladi && eski == GorevDurumu.Atandi)
        {
            gorev.BaslamaTarihi ??= DateTime.Now;

            // SLA AÇILIŞTA DEĞİL BAŞLANGIÇTA damgalanıyor: atanmayı bekleyen
            // bir görevin SLA'sını işletmek, henüz kimseye verilmemiş işi
            // geciktirdi diye personele yazmak olurdu.
            var slaSaat = await _context.GorevTipleri
                .Where(t => t.Id == gorev.GorevTipiId)
                .Select(t => t.SlaSaat)
                .FirstOrDefaultAsync(iptal);

            if (slaSaat is > 0)
                gorev.SlaBitis ??= gorev.BaslamaTarihi.Value.AddHours(slaSaat.Value);
        }

        if (yeni == GorevDurumu.TamamlanmaBekliyor)
            await ZorunluAsamalarBittiMiAsync(gorev.Id, iptal);

        if (yeni == GorevDurumu.Tamamlandi)
        {
            gorev.TamamlanmaTarihi = DateTime.Now;
            gorev.Onaylayan = await _kullanici.GetFullNameAsync();
        }

        // BEKLEME SAYACI. Beklemeye girerken damga, çıkarken hem toplam
        // dakikaya ekleniyor hem SLA bitişi o kadar ileri itiliyor — bekleyen
        // işin SLA'sı işlemez.
        if (yeni == GorevDurumu.Beklemede)
        {
            gorev.BeklemeBaslangic = DateTime.Now;
        }
        else if (eski == GorevDurumu.Beklemede && gorev.BeklemeBaslangic is { } basladi)
        {
            var dakika = (int)Math.Max(0, (DateTime.Now - basladi).TotalMinutes);
            gorev.BeklemeDakika += dakika;
            gorev.BeklemeBaslangic = null;

            if (gorev.SlaBitis is { } bitis)
                gorev.SlaBitis = bitis.AddMinutes(dakika);
        }

        if (!string.IsNullOrWhiteSpace(istek.Gerekce))
            gorev.Gerekce = istek.Gerekce.Trim();

        gorev.Durum = yeni;
        gorev.GuncellemeTarihi = DateTime.Now;
        gorev.Guncelleyen = await _kullanici.GetFullNameAsync();

        await _context.SaveChangesAsync(iptal);

        var olayTipi = yeni switch
        {
            GorevDurumu.TamamlanmaBekliyor => GorevOlayTipi.TamamlanmayaGonderildi,
            GorevDurumu.Tamamlandi => GorevOlayTipi.Onaylandi,
            GorevDurumu.IadeEdildi => GorevOlayTipi.IadeEdildi,
            GorevDurumu.Reddedildi => GorevOlayTipi.Reddedildi,
            GorevDurumu.Iptal => GorevOlayTipi.IptalEdildi,
            _ => GorevOlayTipi.DurumDegisti,
        };

        await _olaylar.YazAsync(IsVarligi.Gorev, gorev.Id, olayTipi, istek.Gerekce,
        [
            new AjandaAlanDegisikligiDto
            {
                Alan = "Durum",
                Eski = GorevDurumAkisi.Ad(eski),
                Yeni = GorevDurumAkisi.Ad(yeni),
            },
        ], iptal);

        await DurumBildirAsync(gorev, yeni, istek.Gerekce, iptal);

        // GÖREV ONAYLANINCA DEVİR KURALLARI TETİKLENİR.
        //
        // Tamamlanma anında, iade edilebilir "onay bekliyor" anında değil:
        // henüz kabul edilmemiş bir iş için başka birime kayıt düşürmek,
        // iade hâlinde o birimi boşuna meşgul ederdi.
        if (yeni == GorevDurumu.Tamamlandi)
            await GelenKutusu.DevirleriUygulaAsync(gorev.Id, iptal);

        return await DetayaCevirAsync(gorev, iptal);
    }

    // ── aşama ──────────────────────────────────────────────────────────

    /// <summary>
    /// Sıradaki aşamayı tamamlar ya da atlar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Sıra atlanamaz.</b> Aşamalar bir işin nasıl yapıldığını anlatıyor;
    /// üçüncü adımı ikinciden önce işaretlemek, kanıtı gerçek sıradan koparır.
    /// </para>
    /// <para>
    /// Zorunlu aşama <b>atlanamaz</b>; açıklama ya da fotoğraf zorunluysa
    /// onlar olmadan tamamlanamaz. Kanıt istenmiş bir adımı kanıtsız
    /// kapatabilmek, kuralı hiç yazmamakla aynı şey olurdu.
    /// </para>
    /// </remarks>
    public async Task<GorevDetayDto> AsamaTamamlaAsync(
        long gorevId, long asamaId, GorevAsamaIstegiDto istek, CancellationToken iptal = default)
    {
        var gorev = await ErisebilirMiAsync(gorevId, iptal);

        if (GorevDurumAkisi.Kapali(gorev.Durum))
            throw new BusinessRuleException("Kapanmış görevde aşama ilerletilemez.");

        if (gorev.Durum is GorevDurumu.Yeni or GorevDurumu.Atandi)
            throw new BusinessRuleException("Aşama ilerletmek için önce görevi BAŞLATIN.");

        if (gorev.Durum == GorevDurumu.Beklemede)
            throw new BusinessRuleException("Beklemedeki görevde aşama ilerletilemez.");

        var asamalar = await _context.GorevAsamalari
            .Where(a => a.GorevId == gorevId)
            .OrderBy(a => a.SiraNo)
            .ToListAsync(iptal);

        var asama = asamalar.FirstOrDefault(a => a.Id == asamaId)
            ?? throw new EntityNotFoundException("Aşama bulunamadı.");

        if (asama.Durum != GorevAsamaDurumu.Bekliyor)
            throw new BusinessRuleException($"\"{asama.Ad}\" aşaması zaten kapatılmış.");

        var sirada = asamalar.FirstOrDefault(a => a.Durum == GorevAsamaDurumu.Bekliyor);
        if (sirada is not null && sirada.Id != asama.Id)
        {
            throw new BusinessRuleException(
                $"Sıradaki aşama \"{sirada.Ad}\". Aşamalar sırayla tamamlanır.");
        }

        if (istek.Atla)
        {
            if (asama.Zorunlu)
                throw new BusinessRuleException($"\"{asama.Ad}\" zorunlu bir aşama; atlanamaz.");

            asama.Durum = GorevAsamaDurumu.Atlandi;
        }
        else
        {
            if (asama.AciklamaZorunlu && string.IsNullOrWhiteSpace(istek.Not))
                throw new BusinessRuleException($"\"{asama.Ad}\" aşamasında açıklama zorunlu.");

            if (asama.FotografZorunlu)
            {
                var ekSayisi = await _context.IsEkleri
                    .CountAsync(e => e.VarlikTuru == IsVarligi.GorevAsama && e.VarlikId == asama.Id, iptal);

                if (ekSayisi == 0)
                {
                    throw new BusinessRuleException(
                        $"\"{asama.Ad}\" aşamasında fotoğraf zorunlu. Önce fotoğraf yükleyin.");
                }
            }

            asama.Durum = GorevAsamaDurumu.Tamamlandi;
        }

        asama.Not = istek.Not;
        asama.TamamlanmaTarihi = DateTime.Now;
        asama.Tamamlayan = await _kullanici.GetFullNameAsync();
        asama.TamamlayanId = await _kullanici.GetUserIdAsync();

        // İlk aşama kapatıldığında görev "devam ediyor"a geçiyor: liste
        // ekranında hâlâ "başladı" görünen bir görev, üzerinde çalışıldığını
        // söylemiyordu.
        if (gorev.Durum == GorevDurumu.Basladi
            && GorevDurumAkisi.Gecerli(gorev.Durum, GorevDurumu.DevamEdiyor))
        {
            gorev.Durum = GorevDurumu.DevamEdiyor;
        }

        gorev.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync(iptal);

        await _olaylar.YazAsync(IsVarligi.Gorev, gorev.Id, GorevOlayTipi.AsamaTamamlandi,
            istek.Atla ? $"{asama.Ad} — atlandı" : asama.Ad, iptal: iptal);

        return await DetayaCevirAsync(gorev, iptal);
    }

    // ── iç: erişim ─────────────────────────────────────────────────────

    /// <summary>
    /// Görev etkin birimin kapsamında mı?
    /// </summary>
    /// <remarks>
    /// Kapsam dışıysa <c>403</c> değil <b>404</b> — "yetkiniz yok" demek, o
    /// kimlikte bir görev OLDUĞUNU söylemek olurdu. Alt ağaç her zaman dahil:
    /// bir yönetici bağlı müdürlüğün görevini açabilmeli.
    /// </remarks>
    private async Task<WorkTask> ErisebilirMiAsync(long id, CancellationToken iptal)
    {
        var gorev = await _context.Gorevler.FirstOrDefaultAsync(g => g.Id == id, iptal)
            ?? throw new EntityNotFoundException("Görev bulunamadı.");

        var kapsam = await _etkinBirim.KapsamAsync(altBirimlerDahil: true, iptal);
        if (!kapsam.Contains(gorev.BirimId))
            throw new EntityNotFoundException("Görev bulunamadı.");

        return gorev;
    }

    /// <summary>Tip var mı, kullanımda mı ve bu birim kullanabiliyor mu?</summary>
    private async Task<TaskType?> TipiCozAsync(long? tipId, long birimId, CancellationToken iptal)
    {
        if (tipId is not { } id) return null;

        var tip = await _context.GorevTipleri
            .Include(t => t.Birimler)
            .FirstOrDefaultAsync(t => t.Id == id, iptal)
            ?? throw new EntityNotFoundException("Görev tipi bulunamadı.");

        if (!tip.Kullanimda)
            throw new BusinessRuleException($"\"{tip.Ad}\" tipi kullanımdan kaldırılmış.");

        // Boş birim listesi = herkes kullanabilir.
        if (tip.Birimler.Count > 0 && !tip.Birimler.Any(b => b.BirimId == birimId))
            throw new BusinessRuleException($"\"{tip.Ad}\" tipi bu birimde kullanılamaz.");

        return tip;
    }

    /// <summary>
    /// Takip numarası — <c>GRV-2026-000142</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Yıl içindeki sıradan üretiliyor. Sayısal kimlik telefonda söylenemez ve
    /// dışarıya söylendiğinde sistemdeki toplam iş sayısını da söyler.
    /// </para>
    /// <para>
    /// <b>Yarış durumu kabul ediliyor ve çarpışma yakalanıyor:</b> iki istek
    /// aynı anda aynı numarayı hesaplayabilir. Kolonda tekillik kısıtı var;
    /// çarpışan istek yeniden deniyor. Postgres dizisi kullanmak yıl başında
    /// sıfırlama sorununu getirirdi.
    /// </para>
    /// </remarks>
    private async Task<string> TakipNoUretAsync(CancellationToken iptal)
    {
        var yil = DateTime.Now.Year;
        var onEk = $"GRV-{yil}-";

        var sonuncu = await _context.Gorevler
            .Where(g => g.TakipNo.StartsWith(onEk))
            .OrderByDescending(g => g.TakipNo)
            .Select(g => g.TakipNo)
            .FirstOrDefaultAsync(iptal);

        var sira = 1;
        if (sonuncu is not null && int.TryParse(sonuncu[onEk.Length..], out var son))
            sira = son + 1;

        return $"{onEk}{sira:D6}";
    }

    /// <summary>Tip aşamalarını görevin KOPYASI olarak yazar.</summary>
    /// <remarks>
    /// Kopya, bağ değil: altı ay sonra tipe yeni bir adım eklendiğinde
    /// tamamlanmış görevler eksik görünmemeli. Bir işin kanıtı sonradan
    /// değişemez.
    /// </remarks>
    private async Task AsamalariKopyalaAsync(long gorevId, TaskType? tip, CancellationToken iptal)
    {
        if (tip is null) return;

        var tanimlar = await _context.GorevTipiAsamalari
            .AsNoTracking()
            .Where(a => a.GorevTipiId == tip.Id)
            .OrderBy(a => a.SiraNo)
            .ToListAsync(iptal);

        if (tanimlar.Count == 0) return;

        foreach (var t in tanimlar)
        {
            _context.GorevAsamalari.Add(new WorkTaskStage
            {
                GorevId = gorevId,
                GorevTipiAsamaId = t.Id,
                SiraNo = t.SiraNo,
                Ad = t.Ad,
                Durum = GorevAsamaDurumu.Bekliyor,
                Zorunlu = t.Zorunlu,
                AciklamaZorunlu = t.AciklamaZorunlu,
                FotografZorunlu = t.FotografZorunlu,
            });
        }

        await _context.SaveChangesAsync(iptal);
    }

    private async Task ZorunluAsamalarBittiMiAsync(long gorevId, CancellationToken iptal)
    {
        var eksik = await _context.GorevAsamalari
            .Where(a => a.GorevId == gorevId
                     && a.Zorunlu
                     && a.Durum == GorevAsamaDurumu.Bekliyor)
            .OrderBy(a => a.SiraNo)
            .Select(a => a.Ad)
            .ToListAsync(iptal);

        if (eksik.Count > 0)
        {
            throw new BusinessRuleException(
                "Zorunlu aşamalar tamamlanmadan görev tamamlanmaya gönderilemez. " +
                $"Eksik: {string.Join(", ", eksik)}");
        }
    }

    // ── iç: atama ve bildirim ──────────────────────────────────────────

    private async Task AtamalariYazAsync(
        WorkTask gorev, List<GorevAtamaIstegiDto> atamalar, CancellationToken iptal)
    {
        foreach (var a in atamalar)
        {
            if (a.KullaniciId is null or <= 0 && a.EkipId is null or <= 0)
                throw new BusinessRuleException("Her atamada bir kişi ya da ekip belirtilmeli.");

            if (a.KullaniciId is > 0 && a.EkipId is > 0)
                throw new BusinessRuleException("Bir atama hem kişiye hem ekibe yapılamaz; ayrı satırlar açın.");
        }

        var oncekiler = await _context.GorevAtamalari
            .Where(x => x.GorevId == gorev.Id)
            .ToListAsync(iptal);

        var oncekiAnahtarlar = oncekiler
            .Select(x => (x.KullaniciId, x.EkipId, x.Rol))
            .ToHashSet();

        _context.GorevAtamalari.RemoveRange(oncekiler);

        var atayan = await _kullanici.GetFullNameAsync();
        var yeniler = new List<WorkTaskAssignment>();

        foreach (var a in atamalar.DistinctBy(x => (x.KullaniciId, x.EkipId, x.Rol)))
        {
            yeniler.Add(new WorkTaskAssignment
            {
                GorevId = gorev.Id,
                KullaniciId = a.KullaniciId is > 0 ? a.KullaniciId : null,
                EkipId = a.EkipId is > 0 ? a.EkipId : null,
                Rol = a.Rol,
                Atayan = atayan,
                AtamaTarihi = DateTime.Now,
            });
        }

        _context.GorevAtamalari.AddRange(yeniler);

        // Atama görevi `Yeni`den çıkarır: atanmış ama hâlâ "yeni" görünen bir
        // görev hiçbir listede doğru yerde durmazdı.
        if (gorev.Durum == GorevDurumu.Yeni && yeniler.Count > 0)
        {
            gorev.Durum = GorevDurumu.Atandi;
            gorev.GuncellemeTarihi = DateTime.Now;
        }

        await _context.SaveChangesAsync(iptal);

        await _olaylar.YazAsync(IsVarligi.Gorev, gorev.Id,
            yeniler.Count == 0 ? GorevOlayTipi.AtamaKaldirildi : GorevOlayTipi.Atandi,
            yeniler.Count == 0 ? "Atamalar kaldırıldı." : null, iptal: iptal);

        // YALNIZCA YENİ atananlara bildirim. Tam liste yazıldığı için her
        // güncellemede herkese bildirmek, tek kişi eklendiğinde ekibin
        // tamamını yeniden rahatsız ederdi.
        var hedefler = new HashSet<long>();

        foreach (var y in yeniler)
        {
            if (oncekiAnahtarlar.Contains((y.KullaniciId, y.EkipId, y.Rol))) continue;

            if (y.KullaniciId is { } kisi) hedefler.Add(kisi);

            // EKİBE atamada bildirim ÖNCE lidere gider; iş dağıtımını lider yapar.
            if (y.EkipId is { } ekip)
            {
                foreach (var uye in await _ekipler.BildirimHedefleriAsync(ekip, iptal))
                    hedefler.Add(uye);
            }
        }

        await BildirAsync(hedefler, gorev,
            "Yeni görev", $"{gorev.TakipNo} — {gorev.Baslik}", iptal);
    }

    /// <summary>Durum değişiminde kime haber verilecek?</summary>
    private async Task DurumBildirAsync(
        WorkTask gorev, GorevDurumu yeni, string? gerekce, CancellationToken iptal)
    {
        switch (yeni)
        {
            // Tamamlanma BEYANI yöneticiye gider — onay kapısını o açacak.
            case GorevDurumu.TamamlanmaBekliyor:
                await BildirAsync(await OnaylayabilirlerAsync(gorev.BirimId, iptal), gorev,
                    "Görev onay bekliyor", $"{gorev.TakipNo} — {gorev.Baslik}", iptal);
                break;

            // Onay, iade ve ret PERSONELE gider — işi yapan sonucu öğrenmeli.
            case GorevDurumu.Tamamlandi:
                await BildirAsync(await SorumlularAsync(gorev.Id, iptal), gorev,
                    "Görev onaylandı", $"{gorev.TakipNo} — {gorev.Baslik}", iptal);
                break;

            case GorevDurumu.IadeEdildi:
                await BildirAsync(await SorumlularAsync(gorev.Id, iptal), gorev,
                    "Görev iade edildi", $"{gorev.TakipNo} — {gerekce}", iptal);
                break;

            case GorevDurumu.Reddedildi:
            case GorevDurumu.Iptal:
                // Reddi ve iptali AÇAN bilmeli: işi kimin istediği ile kimin
                // geri çevirdiği farklı kişiler.
                await BildirAsync(await OnaylayabilirlerAsync(gorev.BirimId, iptal), gorev,
                    yeni == GorevDurumu.Iptal ? "Görev iptal edildi" : "Görev reddedildi",
                    $"{gorev.TakipNo} — {gerekce}", iptal);
                break;
        }
    }

    /// <summary>Görevin üzerindeki kişiler (ekip üyeleri dahil).</summary>
    private async Task<HashSet<long>> SorumlularAsync(long gorevId, CancellationToken iptal)
    {
        var atamalar = await _context.GorevAtamalari
            .AsNoTracking()
            .Where(a => a.GorevId == gorevId)
            .Select(a => new { a.KullaniciId, a.EkipId })
            .ToListAsync(iptal);

        var hedefler = new HashSet<long>();

        foreach (var a in atamalar)
        {
            if (a.KullaniciId is { } kisi) hedefler.Add(kisi);
            if (a.EkipId is { } ekip)
            {
                foreach (var uye in await _ekipler.BildirimHedefleriAsync(ekip, iptal))
                    hedefler.Add(uye);
            }
        }

        return hedefler;
    }

    /// <summary>
    /// Birimde <c>gorev.onayla</c> iznine sahip kullanıcılar.
    /// </summary>
    /// <remarks>
    /// Birimin <c>Yetkili</c> alanı bir METİN, kullanıcı kimliği değil — onunla
    /// bildirim gönderilemez. Onaylayan, izni olan kişidir; izin değişince
    /// bildirimin hedefi de kendiliğinden değişir.
    /// </remarks>
    private async Task<HashSet<long>> OnaylayabilirlerAsync(long birimId, CancellationToken iptal)
    {
        var idler = await (
            from ur in _context.UserRoles
            join ri in _context.RolIzinleri on ur.RoleId equals ri.RolId
            join iz in _context.Izinler on ri.IzinAd equals iz.Ad
            join k in _context.Users on ur.UserId equals k.Id
            where iz.Ad == Izinler.GorevOnayla && iz.Kullanimda && k.BirimId == birimId
            select ur.UserId
        ).Distinct().ToListAsync(iptal);

        return [.. idler];
    }

    /// <summary>
    /// Bildirimi mevcut kuyruğa yazar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>NotifikasyonTip.Always</c> ZORUNLU: <c>HasReceiveNotification</c>
    /// başka bir tipte, kullanıcının ayar satırı yoksa <c>false</c> dönüyor ve
    /// bildirim sessizce düşüyor. Yeni bir tip eklemek, aynı işte ayar kolonu
    /// ve <c>switch</c> kolu da eklemeyi gerektirir.
    /// </para>
    /// <para>
    /// <c>NotificationAction.None</c>: iş takip yalnızca web. Yayındaki mobil
    /// sürümler <c>Gorev</c> varlığını tanımıyor; web istemcisi varlık adına
    /// bakarak doğru yere gider.
    /// </para>
    /// <para>
    /// İşlemi yapan kişiye kendi işlemi bildirilmez.
    /// </para>
    /// </remarks>
    private async Task BildirAsync(
        IEnumerable<long> hedefler, WorkTask gorev, string baslik, string icerik,
        CancellationToken iptal)
    {
        try
        {
            var benim = await _kullanici.GetUserIdAsync();

            var liste = hedefler.Where(h => h > 0 && h != benim).Distinct().ToList();
            if (liste.Count == 0) return;

            var veri = new TokenDataDto(
                NotificationEntity.Gorev, (int)gorev.Id, NotificationAction.None);

            await _mesajlar.CreateForUsersAsync(
                liste, baslik, icerik,
                SendMessageType.PushNotification,
                NotifikasyonTip.Always,
                veri.ToJson());
        }
        catch (Exception hata)
        {
            // Bildirim yazılamadı diye görevin kendisi geri alınmaz.
            _kayit.LogWarning(hata, "Görev bildirimi yazılamadı: {GorevId}", gorev.Id);
        }
    }

    // ── iç: dönüştürme ─────────────────────────────────────────────────

    public async Task<List<GorevOzetDto>> OzetleAsync(
        List<long> idler, CancellationToken iptal = default)
    {
        if (idler.Count == 0) return [];

        var gorevler = await _context.Gorevler
            .AsNoTracking()
            .Where(g => idler.Contains(g.Id))
            .Select(g => new
            {
                g.Id, g.TakipNo, g.Baslik, g.Durum, g.Oncelik, g.Kaynak,
                g.GorevTipiId, g.BirimId, g.UstGorevId, g.Enlem, g.Boylam, g.Adres,
                g.PlanlananBitis, g.SlaBitis, g.OlusturmaTarihi, g.TamamlanmaTarihi,
                GorevTipiAd = g.GorevTipi != null ? g.GorevTipi.Ad : null,
                BirimAd = g.Birim != null ? g.Birim.Ad : null,
                AltGorevSayisi = _context.Gorevler.Count(x => x.UstGorevId == g.Id),
            })
            .ToListAsync(iptal);

        var asamalar = await _context.GorevAsamalari
            .AsNoTracking()
            .Where(a => idler.Contains(a.GorevId))
            .Select(a => new { a.GorevId, a.Durum })
            .ToListAsync(iptal);

        var atamalar = await AtamaAdlariAsync(idler, iptal);

        var simdi = DateTime.Now;

        return [.. idler
            .Select(id => gorevler.FirstOrDefault(g => g.Id == id))
            .Where(g => g is not null)
            .Select(g =>
            {
                var kendiAsamalari = asamalar.Where(a => a.GorevId == g!.Id).ToList();
                var kapali = GorevDurumAkisi.Kapali(g!.Durum);

                return new GorevOzetDto
                {
                    Id = g.Id,
                    TakipNo = g.TakipNo,
                    Baslik = g.Baslik,
                    Durum = g.Durum,
                    DurumAd = GorevDurumAkisi.Ad(g.Durum),
                    DurumRenk = GorevDurumAkisi.Renk(g.Durum),
                    Oncelik = g.Oncelik,
                    OncelikAd = GorevDurumAkisi.OncelikAdi(g.Oncelik),
                    Kaynak = g.Kaynak,
                    KaynakAd = GorevDurumAkisi.KaynakAdi(g.Kaynak),
                    GorevTipiId = g.GorevTipiId,
                    GorevTipiAd = g.GorevTipiAd,
                    BirimId = g.BirimId,
                    BirimAd = g.BirimAd,
                    UstGorevId = g.UstGorevId,
                    AltGorevSayisi = g.AltGorevSayisi,
                    Enlem = g.Enlem,
                    Boylam = g.Boylam,
                    Adres = g.Adres,
                    PlanlananBitis = g.PlanlananBitis,
                    SlaBitis = g.SlaBitis,
                    OlusturmaTarihi = g.OlusturmaTarihi,
                    TamamlanmaTarihi = g.TamamlanmaTarihi,

                    // Kapanmış görev asla geciken sayılmaz: ölçüm bitti,
                    // listede kırmızı durması yalnızca gürültü olurdu.
                    Gecikti = !kapali && g.SlaBitis is { } s && s < simdi,
                    KalanSaat = kapali || g.SlaBitis is null
                        ? null
                        : Math.Round((g.SlaBitis.Value - simdi).TotalHours, 1),

                    AsamaToplam = kendiAsamalari.Count,
                    AsamaBiten = kendiAsamalari.Count(a => a.Durum != GorevAsamaDurumu.Bekliyor),
                    Sorumlular = atamalar.TryGetValue(g.Id, out var s2) ? s2 : [],
                };
            })];
    }

    /// <summary>Görev başına atanan kişi/ekip adları — liste satırındaki "kimde?".</summary>
    private async Task<Dictionary<long, List<string>>> AtamaAdlariAsync(
        List<long> idler, CancellationToken iptal)
    {
        var ham = await _context.GorevAtamalari
            .AsNoTracking()
            .Where(a => idler.Contains(a.GorevId))
            .Select(a => new
            {
                a.GorevId,
                a.Rol,
                KisiAd = _context.Users
                    .Where(k => k.Id == a.KullaniciId)
                    .Select(k => ((k.Ad ?? "") + " " + (k.Soyad ?? "")).Trim())
                    .FirstOrDefault(),
                EkipAd = a.Ekip != null ? a.Ekip.Ad : null,
            })
            .ToListAsync(iptal);

        return ham
            .Where(a => a.Rol != GorevAtamaRolu.Izleyici)
            .GroupBy(a => a.GorevId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => a.KisiAd ?? a.EkipAd)
                      .Where(ad => !string.IsNullOrWhiteSpace(ad))
                      .Select(ad => ad!)
                      .Distinct()
                      .ToList());
    }

    private async Task<GorevDetayDto> DetayaCevirAsync(WorkTask gorev, CancellationToken iptal)
    {
        var ozetler = await OzetleAsync([gorev.Id], iptal);
        var ozet = ozetler.FirstOrDefault()
            ?? throw new EntityNotFoundException("Görev bulunamadı.");

        var detay = new GorevDetayDto
        {
            Id = ozet.Id,
            TakipNo = ozet.TakipNo,
            Baslik = ozet.Baslik,
            Durum = ozet.Durum,
            DurumAd = ozet.DurumAd,
            DurumRenk = ozet.DurumRenk,
            Oncelik = ozet.Oncelik,
            OncelikAd = ozet.OncelikAd,
            Kaynak = ozet.Kaynak,
            KaynakAd = ozet.KaynakAd,
            GorevTipiId = ozet.GorevTipiId,
            GorevTipiAd = ozet.GorevTipiAd,
            BirimId = ozet.BirimId,
            BirimAd = ozet.BirimAd,
            UstGorevId = ozet.UstGorevId,
            AltGorevSayisi = ozet.AltGorevSayisi,
            Enlem = ozet.Enlem,
            Boylam = ozet.Boylam,
            Adres = ozet.Adres,
            PlanlananBitis = ozet.PlanlananBitis,
            SlaBitis = ozet.SlaBitis,
            OlusturmaTarihi = ozet.OlusturmaTarihi,
            TamamlanmaTarihi = ozet.TamamlanmaTarihi,
            Gecikti = ozet.Gecikti,
            KalanSaat = ozet.KalanSaat,
            AsamaToplam = ozet.AsamaToplam,
            AsamaBiten = ozet.AsamaBiten,
            Sorumlular = ozet.Sorumlular,

            Aciklama = gorev.Aciklama,
            Gerekce = gorev.Gerekce,
            MahalleId = gorev.MahalleId,
            PlanlananBaslangic = gorev.PlanlananBaslangic,
            BaslamaTarihi = gorev.BaslamaTarihi,
            BeklemeDakika = gorev.BeklemeDakika,
            Olusturan = gorev.Olusturan,
            Onaylayan = gorev.Onaylayan,
            OlusturanBirimId = gorev.OlusturanBirimId,
            ProjeId = gorev.ProjeId,
            KilometreTasiId = gorev.KilometreTasiId,

            SonrakiDurumlar = [.. GorevDurumAkisi.Sonraki(gorev.Durum)
                .Where(d => d != gorev.Durum)
                .Select(d => new GorevDurumSecenegiDto
                {
                    Durum = d,
                    Ad = GorevDurumAkisi.Ad(d),
                    Renk = GorevDurumAkisi.Renk(d),
                })],
        };

        detay.MahalleAd = gorev.MahalleId is null ? null : await _context.Mahalleler
            .Where(m => m.Id == gorev.MahalleId)
            .Select(m => m.Ad)
            .FirstOrDefaultAsync(iptal);

        detay.OlusturanBirimAd = gorev.OlusturanBirimId is null ? null : await _context.Birimler
            .Where(b => b.Id == gorev.OlusturanBirimId)
            .Select(b => b.Ad)
            .FirstOrDefaultAsync(iptal);

        var asamalar = await _context.GorevAsamalari
            .AsNoTracking()
            .Where(a => a.GorevId == gorev.Id)
            .OrderBy(a => a.SiraNo)
            .ToListAsync(iptal);

        var asamaIdler = asamalar.Select(a => a.Id).ToList();

        var ekSayilari = await _context.IsEkleri
            .AsNoTracking()
            .Where(e => e.VarlikTuru == IsVarligi.GorevAsama && asamaIdler.Contains(e.VarlikId))
            .GroupBy(e => e.VarlikId)
            .Select(g => new { AsamaId = g.Key, Sayi = g.Count() })
            .ToDictionaryAsync(x => x.AsamaId, x => x.Sayi, iptal);

        var siradaki = asamalar.FirstOrDefault(a => a.Durum == GorevAsamaDurumu.Bekliyor);

        detay.Asamalar = [.. asamalar.Select(a => new GorevAsamaDto
        {
            Id = a.Id,
            SiraNo = a.SiraNo,
            Ad = a.Ad,
            Durum = a.Durum,
            DurumAd = AsamaDurumAdi(a.Durum),
            Zorunlu = a.Zorunlu,
            AciklamaZorunlu = a.AciklamaZorunlu,
            FotografZorunlu = a.FotografZorunlu,
            Not = a.Not,
            TamamlanmaTarihi = a.TamamlanmaTarihi,
            Tamamlayan = a.Tamamlayan,
            EkSayisi = ekSayilari.TryGetValue(a.Id, out var sayi) ? sayi : 0,
            Sirada = siradaki is not null && siradaki.Id == a.Id,
        })];

        detay.Atamalar = await _context.GorevAtamalari
            .AsNoTracking()
            .Where(a => a.GorevId == gorev.Id)
            .Select(a => new GorevAtamaDto
            {
                Id = a.Id,
                KullaniciId = a.KullaniciId,
                KullaniciAd = _context.Users
                    .Where(k => k.Id == a.KullaniciId)
                    .Select(k => ((k.Ad ?? "") + " " + (k.Soyad ?? "")).Trim())
                    .FirstOrDefault(),
                EkipId = a.EkipId,
                EkipAd = a.Ekip != null ? a.Ekip.Ad : null,
                Rol = a.Rol,
                Atayan = a.Atayan,
                AtamaTarihi = a.AtamaTarihi,
            })
            .ToListAsync(iptal);

        foreach (var a in detay.Atamalar)
            a.RolAd = AtamaRolAdi(a.Rol);

        var altIdler = await _context.Gorevler
            .AsNoTracking()
            .Where(g => g.UstGorevId == gorev.Id)
            .OrderBy(g => g.OlusturmaTarihi)
            .Select(g => g.Id)
            .ToListAsync(iptal);

        detay.AltGorevler = await OzetleAsync(altIdler, iptal);

        return detay;
    }

    private static string AsamaDurumAdi(GorevAsamaDurumu durum) => durum switch
    {
        GorevAsamaDurumu.Bekliyor => "Bekliyor",
        GorevAsamaDurumu.Tamamlandi => "Tamamlandı",
        GorevAsamaDurumu.Atlandi => "Atlandı",
        _ => durum.ToString(),
    };

    private static string AtamaRolAdi(GorevAtamaRolu rol) => rol switch
    {
        GorevAtamaRolu.Sorumlu => "Sorumlu",
        GorevAtamaRolu.Yardimci => "Yardımcı",
        GorevAtamaRolu.Izleyici => "İzleyici",
        _ => rol.ToString(),
    };
}
