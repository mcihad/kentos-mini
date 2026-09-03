using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Dto.ViewModels;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Data;

namespace KentOS.Kalem.Tests;

/// <summary>
/// SİLİNMİŞ KAYIT LİSTESİ — iki istemci aynı kümeyi aynı sırada görmeli.
/// </summary>
/// <remarks>
/// <para>
/// Silinmiş kayıtlara iki ayrı uçtan bakılıyor: SPA <c>GET etkinlik/silinmis</c>,
/// mobil ise <c>POST etkinlik/ara</c> + <c>SilinmisFiltre.Silinmis</c>. İkisi
/// ayrışınca kullanıcı aynı hesapta bir yerde 8, diğerinde 80 kayıt görüyor ve
/// hangisinin doğru olduğunu bilemiyor.
/// </para>
/// <para>
/// Ayrışmanın iki sebebi vardı ve ikisi de burada kilitleniyor: SIRALAMA
/// anahtarı (etkinlik tarihi mi, silinme tarihi mi) ve SPA'nın varsayılan
/// 30 günlük dönem sınırı.
/// </para>
/// </remarks>
[Collection(SunucuKoleksiyonu.Ad)]
public class SilinmisSiralamaTests : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam;

    public SilinmisSiralamaTests(SunucuTestOrtami ortam) => _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
        {
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres kullanılamıyor");
        }
    }

    /// <summary>
    /// Etkinlik tarihi ile silinme tarihi BİLEREK ters sıralı üç kayıt kurar.
    /// </summary>
    /// <remarks>
    /// İkisi aynı yönde olsaydı test, yanlış anahtarla sıralayan bir kodu da
    /// geçirirdi — sıralama hatası ancak iki anahtar ayrıştığında görünür.
    /// </remarks>
    private async Task<AppDbContext> VeriKurAsync()
    {
        await _ortam.TemelVerileriKurAsync();
        var baglam = _ortam.Baglam();

        // Fixture sınıf başına BİR KEZ kuruluyor; aynı sınıftaki testler aynı
        // veritabanını paylaşıyor. Temizlemeden eklemek, sıralama iddiasını
        // önceki testin kayıtlarıyla bozuyor (ve hata "sıralama yanlış" gibi
        // görünüyor, oysa küme yanlış).
        await baglam.Ajandalar.IgnoreQueryFilters().ExecuteDeleteAsync();

        var kayitlar = new[]
        {
            // (başlık, etkinlik tarihi, silinme tarihi)
            ("En eski etkinlik, EN SON silindi", new DateTime(2026, 1, 5), new DateTime(2026, 8, 10)),
            ("Orta etkinlik, ortada silindi",    new DateTime(2026, 4, 5), new DateTime(2026, 5, 10)),
            ("En yeni etkinlik, EN ÖNCE silindi", new DateTime(2026, 7, 5), new DateTime(2026, 2, 10)),
        };

        foreach (var (baslik, etkinlik, silinme) in kayitlar)
        {
            baglam.Ajandalar.Add(new Ajanda
            {
                Baslik = baslik,
                BaslangicTarihi = etkinlik,
                BitisTarihi = etkinlik.AddHours(1),
                BirimId = 1,
                KullaniciId = "ekleyen",
                RandevuTipId = 1,
                DurumId = 1,
                IsDeleted = true,
                GuncellemeTarihi = silinme,
            });
        }

        await baglam.SaveChangesAsync();
        return baglam;
    }

    [Fact]
    public async Task Silinmis_listesi_SILINME_tarihine_gore_siralanir()
    {
        PostgresYoksaAtla();
        using var baglam = await VeriKurAsync();

        var kullanici = new SahteKullaniciServisi(1, "ekleyen", 1);
        var (ajanda, _, _) = TestServisFabrikasi.Kur(baglam, kullanici, _ortam.Mapper);

        var sonuc = (await ajanda.SearchAsync(new AjandaSearchParametersDto
        {
            SilinmisFiltre = SilinmisFiltre.Silinmis,
        })).ToList();

        var basliklar = sonuc.Select(x => x.Baslik).ToList();

        // En son silinen başta. Etkinlik tarihine göre sıralansaydı sıra tam
        // TERS olurdu — bu yüzden veri o şekilde kuruldu.
        Assert.Equal("En eski etkinlik, EN SON silindi", basliklar[0]);
        Assert.Equal("Orta etkinlik, ortada silindi", basliklar[1]);
        Assert.Equal("En yeni etkinlik, EN ÖNCE silindi", basliklar[2]);
    }

    [Fact]
    public async Task Aktif_liste_ETKINLIK_tarihine_gore_siralanir()
    {
        PostgresYoksaAtla();
        using var baglam = await VeriKurAsync();

        baglam.Ajandalar.Add(new Ajanda
        {
            Baslik = "Aktif kayıt",
            BaslangicTarihi = new DateTime(2026, 9, 1),
            BirimId = 1,
            KullaniciId = "ekleyen",
            RandevuTipId = 1,
            DurumId = 1,
        });
        await baglam.SaveChangesAsync();

        var kullanici = new SahteKullaniciServisi(1, "ekleyen", 1);
        var (ajanda, _, _) = TestServisFabrikasi.Kur(baglam, kullanici, _ortam.Mapper);

        var sonuc = (await ajanda.SearchAsync(new AjandaSearchParametersDto
        {
            SilinmisFiltre = SilinmisFiltre.Aktif,
        })).ToList();

        // Silinmişe özel sıralama, aktif listeyi ETKİLEMEMELİ.
        Assert.Equal("Aktif kayıt", sonuc[0].Baslik);
    }

    /// <summary>
    /// Silinmiş kümesi iki uçta da AYNI olmalı.
    /// </summary>
    /// <remarks>
    /// SPA ucu bir dönem varsayılan 30 günle açılıyordu; aynı hesapta eski
    /// arayüz ve mobil 80 kayıt gösterirken SPA 8 gösteriyordu. Sınırı
    /// açıkça <c>gun=0</c> ile kaldırdığımızda kümeler örtüşmeli.
    /// </remarks>
    [Fact]
    public async Task Iki_ucun_silinmis_kumesi_AYNI()
    {
        PostgresYoksaAtla();
        using var baglam = await VeriKurAsync();

        var kullanici = new SahteKullaniciServisi(1, "ekleyen", 1);
        var (ajanda, _, _) = TestServisFabrikasi.Kur(baglam, kullanici, _ortam.Mapper);

        var aramaUcu = (await ajanda.SearchAsync(new AjandaSearchParametersDto
        {
            SilinmisFiltre = SilinmisFiltre.Silinmis,
        })).Select(x => x.Id).ToHashSet();

        var silinmisUcu = (await ajanda.GetDeletedAsync()).Select(x => x.Id).ToHashSet();

        Assert.Equal(silinmisUcu, aramaUcu);
        Assert.Equal(3, aramaUcu.Count);
    }
}
