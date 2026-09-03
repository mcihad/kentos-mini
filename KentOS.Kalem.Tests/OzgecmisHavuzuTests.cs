using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Tests;

/// <summary>
/// ÖZGEÇMİŞ HAVUZU.
/// </summary>
/// <remarks>
/// Modülün iki sözü var ve testler ikisini de tutuyor:
/// <b>her yerden görünür</b> (birim süzgeci YOK — sistemin geri kalanının
/// tersi, bu yüzden kolayca "düzeltilip" bozulabilir) ve <b>iki kaynak tek
/// liste</b> (havuza doğrudan eklenen + talepten gelen).
/// </remarks>
[Collection(SunucuKoleksiyonu.Ad)]
public class OzgecmisHavuzuTests : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam;

    public OzgecmisHavuzuTests(SunucuTestOrtami ortam) => _ortam = ortam;

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
            "TRUNCATE ozgecmis_paylasimlari, ozgecmisler RESTART IDENTITY CASCADE;");
        await b.Database.ExecuteSqlRawAsync("DELETE FROM randevular;");
        await _ortam.TemelVerileriKurAsync();

        // `randevular` şemasında `mahalle_id` ve `randevu_durum_id` NOT NULL
        // (model `long?` gösterse de) — talep yazan testler bunlar olmadan
        // kaydetmede düşer.
        if (!await b.RandevuDurumlar.AnyAsync())
            b.RandevuDurumlar.Add(new RandevuDurum { DurumAd = "Beklemede", Renk = "#B07D2B" });
        if (!await b.Mahalleler.AnyAsync())
            b.Mahalleler.Add(new Mahalle { Ad = "Merkez" });
        await b.SaveChangesAsync();
    }

    /// <summary>Şemanın zorunlu kıldığı alanlarla birlikte talep.</summary>
    private async Task<Randevu> TalepYazAsync(AppDbContext b, string ad, string soyad,
        string? telefon = null, string? meslek = null)
    {
        var talep = new Randevu
        {
            Konu = "İş talebi",
            Ad = ad,
            Soyad = soyad,
            Telefon = telefon,
            Meslek = meslek,
            BirimId = 1,
            MahalleId = await b.Mahalleler.Select(m => m.Id).FirstAsync(),
            RandevuDurumId = await b.RandevuDurumlar.Select(d => d.Id).FirstAsync(),
            RandevuTipId = await b.RandevuTipleri.Select(t => (long?)t.Id).FirstOrDefaultAsync(),
            BaslangicTarih = DateTime.Now,
            BitisTarih = DateTime.Now.AddHours(1),
        };
        b.Randevular.Add(talep);
        await b.SaveChangesAsync();
        return talep;
    }

    private static (OzgecmisServisi servis, SahteMesajServisi mesaj) Servis(
        AppDbContext b, long kullaniciId = 1, string kullanici = "kalem1", long birimId = 1)
    {
        var mesaj = new SahteMesajServisi();
        return (new OzgecmisServisi(
            b,
            new SahteKullaniciServisi(kullaniciId, kullanici, birimId),
            mesaj,
            new TestServisFabrikasi.SahteDepo()), mesaj);
    }

    private static YuklenenDosya Dosya(string ad = "cv.pdf") =>
        new(ad, "application/pdf", "%PDF-1.4 sahte"u8.ToArray());

    private static OzgecmisIstegi Istek(
        string adSoyad = "Kemal Yıldırım", string? telefon = "0532 604 22 11") => new()
        {
            AdSoyad = adSoyad,
            Telefon = telefon,
            Aciklama = "Kaynakçı · 8 yıl deneyim",
        };

    // ── kaynak ─────────────────────────────────────────────────────────

    /// <summary>
    /// Talebe yüklenen özgeçmiş havuzda GÖRÜNÜR ve talebe bağlı olduğu bellidir.
    /// </summary>
    /// <remarks>
    /// Modülün varlık sebebi bu: "elimizde kaynakçı var mı?" sorusunun cevabı
    /// için talepleri tek tek açmak gerekiyordu.
    /// </remarks>
    [Fact]
    public async Task Talep_ozgecmisi_havuza_yansir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (servis, _) = Servis(b);

        var talep = await TalepYazAsync(b, "Mustafa", "Taş", "0541 298 34 50", "Kaynakçı");

        await servis.TalepOzgecmisiniYansitAsync(talep, "abc.pdf", "mustafa-cv.pdf", 120, "application/pdf");

        var liste = await servis.ListeAsync(new OzgecmisSuzgeci());
        var kayit = Assert.Single(liste.Veriler);

        Assert.Equal("Mustafa Taş", kayit.AdSoyad);
        Assert.Equal(talep.Id, kayit.TalepId);
        Assert.Equal("Talep", kayit.KaynakAd);
        Assert.Equal("Kaynakçı", kayit.MeslekAd);
    }

    /// <summary>
    /// Aynı talebe ikinci kez yükleme YENİ SATIR AÇMAZ.
    /// </summary>
    /// <remarks>
    /// Yanlış dosyayı yükleyip düzelten kullanıcı, havuzda aynı kişiyi iki
    /// kez bırakıyordu.
    /// </remarks>
    [Fact]
    public async Task Ayni_talep_iki_kez_yuklenince_tek_kayit_kalir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (servis, _) = Servis(b);

        var talep = await TalepYazAsync(b, "Ayşe", "Demir");

        await servis.TalepOzgecmisiniYansitAsync(talep, "ilk.pdf", "ilk.pdf", 10, "application/pdf");
        await servis.TalepOzgecmisiniYansitAsync(talep, "son.pdf", "son.pdf", 20, "application/pdf");

        var liste = await servis.ListeAsync(new OzgecmisSuzgeci());
        var kayit = Assert.Single(liste.Veriler);
        Assert.Equal("son.pdf", kayit.DosyaAdi);
    }

    /// <summary>Kaynak süzgeci iki kümeyi ayırır.</summary>
    [Fact]
    public async Task Kaynak_suzgeci_havuz_ve_talebi_ayirir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (servis, _) = Servis(b);

        var talep = await TalepYazAsync(b, "Talepten", "Gelen");

        await servis.OlusturAsync(Istek("Havuza Eklenen"), Dosya());
        await servis.TalepOzgecmisiniYansitAsync(talep, "a.pdf", "a.pdf", 5, "application/pdf");

        var havuz = await servis.ListeAsync(new OzgecmisSuzgeci { Kaynak = "havuz" });
        var talepten = await servis.ListeAsync(new OzgecmisSuzgeci { Kaynak = "talep" });
        var hepsi = await servis.ListeAsync(new OzgecmisSuzgeci());

        Assert.Equal("Havuza Eklenen", Assert.Single(havuz.Veriler).AdSoyad);
        Assert.Equal("Talepten Gelen", Assert.Single(talepten.Veriler).AdSoyad);
        Assert.Equal(2, hepsi.Toplam);
    }

    // ── arama ──────────────────────────────────────────────────────────

    /// <summary>
    /// Telefon araması YAZIMDAN bağımsız.
    /// </summary>
    /// <remarks>
    /// Aynı numara veritabanında <c>0532 604 22 11</c>, <c>05326042211</c> ve
    /// <c>+90 532…</c> diye üç türlü duruyor; ham sütunda arama bitişik
    /// yazınca bulmuyordu. Sade sütun tam da bunun için var.
    /// </remarks>
    [Theory]
    [InlineData("0532 604 22 11")]
    [InlineData("05326042211")]
    [InlineData("+90 532 604 22 11")]
    [InlineData("6042211")]
    public async Task Telefon_aramasi_yazimdan_bagimsiz(string terim)
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (servis, _) = Servis(b);

        await servis.OlusturAsync(Istek(telefon: "0532 604 22 11"), Dosya());

        var sonuc = await servis.ListeAsync(new OzgecmisSuzgeci { Ara = terim });
        Assert.Equal(1, sonuc.Toplam);
    }

    /// <summary>Ad, meslek ve açıklama da aranır.</summary>
    [Theory]
    [InlineData("kemal")]
    [InlineData("kaynak")]
    public async Task Metin_aramasi_ad_ve_aciklamada_calisir(string terim)
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (servis, _) = Servis(b);

        await servis.OlusturAsync(Istek(), Dosya());

        var sonuc = await servis.ListeAsync(new OzgecmisSuzgeci { Ara = terim });
        Assert.Equal(1, sonuc.Toplam);
    }

    // ── görünürlük ─────────────────────────────────────────────────────

    /// <summary>
    /// Havuz BİRİMDEN BAĞIMSIZ.
    /// </summary>
    /// <remarks>
    /// Sistemin geri kalanında kayıt birim süzgecinden geçer; burada geçmez.
    /// Bu test o kasıtlı istisnayı kilitliyor — "tutarlılık" adına eklenecek
    /// bir birim süzgeci modülü işlevsiz bırakır: bir müdürlüğün elindeki
    /// özgeçmişi işe alacak olan başka müdürlük göremezdi.
    /// </remarks>
    [Fact]
    public async Task Havuz_birim_suzgecinden_gecmez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();

        var (birinci, _) = Servis(b, kullaniciId: 1, kullanici: "kalem1", birimId: 1);
        await birinci.OlusturAsync(Istek("Birinci Birimin Kaydı"), Dosya());

        var (ikinci, _) = Servis(b, kullaniciId: 2, kullanici: "fen1", birimId: 2);
        var sonuc = await ikinci.ListeAsync(new OzgecmisSuzgeci());

        Assert.Equal("Birinci Birimin Kaydı", Assert.Single(sonuc.Veriler).AdSoyad);

        // Birim yine de SÜZGEÇ olarak sunuluyor: "bizim eklediklerimiz"
        // meşru bir soru, ama varsayılan kısıt değil.
        var yalnizIkinci = await ikinci.ListeAsync(new OzgecmisSuzgeci { BirimId = 2 });
        Assert.Empty(yalnizIkinci.Veriler);
    }

    // ── paylaşım ───────────────────────────────────────────────────────

    /// <summary>Paylaşım kaydı kalır ve alıcıya bildirim gider.</summary>
    [Fact]
    public async Task Paylasim_kaydi_ve_bildirim_uretir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (servis, mesaj) = Servis(b);

        var kayit = await servis.OlusturAsync(Istek(), Dosya());
        var alici = await b.Users.Select(k => k.Id).FirstAsync();

        var adet = await servis.PaylasAsync(kayit.Id, new PaylasimIstegi
        {
            AliciIdler = [alici],
            Not = "Fen İşleri için uygun olabilir.",
        });

        Assert.Equal(1, adet);
        Assert.Single(mesaj.TekKisiyeGidenMesajlar);

        var detay = await servis.DetayAsync(kayit.Id);
        var paylasim = Assert.Single(detay.Paylasimlar);
        Assert.Equal("Fen İşleri için uygun olabilir.", paylasim.Not);
    }

    /// <summary>Alıcısız paylaşım iş kuralına takılır.</summary>
    [Fact]
    public async Task Alicisiz_paylasim_reddedilir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (servis, _) = Servis(b);

        var kayit = await servis.OlusturAsync(Istek(), Dosya());

        await Assert.ThrowsAsync<Web.Exceptions.BusinessRuleException>(
            () => servis.PaylasAsync(kayit.Id, new PaylasimIstegi()));
    }

    // ── silme ──────────────────────────────────────────────────────────

    /// <summary>
    /// Silme YUMUŞAK: kayıt listeden çıkar, satır durur.
    /// </summary>
    /// <remarks>
    /// Dosya kurumun elindeki tek kopya olabilir ve aynı dosya bir talebe de
    /// bağlı olabilir; diskten silmek geri dönüşü olmayan bir kayıp olurdu.
    /// </remarks>
    [Fact]
    public async Task Silme_yumusaktir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using var b = _ortam.Baglam();
        var (servis, _) = Servis(b);

        var kayit = await servis.OlusturAsync(Istek(), Dosya());
        await servis.SilAsync(kayit.Id);

        var liste = await servis.ListeAsync(new OzgecmisSuzgeci());
        Assert.Empty(liste.Veriler);
        Assert.True(await b.Ozgecmisler.IgnoreQueryFilters().AnyAsync(o => o.Id == kayit.Id));
    }
}
