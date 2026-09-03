using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Kalem.Application.Dto.Randevu;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Services;
using KentOS.Kalem.Web.Services.V2;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// Talebin etkinliğe dönüştürülmesi.
///
/// <para>
/// Akış şu: memur talebi girer, yetkili (çoğu zaman başkan) onaylar, ekleme
/// işini personel yapar. Yani <b>onaylayan ile ekleyen aynı kişi değil</b> ve
/// aradaki haberleşme bildirimlerle yürüyor.
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class TalepAjandayaEklemeTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;

    private const long Birim = 1;
    private const long EkleyenId = 1;
    private const string Ekleyen = "ekleyen";

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    private async Task TemizleAsync()
    {
        using var b = _ortam.Baglam();
        await b.Database.ExecuteSqlRawAsync(
            "TRUNCATE notlar, randevular, ajandalar RESTART IDENTITY CASCADE;");
        await _ortam.TemelVerileriKurAsync();
        if (!await b.RandevuDurumlar.AnyAsync(x => x.Id == 1))
        {
            b.RandevuDurumlar.Add(new RandevuDurum { Id = 1, DurumAd = "Beklemede", Renk = "#999" });
            b.RandevuDurumlar.Add(new RandevuDurum { Id = 2, DurumAd = "Onaylandı", Renk = "#0a0" });
        }
        // `randevular.mahalle_id` NOT NULL.
        if (!await b.Mahalleler.AnyAsync(x => x.Id == 1))
        {
            b.Mahalleler.Add(new Mahalle { Id = 1, Ad = "Merkez" });
        }
        await b.SaveChangesAsync();
    }

    private (RandevuService servis, SahteMesajServisi mesaj, AppDbContext baglam) Kur()
    {
        var baglam = _ortam.Baglam();
        var mesaj = new SahteMesajServisi();
        var servis = new RandevuService(
            baglam,
            new SahteKullaniciServisi(EkleyenId, Ekleyen, Birim),
            mesaj,
            NullLogger<RandevuService>.Instance,
            new TestServisFabrikasi.SahteDepo(),
            new OzgecmisServisi(baglam,
                new SahteKullaniciServisi(EkleyenId, Ekleyen, Birim),
                mesaj,
                new TestServisFabrikasi.SahteDepo()),
            _ortam.Mapper);
        return (servis, mesaj, baglam);
    }

    private async Task<long> TalepYazAsync()
    {
        using var db = _ortam.Baglam();
        var r = new Randevu
        {
            Konu = "Yol talebi",
            Ad = "Ayşe",
            Soyad = "Yılmaz",
            Telefon = "05551112233",
            BaslangicTarih = DateTime.Now.AddDays(1),
            // Şemada NOT NULL — entity isteğe bağlı ilan etse de sütun zorunlu.
            BitisTarih = DateTime.Now.AddDays(1).AddMinutes(30),
            BirimId = Birim,
            RandevuTipId = 1,
            RandevuDurumId = 2,
            MahalleId = 1,
            OlusturmaTarih = DateTime.Now,
        };
        db.Randevular.Add(r);
        await db.SaveChangesAsync();
        return r.Id;
    }

    private static RandevuToAjandaDto Istek(long talepId, DateTime ne) => new()
    {
        RandevuId = talepId,
        BaslangicTarih = ne,
        AjandaDurumId = 1,
    };

    [Fact]
    public async Task Talep_AJANDAYA_EKLENDI_isaretlenir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var talepId = await TalepYazAsync();
        var (servis, _, baglam) = Kur();
        using (baglam)
        {
            await servis.TalebiEtkinligeCevirAsync(Istek(talepId, DateTime.Now.AddDays(10)));
        }

        using var kontrol = _ortam.Baglam();
        var talep = await kontrol.Randevular.FirstAsync(r => r.Id == talepId);

        // Bayrak HİÇ yazılmıyordu: etkinlik oluşuyor ama talep bunu bilmiyor,
        // listede "Ajandada: Hayır" görünüyor ve "eklenmemiş" süzgeci her şeyi
        // döndürüyordu.
        Assert.True(talep.AjandaDurum);
    }

    [Fact]
    public async Task ILERI_bir_tarihe_eklenebilir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var talepId = await TalepYazAsync();
        var hedef = new DateTime(2026, 12, 24, 14, 30, 0);

        long etkinlikId;
        var (servis, _, baglam) = Kur();
        using (baglam)
        {
            etkinlikId = await servis.TalebiEtkinligeCevirAsync(Istek(talepId, hedef));
        }

        using var kontrol = _ortam.Baglam();
        var etkinlik = await kontrol.Ajandalar.FirstAsync(a => a.Id == etkinlikId);

        // Talebin kendi tarihi değil, ekleyenin SEÇTİĞİ tarih geçerli:
        // vatandaş bugün başvurur, görüşme haftaya olur.
        Assert.Equal(hedef, etkinlik.BaslangicTarihi);
        Assert.Equal(hedef.AddMinutes(30), etkinlik.BitisTarihi);
    }

    [Fact]
    public async Task Olusan_etkinligin_SAHIBI_ekleyendir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var talepId = await TalepYazAsync();
        long etkinlikId;
        var (servis, _, baglam) = Kur();
        using (baglam)
        {
            etkinlikId = await servis.TalebiEtkinligeCevirAsync(Istek(talepId, DateTime.Now.AddDays(3)));
        }

        using var kontrol = _ortam.Baglam();
        var etkinlik = await kontrol.Ajandalar.FirstAsync(a => a.Id == etkinlikId);

        // `Ajanda.KullaniciId` kullanıcı ADI tutuyor ve boş kalınca etkinliğin
        // sahibi olmuyordu: kayıt bilgilerinde "ekleyen" boş çıkıyor, gizlilik
        // kuralındaki oluşturan eşleşmesi de hiç tutmuyordu.
        Assert.Equal(Ekleyen, etkinlik.KullaniciId);
    }

    [Fact]
    public async Task Bildirim_ETKINLIGI_acar_talebi_degil()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var talepId = await TalepYazAsync();
        long etkinlikId;
        SahteMesajServisi mesajServisi;

        var (servis, mesaj, baglam) = Kur();
        mesajServisi = mesaj;
        using (baglam)
        {
            etkinlikId = await servis.TalebiEtkinligeCevirAsync(Istek(talepId, DateTime.Now.AddDays(5)));
        }

        var bildirim = Assert.Single(mesajServisi.BirimeGidenler);

        // Bildirimin haber verdiği şey artık TAKVİMDEKİ kayıt. Talebe
        // götürmek, kullanıcıyı "eklendi mi?" diye bakacağı yere değil
        // geldiği yere geri gönderiyordu.
        Assert.Contains("\"Ajanda\"", bildirim.Data);
        Assert.Contains($"\"id\":{etkinlikId}", bildirim.Data);
    }

    [Fact]
    public async Task Durum_degisiminde_bildirim_TALEBI_acar()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var talepId = await TalepYazAsync();
        SahteMesajServisi mesajServisi;

        var (servis, mesaj, baglam) = Kur();
        mesajServisi = mesaj;
        using (baglam)
        {
            await servis.ChangeDurumAsync(talepId, 2);
        }

        var bildirim = Assert.Single(mesajServisi.BirimeGidenler);

        // Burada tam tersi: kullanıcı talebe gitmeli ki onu ajandaya
        // ekleyebilsin. Başkan onaylar, personel ekler.
        Assert.Contains("\"Talep\"", bildirim.Data);
        Assert.Contains($"\"id\":{talepId}", bildirim.Data);
    }

    [Fact]
    public async Task Zaman_cizelgesine_not_dusulur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var talepId = await TalepYazAsync();
        var (servis, _, baglam) = Kur();
        using (baglam)
        {
            await servis.TalebiEtkinligeCevirAsync(Istek(talepId, new DateTime(2026, 12, 1, 9, 0, 0)));
        }

        using var kontrol = _ortam.Baglam();
        var notlar = await kontrol.Notlar.Where(n => n.RandevuId == talepId).ToListAsync();

        // Talebin geçmişinde "ne zamana eklendi" bilgisi kalmalı; başka biri
        // aynı talebi ikinci kez eklemeye kalkmasın.
        var not = Assert.Single(notlar);
        Assert.Equal("Ajandaya Ekleme", not.Tip);
        Assert.Contains("01.12.2026 09:00", not.Not);
    }

    [Fact]
    public async Task Olmayan_talep_HATA_firlatir_sessizce_false_donmez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, _, baglam) = Kur();
        using (baglam)
        {
            // Eski yol `catch { return false; }` ile yutuyordu: kullanıcı
            // "eklenemedi" görüyor ama sebebini kimse öğrenemiyordu.
            await Assert.ThrowsAnyAsync<Exception>(() =>
                servis.TalebiEtkinligeCevirAsync(Istek(999999, DateTime.Now)));
        }
    }
}
