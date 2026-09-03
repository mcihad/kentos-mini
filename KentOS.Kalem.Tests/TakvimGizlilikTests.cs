using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto.V2.Etkinlik;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Tests;

/// <summary>
/// v2 takvim sorgularının gizlilik kapısını geçtiğini kanıtlar.
///
/// <para>
/// Bu projedeki en tehlikeli tek satır, <c>GorunurOlanlar</c> çağrısını unutan
/// bir sorgudur: derleme başarılı olur, ekran çalışır, ama sistemdeki BÜTÜN
/// gizli etkinlikler herkese görünür. Hata sessizdir — bu yüzden test var.
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class TakvimGizlilikTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;

    private const long EkleyenId = 1;
    private const string EkleyenAdi = "ekleyen";
    private const long KatilimciId = 2;
    private const long YabanciId = 3;
    private const string YabanciAdi = "yabanci";

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    /// <summary>Bir açık, bir gizli (ekleyen + katılımcı) etkinlik kurar.</summary>
    private async Task<(long acikId, long gizliId)> VeriKurAsync()
    {
        await _ortam.TemelVerileriKurAsync();

        using var db = _ortam.Baglam();
        await db.Ajandalar.IgnoreQueryFilters().ExecuteDeleteAsync();

        var bugun = DateTime.Now.Date.AddHours(10);

        var acik = new Ajanda
        {
            Baslik = "Açık toplantı",
            BaslangicTarihi = bugun,
            BitisTarihi = bugun.AddHours(1),
            KullaniciId = EkleyenAdi,
            BirimId = 1,
            RandevuTipId = 1,
            DurumId = 1,
            Gizli = false,
        };

        var gizli = new Ajanda
        {
            Baslik = "Gizli görüşme",
            BaslangicTarihi = bugun.AddHours(2),
            BitisTarihi = bugun.AddHours(3),
            KullaniciId = EkleyenAdi,
            BirimId = 1,
            RandevuTipId = 1,
            DurumId = 1,
            Gizli = true,
            Katilimcilar = [new AjandaKatilimci { KullaniciId = KatilimciId }],
        };

        db.Ajandalar.AddRange(acik, gizli);
        await db.SaveChangesAsync();
        return (acik.Id, gizli.Id);
    }

    private async Task<List<EtkinlikOzetDto>> AralikAsync(long kullaniciId, string kullaniciAdi)
    {
        using var db = _ortam.Baglam();
        var servis = new TakvimSorguServisi(
            db, new SahteKullaniciServisi(kullaniciId, kullaniciAdi, 1));

        var bugun = DateTime.Now.Date;
        return await servis.AralikAsync(new AralikIstegi
        {
            Baslangic = bugun,
            Bitis = bugun.AddDays(1),
        });
    }

    [Fact]
    public async Task Ekleyen_kendi_gizli_etkinligini_gorur()
    {
        PostgresYoksaAtla();
        var (acikId, gizliId) = await VeriKurAsync();

        var sonuc = await AralikAsync(EkleyenId, EkleyenAdi);

        Assert.Contains(sonuc, e => e.Id == acikId);
        Assert.Contains(sonuc, e => e.Id == gizliId);
    }

    [Fact]
    public async Task Katilimci_gizli_etkinligi_gorur()
    {
        PostgresYoksaAtla();
        var (_, gizliId) = await VeriKurAsync();

        var sonuc = await AralikAsync(KatilimciId, "katilimci");

        Assert.Contains(sonuc, e => e.Id == gizliId);
    }

    [Fact]
    public async Task Yabanci_gizli_etkinligi_GOREMEZ()
    {
        PostgresYoksaAtla();
        var (acikId, gizliId) = await VeriKurAsync();

        var sonuc = await AralikAsync(YabanciId, YabanciAdi);

        // Açık etkinliği görür…
        Assert.Contains(sonuc, e => e.Id == acikId);
        // …gizli olanı ASLA.
        Assert.DoesNotContain(sonuc, e => e.Id == gizliId);
    }

    [Fact]
    public async Task Gun_sayaclari_da_gizlilik_suzgecinden_gecer()
    {
        PostgresYoksaAtla();
        await VeriKurAsync();

        using var db = _ortam.Baglam();
        var yil = DateTime.Now.Year;

        var ekleyenSayac = await new TakvimSorguServisi(
            db, new SahteKullaniciServisi(EkleyenId, EkleyenAdi, 1)).GunSayaclariAsync(yil);

        using var db2 = _ortam.Baglam();
        var yabanciSayac = await new TakvimSorguServisi(
            db2, new SahteKullaniciServisi(YabanciId, YabanciAdi, 1)).GunSayaclariAsync(yil);

        var bugun = DateTime.Now.Date;
        var ekleyenBugun = ekleyenSayac.FirstOrDefault(s => s.Gun == bugun)?.Adet ?? 0;
        var yabanciBugun = yabanciSayac.FirstOrDefault(s => s.Gun == bugun)?.Adet ?? 0;

        // Sayaç da sızdırmamalı: ekleyen 2, yabancı 1 görmeli.
        Assert.Equal(2, ekleyenBugun);
        Assert.Equal(1, yabanciBugun);
    }

    [Fact]
    public async Task Aralik_disindaki_etkinlik_donmez()
    {
        PostgresYoksaAtla();
        await VeriKurAsync();

        using var db = _ortam.Baglam();
        var servis = new TakvimSorguServisi(db, new SahteKullaniciServisi(EkleyenId, EkleyenAdi, 1));

        var gelecekHafta = DateTime.Now.Date.AddDays(7);
        var sonuc = await servis.AralikAsync(new AralikIstegi
        {
            Baslangic = gelecekHafta,
            Bitis = gelecekHafta.AddDays(1),
        });

        Assert.Empty(sonuc);
    }
}
