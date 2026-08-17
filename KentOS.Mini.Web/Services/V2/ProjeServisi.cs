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
/// PROJE — görevlerin çatısı.
/// </summary>
/// <remarks>
/// <para>
/// <b>Proje iş yapmaz, işleri toplar.</b> İlerleme, gecikme ve risk
/// göstergelerinin hepsi altındaki görevlerden hesaplanıyor; projede saklanan
/// bir "yüzde" kolonu yok. Olsaydı görevlerle çelişebilen ikinci bir gerçek
/// doğar ve hangisine bakılacağı belirsiz kalırdı.
/// </para>
/// <para>
/// <b>Görünürlük kapısı birim</b> — görevlerdeki kuralın aynısı. Kapsam dışı
/// proje <c>403</c> değil <c>404</c> döner.
/// </para>
/// </remarks>
public interface IProjeServisi
{
    Task<SayfaliSonuc<ProjeOzetDto>> ListeAsync(ProjeSuzgecDto suzgec, CancellationToken iptal = default);
    Task<ProjeDetayDto> GetirAsync(long id, CancellationToken iptal = default);
    Task<ProjeDetayDto> OlusturAsync(ProjeKayitDto istek, CancellationToken iptal = default);
    Task<ProjeDetayDto> GuncelleAsync(long id, ProjeKayitDto istek, CancellationToken iptal = default);
    Task SilAsync(long id, CancellationToken iptal = default);

    Task<PanoDto> PanoAsync(long id, CancellationToken iptal = default);
    Task<PanoDto> KartTasiAsync(long id, KartTasimaDto istek, CancellationToken iptal = default);
    Task<List<GanttSatiriDto>> GanttAsync(long id, CancellationToken iptal = default);

    Task<KilometreTasiDto> KilometreTasiTamamlaAsync(
        long projeId, long tasId, bool tamamlandi, CancellationToken iptal = default);

    /// <summary>Üye listesini ve proje yöneticisini yazar — TAM LİSTE.</summary>
    Task<ProjeDetayDto> UyeleriYazAsync(
        long id, List<ProjeUyeIstegiDto> uyeler, long? yoneticiId, CancellationToken iptal = default);
}

/// <summary>Proje listesi süzgeci.</summary>
public class ProjeSuzgecDto : SayfaIstegi
{
    public List<ProjeDurumu>? Durumlar { get; set; }
    public long? YoneticiId { get; set; }
    public bool AltBirimlerDahil { get; set; }

    /// <summary>Yalnızca AÇIK projeler — tamamlanan ve iptal edilen düşer.</summary>
    public bool YalnizAcik { get; set; }
}

