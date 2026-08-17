using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Controllers.V2;
using KentOS.Mini.Web.Services.V2;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// BİRİM AĞACI ve ETKİN BİRİM (vekâlet) sözleşmesi.
///
/// <para>
/// Bu iki parça iş takip modülünün görünürlük temeli. Bir hata burada
/// sızarsa bir müdürlük başka bir müdürlüğün bütün işini okur — ve hata
/// sessizdir: sorgu çalışır, liste dolu gelir, kimse yanlış veriye baktığını
/// anlamaz.
/// </para>
/// </summary>
[Collection(SunucuKoleksiyonu.Ad)]
public class EtkinBirimTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    /// <summary>
    /// Üç seviyeli ağaç kurar: 1 (kök) → 10 (başkan yrd.) → 11, 12 (müdürlük).
    /// 2 numaralı birim ağacın DIŞINDA kalır — vekâletin sınırını sınamak için.
    /// </summary>
    private async Task AgacKurAsync()
    {
        using var b = _ortam.Baglam();
        await _ortam.TemelVerileriKurAsync();

        foreach (var (id, ad, ust) in new (long, string, long?)[]
                 {
                     (10, "Başkan Yardımcılığı", 1),
                     (11, "Park ve Bahçeler Müdürlüğü", 10),
                     (12, "Fen İşleri Müdürlüğü", 10),
                     (13, "Alt Şeflik", 11),
                 })
        {
            if (!await b.Birimler.AnyAsync(x => x.Id == id))
            {
                b.Birimler.Add(new Birim
                {
                    Id = id, Ad = ad, UstBirimId = ust,
                    Yetkili = "Yetkili", Unvan = "Müdür",
                });
            }
        }

        await b.SaveChangesAsync();
    }

    private BirimAgaci Agac() => new(_ortam.Baglam(), new MemoryCache(new MemoryCacheOptions()));

    // ── birim ağacı ────────────────────────────────────────────────────

    [Fact]
    public async Task Alt_agac_butun_torunlari_getirir()
    {
        PostgresYoksaAtla();
        await AgacKurAsync();

        var kume = await Agac().AltAgacAsync(10);

        // 10'un altında 11, 12 ve 11'in altında 13 var. Kökün kendisi de dahil.
        Assert.Equal(new HashSet<long> { 10, 11, 12, 13 }, kume);
    }

    [Fact]
    public async Task Yaprak_birim_yalnizca_kendisini_getirir()
    {
        PostgresYoksaAtla();
        await AgacKurAsync();

        Assert.Equal(new HashSet<long> { 13 }, await Agac().AltAgacAsync(13));
    }

    /// <summary>
    /// YAN ve ÜST birimler ağaca GİRMEZ — vekâletin tek yönlü olmasının temeli.
    /// </summary>
    [Fact]
    public async Task Ust_ve_yan_birimler_alt_agacta_yok()
    {
        PostgresYoksaAtla();
        await AgacKurAsync();

        var kume = await Agac().AltAgacAsync(11);

        Assert.Contains(13L, kume);       // kendi altı
        Assert.DoesNotContain(10L, kume); // üstü
        Assert.DoesNotContain(12L, kume); // kardeşi
        Assert.DoesNotContain(1L, kume);  // kökü
    }

    [Fact]
    public async Task Gecersiz_kok_bos_kume_dondurur()
    {
        PostgresYoksaAtla();
        await AgacKurAsync();

        Assert.Empty(await Agac().AltAgacAsync(0));
        Assert.Empty(await Agac().AltAgacAsync(-5));
        Assert.Empty(await Agac().AltAgacAsync(999_999));
    }

    // ── etkin birim (vekâlet) ──────────────────────────────────────────

    /// <summary>
    /// Başlık yoksa kullanıcının KENDİ birimi. Uygulamanın bugünkü davranışı
    /// bu; vekâlet bir ek, varsayılan değil.
    /// </summary>
    [Fact]
    public async Task Baslik_yoksa_kendi_birimi()
    {
        PostgresYoksaAtla();
        await AgacKurAsync();

        var etkin = Kur(kullaniciBirim: 10, baslik: null, izinVar: true);

        Assert.Equal(10, await etkin.IdAsync());
        Assert.False(await etkin.VekaletVarMiAsync());
    }

    [Fact]
    public async Task Alt_birim_secilebilir()
    {
        PostgresYoksaAtla();
        await AgacKurAsync();

        var etkin = Kur(kullaniciBirim: 10, baslik: "11", izinVar: true);

        Assert.Equal(11, await etkin.IdAsync());
        Assert.True(await etkin.VekaletVarMiAsync());
    }

    /// <summary>
    /// KARDEŞ birim reddedilir. Başlığa güvenmek, bir müdürün yandaki
    /// müdürlüğün bütün işini okuması demekti.
    /// </summary>
    [Fact]
    public async Task Kardes_birim_REDDEDILIR()
    {
        PostgresYoksaAtla();
        await AgacKurAsync();

        var etkin = Kur(kullaniciBirim: 11, baslik: "12", izinVar: true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => etkin.IdAsync());
    }

    /// <summary>ÜST birim de reddedilir — vekâlet yalnızca aşağı doğru.</summary>
    [Fact]
    public async Task Ust_birim_REDDEDILIR()
    {
        PostgresYoksaAtla();
        await AgacKurAsync();

        var etkin = Kur(kullaniciBirim: 11, baslik: "10", izinVar: true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => etkin.IdAsync());
    }

    /// <summary>
    /// İzni olmayanın başlığı YOK SAYILMAZ, reddedilir.
    /// </summary>
    /// <remarks>
    /// Sessizce kendi birimine düşmek, kullanıcıya seçtiği birimin verisini
    /// gösterdiğini sandırırdı — yanlış birimin listesine bakıp "burada iş
    /// yok" demek, hata mesajı görmekten çok daha kötü.
    /// </remarks>
    [Fact]
    public async Task Izin_yoksa_vekalet_REDDEDILIR()
    {
        PostgresYoksaAtla();
        await AgacKurAsync();

        var etkin = Kur(kullaniciBirim: 10, baslik: "11", izinVar: false);

        await Assert.ThrowsAsync<BusinessRuleException>(() => etkin.IdAsync());
    }

    /// <summary>
    /// Bozuk başlık HATA DEĞİL: eski bir istemci sürümü ya da araya giren bir
    /// vekil sunucu olabilir. Doğru davranış kullanıcının kendi birimi.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("0")]
    public async Task Bozuk_baslik_kendi_birimine_duser(string baslik)
    {
        PostgresYoksaAtla();
        await AgacKurAsync();

        var etkin = Kur(kullaniciBirim: 10, baslik: baslik, izinVar: true);

        Assert.Equal(10, await etkin.IdAsync());
    }

    /// <summary>Kendi birimini seçmek vekâlet sayılmaz ve izin istemez.</summary>
    [Fact]
    public async Task Kendi_birimini_secmek_izin_istemez()
    {
        PostgresYoksaAtla();
        await AgacKurAsync();

        var etkin = Kur(kullaniciBirim: 10, baslik: "10", izinVar: false);

        Assert.Equal(10, await etkin.IdAsync());
        Assert.False(await etkin.VekaletVarMiAsync());
    }

    [Fact]
    public async Task Kapsam_alt_birimleri_isteğe_bagli_katar()
    {
        PostgresYoksaAtla();
        await AgacKurAsync();

        var etkin = Kur(kullaniciBirim: 10, baslik: null, izinVar: true);

        Assert.Equal(new HashSet<long> { 10 }, await etkin.KapsamAsync(altBirimlerDahil: false));
        Assert.Equal(new HashSet<long> { 10, 11, 12, 13 },
                     await etkin.KapsamAsync(altBirimlerDahil: true));
    }

    // ── kapsam listesi: AĞAÇ SIRASI ────────────────────────────────────

    /// <summary>
    /// Liste ÖN SIRALI gezinme düzeninde gelir: çocuk, ebeveyninin hemen
    /// altında.
    /// </summary>
    /// <remarks>
    /// İlk sürüm "derinlik sonra ad" diye sıralıyordu. Canlı ölçümde bütün
    /// 2. seviye birimler, 1. seviyenin SONUNCUSUNUN altındaymış gibi
    /// göründü — girinti ile sıra birbirini yalanlıyordu ve kullanıcı "Park
    /// Müdürlüğü Zabıta'ya mı bağlı?" diye sorardı.
    /// </remarks>
    [Fact]
    public async Task Kapsam_listesi_agac_sirasinda_gelir()
    {
        PostgresYoksaAtla();
        await AgacKurAsync();

        var liste = await new BirimKapsamController(
            _ortam.Baglam(),
            new SahteKullaniciServisi(1, "test", 10),
            Agac()).ListeAsync(CancellationToken.None);

        var adlar = liste.Select(b => b.Ad).ToList();

        var park = adlar.IndexOf("Park ve Bahçeler Müdürlüğü");
        var altSeflik = adlar.IndexOf("Alt Şeflik");
        var fen = adlar.IndexOf("Fen İşleri Müdürlüğü");

        // ASIL DEĞİŞMEZ: çocuk, EBEVEYNİNİN HEMEN ARDINDAN gelir.
        // Bozuk sürümde "Alt Şeflik" listenin sonuna, son 1. seviye birimin
        // altına düşüyordu.
        Assert.Equal(park + 1, altSeflik);

        // Kardeşler kendi aralarında ada göre: "Fen" < "Park".
        Assert.True(fen < park, "Kardeşler ada göre sıralanmalı.");

        // Kendi birimi başta ve derinliği 0.
        Assert.True(liste[0].KendiBirimi);
        Assert.Equal(0, liste[0].Derinlik);
        Assert.Equal(1, liste[park].Derinlik);
        Assert.Equal(2, liste[altSeflik].Derinlik);
    }

    // ── kurulum yardımcısı ─────────────────────────────────────────────

    private EtkinBirim Kur(long kullaniciBirim, string? baslik, bool izinVar)
    {
        var baglam = new DefaultHttpContext();
        if (baslik is not null)
        {
            baglam.Request.Headers[IEtkinBirim.BaslikAdi] = baslik;
        }

        var erisim = new HttpContextAccessor { HttpContext = baglam };
        var kullanici = new SahteKullaniciServisi(1, "test", kullaniciBirim);

        return new EtkinBirim(erisim, kullanici, Agac(), new SahteIzinServisi(izinVar));
    }

    /// <summary>Yalnızca vekâlet iznini taşıyan/taşımayan sahte.</summary>
    private sealed class SahteIzinServisi(bool kapsamIzniVar) : IIzinServisi
    {
        public Task<IReadOnlySet<string>> IzinleriAsync(long kullaniciId) =>
            Task.FromResult<IReadOnlySet<string>>(
                kapsamIzniVar
                    ? new HashSet<string> { Izinler.GorevBirimKapsam }
                    : new HashSet<string>());

        public Task<bool> VarMiAsync(long kullaniciId, string izin) =>
            Task.FromResult(kapsamIzniVar && izin == Izinler.GorevBirimKapsam);

        public void Dusur(long kullaniciId) { }
        public Task RolDegistiAsync(long rolId) => Task.CompletedTask;
    }
}
