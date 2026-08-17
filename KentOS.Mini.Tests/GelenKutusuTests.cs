using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Mini.Application.Dto.V2.IsTakip;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Services.V2;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// BİRİM GELEN KUTUSU — birimden birime iş devri.
/// </summary>
/// <remarks>
/// Kilitlenen kurallar: devir <b>tamamlanma anında</b> tetiklenir (onay
/// beklerken değil), aynı görevden aynı birime <b>iki kez düşmez</b>, kabul
/// görevi <b>hedef birimde</b> açar ve bilgilendirme kaydı karar istemez.
/// </remarks>
[Collection(SunucuKoleksiyonu.Ad)]
public class GelenKutusuTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;
    private readonly SahteMesajServisi _mesajlar = new();

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    /// <summary>Görev ve gelen kutusu servislerini BİRBİRİNE bağlı kurar.</summary>
    private (IGorevServisi Gorev, IGelenKutusuServisi Kutu, AppDbContext Baglam) Kur(long birimId = 1)
    {
        var baglam = _ortam.Baglam();
        var kullanici = new SahteKullaniciServisi(1, "test", birimId);

        var etkin = new EtkinBirim(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            kullanici,
            new BirimAgaci(_ortam.Baglam(), new MemoryCache(new MemoryCacheOptions())),
            new HerSeyeIzinli());

        var olaylar = new IsOlayServisi(baglam, kullanici, etkin, NullLogger<IsOlayServisi>.Instance);
        var ekler = new IsEkServisi(baglam, new TestServisFabrikasi.SahteDepo(), kullanici,
            NullLogger<IsEkServisi>.Instance);
        var yorumlar = new IsYorumServisi(baglam, kullanici);

        // Dairesel bağ: görev servisi devri sağlayıcıdan çözüyor, devir
        // servisi görev servisini kurucudan alıyor. Testte bu düğüm elle
        // atılıyor — üretimde DI kapsayıcısı aynı işi yapıyor.
        GorevServisi? gorev = null;
        var kutuSaglayici = new TekServisSaglayici(() => new GelenKutusuServisi(
            baglam, kullanici, etkin, olaylar, gorev!, _mesajlar,
            NullLogger<GelenKutusuServisi>.Instance));

        gorev = new GorevServisi(
            baglam, kullanici, etkin, olaylar, ekler, yorumlar,
            new EkipServisi(baglam, etkin), _mesajlar, kutuSaglayici,
            NullLogger<GorevServisi>.Instance);

        return (gorev, kutuSaglayici.Kutu, baglam);
    }

    /// <summary>Devir kuralı olan bir tip kurar.</summary>
    private async Task<long> DevirliTipAsync(long hedefBirim, bool isTalebi = true)
    {
        using var b = _ortam.Baglam();
        await _ortam.TemelVerileriKurAsync();

        var tip = new TaskType { Ad = "Devirli " + Guid.NewGuid().ToString("N")[..6], Kullanimda = true };
        b.GorevTipleri.Add(tip);
        await b.SaveChangesAsync();

        b.GorevTipiDevirleri.Add(new TaskTypeHandoff
        {
            GorevTipiId = tip.Id,
            HedefBirimId = hedefBirim,
            IsTalebi = isTalebi,
            Not = "Devir notu.",
        });
        await b.SaveChangesAsync();

        return tip.Id;
    }

    /// <summary>Görevi açıp onaya kadar götürür.</summary>
    private async Task<long> TamamlanmisGorevAsync(IGorevServisi gorev, long tipId)
    {
        var g = await gorev.OlusturAsync(new GorevKayitDto
        {
            Baslik = "Yol yaması",
            GorevTipiId = tipId,
            Atamalar = [new GorevAtamaIstegiDto { KullaniciId = 2 }],
        });

        await gorev.DurumDegistirAsync(g.Id, new GorevDurumIstegiDto { Durum = GorevDurumu.Basladi });
        await gorev.DurumDegistirAsync(g.Id, new GorevDurumIstegiDto { Durum = GorevDurumu.TamamlanmaBekliyor });

        return g.Id;
    }

    // ── tetikleme ──────────────────────────────────────────────────────

    /// <summary>
    /// DEVİR TAMAMLANMA ANINDA TETİKLENİR, onay beklerken DEĞİL.
    /// </summary>
    /// <remarks>
    /// Henüz kabul edilmemiş bir iş için başka birime kayıt düşürmek, iade
    /// hâlinde o birimi boşuna meşgul ederdi.
    /// </remarks>
    [Fact]
    public async Task Devir_onay_beklerken_TETIKLENMEZ()
    {
        PostgresYoksaAtla();

        var tipId = await DevirliTipAsync(hedefBirim: 2);
        var (gorev, _, baglam) = Kur();

        var gorevId = await TamamlanmisGorevAsync(gorev, tipId);

        Assert.False(await baglam.BirimGelenKutusu.AnyAsync(k => k.KaynakGorevId == gorevId));

        await gorev.DurumDegistirAsync(gorevId, new GorevDurumIstegiDto { Durum = GorevDurumu.Tamamlandi });

        Assert.True(await baglam.BirimGelenKutusu.AnyAsync(k => k.KaynakGorevId == gorevId));
    }

    /// <summary>
    /// AYNI GÖREVDEN AYNI BİRİME İKİ KEZ DÜŞMEZ.
    /// </summary>
    /// <remarks>
    /// Görev iade edilip yeniden tamamlanırsa ikinci bir kayıt doğar ve
    /// hedef birim aynı işi iki kez karara bağlardı.
    /// </remarks>
    [Fact]
    public async Task Ayni_gorevden_IKI_KEZ_dusmez()
    {
        PostgresYoksaAtla();

        var tipId = await DevirliTipAsync(hedefBirim: 2);
        var (gorev, kutu, baglam) = Kur();

        var gorevId = await TamamlanmisGorevAsync(gorev, tipId);
        await gorev.DurumDegistirAsync(gorevId, new GorevDurumIstegiDto { Durum = GorevDurumu.Tamamlandi });

        // İkinci kez elle tetikleniyor: iade + yeniden tamamlama akışının
        // sonucu aynı çağrı.
        await kutu.DevirleriUygulaAsync(gorevId);

        Assert.Equal(1, await baglam.BirimGelenKutusu.CountAsync(k => k.KaynakGorevId == gorevId));
    }

    /// <summary>KENDİ birimine devir anlamsız — iş zaten orada bitti.</summary>
    [Fact]
    public async Task Kendi_birimine_devir_dusmez()
    {
        PostgresYoksaAtla();

        var tipId = await DevirliTipAsync(hedefBirim: 1);
        var (gorev, _, baglam) = Kur(birimId: 1);

        var gorevId = await TamamlanmisGorevAsync(gorev, tipId);
        await gorev.DurumDegistirAsync(gorevId, new GorevDurumIstegiDto { Durum = GorevDurumu.Tamamlandi });

        Assert.False(await baglam.BirimGelenKutusu.AnyAsync(k => k.KaynakGorevId == gorevId));
    }

    // ── karar ──────────────────────────────────────────────────────────

    /// <summary>
    /// KABUL, görevi HEDEF BİRİMDE açar.
    /// </summary>
    [Fact]
    public async Task Kabul_hedef_birimde_gorev_acar()
    {
        PostgresYoksaAtla();

        var tipId = await DevirliTipAsync(hedefBirim: 2);
        var (gorev, _, baglam) = Kur(birimId: 1);

        var gorevId = await TamamlanmisGorevAsync(gorev, tipId);
        await gorev.DurumDegistirAsync(gorevId, new GorevDurumIstegiDto { Durum = GorevDurumu.Tamamlandi });

        var kayit = await baglam.BirimGelenKutusu.AsNoTracking()
            .FirstAsync(k => k.KaynakGorevId == gorevId);

        // Karar HEDEF birimin kullanıcısı tarafından veriliyor.
        var (_, kutu2, baglam2) = Kur(birimId: 2);
        var sonuc = await kutu2.KabulAsync(kayit.Id, new GelenKutusuKabulDto());

        Assert.Equal(GelenKutusuDurumu.Kabul, sonuc.Durum);
        Assert.NotNull(sonuc.GorevId);

        var yeniGorev = await baglam2.Gorevler.AsNoTracking().FirstAsync(g => g.Id == sonuc.GorevId);

        Assert.Equal(2, yeniGorev.BirimId);
        Assert.Equal(GorevKaynagi.BirimDevri, yeniGorev.Kaynak);
        Assert.Equal(gorevId, yeniGorev.KaynakId);
    }

    /// <summary>RET GEREKÇESİZ yapılamaz — kaynak neyi düzelteceğini bilmeli.</summary>
    [Fact]
    public async Task Ret_gerekcesiz_yapilamaz()
    {
        PostgresYoksaAtla();

        var tipId = await DevirliTipAsync(hedefBirim: 2);
        var (gorev, _, baglam) = Kur(birimId: 1);

        var gorevId = await TamamlanmisGorevAsync(gorev, tipId);
        await gorev.DurumDegistirAsync(gorevId, new GorevDurumIstegiDto { Durum = GorevDurumu.Tamamlandi });

        var kayit = await baglam.BirimGelenKutusu.AsNoTracking()
            .FirstAsync(k => k.KaynakGorevId == gorevId);

        var (_, kutu2, _) = Kur(birimId: 2);

        await Assert.ThrowsAsync<BusinessRuleException>(() => kutu2.ReddetAsync(kayit.Id, "  "));

        var red = await kutu2.ReddetAsync(kayit.Id, "Bizim işimiz değil.");
        Assert.Equal(GelenKutusuDurumu.Ret, red.Durum);
        Assert.Null(red.GorevId);
    }

    /// <summary>İKİNCİ karar reddedilir — kayıt bir kez işlenir.</summary>
    [Fact]
    public async Task Ikinci_karar_REDDEDILIR()
    {
        PostgresYoksaAtla();

        var tipId = await DevirliTipAsync(hedefBirim: 2);
        var (gorev, _, baglam) = Kur(birimId: 1);

        var gorevId = await TamamlanmisGorevAsync(gorev, tipId);
        await gorev.DurumDegistirAsync(gorevId, new GorevDurumIstegiDto { Durum = GorevDurumu.Tamamlandi });

        var kayit = await baglam.BirimGelenKutusu.AsNoTracking()
            .FirstAsync(k => k.KaynakGorevId == gorevId);

        var (_, kutu2, _) = Kur(birimId: 2);
        await kutu2.KabulAsync(kayit.Id, new GelenKutusuKabulDto());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            kutu2.ReddetAsync(kayit.Id, "Vazgeçtim"));
    }

    /// <summary>
    /// BİLGİLENDİRME kaydı karar İSTEMEZ.
    /// </summary>
    /// <remarks>
    /// İkisini ayırmasaydık hedef birim her bilgilendirme için de karar
    /// vermek zorunda kalır ve gelen kutusu hızla kullanılamaz hâle gelirdi.
    /// </remarks>
    [Fact]
    public async Task Bilgilendirme_kaydi_gorev_acmaz()
    {
        PostgresYoksaAtla();

        var tipId = await DevirliTipAsync(hedefBirim: 2, isTalebi: false);
        var (gorev, _, baglam) = Kur(birimId: 1);

        var gorevId = await TamamlanmisGorevAsync(gorev, tipId);
        await gorev.DurumDegistirAsync(gorevId, new GorevDurumIstegiDto { Durum = GorevDurumu.Tamamlandi });

        var kayit = await baglam.BirimGelenKutusu.AsNoTracking()
            .FirstAsync(k => k.KaynakGorevId == gorevId);

        var (_, kutu2, _) = Kur(birimId: 2);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            kutu2.KabulAsync(kayit.Id, new GelenKutusuKabulDto()));

        var okundu = await kutu2.OkunduAsync(kayit.Id);
        Assert.Equal(GelenKutusuDurumu.Okundu, okundu.Durum);
        Assert.Null(okundu.GorevId);
    }

    /// <summary>Başka birimin gelen kutusu 403 değil BULUNAMADI döner.</summary>
    [Fact]
    public async Task Baska_birimin_kaydi_BULUNAMADI_doner()
    {
        PostgresYoksaAtla();

        var tipId = await DevirliTipAsync(hedefBirim: 2);
        var (gorev, kutu1, baglam) = Kur(birimId: 1);

        var gorevId = await TamamlanmisGorevAsync(gorev, tipId);
        await gorev.DurumDegistirAsync(gorevId, new GorevDurumIstegiDto { Durum = GorevDurumu.Tamamlandi });

        var kayit = await baglam.BirimGelenKutusu.AsNoTracking()
            .FirstAsync(k => k.KaynakGorevId == gorevId);

        // Kayıt 2 numaralı birime düştü; 1 numaralı birim onu göremez.
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            kutu1.KabulAsync(kayit.Id, new GelenKutusuKabulDto()));

        var liste = await kutu1.ListeAsync(new SayfaIstegi { Boyut = 50 }, null, false);
        Assert.DoesNotContain(liste.Veriler, k => k.Id == kayit.Id);
    }

    /// <summary>Konum kaynak görevden KOPYALANIR — kabul eden aynı yere gidecek.</summary>
    [Fact]
    public async Task Konum_kaynak_gorevden_kopyalanir()
    {
        PostgresYoksaAtla();

        var tipId = await DevirliTipAsync(hedefBirim: 2);
        var (gorev, _, baglam) = Kur(birimId: 1);

        var g = await gorev.OlusturAsync(new GorevKayitDto
        {
            Baslik = "Konumlu iş",
            GorevTipiId = tipId,
            Enlem = 39.7477,
            Boylam = 37.0179,
            Adres = "Atatürk Caddesi",
            Atamalar = [new GorevAtamaIstegiDto { KullaniciId = 2 }],
        });

        await gorev.DurumDegistirAsync(g.Id, new GorevDurumIstegiDto { Durum = GorevDurumu.Basladi });
        await gorev.DurumDegistirAsync(g.Id, new GorevDurumIstegiDto { Durum = GorevDurumu.TamamlanmaBekliyor });
        await gorev.DurumDegistirAsync(g.Id, new GorevDurumIstegiDto { Durum = GorevDurumu.Tamamlandi });

        var kayit = await baglam.BirimGelenKutusu.AsNoTracking()
            .FirstAsync(k => k.KaynakGorevId == g.Id);

        Assert.Equal(39.7477, kayit.Enlem);
        Assert.Equal("Atatürk Caddesi", kayit.Adres);
    }

    /// <summary>Yalnızca gelen kutusu servisini çözen küçük sağlayıcı.</summary>
    private sealed class TekServisSaglayici(Func<IGelenKutusuServisi> fabrika) : IServiceProvider
    {
        private IGelenKutusuServisi? _kutu;

        public IGelenKutusuServisi Kutu => _kutu ??= fabrika();

        public object? GetService(Type tur) =>
            tur == typeof(IGelenKutusuServisi) ? Kutu : null;
    }

    /// <summary>Bu testlerin konusu yetki değil AKIŞ; izin kapısı açık.</summary>
    private sealed class HerSeyeIzinli : IIzinServisi
    {
        public Task<IReadOnlySet<string>> IzinleriAsync(long kullaniciId) =>
            Task.FromResult<IReadOnlySet<string>>(Izinler.Adlar.ToHashSet());

        public Task<bool> VarMiAsync(long kullaniciId, string izin) => Task.FromResult(true);
        public void Dusur(long kullaniciId) { }
        public Task RolDegistiAsync(long rolId) => Task.CompletedTask;
    }
}
