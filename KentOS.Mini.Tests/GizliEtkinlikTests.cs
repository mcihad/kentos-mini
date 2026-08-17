using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Dto.Randevu;
using KentOS.Mini.Application.Dto.ViewModels;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Services;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// GİZLİ ETKİNLİK görünürlük matrisi — gerçek Postgres, gerçek servisler.
///
/// Kilitlenen sözleşme: gizli etkinliği YALNIZCA ekleyen ve katılımcılar görür;
/// aynı birimdeki başka bir kullanıcı hiçbir okuma yolundan (liste, arama, tarih,
/// gün sayıları, notlar, fotoğraflar, çiçek, zaman çizelgesi, silinenler, medya,
/// istatistik) erişemez. Bildirimler de yalnızca bu kişilere gider.
///
/// Bu testler bir GÜVENLİK sözleşmesidir: biri gelecekte yeni bir okuma yolu
/// eklerken filtreyi unutursa burası kırmızıya döner.
/// </summary>
[Collection("SeriPostgres")]
public class GizliEtkinlikTests : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam;

    public GizliEtkinlikTests(SunucuTestOrtami ortam) => _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
        {
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres kullanılamıyor");
        }
    }

    // Kullanıcılar (SunucuTestOrtami.TemelVerileriKurAsync ile eşleşir)
    private const long EkleyenId = 1;      private const string Ekleyen = "ekleyen";
    private const long KatilimciId = 2;    private const string Katilimci = "katilimci";
    private const long YabanciId = 3;      private const string Yabanci = "yabanci";
    private const long Birim = 1;

    private async Task TemizleAsync()
    {
        using var b = _ortam.Baglam();
        await b.Database.ExecuteSqlRawAsync(
            "TRUNCATE ajanda_katilimcilar, ajanda_notlar, ajanda_olaylar, ajandalar, ajanda_seriler RESTART IDENTITY CASCADE;");
        await _ortam.TemelVerileriKurAsync();
    }

    private (AjandaService ajanda, AjandaSeriService seri, SahteMesajServisi mesaj, AppDbContext baglam) Kur(
        long kullaniciId, string kullaniciAdi)
    {
        var baglam = _ortam.Baglam();
        var kullanici = new SahteKullaniciServisi(kullaniciId, kullaniciAdi, Birim);
        var (ajanda, seri, mesaj) = TestServisFabrikasi.Kur(baglam, kullanici, _ortam.Mapper);
        return (ajanda, seri, mesaj, baglam);
    }

    private static AjandaDto GizliSablon(bool gizli = true, params long[] katilimcilar) => new()
    {
        Baslik = gizli ? "Gizli Görüşme" : "Açık Toplantı",
        Aciklama = "Gizli açıklama",
        Konum = "Başkanlık Odası",
        BaslangicTarihi = new DateTime(2026, 9, 10, 15, 0, 0),
        BitisTarihi = new DateTime(2026, 9, 10, 16, 0, 0),
        RandevuTipId = 1,
        DurumId = 1,
        Gizli = gizli,
        KatilimciIdler = katilimcilar.ToList()
    };

    /// <summary>Ekleyen kullanıcı olarak gizli bir etkinlik oluşturur.</summary>
    private async Task<AjandaDto> GizliEtkinlikOlusturAsync(params long[] katilimcilar)
    {
        var (ajandaServisi, _, _, baglam) = Kur(EkleyenId, Ekleyen);
        using (baglam)
        {
            return await ajandaServisi.CreateAsync(GizliSablon(true, katilimcilar));
        }
    }

    // ===================================================================
    //  OLUŞTURMA + KATILIMCI
    // ===================================================================
    [Fact]
    public async Task Gizli_Etkinlik_Katilimcilariyla_Kaydedilir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var etkinlik = await GizliEtkinlikOlusturAsync(KatilimciId);

        using var kontrol = _ortam.Baglam();
        var kayit = await kontrol.Ajandalar.Include(a => a.Katilimcilar).FirstAsync(a => a.Id == etkinlik.Id);

        Assert.True(kayit.Gizli);
        Assert.Single(kayit.Katilimcilar);
        Assert.Equal(KatilimciId, kayit.Katilimcilar.First().KullaniciId);
    }

    [Fact]
    public async Task Gizli_Etkinlikte_Basin_Katilsin_Reddedilir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, _, baglam) = Kur(EkleyenId, Ekleyen);
        using (baglam)
        {
            var dto = GizliSablon(true, KatilimciId);
            dto.BasinKatilsin = true;

            var hata = await Assert.ThrowsAsync<BusinessRuleException>(() => ajandaServisi.CreateAsync(dto));
            Assert.Contains("Basın", hata.Message);
        }
    }

    // ===================================================================
    //  GÖRÜNÜRLÜK MATRİSİ
    // ===================================================================
    [Theory]
    [InlineData(EkleyenId, Ekleyen, true)]        // ekleyen görür
    [InlineData(KatilimciId, Katilimci, true)]    // katılımcı görür
    [InlineData(YabanciId, Yabanci, false)]       // aynı birimden yabancı GÖRMEZ
    public async Task Gizli_Etkinlik_Tum_Okuma_Yollarinda_Filtrelenir(long kullaniciId, string kullaniciAdi, bool gorebilmeli)
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var etkinlik = await GizliEtkinlikOlusturAsync(KatilimciId);

        var (servis, seriServisi, _, baglam) = Kur(kullaniciId, kullaniciAdi);
        using (baglam)
        {
            // 1) Tüm liste
            var liste = await servis.GetAllAsync();
            Assert.Equal(gorebilmeli, liste.Any(a => a.Id == etkinlik.Id));

            // 2) Arama (metinle)
            var arama = await servis.SearchAsync(new AjandaSearchParametersDto { SearchString = "Gizli" });
            Assert.Equal(gorebilmeli, arama.Any(a => a.Id == etkinlik.Id));

            // 3) Arama (silinmişler dahil — IgnoreQueryFilters yolunu da kapsar)
            var aramaTumu = await servis.SearchAsync(new AjandaSearchParametersDto
            {
                SilinmisFiltre = SilinmisFiltre.Tumu
            });
            Assert.Equal(gorebilmeli, aramaTumu.Any(a => a.Id == etkinlik.Id));

            // 4) Tarihe göre (DateOnly ve DTO sürümleri)
            var tarihe = await servis.GetByDateAsync(new DateOnly(2026, 9, 10));
            Assert.Equal(gorebilmeli, tarihe.Any(a => a.Id == etkinlik.Id));

            var tariheDto = await servis.GetByDateAsync(new AjandaDateSearchDto { Date = new DateTime(2026, 9, 10) });
            Assert.Equal(gorebilmeli, tariheDto.Any(a => a.Id == etkinlik.Id));

            // 5) Gün sayıları (takvim yoğunluk göstergesi bile varlığı sızdırmamalı)
            var sayilar = await servis.GetCountByDayAsync(9, 2026);
            var gununSayisi = sayilar.FirstOrDefault(s => s.Day == 10)?.Count ?? 0;
            Assert.Equal(gorebilmeli ? 1 : 0, gununSayisi);

            // 6) Tek kayıt
            if (gorebilmeli)
            {
                var kayit = await servis.GetAsync(etkinlik.Id);
                Assert.Equal(etkinlik.Id, kayit.Id);
            }
            else
            {
                await Assert.ThrowsAsync<EntityNotFoundException>(() => servis.GetAsync(etkinlik.Id));
                await Assert.ThrowsAsync<EntityNotFoundException>(() => servis.GetByIdAsync(etkinlik.Id));
            }

            // 7) Alt kaynaklar: not, fotoğraf, çiçek, zaman çizelgesi
            Assert.Equal(gorebilmeli, await servis.GorebilirMiAsync(etkinlik.Id));

            // 8) Bugünden itibaren (web zaman çizelgesi) ve medya listesi
            var bugundenSonra = await servis.GetAllFromTodayAsync();
            Assert.Equal(gorebilmeli, bugundenSonra.Any(a => a.Id == etkinlik.Id));

            var medya = await servis.GetAllMediaJoinsAsync();
            Assert.DoesNotContain(medya, a => a.Id == etkinlik.Id);   // basın listesine HİÇ girmez

            // 9) Tekrar serisi bilgisi
            Assert.Null(await seriServisi.GetirAsync(etkinlik.Id));   // tek seferlik etkinlik
        }
    }

    [Fact]
    public async Task Yabanci_Gizli_Etkinligin_Notunu_Fotografini_Cicegini_Goremez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var etkinlik = await GizliEtkinlikOlusturAsync(KatilimciId);

        // Ekleyen not ekler.
        var (ekleyenServis, _, _, ekleyenBaglam) = Kur(EkleyenId, Ekleyen);
        using (ekleyenBaglam)
        {
            await ekleyenServis.CreateNoteAsync(new AjandaNotDto { AjandaId = etkinlik.Id, Not = "Gizli not" });
        }

        // Katılımcı görebilir.
        var (katilimciServis, _, _, katilimciBaglam) = Kur(KatilimciId, Katilimci);
        using (katilimciBaglam)
        {
            Assert.Single(await katilimciServis.GetNotesAsync(etkinlik.Id));
            Assert.Single(await katilimciServis.GetAllNoteAsync(etkinlik.Id));
        }

        // Yabancı göremez.
        var (yabanciServis, _, _, yabanciBaglam) = Kur(YabanciId, Yabanci);
        using (yabanciBaglam)
        {
            Assert.Empty(await yabanciServis.GetNotesAsync(etkinlik.Id));
            Assert.Empty(await yabanciServis.GetAllNoteAsync(etkinlik.Id));
            Assert.Empty(await yabanciServis.GetAjandaPhotosAsync(etkinlik.Id));
            Assert.Null(await yabanciServis.GetCicekAsync(etkinlik.Id));
        }
    }

    [Fact]
    public async Task Yabanci_Silinmis_Gizli_Etkinligi_De_Goremez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var etkinlik = await GizliEtkinlikOlusturAsync(KatilimciId);

        var (ekleyenServis, _, _, ekleyenBaglam) = Kur(EkleyenId, Ekleyen);
        using (ekleyenBaglam)
        {
            await ekleyenServis.DeleteAsync(etkinlik.Id);
        }

        var (yabanciServis, _, _, yabanciBaglam) = Kur(YabanciId, Yabanci);
        using (yabanciBaglam)
        {
            var silinenler = await yabanciServis.GetDeletedAsync();
            Assert.DoesNotContain(silinenler, a => a.Id == etkinlik.Id);
        }

        var (ekleyenServis2, _, _, ekleyenBaglam2) = Kur(EkleyenId, Ekleyen);
        using (ekleyenBaglam2)
        {
            var silinenler = await ekleyenServis2.GetDeletedAsync();
            Assert.Contains(silinenler, a => a.Id == etkinlik.Id);
        }
    }

    [Fact]
    public async Task Yabanci_Gizli_Etkinligi_Guncelleyemez_Silemez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var etkinlik = await GizliEtkinlikOlusturAsync(KatilimciId);

        var (yabanciServis, _, _, baglam) = Kur(YabanciId, Yabanci);
        using (baglam)
        {
            var dto = GizliSablon(true, KatilimciId);
            dto.Id = etkinlik.Id;
            dto.Baslik = "Ele geçirildi";

            await Assert.ThrowsAsync<EntityNotFoundException>(() => yabanciServis.UpdateAsync(dto));
            await Assert.ThrowsAsync<EntityNotFoundException>(() => yabanciServis.DeleteAsync(etkinlik.Id));
            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                yabanciServis.PostponeAsync(new AjandaErteleDto { Id = etkinlik.Id, Tarih = DateTime.Now }));
            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                yabanciServis.ChangeDurumId(etkinlik.Id, 1));
        }

        using var kontrol = _ortam.Baglam();
        var kayit = await kontrol.Ajandalar.FirstAsync(a => a.Id == etkinlik.Id);
        Assert.Equal("Gizli Görüşme", kayit.Baslik);
        Assert.False(kayit.IsDeleted);
    }

    [Fact]
    public async Task Anonim_Cicek_Karti_Gizli_Etkinligi_Paylasmaz()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var gizli = await GizliEtkinlikOlusturAsync(KatilimciId);

        var (ajandaServisi, _, _, baglam) = Kur(EkleyenId, Ekleyen);
        using (baglam)
        {
            // Anonim çiçekçi sayfasının kullandığı yol: gizli etkinlik "yok" sayılır.
            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                ajandaServisi.GetByIdWithoutUserRestrictionAsync(gizli.Id));

            // Gizli etkinliğe çiçek talimatı da verilemez (kurum dışına bilgi gider).
            var hata = await Assert.ThrowsAsync<BusinessRuleException>(() =>
                ajandaServisi.CicekGonderAsync(new AjandaCicekGonderDto { AjandaId = gizli.Id, CicekciId = 1 }));
            Assert.Contains("Gizli etkinlik", hata.Message);

            // Açık etkinlikte aynı yol çalışmaya devam eder (regresyon kilidi).
            var acik = await ajandaServisi.CreateAsync(GizliSablon(false));
            var acikKayit = await ajandaServisi.GetByIdWithoutUserRestrictionAsync(acik.Id);
            Assert.Equal(acik.Id, acikKayit.Id);
        }
    }

    [Fact]
    public async Task Gizli_Etkinlik_Havale_Edilemez_Ve_Birim_Smsi_Gonderilemez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var etkinlik = await GizliEtkinlikOlusturAsync(KatilimciId);

        var (ajandaServisi, _, _, baglam) = Kur(EkleyenId, Ekleyen);
        using (baglam)
        {
            var havaleHatasi = await Assert.ThrowsAsync<BusinessRuleException>(() =>
                ajandaServisi.HavaleAsync(new AjandaHavaleDto { Id = etkinlik.Id, BirimId = 2 }));
            Assert.Contains("havale", havaleHatasi.Message);

            var smsHatasi = await Assert.ThrowsAsync<BusinessRuleException>(() =>
                ajandaServisi.SendSmsToBirimAsync(new SendSmsToBirimDto
                {
                    AjandaId = etkinlik.Id,
                    BirimIds = [2],
                    Message = "Deneme"
                }));
            Assert.Contains("SMS", smsHatasi.Message);
        }
    }

    // ===================================================================
    //  BİLDİRİM HEDEFLEME
    // ===================================================================
    [Fact]
    public async Task Gizli_Etkinlik_Bildirimi_Yalnizca_Katilimci_Ve_Ekleyene_Gider()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, mesaj, baglam) = Kur(EkleyenId, Ekleyen);
        using (baglam)
        {
            await ajandaServisi.CreateAsync(GizliSablon(true, KatilimciId));

            // Birim geneline HİÇ bildirim gitmemeli.
            Assert.Empty(mesaj.BirimeGidenler);
            Assert.Single(mesaj.KisilereGidenler);

            var bildirim = mesaj.KisilereGidenler[0];
            Assert.Equal([EkleyenId, KatilimciId], bildirim.KullaniciIdler.OrderBy(x => x).ToList());
            // Bildirimde gizlilik işareti bulunmalı (kullanıcı isteği).
            Assert.Contains("Gizli", bildirim.Baslik);
            // Metin "katılımcılar" diyordu; katılımcı artık toplantıya çağrılan
            // BİRİM demek ve gizli etkinliği göremiyor. Bildirimin dili de
            // görünürlük listesini işaret etmeli.
            Assert.Contains("yalnızca görmesine izin verilenler", bildirim.Icerik);
        }
    }

    [Fact]
    public async Task Acik_Etkinlik_Bildirimi_Bugunku_Gibi_Birime_Gider()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, mesaj, baglam) = Kur(EkleyenId, Ekleyen);
        using (baglam)
        {
            await ajandaServisi.CreateAsync(GizliSablon(false));

            // REGRESYON KİLİDİ: gizli olmayan etkinlikte davranış değişmedi.
            Assert.Single(mesaj.BirimeGidenler);
            Assert.Equal(Birim, mesaj.BirimeGidenler[0].BirimId);
            Assert.Empty(mesaj.KisilereGidenler);
            Assert.DoesNotContain("Gizli", mesaj.BirimeGidenler[0].Baslik);
        }
    }

    [Fact]
    public async Task Gizlilik_Kaldirilinca_Katilimcilar_Temizlenir_Ve_Bildirim_Birime_Doner()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var etkinlik = await GizliEtkinlikOlusturAsync(KatilimciId);

        var (ajandaServisi, _, mesaj, baglam) = Kur(EkleyenId, Ekleyen);
        using (baglam)
        {
            var dto = GizliSablon(false);
            dto.Id = etkinlik.Id;
            dto.Gizli = false;

            await ajandaServisi.UpdateAsync(dto);

            Assert.Single(mesaj.BirimeGidenler);
            Assert.Empty(mesaj.KisilereGidenler);
        }

        using var kontrol = _ortam.Baglam();
        Assert.Empty(await kontrol.AjandaKatilimcilar.Where(k => k.AjandaId == etkinlik.Id).ToListAsync());
        Assert.False((await kontrol.Ajandalar.FirstAsync(a => a.Id == etkinlik.Id)).Gizli);
    }

    [Fact]
    public async Task Katilimci_Listesi_Kismi_Guncellemede_Korunur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var etkinlik = await GizliEtkinlikOlusturAsync(KatilimciId);

        var (ajandaServisi, _, _, baglam) = Kur(EkleyenId, Ekleyen);
        using (baglam)
        {
            // ESKİ İSTEMCİ DAVRANIŞI: katilimciIdler alanı hiç gönderilmiyor (null).
            var dto = GizliSablon(true);
            dto.Id = etkinlik.Id;
            dto.KatilimciIdler = null;
            dto.Baslik = "Gizli Görüşme (güncel)";

            await ajandaServisi.UpdateAsync(dto);
        }

        using var kontrol = _ortam.Baglam();
        var katilimcilar = await kontrol.AjandaKatilimcilar.Where(k => k.AjandaId == etkinlik.Id).ToListAsync();
        Assert.Single(katilimcilar);   // liste SİLİNMEMELİ
    }

    // ===================================================================
    //  GİZLİ + TEKRARLANAN BİRLİKTE
    // ===================================================================
    [Fact]
    public async Task Gizli_Tekrarlanan_Etkinlikte_Katilimcilar_Her_Tekrara_Kopyalanir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, _, baglam) = Kur(EkleyenId, Ekleyen);
        using (baglam)
        {
            var dto = GizliSablon(true, KatilimciId);
            dto.Tekrar = new AjandaSeriOlusturDto { Rrule = "FREQ=WEEKLY;BYDAY=TH;COUNT=3" };

            var ilk = await ajandaServisi.CreateAsync(dto);

            using var kontrol = _ortam.Baglam();
            var tekrarlar = await kontrol.Ajandalar
                .Include(a => a.Katilimcilar)
                .Where(a => a.SeriId == ilk.SeriId)
                .ToListAsync();

            Assert.Equal(3, tekrarlar.Count);
            Assert.All(tekrarlar, t => Assert.True(t.Gizli));
            Assert.All(tekrarlar, t => Assert.Single(t.Katilimcilar));
            Assert.All(tekrarlar, t => Assert.Equal(KatilimciId, t.Katilimcilar.First().KullaniciId));
        }

        // Yabancı hiçbir tekrarı görmemeli.
        var (yabanciServis, _, _, yabanciBaglam) = Kur(YabanciId, Yabanci);
        using (yabanciBaglam)
        {
            var liste = await yabanciServis.GetAllAsync();
            Assert.Empty(liste);
        }
    }

    [Fact]
    public async Task Istatistikler_Baskasinin_Gizli_Etkinligini_Saymaz()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        await GizliEtkinlikOlusturAsync(KatilimciId);

        // Ekleyen: 1 etkinlik sayar.
        using (var baglam = _ortam.Baglam())
        {
            var servis = new AjandaIstatistikService(baglam, new SahteKullaniciServisi(EkleyenId, Ekleyen, Birim));
            var sonuc = await servis.GetIstatistiklerAsync(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1));
            Assert.Equal(1, sonuc.Ozet.ToplamEtkinlik);
        }

        // Yabancı: 0 etkinlik sayar (varlık sayılardan da anlaşılmasın).
        using (var baglam = _ortam.Baglam())
        {
            var servis = new AjandaIstatistikService(baglam, new SahteKullaniciServisi(YabanciId, Yabanci, Birim));
            var sonuc = await servis.GetIstatistiklerAsync(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1));
            Assert.Equal(0, sonuc.Ozet.ToplamEtkinlik);
        }
    }

    [Fact]
    public async Task Zaman_Cizelgesi_Gizli_Etkinlikte_Yabanciya_Kapali()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var etkinlik = await GizliEtkinlikOlusturAsync(KatilimciId);

        using (var baglam = _ortam.Baglam())
        {
            var servis = new AjandaOlayService(baglam, new SahteKullaniciServisi(EkleyenId, Ekleyen, Birim),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<AjandaOlayService>.Instance);
            Assert.NotEmpty(await servis.GetirAsync(etkinlik.Id));
        }

        using (var baglam = _ortam.Baglam())
        {
            var servis = new AjandaOlayService(baglam, new SahteKullaniciServisi(YabanciId, Yabanci, Birim),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<AjandaOlayService>.Instance);
            Assert.Empty(await servis.GetirAsync(etkinlik.Id));
        }
    }

    [Fact]
    public async Task Gizli_Filtresi_Aramada_Calisir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, _, baglam) = Kur(EkleyenId, Ekleyen);
        using (baglam)
        {
            await ajandaServisi.CreateAsync(GizliSablon(true, KatilimciId));
            await ajandaServisi.CreateAsync(GizliSablon(false));

            var hepsi = await ajandaServisi.SearchAsync(new AjandaSearchParametersDto());
            var sadeceGizli = await ajandaServisi.SearchAsync(new AjandaSearchParametersDto
            {
                GizliFiltre = OzellikFiltre.Sadece
            });
            var gizliHaric = await ajandaServisi.SearchAsync(new AjandaSearchParametersDto
            {
                GizliFiltre = OzellikFiltre.Haric
            });

            Assert.Equal(2, hepsi.Count());
            Assert.Single(sadeceGizli);
            Assert.All(sadeceGizli, a => Assert.True(a.Gizli));
            Assert.Single(gizliHaric);
            Assert.All(gizliHaric, a => Assert.False(a.Gizli));
        }
    }

    [Fact]
    public async Task Katilimci_Listesi_Dtoda_Ad_Unvan_Ile_Doner_Iletisim_Bilgisi_Sizdirmaz()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var etkinlik = await GizliEtkinlikOlusturAsync(KatilimciId);

        var (servis, _, _, baglam) = Kur(KatilimciId, Katilimci);
        using (baglam)
        {
            var kayit = await servis.GetAsync(etkinlik.Id);

            Assert.Single(kayit.Katilimcilar);
            var k = kayit.Katilimcilar[0];
            Assert.Equal(KatilimciId, k.Id);
            Assert.Equal("katilimci", k.Ad);
            Assert.Equal("Test", k.Soyad);
            Assert.Equal("Uzman", k.Unvan);
            Assert.Equal("katilimci Test", k.TamAd);
        }
    }

    // ── ÖLÜMCÜL: güncelleme etkinliğin sahibini silmemeli ─────────────

    /// <summary>
    /// Güncelleme etkinliğin SAHİBİNİ korur.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bulunan gerçek hata: <c>_mapper.Map(dto, entity)</c> gövdede olmayan
    /// alanları da yazıyordu ve istemciler <c>kullaniciId</c> göndermediği
    /// için HER GÜNCELLEME sahibi NULL'a çekiyordu.
    /// </para>
    /// <para>
    /// Sonucu ölümcül: gizli etkinliğin görünürlüğü "oluşturan" eşleşmesine
    /// bakar; sahipsiz kalmış bir kayıt gizliye çevrildiğinde OLUŞTURANIN DA
    /// gözünden kayboluyor ve detay 404 veriyordu. İstisna atılmadığı için
    /// sistem hatası da düşmüyordu — kullanıcı yalnızca "kaydettim, etkinlik
    /// kayboldu" diyebiliyordu.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Guncelleme_etkinligin_sahibini_silmez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, _, _, baglam) = Kur(EkleyenId, Ekleyen);
        using (baglam)
        {
            var olusan = await servis.CreateAsync(GizliSablon(gizli: false));

            var dto = GizliSablon(gizli: false);
            dto.Id = olusan.Id;
            dto.Baslik = "Başlık değişti";
            // İstemciler `KullaniciId` GÖNDERMEZ — hatanın tetikleyicisi bu.
            dto.KullaniciId = null;

            await servis.UpdateAsync(dto);

            var kayit = await baglam.Ajandalar.AsNoTracking()
                .FirstAsync(a => a.Id == olusan.Id);
            Assert.Equal(Ekleyen, kayit.KullaniciId);
        }
    }

    /// <summary>
    /// Açık etkinlik GİZLİYE çevrilince oluşturan onu görmeye devam eder.
    /// </summary>
    [Fact]
    public async Task Gizliye_cevirme_etkinligi_olusturandan_gizlemez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, _, _, baglam) = Kur(EkleyenId, Ekleyen);
        using (baglam)
        {
            var olusan = await servis.CreateAsync(GizliSablon(gizli: false));

            var dto = GizliSablon(gizli: true, KatilimciId);
            dto.Id = olusan.Id;
            dto.KullaniciId = null;

            await servis.UpdateAsync(dto);

            // Detay AÇILIR
            var detay = await servis.GetByIdAsync(olusan.Id);
            Assert.NotNull(detay);
            Assert.True(detay!.Gizli);

            // ve listede DURUR.
            var liste = await servis.GetAllAsync();
            Assert.Contains(liste, x => x.Id == olusan.Id);
        }
    }

    /// <summary>
    /// Katılımcı BİRİMLER açık etkinlikte de geri okunur.
    /// </summary>
    /// <remarks>
    /// Bulunan gerçek hata: katılımcı listesi yalnızca GİZLİ etkinlikler için
    /// yükleniyordu (katılımcı bir zamanlar "gizliyi kim görebilir" demekti).
    /// Katılımcı birim eklendiğinde anlam genişledi ama satır olduğu gibi
    /// kaldı: birimler kaydediliyor, hiçbir ekranda görünmüyordu — düzenlemeye
    /// girildiğinde liste boş geliyor, kaydedince seçim siliniyordu.
    /// </remarks>
    [Fact]
    public async Task Katilimci_birimler_acik_etkinlikte_de_okunur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, _, _, baglam) = Kur(EkleyenId, Ekleyen);
        using (baglam)
        {
            var dto = GizliSablon(gizli: false);
            dto.KatilimciBirimIdler = [Birim];

            var olusan = await servis.CreateAsync(dto);
            Assert.Contains(olusan.Katilimcilar, k => k.BirimId == Birim);

            var detay = await servis.GetByIdAsync(olusan.Id);
            Assert.Contains(detay!.Katilimcilar, k => k.BirimId == Birim);

            var liste = await servis.GetAllAsync();
            var listeKaydi = liste.First(x => x.Id == olusan.Id);
            Assert.Contains(listeKaydi.Katilimcilar, k => k.BirimId == Birim);
        }
    }

    /// <summary>
    /// Katılımcı BİRİM açık etkinliğin DETAYINI da açabilir.
    /// </summary>
    /// <remarks>
    /// Bulunan gerçek hata: liste `BirimKapsami` (kendi birimi VEYA katılımcı
    /// birim) kullanıyordu ama detay yalnızca <c>BirimId == kendi birim</c>
    /// süzüyordu. Davet edilen birim etkinliği kendi ajandasında GÖRÜYOR,
    /// üstüne dokununca 404 alıyordu — aynı kayıt için iki farklı kapsam.
    /// </remarks>
    [Fact]
    public async Task Katilimci_birim_acik_etkinligin_detayini_acabilir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        // Sahibi birim 1, davetli birim 2.
        long davetliBirim;
        long etkinlikId;

        var (sahipServis, _, _, sahipBaglam) = Kur(EkleyenId, Ekleyen);
        using (sahipBaglam)
        {
            davetliBirim = await sahipBaglam.Birimler
                .Where(b => b.Id != Birim).Select(b => b.Id).FirstAsync();

            var dto = GizliSablon(gizli: false);
            dto.KatilimciBirimIdler = [davetliBirim];
            etkinlikId = (await sahipServis.CreateAsync(dto)).Id;
        }

        // Davetli birimden bir kullanıcı: listede DE görür, detayı DA açar.
        var davetliBaglam = _ortam.Baglam();
        using (davetliBaglam)
        {
            var kullanici = new SahteKullaniciServisi(99, "davetli", davetliBirim);
            var (servis, _, _) = TestServisFabrikasi.Kur(davetliBaglam, kullanici, _ortam.Mapper);

            var liste = await servis.GetAllAsync();
            Assert.Contains(liste, x => x.Id == etkinlikId);

            var detay = await servis.GetAsync(etkinlikId);
            Assert.NotNull(detay);
            Assert.Equal(etkinlikId, detay!.Id);
        }
    }

    /// <summary>
    /// Katılımcı birim GİZLİ etkinliğin detayını AÇAMAZ.
    /// </summary>
    /// <remarks>
    /// Detay kapsamı genişletilirken gizlilik kapısının açılmadığını kilitler:
    /// davet etmek ile "içeriği görebilsin" demek aynı şey değil.
    /// </remarks>
    [Fact]
    public async Task Katilimci_birim_gizli_etkinligin_detayini_acamaz()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        long davetliBirim;
        long etkinlikId;

        var (sahipServis, _, _, sahipBaglam) = Kur(EkleyenId, Ekleyen);
        using (sahipBaglam)
        {
            davetliBirim = await sahipBaglam.Birimler
                .Where(b => b.Id != Birim).Select(b => b.Id).FirstAsync();

            var dto = GizliSablon(gizli: true, KatilimciId);
            dto.KatilimciBirimIdler = [davetliBirim];
            etkinlikId = (await sahipServis.CreateAsync(dto)).Id;
        }

        var davetliBaglam = _ortam.Baglam();
        using (davetliBaglam)
        {
            var kullanici = new SahteKullaniciServisi(99, "davetli", davetliBirim);
            var (servis, _, _) = TestServisFabrikasi.Kur(davetliBaglam, kullanici, _ortam.Mapper);

            Assert.DoesNotContain(await servis.GetAllAsync(), x => x.Id == etkinlikId);

            // Detay "bulunamadı" der: yetkisizlik mesajı bile kaydın VAR
            // olduğunu ele verirdi.
            await Assert.ThrowsAsync<EntityNotFoundException>(
                () => servis.GetAsync(etkinlikId));
        }
    }
}
