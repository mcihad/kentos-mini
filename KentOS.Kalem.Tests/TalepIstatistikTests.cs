using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Services;

namespace KentOS.Kalem.Tests;

/// <summary>
/// TALEP PANOSU — mahalle, meslek ve diğer dağılımlar.
/// </summary>
/// <remarks>
/// Panonun asıl sebebi mahalle ve meslek: talebin nereden ve kimden geldiğini
/// gösteren tek iki alan bunlar. Meslek <b>serbest metin</b> bir sütun olduğu
/// için normalleştirme burada kilitleniyor — "Çiftçi", "çiftçi " ve "ÇİFTÇİ"
/// tek dilim olmalı, yoksa en kalabalık meslek üçe bölünüp listenin dibine
/// düşerdi.
/// </remarks>
[Collection(SunucuKoleksiyonu.Ad)]
public class TalepIstatistikTests : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam;

    public TalepIstatistikTests(SunucuTestOrtami ortam) => _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
        {
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres kullanılamıyor");
        }
    }

    private async Task<(AppDbContext, TalepIstatistikServisi)> KurAsync(
        params (string meslek, long? mahalleId, bool arsiv, bool ajanda, string? durum)[] kayitlar)
    {
        await _ortam.TemelVerileriKurAsync();
        var baglam = _ortam.Baglam();

        // Fixture sınıf başına bir kez kuruluyor; temizlemeden eklemek
        // yüzdeleri önceki testin kayıtlarıyla bozar.
        await baglam.Randevular.IgnoreQueryFilters().ExecuteDeleteAsync();
        await baglam.Mahalleler.Where(m => m.Id >= 900).ExecuteDeleteAsync();

        baglam.Mahalleler.Add(new Mahalle { Id = 901, Ad = "Akdeğirmen" });
        baglam.Mahalleler.Add(new Mahalle { Id = 902, Ad = "Gültepe" });
        await baglam.SaveChangesAsync();

        // `randevu_durum_id` NOT NULL: durumu verilmeyen kayıtlar için de bir
        // varsayılan kurulur, yoksa kurulum INSERT'te düşüyor.
        var durumlar = new Dictionary<string, long>();
        var adlar = kayitlar.Select(k => k.durum ?? "Beklemede").Distinct().ToList();
        foreach (var ad in adlar)
        {
            var mevcut = await baglam.RandevuDurumlar.FirstOrDefaultAsync(d => d.DurumAd == ad);
            if (mevcut is null)
            {
                mevcut = new RandevuDurum { DurumAd = ad!, Renk = "#123456" };
                baglam.RandevuDurumlar.Add(mevcut);
                await baglam.SaveChangesAsync();
            }
            durumlar[ad!] = mevcut.Id;
        }

        foreach (var (meslek, mahalleId, arsiv, ajanda, durum) in kayitlar)
        {
            baglam.Randevular.Add(new Randevu
            {
                Konu = "Test talebi",
                Ad = "Vatandaş",
                Soyad = "Test",
                Meslek = meslek,
                MahalleId = mahalleId,
                BirimId = 1,
                Arsivlendi = arsiv,
                AjandaDurum = ajanda,
                RandevuDurumId = durumlar[durum ?? "Beklemede"],
                BaslangicTarih = new DateTime(2026, 6, 20, 9, 0, 0),
                BitisTarih = new DateTime(2026, 6, 20, 10, 0, 0),
                OlusturmaTarih = new DateTime(2026, 6, 15, 10, 0, 0),
                Olusturan = "kalem1",
            });
        }
        await baglam.SaveChangesAsync();

        var kullanici = new SahteKullaniciServisi(1, "kalem1", 1);
        return (baglam, new TalepIstatistikServisi(baglam, kullanici));
    }

    /// <summary>
    /// Meslek serbest metin: yazım farkları TEK dilimde toplanmalı.
    /// </summary>
    [Fact]
    public async Task Meslek_yazim_farklari_TEK_dilimde_toplanir()
    {
        PostgresYoksaAtla();
        var (baglam, servis) = await KurAsync(
            ("Çiftçi", 901, false, false, null),
            ("çiftçi ", 901, false, false, null),
            (" ÇİFTÇİ", 902, false, false, null),
            ("Avukat", 902, false, false, null));
        using var _ = baglam;

        var d = await servis.PanoAsync(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        var ciftci = Assert.Single(d.MeslegeGore, x => x.Etiket.Equals("Çiftçi", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ciftci.Deger);
        Assert.Equal(75, ciftci.Yuzde);
    }

    [Fact]
    public async Task Mahalle_dagilimi_ada_gore_gruplar()
    {
        PostgresYoksaAtla();
        var (baglam, servis) = await KurAsync(
            ("Esnaf", 901, false, false, null),
            ("Esnaf", 901, false, false, null),
            ("Esnaf", 902, false, false, null));
        using var _ = baglam;

        var d = await servis.PanoAsync(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal("Akdeğirmen", d.MahalleyeGore[0].Etiket);
        Assert.Equal(2, d.MahalleyeGore[0].Deger);
        Assert.Equal("Gültepe", d.MahalleyeGore[1].Etiket);
    }

    /// <summary>
    /// Mesleği boş kayıtlar SESSİZCE DÜŞMEZ.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Boş alanı atlamak yüzdeleri bozuyor ve "verinin ne kadarı eksik"
    /// sorusunu görünmez kılıyordu; eksik veri de bir bulgudur.
    /// </para>
    /// <para>
    /// <b>Mahalle boş olamıyor:</b> model <c>long?</c> gösterse de veritabanında
    /// <c>mahalle_id</c> NOT NULL. Yani mahalle dağılımında "Belirtilmemiş"
    /// kovası sahada hiç oluşmuyor; kural yine de kodda duruyor çünkü sütun
    /// ileride gevşetilirse dağılım sessizce kayıt kaybetmemeli.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Bos_meslek_Belirtilmemis_altinda_toplanir()
    {
        PostgresYoksaAtla();
        var (baglam, servis) = await KurAsync(
            ("Esnaf", 901, false, false, null),
            (null!, 901, false, false, null),
            ("  ", 902, false, false, null));
        using var _ = baglam;

        var d = await servis.PanoAsync(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(2, d.MeslegeGore.Single(x => x.Etiket == "Belirtilmemiş").Deger);
        Assert.Equal(3, d.Ozet.ToplamTalep);
        // Mahalle kaybolmadı: üç kaydın hepsi bir mahalleye düştü.
        Assert.Equal(3, d.MahalleyeGore.Sum(x => x.Deger));
    }

    /// <summary>
    /// ARŞİVLENMİŞ talepler istatistiğe GİRER.
    /// </summary>
    /// <remarks>
    /// `Randevu` üzerinde `!Arsivlendi` global filtresi var. "Kaç talep geldi"
    /// sorusunun cevabı, kayıt arşive kaldırıldı diye değişmemeli; aktif/arşiv
    /// ayrımı ayrı bir dağılım olarak veriliyor.
    /// </remarks>
    [Fact]
    public async Task Arsivlenmis_talepler_sayima_DAHIL()
    {
        PostgresYoksaAtla();
        var (baglam, servis) = await KurAsync(
            ("Esnaf", 901, false, false, null),
            ("Esnaf", 901, true, false, null),
            ("Esnaf", 902, true, false, null));
        using var _ = baglam;

        var d = await servis.PanoAsync(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(3, d.Ozet.ToplamTalep);
        Assert.Equal(1, d.Ozet.AktifTalep);
        Assert.Equal(2, d.Ozet.ArsivlenmisTalep);
    }

    /// <summary>
    /// "Onaylı ama ajandaya eklenmemiş" — panodaki en işe yarar sayı.
    /// </summary>
    [Fact]
    public async Task Onayli_ama_eklenmemis_dogru_sayilir()
    {
        PostgresYoksaAtla();
        var (baglam, servis) = await KurAsync(
            ("Esnaf", 901, false, false, "Onaylandı"),   // sayılır
            ("Esnaf", 901, false, true, "Onaylandı"),    // eklendi → sayılmaz
            ("Esnaf", 902, false, false, "Beklemede"));  // onaylı değil
        using var _ = baglam;

        var d = await servis.PanoAsync(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(1, d.Ozet.OnayliAmaEklenmemis);
        Assert.Equal(1, d.Ozet.AjandayaEklenen);
    }

    /// <summary>
    /// Haftanın günleri KAYITSIZ günü de gösterir.
    /// </summary>
    /// <remarks>
    /// Boş günü listeden düşürmek, "salı hiç talep gelmiyor" bilgisini de
    /// düşürüyordu — oysa aranan cevap tam olarak o.
    /// </remarks>
    [Fact]
    public async Task Hafta_gunleri_YEDI_satir_doner()
    {
        PostgresYoksaAtla();
        var (baglam, servis) = await KurAsync(("Esnaf", 901, false, false, null));
        using var _ = baglam;

        var d = await servis.PanoAsync(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(7, d.HaftaGunineGore.Count);
        Assert.Equal("Pazartesi", d.HaftaGunineGore[0].Etiket);
    }

    /// <summary>Birimi farklı olan talep GÖRÜNMEZ.</summary>
    [Fact]
    public async Task Baska_birimin_talebi_sayilmaz()
    {
        PostgresYoksaAtla();
        var (baglam, servis) = await KurAsync(("Esnaf", 901, false, false, null));

        baglam.Randevular.Add(new Randevu
        {
            Konu = "Yabancı birim", Ad = "X", Soyad = "Y", BirimId = 2,
            MahalleId = 901,
            RandevuDurumId = baglam.RandevuDurumlar.First().Id,
            BaslangicTarih = new DateTime(2026, 6, 20), BitisTarih = new DateTime(2026, 6, 20),
            OlusturmaTarih = new DateTime(2026, 6, 15), Olusturan = "yabanci",
        });
        await baglam.SaveChangesAsync();
        using var _ = baglam;

        var d = await servis.PanoAsync(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(1, d.Ozet.ToplamTalep);
    }
}