public class ProjeServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici,
    IEtkinBirim _etkinBirim,
    IIsOlayServisi _olaylar,
    IIsEkServisi _ekler,
    IIsYorumServisi _yorumlar,
    IGorevServisi _gorevler) : IProjeServisi
{
    /// <summary>Kapanmış proje durumları — ölçüm bitti.</summary>
    private static readonly ProjeDurumu[] Kapali = [ProjeDurumu.Tamamlandi, ProjeDurumu.Iptal];

    // ── liste ──────────────────────────────────────────────────────────

    public async Task<SayfaliSonuc<ProjeOzetDto>> ListeAsync(
        ProjeSuzgecDto suzgec, CancellationToken iptal = default)
    {
        var kapsam = await _etkinBirim.KapsamAsync(suzgec.AltBirimlerDahil, iptal);

        var sorgu = _context.Projeler
            .AsNoTracking()
            .Where(p => kapsam.Contains(p.BirimId));

        if (suzgec.Durumlar is { Count: > 0 })
            sorgu = sorgu.Where(p => suzgec.Durumlar.Contains(p.Durum));

        if (suzgec.YalnizAcik)
            sorgu = sorgu.Where(p => !Kapali.Contains(p.Durum));

        if (suzgec.YoneticiId is { } yonetici)
            sorgu = sorgu.Where(p => p.YoneticiId == yonetici);

        if (suzgec.TemizArama is { } ara)
        {
            sorgu = sorgu.Where(p =>
                EF.Functions.ILike(p.Ad, $"%{ara}%") ||
                (p.Kod != null && EF.Functions.ILike(p.Kod, $"%{ara}%")));
        }

        var toplam = await sorgu.LongCountAsync(iptal);

        sorgu = suzgec.Sirala?.ToLowerInvariant() switch
        {
            "ad" => suzgec.Azalan ? sorgu.OrderByDescending(p => p.Ad) : sorgu.OrderBy(p => p.Ad),
            // Bitiş tarihi OLMAYAN proje sona gider: `null` Postgres'te en
            // büyük sayılıyor ve artan sıralamada en yakın teslim en alta
            // düşerdi.
            "bitis" => suzgec.Azalan
                ? sorgu.OrderByDescending(p => p.Bitis == null).ThenByDescending(p => p.Bitis)
                : sorgu.OrderBy(p => p.Bitis == null).ThenBy(p => p.Bitis),
            _ => suzgec.Azalan
                ? sorgu.OrderBy(p => p.OlusturmaTarihi)
                : sorgu.OrderByDescending(p => p.OlusturmaTarihi),
        };

        var idler = await sorgu
            .Skip(suzgec.Atla)
            .Take(suzgec.Boyut)
            .Select(p => p.Id)
            .ToListAsync(iptal);

        return SayfaliSonuc<ProjeOzetDto>.Olustur(await OzetleAsync(idler, iptal), toplam, suzgec);
    }

    public async Task<ProjeDetayDto> GetirAsync(long id, CancellationToken iptal = default)
    {
        var proje = await ErisebilirMiAsync(id, iptal);
        return await DetayaCevirAsync(proje, iptal);
    }

    // ── yazma ──────────────────────────────────────────────────────────

    public async Task<ProjeDetayDto> OlusturAsync(
        ProjeKayitDto istek, CancellationToken iptal = default)
    {
        var birim = await _etkinBirim.IdAsync(iptal);
        if (birim <= 0) throw new BusinessRuleException("Proje açmak için bir birime bağlı olmalısınız.");

        TarihleriDogrula(istek);

        var proje = new Project
        {
            Ad = istek.Ad.Trim(),
            Kod = istek.Kod?.Trim(),
            Aciklama = istek.Aciklama,
            Renk = istek.Renk,
            Durum = istek.Durum,
            BirimId = birim,
            YoneticiId = istek.YoneticiId,
            Baslangic = istek.Baslangic,
            Bitis = istek.Bitis,
            Butce = istek.Butce,
            Enlem = istek.Enlem,
            Boylam = istek.Boylam,
            Adres = istek.Adres,
            Olusturan = await _kullanici.GetFullNameAsync(),
            OlusturmaTarihi = DateTime.Now,
        };

        _context.Projeler.Add(proje);
        await _context.SaveChangesAsync(iptal);

        await AltKayitlariYazAsync(proje.Id, istek, iptal);

        await _olaylar.YazAsync(IsVarligi.Proje, proje.Id, GorevOlayTipi.Olusturuldu,
            $"{proje.Ad} açıldı.", iptal: iptal);

        return await DetayaCevirAsync(proje, iptal);
    }

    public async Task<ProjeDetayDto> GuncelleAsync(
        long id, ProjeKayitDto istek, CancellationToken iptal = default)
    {
        var proje = await ErisebilirMiAsync(id, iptal);
        TarihleriDogrula(istek);

        var eskiDurum = proje.Durum;

        proje.Ad = istek.Ad.Trim();
        proje.Kod = istek.Kod?.Trim();
        proje.Aciklama = istek.Aciklama;
        proje.Renk = istek.Renk;
        proje.Durum = istek.Durum;
        proje.YoneticiId = istek.YoneticiId;
        proje.Baslangic = istek.Baslangic;
        proje.Bitis = istek.Bitis;
        proje.Butce = istek.Butce;
        proje.Enlem = istek.Enlem;
        proje.Boylam = istek.Boylam;
        proje.Adres = istek.Adres;
        proje.GuncellemeTarihi = DateTime.Now;
        proje.Guncelleyen = await _kullanici.GetFullNameAsync();

        // Tamamlanma anı bir KEZ damgalanır ve durum değiştiğinde silinir:
        // aksi hâlde yeniden açılan bir projede eski tamamlanma tarihi
        // kalır ve rapor onu bitmiş sayardı.
        if (proje.Durum == ProjeDurumu.Tamamlandi && eskiDurum != ProjeDurumu.Tamamlandi)
            proje.TamamlanmaTarihi = DateTime.Now;
        else if (proje.Durum != ProjeDurumu.Tamamlandi)
            proje.TamamlanmaTarihi = null;

        await _context.SaveChangesAsync(iptal);
        await AltKayitlariYazAsync(id, istek, iptal);

        if (eskiDurum != proje.Durum)
        {
            await _olaylar.YazAsync(IsVarligi.Proje, id, GorevOlayTipi.DurumDegisti, null,
            [
                new AjandaAlanDegisikligiDto
                {
                    Alan = "Durum",
                    Eski = DurumAdi(eskiDurum),
                    Yeni = DurumAdi(proje.Durum),
                },
            ], iptal);
        }

        return await DetayaCevirAsync(proje, iptal);
    }

    /// <summary>
    /// Projeyi siler — GÖREVLERİ SİLMEZ.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Görevlerin <c>proje_id</c> bağı boşaltılıyor, kendileri duruyor.
    /// Cascade kursaydık bir projeyi silmek altındaki bütün işi, aşama
    /// kanıtlarını ve zaman çizelgesini de götürürdü — proje bir çatı,
    /// işin sahibi değil.
    /// </para>
    /// <para>
    /// Üye, kilometre taşı ve pano sütunu veritabanı cascade'iyle gidiyor;
    /// ek, yorum ve olaylar yabancı anahtar taşımadığı için elle siliniyor.
    /// </para>
    /// </remarks>
    public async Task SilAsync(long id, CancellationToken iptal = default)
    {
        var proje = await ErisebilirMiAsync(id, iptal);

        var tasIdler = await _context.KilometreTaslari
            .Where(k => k.ProjeId == id)
            .Select(k => k.Id)
            .ToListAsync(iptal);

        await _context.Gorevler
            .Where(g => g.ProjeId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(g => g.ProjeId, (long?)null)
                .SetProperty(g => g.KilometreTasiId, (long?)null), iptal);

        foreach (var tasId in tasIdler)
            await _olaylar.VarligaAitleriSilAsync(IsVarligi.KilometreTasi, tasId, iptal);

        await _ekler.VarligaAitleriSilAsync(IsVarligi.Proje, id, iptal);
        await _yorumlar.VarligaAitleriSilAsync(IsVarligi.Proje, id, iptal);
        await _olaylar.VarligaAitleriSilAsync(IsVarligi.Proje, id, iptal);

        await _context.ProjeUyeleri.Where(u => u.ProjeId == id).ExecuteDeleteAsync(iptal);
        await _context.KilometreTaslari.Where(k => k.ProjeId == id).ExecuteDeleteAsync(iptal);
        await _context.PanoSutunlari.Where(s => s.ProjeId == id).ExecuteDeleteAsync(iptal);

        // `Remove` DEĞİL: aynı bağlamda alt kayıtlar izlenmişse ikinci bir
        // DELETE üretiyor ve 0 satır etkilendiği için EF bunu eşzamanlılık
        // çakışması sanıyor. Aynı gerekçe `GorevTipiServisi`de de yazılı.
        _context.Entry(proje).State = EntityState.Detached;
        await _context.Projeler.Where(p => p.Id == id).ExecuteDeleteAsync(iptal);
    }

    // ── kanban ─────────────────────────────────────────────────────────

    public async Task<PanoDto> PanoAsync(long id, CancellationToken iptal = default)
    {
        var proje = await ErisebilirMiAsync(id, iptal);

        var sutunlar = await _context.PanoSutunlari
            .AsNoTracking()
            .Where(s => s.ProjeId == proje.Id)
            .OrderBy(s => s.SiraNo)
            .ToListAsync(iptal);

        var gorevIdler = await _context.Gorevler
            .AsNoTracking()
            .Where(g => g.ProjeId == proje.Id)
            .OrderByDescending(g => g.Oncelik)
            .ThenBy(g => g.SlaBitis == null)
            .ThenBy(g => g.SlaBitis)
            .Select(g => new { g.Id, g.Durum })
            .ToListAsync(iptal);

        var kartlar = await _gorevler.OzetleAsync([.. gorevIdler.Select(g => g.Id)], iptal);
        var kartSozlugu = kartlar.ToDictionary(k => k.Id);

        var pano = new PanoDto { ProjeId = proje.Id };

        // Bir durum birden çok sütuna eşlenmişse kart YALNIZCA İLKİNE düşer.
        // İki sütunda birden görünen kart, aynı işi iki kez saydırırdı.
        var yerlesenler = new HashSet<long>();

        foreach (var sutun in sutunlar)
        {
            var sutunKartlari = gorevIdler
                .Where(g => g.Durum == sutun.GorevDurumu && !yerlesenler.Contains(g.Id))
                .Select(g => g.Id)
                .ToList();

            foreach (var gid in sutunKartlari) yerlesenler.Add(gid);

            pano.Sutunlar.Add(new PanoSutunKartlariDto
            {
                Sutun = SutunaCevir(sutun),
                Kartlar = [.. sutunKartlari
                    .Where(kartSozlugu.ContainsKey)
                    .Select(g => kartSozlugu[g])],
            });
        }

        // Hiçbir sütuna düşmeyenler AYRI listede. Panoda karşılığı olmayan bir
        // durumdaki görev sessizce kaybolsaydı, pano yapılmakta olan işin
        // eksik bir resmini gösterirdi.
        pano.Dagitilmayanlar = [.. gorevIdler
            .Where(g => !yerlesenler.Contains(g.Id))
            .Where(g => kartSozlugu.ContainsKey(g.Id))
            .Select(g => kartSozlugu[g.Id])];

        return pano;
    }

    /// <summary>
    /// Kartı başka sütuna taşır — yani GÖREVİN DURUMUNU değiştirir.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sürükleme kendi başına bir kayıt tutmaz; sütunun eşlendiği duruma
    /// geçiş yapılır ve geçiş <b>durum akışından</b> geçer. Panoyu akışın
    /// dışında tutsaydık, kartı sürükleyerek onay kapısını atlamak mümkün
    /// olurdu.
    /// </para>
    /// <para>
    /// Hedef sütun görevin ZATEN bulunduğu duruma eşliyse hiçbir şey
    /// yapılmıyor: aynı duruma eşlenmiş iki sütun arasında taşımak yalnızca
    /// görsel bir düzenleme.
    /// </para>
    /// </remarks>
    public async Task<PanoDto> KartTasiAsync(
        long id, KartTasimaDto istek, CancellationToken iptal = default)
    {
        var proje = await ErisebilirMiAsync(id, iptal);

        var sutun = await _context.PanoSutunlari
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == istek.HedefSutunId && s.ProjeId == proje.Id, iptal)
            ?? throw new EntityNotFoundException("Pano sütunu bulunamadı.");

        var gorev = await _context.Gorevler
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == istek.GorevId && g.ProjeId == proje.Id, iptal)
            ?? throw new EntityNotFoundException("Görev bu projeye bağlı değil.");

        if (gorev.Durum != sutun.GorevDurumu)
        {
            if (!GorevDurumAkisi.Gecerli(gorev.Durum, sutun.GorevDurumu))
            {
                throw new BusinessRuleException(
                    $"\"{GorevDurumAkisi.Ad(gorev.Durum)}\" durumundaki görev " +
                    $"\"{sutun.Ad}\" sütununa taşınamaz " +
                    $"({GorevDurumAkisi.Ad(sutun.GorevDurumu)} durumuna geçilemiyor).");
            }

            // Görev servisinden geçiyor: SLA damgası, bekleme sayacı, zaman
            // çizelgesi ve bildirimler orada. Durumu doğrudan yazsaydık
            // panodan yapılan değişiklikler bunların hiçbirini tetiklemezdi.
            await _gorevler.DurumDegistirAsync(
                gorev.Id,
                new GorevDurumIstegiDto { Durum = sutun.GorevDurumu },
                iptal);
        }

        return await PanoAsync(id, iptal);
    }

    // ── gantt ──────────────────────────────────────────────────────────

    /// <summary>
    /// Zaman çizelgesi satırları — kilometre taşları ve görevler bir arada.
    /// </summary>
    /// <remarks>
    /// <b>Tarihsiz satır atlanır.</b> Başlangıcı ve bitişi olmayan bir işin
    /// gantt'ta yeri yok; çizmeye çalışmak onu ya bugüne ya da sonsuza
    /// yapıştırırdı ve iki durumda da yanlış bilgi verirdi.
    /// </remarks>
    public async Task<List<GanttSatiriDto>> GanttAsync(long id, CancellationToken iptal = default)
    {
        var proje = await ErisebilirMiAsync(id, iptal);
        var simdi = DateTime.Now;

        var satirlar = new List<GanttSatiriDto>();

        var taslar = await _context.KilometreTaslari
            .AsNoTracking()
            .Where(k => k.ProjeId == proje.Id)
            .OrderBy(k => k.SiraNo)
            .ToListAsync(iptal);

        var tasGorevleri = await _context.Gorevler
            .AsNoTracking()
            .Where(g => g.ProjeId == proje.Id && g.KilometreTasiId != null)
            .Select(g => new { TasId = g.KilometreTasiId!.Value, g.Durum })
            .ToListAsync(iptal);

        foreach (var t in taslar)
        {
            if (t.HedefTarih is null) continue;

            var bagli = tasGorevleri.Where(g => g.TasId == t.Id).ToList();
            var biten = bagli.Count(g => g.Durum == GorevDurumu.Tamamlandi);

            satirlar.Add(new GanttSatiriDto
            {
                Id = t.Id,
                Tur = "kilometreTasi",
                Ad = t.Ad,
                // Kilometre taşı bir NOKTA, bir aralık değil. Çizimde
                // görünebilmesi için başlangıç ile bitiş aynı gün veriliyor;
                // istemci bunu elmas işaretiyle çiziyor.
                Baslangic = t.HedefTarih,
                Bitis = t.HedefTarih,
                Renk = t.Tamamlandi ? "#4A7A2B" : "#A78952",
                Ilerleme = bagli.Count == 0 ? (t.Tamamlandi ? 100 : 0) : biten * 100 / bagli.Count,
                Gecikti = !t.Tamamlandi && t.HedefTarih < simdi,
                DurumAd = t.Tamamlandi ? "Tamamlandı" : "Bekliyor",
            });
        }

        var gorevler = await _context.Gorevler
            .AsNoTracking()
            .Where(g => g.ProjeId == proje.Id)
            .Select(g => new
            {
                g.Id, g.Baslik, g.Durum, g.KilometreTasiId,
                g.PlanlananBaslangic, g.PlanlananBitis, g.BaslamaTarihi,
                g.TamamlanmaTarihi, g.SlaBitis, g.OlusturmaTarihi,
                Toplam = g.Asamalar.Count,
                Biten = g.Asamalar.Count(a => a.Durum != GorevAsamaDurumu.Bekliyor),
            })
            .ToListAsync(iptal);

        foreach (var g in gorevler)
        {
            // Başlangıç: planlanan → gerçekleşen → açılış. Bitiş: planlanan →
            // gerçekleşen → SLA. Hiçbiri yoksa satır çizilmez.
            var bas = g.PlanlananBaslangic ?? g.BaslamaTarihi ?? g.OlusturmaTarihi;
            var bit = g.PlanlananBitis ?? g.TamamlanmaTarihi ?? g.SlaBitis;
            if (bit is null) continue;

            // Ters aralık çizilemez: bitiş başlangıçtan önceyse (elle girilmiş
            // tarih hatası) çubuk negatif genişlikte olurdu.
            if (bit < bas) bit = bas;

            var kapali = GorevDurumAkisi.Kapali(g.Durum);

            satirlar.Add(new GanttSatiriDto
            {
                Id = g.Id,
                Tur = "gorev",
                Ad = g.Baslik,
                Baslangic = bas,
                Bitis = bit,
                Renk = GorevDurumAkisi.Renk(g.Durum),
                Ilerleme = g.Toplam == 0
                    ? (g.Durum == GorevDurumu.Tamamlandi ? 100 : 0)
                    : g.Biten * 100 / g.Toplam,
                Gecikti = !kapali && bit < simdi,
                DurumAd = GorevDurumAkisi.Ad(g.Durum),
                KilometreTasiId = g.KilometreTasiId,
            });
        }

        return [.. satirlar.OrderBy(s => s.Baslangic).ThenBy(s => s.Ad)];
    }

    // ── kilometre taşı ─────────────────────────────────────────────────

    public async Task<KilometreTasiDto> KilometreTasiTamamlaAsync(
        long projeId, long tasId, bool tamamlandi, CancellationToken iptal = default)
    {
        await ErisebilirMiAsync(projeId, iptal);

        var tas = await _context.KilometreTaslari
            .FirstOrDefaultAsync(k => k.Id == tasId && k.ProjeId == projeId, iptal)
            ?? throw new EntityNotFoundException("Kilometre taşı bulunamadı.");

        tas.Tamamlandi = tamamlandi;
        tas.TamamlanmaTarihi = tamamlandi ? DateTime.Now : null;

        await _context.SaveChangesAsync(iptal);

        await _olaylar.YazAsync(IsVarligi.Proje, projeId, GorevOlayTipi.Guncellendi,
            tamamlandi ? $"{tas.Ad} tamamlandı." : $"{tas.Ad} yeniden açıldı.", iptal: iptal);

        var sayilar = await TasSayilariAsync(projeId, iptal);
        return TasaCevir(tas, sayilar, DateTime.Now);
    }

    /// <summary>
    /// Proje ekibini yazar — projenin ötekilerine DOKUNMAZ.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ayrı bir uç ve ayrı bir izin (<c>proje.uyeYonet</c>). Ekibi
    /// düzenlemek ile projenin tarihini, bütçesini ve kilometre taşlarını
    /// değiştirmek farklı ağırlıkta işler: proje yöneticisine ekibini kurma
    /// yetkisi verirken bütçeyi de açmak zorunda kalmamak gerekiyor.
    /// </para>
    /// <para>
    /// Tam kaydetme (<see cref="GuncelleAsync"/>) üye listesini de yazıyor ve
    /// bu bir çelişki değil: her şeyi düzenleyebilen zaten ekibi de
    /// düzenleyebilir. Dar izin geniş olanın alt kümesi.
    /// </para>
    /// </remarks>
    public async Task<ProjeDetayDto> UyeleriYazAsync(
        long id, List<ProjeUyeIstegiDto> uyeler, long? yoneticiId,
        CancellationToken iptal = default)
    {
        var proje = await ErisebilirMiAsync(id, iptal);

        // Yönetici üyeler arasında OLMALI: projeyi yürüten kişinin ekipte
        // görünmemesi, "bu iş kimde?" sorusunu üye listesinden cevaplanamaz
        // kılardı. Ekip kuralının aynısı.
        if (yoneticiId is { } y && y > 0 && uyeler.All(u => u.KullaniciId != y))
            throw new BusinessRuleException("Proje yöneticisi, proje ekibinin üyesi olmalı.");

        await _context.ProjeUyeleri.Where(u => u.ProjeId == id).ExecuteDeleteAsync(iptal);

        foreach (var u in uyeler.DistinctBy(x => x.KullaniciId))
        {
            _context.ProjeUyeleri.Add(new ProjectMember
            {
                ProjeId = id,
                KullaniciId = u.KullaniciId,
                Rol = u.Rol,
                EklenmeTarihi = DateTime.Now,
            });
        }

        proje.YoneticiId = yoneticiId;
        proje.GuncellemeTarihi = DateTime.Now;
        proje.Guncelleyen = await _kullanici.GetFullNameAsync();

        await _context.SaveChangesAsync(iptal);

        await _olaylar.YazAsync(IsVarligi.Proje, id, GorevOlayTipi.Atandi,
            $"Proje ekibi güncellendi ({uyeler.Count} kişi).", iptal: iptal);

        return await DetayaCevirAsync(proje, iptal);
    }

    // ── iç: erişim ve doğrulama ────────────────────────────────────────

    private async Task<Project> ErisebilirMiAsync(long id, CancellationToken iptal)
    {
        var proje = await _context.Projeler.FirstOrDefaultAsync(p => p.Id == id, iptal)
            ?? throw new EntityNotFoundException("Proje bulunamadı.");

        var kapsam = await _etkinBirim.KapsamAsync(altBirimlerDahil: true, iptal);
        if (!kapsam.Contains(proje.BirimId))
            throw new EntityNotFoundException("Proje bulunamadı.");

        return proje;
    }

    /// <summary>Bitiş başlangıçtan önce olamaz — gantt çubuğu negatif genişlik alırdı.</summary>
    private static void TarihleriDogrula(ProjeKayitDto istek)
    {
        if (istek.Baslangic is { } bas && istek.Bitis is { } bit && bit < bas)
            throw new BusinessRuleException("Bitiş tarihi başlangıçtan önce olamaz.");
    }

    /// <summary>
    /// Varsayılan kanban sütunları — NORMAL AKIŞIN TAMAMI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Basladi</c> sütunu ÖNCE yoktu ve tarayıcıda ölçüldüğünde şu çıktı:
    /// başlatılan görev hiçbir sütuna eşleşmiyor, "Sütunsuz"a düşüyor ve
    /// oradan sürüklenemediği için panoda kilitleniyordu. Panonun varsayılanı,
    /// görevin gerçekten geçtiği bütün durumları kapsamak zorunda.
    /// </para>
    /// <para>
    /// <c>Beklemede</c>, <c>IadeEdildi</c> ve <c>Reddedildi</c> BİLEREK yok:
    /// üçü de istisna hâli ve görev ekranından yönetiliyor. Panoya konsalardı
    /// çoğu zaman boş duran üç sütun, asıl işi ekranın dışına iterdi.
    /// </para>
    /// </remarks>
    private static List<PanoSutunuDto> VarsayilanPano() =>
    [
        new() { Ad = "Atandı", GorevDurumu = GorevDurumu.Atandi, Renk = "#0E4C5C" },
        new() { Ad = "Başladı", GorevDurumu = GorevDurumu.Basladi, Renk = "#157F7F" },
        new() { Ad = "Devam ediyor", GorevDurumu = GorevDurumu.DevamEdiyor, Renk = "#1E5FBF" },
        new() { Ad = "Onay bekliyor", GorevDurumu = GorevDurumu.TamamlanmaBekliyor, Renk = "#A78952" },
        new() { Ad = "Tamamlandı", GorevDurumu = GorevDurumu.Tamamlandi, Renk = "#4A7A2B" },
    ];

    private async Task AltKayitlariYazAsync(
        long projeId, ProjeKayitDto istek, CancellationToken iptal)
    {
        await _context.ProjeUyeleri.Where(u => u.ProjeId == projeId).ExecuteDeleteAsync(iptal);
        await _context.PanoSutunlari.Where(s => s.ProjeId == projeId).ExecuteDeleteAsync(iptal);

        /*
          PANO HİÇBİR ZAMAN BOŞ KALMAZ.

          Sütun listesi tam liste olarak yazılıyor; gövdesinde sütun taşımayan
          bir güncelleme panoyu siliyordu ve ölçümde görüldü: kanban sekmesi
          "pano kurulmamış" diyor ama arayüzde sütun ekleme yolu yok, yani
          proje kalıcı olarak panosuz kalıyordu.

          Sıfır sütunlu bir panonun meşru bir kullanımı da yok — bu yüzden
          liste boş kaldığında varsayılan geri kuruluyor. Kurum kendi
          sütunlarını tanımladığında bu dal hiç çalışmaz.
        */
        var sutunlar = istek.PanoSutunlari.Count > 0 ? istek.PanoSutunlari : VarsayilanPano();

        foreach (var u in istek.Uyeler.DistinctBy(x => x.KullaniciId))
        {
            _context.ProjeUyeleri.Add(new ProjectMember
            {
                ProjeId = projeId,
                KullaniciId = u.KullaniciId,
                Rol = u.Rol,
                EklenmeTarihi = DateTime.Now,
            });
        }

        var sutunSira = 1;
        foreach (var s in sutunlar)
        {
            _context.PanoSutunlari.Add(new BoardColumn
            {
                ProjeId = projeId,
                Ad = s.Ad.Trim(),
                SiraNo = sutunSira++,
                Renk = s.Renk,
                GorevDurumu = s.GorevDurumu,
            });
        }

        /*
          KİLOMETRE TAŞLARI SİL-YAZ DEĞİL, EŞLEŞTİRİLİR.

          Üye ve sütundan farklı: görevler `kilometre_tasi_id` ile taşlara
          BAĞLI. Sil-yeniden-yaz yapsaydık her kayıtta yeni kimlikler
          üretilir ve bağlı görevlerin hepsi sahipsiz kalırdı — projeyi
          düzenlemek, görevlerin hangi hedefe ait olduğunu silmek olurdu.
        */
        var mevcut = await _context.KilometreTaslari
            .Where(k => k.ProjeId == projeId)
            .ToListAsync(iptal);

        var kalanIdler = istek.KilometreTaslari.Where(k => k.Id > 0).Select(k => k.Id).ToHashSet();

        foreach (var eski in mevcut.Where(k => !kalanIdler.Contains(k.Id)))
        {
            await _context.Gorevler
                .Where(g => g.KilometreTasiId == eski.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(g => g.KilometreTasiId, (long?)null), iptal);

            _context.KilometreTaslari.Remove(eski);
        }

        var tasSira = 1;
        foreach (var t in istek.KilometreTaslari)
        {
            var kayit = mevcut.FirstOrDefault(k => k.Id == t.Id && t.Id > 0);

            if (kayit is null)
            {
                _context.KilometreTaslari.Add(new Milestone
                {
                    ProjeId = projeId,
                    Ad = t.Ad.Trim(),
                    Aciklama = t.Aciklama,
                    HedefTarih = t.HedefTarih,
                    Tamamlandi = t.Tamamlandi,
                    TamamlanmaTarihi = t.Tamamlandi ? DateTime.Now : null,
                    SiraNo = tasSira++,
                });
            }
            else
            {
                kayit.Ad = t.Ad.Trim();
                kayit.Aciklama = t.Aciklama;
                kayit.HedefTarih = t.HedefTarih;
                kayit.SiraNo = tasSira++;

                if (kayit.Tamamlandi != t.Tamamlandi)
                {
                    kayit.Tamamlandi = t.Tamamlandi;
                    kayit.TamamlanmaTarihi = t.Tamamlandi ? DateTime.Now : null;
                }
            }
        }

        await _context.SaveChangesAsync(iptal);
    }

    // ── iç: dönüştürme ─────────────────────────────────────────────────

    private async Task<List<ProjeOzetDto>> OzetleAsync(List<long> idler, CancellationToken iptal)
    {
        if (idler.Count == 0) return [];

        var simdi = DateTime.Now;

        var projeler = await _context.Projeler
            .AsNoTracking()
            .Where(p => idler.Contains(p.Id))
            .Select(p => new
            {
                p.Id, p.Ad, p.Kod, p.Renk, p.Durum, p.BirimId, p.YoneticiId,
                p.Baslangic, p.Bitis, p.TamamlanmaTarihi, p.Butce,
                p.Enlem, p.Boylam, p.Adres, p.Aciklama, p.Olusturan, p.OlusturmaTarihi,
                BirimAd = p.Birim != null ? p.Birim.Ad : null,
                YoneticiAd = _context.Users
                    .Where(k => k.Id == p.YoneticiId)
                    .Select(k => ((k.Ad ?? "") + " " + (k.Soyad ?? "")).Trim())
                    .FirstOrDefault(),
                UyeSayisi = p.Uyeler.Count,
            })
            .ToListAsync(iptal);

        var gorevSayilari = await _context.Gorevler
            .AsNoTracking()
            .Where(g => g.ProjeId != null && idler.Contains(g.ProjeId.Value))
            .GroupBy(g => g.ProjeId!.Value)
            .Select(g => new
            {
                ProjeId = g.Key,
                Toplam = g.Count(),
                Biten = g.Count(x => x.Durum == GorevDurumu.Tamamlandi),
                Geciken = g.Count(x =>
                    x.SlaBitis != null && x.SlaBitis < simdi &&
                    x.Durum != GorevDurumu.Tamamlandi && x.Durum != GorevDurumu.Iptal),
            })
            .ToDictionaryAsync(x => x.ProjeId, iptal);

        var tasSayilari = await _context.KilometreTaslari
            .AsNoTracking()
            .Where(k => idler.Contains(k.ProjeId))
            .GroupBy(k => k.ProjeId)
            .Select(g => new { ProjeId = g.Key, Toplam = g.Count(), Biten = g.Count(x => x.Tamamlandi) })
            .ToDictionaryAsync(x => x.ProjeId, iptal);

        return [.. idler
            .Select(id => projeler.FirstOrDefault(p => p.Id == id))
            .Where(p => p is not null)
            .Select(p =>
            {
                var g = gorevSayilari.GetValueOrDefault(p!.Id);
                var t = tasSayilari.GetValueOrDefault(p.Id);

                return new ProjeOzetDto
                {
                    Id = p.Id,
                    Ad = p.Ad,
                    Kod = p.Kod,
                    Renk = p.Renk,
                    Durum = p.Durum,
                    DurumAd = DurumAdi(p.Durum),
                    DurumRenk = DurumRengi(p.Durum),
                    BirimId = p.BirimId,
                    BirimAd = p.BirimAd,
                    YoneticiId = p.YoneticiId,
                    YoneticiAd = p.YoneticiAd,
                    Baslangic = p.Baslangic,
                    Bitis = p.Bitis,
                    TamamlanmaTarihi = p.TamamlanmaTarihi,
                    Butce = p.Butce,
                    Enlem = p.Enlem,
                    Boylam = p.Boylam,
                    Adres = p.Adres,
                    UyeSayisi = p.UyeSayisi,
                    GorevToplam = g?.Toplam ?? 0,
                    GorevBiten = g?.Biten ?? 0,
                    GorevGeciken = g?.Geciken ?? 0,
                    KilometreTasiToplam = t?.Toplam ?? 0,
                    KilometreTasiBiten = t?.Biten ?? 0,

                    // Kapanmış proje gecikmiş sayılmaz — ölçüm bitti.
                    Gecikti = !Kapali.Contains(p.Durum) && p.Bitis is { } b && b < simdi,
                };
            })];
    }

    private async Task<ProjeDetayDto> DetayaCevirAsync(Project proje, CancellationToken iptal)
    {
        var ozet = (await OzetleAsync([proje.Id], iptal)).FirstOrDefault()
            ?? throw new EntityNotFoundException("Proje bulunamadı.");

        var detay = new ProjeDetayDto
        {
            Id = ozet.Id, Ad = ozet.Ad, Kod = ozet.Kod, Renk = ozet.Renk,
            Durum = ozet.Durum, DurumAd = ozet.DurumAd, DurumRenk = ozet.DurumRenk,
            BirimId = ozet.BirimId, BirimAd = ozet.BirimAd,
            YoneticiId = ozet.YoneticiId, YoneticiAd = ozet.YoneticiAd,
            Baslangic = ozet.Baslangic, Bitis = ozet.Bitis,
            TamamlanmaTarihi = ozet.TamamlanmaTarihi, Butce = ozet.Butce,
            Enlem = ozet.Enlem, Boylam = ozet.Boylam, Adres = ozet.Adres,
            UyeSayisi = ozet.UyeSayisi,
            GorevToplam = ozet.GorevToplam, GorevBiten = ozet.GorevBiten,
            GorevGeciken = ozet.GorevGeciken,
            KilometreTasiToplam = ozet.KilometreTasiToplam,
            KilometreTasiBiten = ozet.KilometreTasiBiten,
            Gecikti = ozet.Gecikti,

            Aciklama = proje.Aciklama,
            Olusturan = proje.Olusturan,
            OlusturmaTarihi = proje.OlusturmaTarihi,
        };

        detay.Uyeler = await _context.ProjeUyeleri
            .AsNoTracking()
            .Where(u => u.ProjeId == proje.Id)
            .Join(_context.Users, u => u.KullaniciId, k => k.Id, (u, k) => new ProjeUyeDto
            {
                Id = u.Id,
                KullaniciId = u.KullaniciId,
                Ad = ((k.Ad ?? "") + " " + (k.Soyad ?? "")).Trim(),
                Rol = u.Rol,
                YoneticiMi = u.KullaniciId == proje.YoneticiId,
            })
            .ToListAsync(iptal);

        foreach (var u in detay.Uyeler) u.RolAd = UyeRolAdi(u.Rol);
        detay.Uyeler = [.. detay.Uyeler.OrderBy(u => u.Rol).ThenBy(u => u.Ad, StringComparer.CurrentCulture)];

        var simdi = DateTime.Now;
        var sayilar = await TasSayilariAsync(proje.Id, iptal);

        detay.KilometreTaslari = [.. (await _context.KilometreTaslari
            .AsNoTracking()
            .Where(k => k.ProjeId == proje.Id)
            .OrderBy(k => k.SiraNo)
            .ToListAsync(iptal))
            .Select(k => TasaCevir(k, sayilar, simdi))];

        detay.PanoSutunlari = [.. (await _context.PanoSutunlari
            .AsNoTracking()
            .Where(s => s.ProjeId == proje.Id)
            .OrderBy(s => s.SiraNo)
            .ToListAsync(iptal))
            .Select(SutunaCevir)];

        return detay;
    }

    private async Task<Dictionary<long, (int Toplam, int Biten)>> TasSayilariAsync(
        long projeId, CancellationToken iptal)
    {
        var ham = await _context.Gorevler
            .AsNoTracking()
            .Where(g => g.ProjeId == projeId && g.KilometreTasiId != null)
            .GroupBy(g => g.KilometreTasiId!.Value)
            .Select(g => new
            {
                TasId = g.Key,
                Toplam = g.Count(),
                Biten = g.Count(x => x.Durum == GorevDurumu.Tamamlandi),
            })
            .ToListAsync(iptal);

        return ham.ToDictionary(x => x.TasId, x => (x.Toplam, x.Biten));
    }

    private static KilometreTasiDto TasaCevir(
        Milestone k, Dictionary<long, (int Toplam, int Biten)> sayilar, DateTime simdi)
    {
        var s = sayilar.GetValueOrDefault(k.Id);

        return new KilometreTasiDto
        {
            Id = k.Id,
            SiraNo = k.SiraNo,
            Ad = k.Ad,
            Aciklama = k.Aciklama,
            HedefTarih = k.HedefTarih,
            Tamamlandi = k.Tamamlandi,
            TamamlanmaTarihi = k.TamamlanmaTarihi,
            GorevToplam = s.Toplam,
            GorevBiten = s.Biten,
            Gecikti = !k.Tamamlandi && k.HedefTarih is { } h && h < simdi,
        };
    }

    private static PanoSutunuDto SutunaCevir(BoardColumn s) => new()
    {
        Id = s.Id,
        SiraNo = s.SiraNo,
        Ad = s.Ad,
        Renk = s.Renk,
        GorevDurumu = s.GorevDurumu,
        GorevDurumuAd = GorevDurumAkisi.Ad(s.GorevDurumu),
    };

    // ── etiketler (SUNUCUDA üretilir) ──────────────────────────────────

    public static string DurumAdi(ProjeDurumu durum) => durum switch
    {
        ProjeDurumu.Planlaniyor => "Planlanıyor",
        ProjeDurumu.Devam => "Devam ediyor",
        ProjeDurumu.Durduruldu => "Durduruldu",
        ProjeDurumu.Tamamlandi => "Tamamlandı",
        ProjeDurumu.Iptal => "İptal edildi",
        _ => durum.ToString(),
    };

    public static string DurumRengi(ProjeDurumu durum) => durum switch
    {
        ProjeDurumu.Planlaniyor => "#7C8592",
        ProjeDurumu.Devam => "#1E5FBF",
        ProjeDurumu.Durduruldu => "#A65A2E",
        ProjeDurumu.Tamamlandi => "#4A7A2B",
        ProjeDurumu.Iptal => "#4D4D4F",
        _ => "#7C8592",
    };

    public static string UyeRolAdi(ProjeUyeRolu rol) => rol switch
    {
        ProjeUyeRolu.Yonetici => "Yönetici",
        ProjeUyeRolu.Uye => "Üye",
        ProjeUyeRolu.Izleyici => "İzleyici",
        _ => rol.ToString(),
    };
}
