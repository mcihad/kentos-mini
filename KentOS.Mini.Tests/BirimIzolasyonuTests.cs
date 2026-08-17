using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Dto.ViewModels;
using KentOS.Mini.Application.Dto.V2.Etkinlik;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Services.V2;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// BİRİM İZOLASYONU — bir kullanıcı yalnızca KENDİ biriminin etkinliklerini görür.
///
/// <para>
/// GERÇEK HATA: v2 okuma sorguları yalnızca gizlilik süzgecini uyguluyor, birim
/// süzgecini atlıyordu. Yeni web arayüzü <b>başka birimlerin etkinliklerini</b>
/// listeliyordu; eski arayüzde ve mobilde görünmeyen kayıtlar takvimde
/// çıkıyordu. Kullanıcı bunu "üstte deneme isimli bir etkinlik var, oysa eski
/// ajandada yok" diye bildirdi.
/// </para>
///
/// <para>
/// Kural v1 ile AYNI olmalı (<c>AjandaService.GetAllAsync</c> ve
/// <c>GetByIdAsync</c>): <c>BirimId == kullanıcının birimi</c> <b>VE</b>
/// gizlilik süzgeci. Mobil v1 kullandığı için mobilin davranışı referans.
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class BirimIzolasyonuTests : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam;

    // SunucuTestOrtami: 1/2/3 → birim 1, 4 → birim 2.
    private const long Birim1Kullanici = 1;
    private const string Birim1KullaniciAdi = "ekleyen";
    private const long Birim2Kullanici = 4;
    private const string Birim2KullaniciAdi = "digerbirim";

    public BirimIzolasyonuTests(SunucuTestOrtami ortam) => _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
        {
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres kullanılamıyor");
        }
    }

    private async Task TemizleAsync()
    {
        using var b = _ortam.Baglam();
        await b.Database.ExecuteSqlRawAsync(
            "TRUNCATE ajanda_katilimcilar, ajanda_notlar, ajanda_olaylar, ajandalar, ajanda_seriler RESTART IDENTITY CASCADE;");
        await _ortam.TemelVerileriKurAsync();
    }

    /// <summary>Verilen birimde, verilen kullanıcı adına bir etkinlik yazar.</summary>
    private async Task<long> EtkinlikYazAsync(
        string baslik, long birimId, string kullaniciAdi, bool gizli = false)
    {
        using var b = _ortam.Baglam();

        var kayit = new Ajanda
        {
            Baslik = baslik,
            BaslangicTarihi = new DateTime(2026, 12, 1, 10, 0, 0),
            BitisTarihi = new DateTime(2026, 12, 1, 11, 0, 0),
            RandevuTipId = 1,
            DurumId = 1,
            BirimId = birimId,
            KullaniciId = kullaniciAdi,
            Gizli = gizli,
            OlusturmaTarihi = DateTime.Now,
        };

        b.Ajandalar.Add(kayit);
        await b.SaveChangesAsync();
        return kayit.Id;
    }

    private static AralikIstegi TumYil => new()
    {
        Baslangic = new DateTime(2026, 1, 1),
        Bitis = new DateTime(2027, 1, 1),
    };

    private TakvimSorguServisi Servis(AppDbContext baglam, long kullaniciId, string kullaniciAdi, long birimId)
        => new(baglam, new SahteKullaniciServisi(kullaniciId, kullaniciAdi, birimId));

    [Fact]
    public async Task Takvim_baska_birimin_etkinligini_gostermez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        await EtkinlikYazAsync("Birim 1 toplantısı", 1, Birim1KullaniciAdi);
        await EtkinlikYazAsync("Deneme", 2, Birim2KullaniciAdi);

        using var baglam = _ortam.Baglam();
        var sonuc = await Servis(baglam, Birim1Kullanici, Birim1KullaniciAdi, 1)
            .AralikAsync(TumYil);

        var basliklar = sonuc.Select(e => e.Baslik).ToList();

        Assert.Contains("Birim 1 toplantısı", basliklar);
        Assert.DoesNotContain("Deneme", basliklar);
    }

    [Fact]
    public async Task Gun_sayaclari_da_birime_gore_suzulur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        await EtkinlikYazAsync("Birim 1 toplantısı", 1, Birim1KullaniciAdi);
        await EtkinlikYazAsync("Birim 2 toplantısı", 2, Birim2KullaniciAdi);

        using var baglam = _ortam.Baglam();

        var birim1 = await Servis(baglam, Birim1Kullanici, Birim1KullaniciAdi, 1)
            .GunSayaclariAsync(2026);
        var birim2 = await Servis(baglam, Birim2Kullanici, Birim2KullaniciAdi, 2)
            .GunSayaclariAsync(2026);

        // Aynı güne iki birimin birer etkinliği var; her biri YALNIZCA kendisininkini saymalı.
        Assert.Equal(1, birim1.Sum(g => g.Adet));
        Assert.Equal(1, birim2.Sum(g => g.Adet));
    }

    /// <summary>
    /// İki kural ANDlenir: başka birimin GİZLİ OLMAYAN etkinliği de görünmez.
    /// </summary>
    /// <remarks>
    /// Gizlilik süzgeci tek başına yeterli sanılırsa bu test kırmızıya döner —
    /// gizli olmayan kayıtlar o süzgeçten sorunsuz geçiyor.
    /// </remarks>
    [Fact]
    public async Task Baska_birimin_ACIK_etkinligi_de_gorunmez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        await EtkinlikYazAsync("Herkese açık ama başka birim", 2, Birim2KullaniciAdi, gizli: false);

        using var baglam = _ortam.Baglam();
        var sonuc = await Servis(baglam, Birim1Kullanici, Birim1KullaniciAdi, 1)
            .AralikAsync(TumYil);

        Assert.Empty(sonuc);
    }

    /// <summary>
    /// Aynı birimdeki başkasının GİZLİ etkinliği görünmez — iki kural birlikte.
    /// </summary>
    [Fact]
    public async Task Ayni_birimdeki_baskasinin_gizli_etkinligi_gorunmez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        // Aynı birimde ama BAŞKA kullanıcının gizli etkinliği.
        await EtkinlikYazAsync("Gizli görüşme", 1, "katilimci", gizli: true);
        await EtkinlikYazAsync("Açık toplantı", 1, "katilimci");

        using var baglam = _ortam.Baglam();
        var sonuc = await Servis(baglam, Birim1Kullanici, Birim1KullaniciAdi, 1)
            .AralikAsync(TumYil);

        var basliklar = sonuc.Select(e => e.Baslik).ToList();

        Assert.Contains("Açık toplantı", basliklar);
        Assert.DoesNotContain("Gizli görüşme", basliklar);
    }

    /// <summary>Kendi gizli etkinliğini oluşturan görür.</summary>
    [Fact]
    public async Task Kendi_gizli_etkinligini_gorur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        await EtkinlikYazAsync("Kendi gizlim", 1, Birim1KullaniciAdi, gizli: true);

        using var baglam = _ortam.Baglam();
        var sonuc = await Servis(baglam, Birim1Kullanici, Birim1KullaniciAdi, 1)
            .AralikAsync(TumYil);

        Assert.Single(sonuc);
        Assert.Equal("Kendi gizlim", sonuc[0].Baslik);
    }

    /// <summary>Katılımcı, kendi birimindeki gizli etkinliği görür.</summary>
    [Fact]
    public async Task Katilimci_gizli_etkinligi_gorur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var id = await EtkinlikYazAsync("Katılımcılı gizli", 1, "katilimci", gizli: true);

        using (var b = _ortam.Baglam())
        {
            b.AjandaKatilimcilar.Add(new AjandaKatilimci
            {
                AjandaId = id,
                KullaniciId = Birim1Kullanici,
            });
            await b.SaveChangesAsync();
        }

        using var baglam = _ortam.Baglam();
        var sonuc = await Servis(baglam, Birim1Kullanici, Birim1KullaniciAdi, 1)
            .AralikAsync(TumYil);

        Assert.Single(sonuc);
        Assert.Equal("Katılımcılı gizli", sonuc[0].Baslik);
    }

    /// <summary>
    /// v1 ile v2 AYNI kümeyi döndürmeli.
    /// </summary>
    /// <remarks>
    /// Mobil v1, yeni web v2 kullanıyor. İki yüzeyin farklı kayıt göstermesi,
    /// kullanıcının "mobilde yok ama webde var" diye bildirdiği hatanın ta
    /// kendisi. Bu test iki yolu aynı veriye karşı koşturup karşılaştırıyor.
    /// </remarks>
    [Fact]
    public async Task v1_ve_v2_ayni_etkinlikleri_dondurur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        await EtkinlikYazAsync("Kendi birimim açık", 1, Birim1KullaniciAdi);
        await EtkinlikYazAsync("Kendi gizlim", 1, Birim1KullaniciAdi, gizli: true);
        await EtkinlikYazAsync("Başkasının gizlisi", 1, "katilimci", gizli: true);
        await EtkinlikYazAsync("Deneme", 2, Birim2KullaniciAdi);

        using var baglam = _ortam.Baglam();
        var kullanici = new SahteKullaniciServisi(Birim1Kullanici, Birim1KullaniciAdi, 1);

        var (v1Servis, _, _) = TestServisFabrikasi.Kur(baglam, kullanici, _ortam.Mapper);
        var v1 = (await v1Servis.GetAllAsync(new AjandaSearchParametersDto()))
            .Select(a => a.Baslik)
            .OrderBy(x => x)
            .ToList();

        var v2 = (await new TakvimSorguServisi(baglam, kullanici).AralikAsync(TumYil))
            .Select(e => e.Baslik)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(v1, v2);
        Assert.DoesNotContain("Deneme", v2);
        Assert.DoesNotContain("Başkasının gizlisi", v2);
    }
}
