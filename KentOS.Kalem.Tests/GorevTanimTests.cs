using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Kalem.Application.Dto.V2.IsTakip;
using KentOS.Kalem.Application.Dto.V2.Ortak;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Web.AuthPolicies;
using KentOS.Kalem.Web.Exceptions;
using KentOS.Kalem.Web.Services.V2;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// GÖREV TİPİ ve EKİP tanımları.
/// </summary>
/// <remarks>
/// Bu iki tanım görev akışının GİRDİSİ: tip bir işin nasıl ölçüleceğini,
/// ekip bildirimin kime gideceğini belirliyor. Buradaki bir hata akışta
/// değil, akışın beslendiği yerde sessizce durur.
/// </remarks>
[Collection(SunucuKoleksiyonu.Ad)]
public class GorevTanimTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    private IEtkinBirim EtkinBirim(long birimId) => new EtkinBirim(
        new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
        new SahteKullaniciServisi(1, "test", birimId),
        new BirimAgaci(_ortam.Baglam(), new MemoryCache(new MemoryCacheOptions())),
        new HerSeyeIzinli());

    private IGorevTipiServisi TipServisi(long birimId = 1) => new GorevTipiServisi(
        _ortam.Baglam(), new SahteKullaniciServisi(1, "test", birimId), EtkinBirim(birimId));

    private IEkipServisi EkipServisi(long birimId = 1) =>
        new EkipServisi(_ortam.Baglam(), EtkinBirim(birimId));

    private static GorevTipiKayitDto TipKaydi(string? ad = null) => new()
    {
        Ad = ad ?? "Çukur Onarımı " + Guid.NewGuid().ToString("N")[..6],
        SlaSaat = 48,
        HizmetStandardiGun = 5,
        Asamalar =
        [
            new GorevTipiAsamaDto { SiraNo = 7, Ad = "Keşif", Zorunlu = true },
            new GorevTipiAsamaDto { SiraNo = 7, Ad = "Uygulama", Zorunlu = true, FotografZorunlu = true },
        ],
    };

    // ── görev tipi ─────────────────────────────────────────────────────

    /// <summary>
    /// Aşama sırası SUNUCUDA yeniden numaralanır.
    /// </summary>
    /// <remarks>
    /// Sürükle-bırak sonrası arayüz 1, 2, 5 gibi boşluklu ya da çakışan
    /// değerler gönderebilir. Sıra numarası doğrudan kabul edilseydi
    /// aşamaların gösterim sırası ile tamamlanma sırası ayrışırdı.
    /// </remarks>
    [Fact]
    public async Task Asama_sirasi_sunucuda_yeniden_numaralanir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var tip = await TipServisi().OlusturAsync(TipKaydi());

        Assert.Equal([1, 2], tip.Asamalar.Select(a => a.SiraNo));
        Assert.Equal(["Keşif", "Uygulama"], tip.Asamalar.Select(a => a.Ad));
    }

    /// <summary>Aşama listesi TAM LİSTE — gövdede olmayan aşama silinir.</summary>
    [Fact]
    public async Task Asama_listesi_TAM_LISTE()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var servis = TipServisi();
        var tip = await servis.OlusturAsync(TipKaydi());

        var kayit = TipKaydi(tip.Ad);
        kayit.Asamalar = [new GorevTipiAsamaDto { Ad = "Tek aşama", Zorunlu = true }];

        var guncel = await servis.GuncelleAsync(tip.Id, kayit);

        Assert.Single(guncel.Asamalar);
        Assert.Equal("Tek aşama", guncel.Asamalar[0].Ad);
    }

    /// <summary>Aynı adla ikinci tip açılamaz — listede hangisi olduğu anlaşılmazdı.</summary>
    [Fact]
    public async Task Ayni_adla_ikinci_tip_acilamaz()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var servis = TipServisi();
        var tip = await servis.OlusturAsync(TipKaydi());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.OlusturAsync(TipKaydi(tip.Ad)));

        // Kendi adıyla güncellemek çarpışma SAYILMAZ.
        var guncel = await servis.GuncelleAsync(tip.Id, TipKaydi(tip.Ad));
        Assert.Equal(tip.Ad, guncel.Ad);
    }

    /// <summary>
    /// BOŞ birim listesi "herkes kullanabilir" demek.
    /// </summary>
    /// <remarks>
    /// Kurum geneli tipler (şikayet, talep) için her birimi tek tek
    /// işaretlemek zorunda kalmamak adına. Ayrı bir "kurum geneli" bayrağı,
    /// iki ayarın çelişebileceği bir durum yaratırdı.
    /// </remarks>
    [Fact]
    public async Task Bos_birim_listesi_herkese_acik()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var tip = await TipServisi(birimId: 1).OlusturAsync(TipKaydi());

        var baskaBirim = await TipServisi(birimId: 2).KullanilabilirlerAsync();
        Assert.Contains(baskaBirim, t => t.Id == tip.Id);
    }

    /// <summary>Birim listesi doluysa YALNIZCA o birimler kullanabilir.</summary>
    [Fact]
    public async Task Birim_listesi_doluysa_digerleri_kullanamaz()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var kayit = TipKaydi();
        kayit.BirimIdler = [1];

        var tip = await TipServisi(birimId: 1).OlusturAsync(kayit);

        Assert.Contains(await TipServisi(1).KullanilabilirlerAsync(), t => t.Id == tip.Id);
        Assert.DoesNotContain(await TipServisi(2).KullanilabilirlerAsync(), t => t.Id == tip.Id);
    }

    /// <summary>Kullanımdan kaldırılmış tip görev açma listesinde ÇIKMAZ.</summary>
    [Fact]
    public async Task Kullanimdan_kaldirilan_tip_secilemez()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var servis = TipServisi();
        var tip = await servis.OlusturAsync(TipKaydi());

        var kapali = TipKaydi(tip.Ad);
        kapali.Kullanimda = false;
        await servis.GuncelleAsync(tip.Id, kapali);

        Assert.DoesNotContain(await servis.KullanilabilirlerAsync(), t => t.Id == tip.Id);

        // Yönetim listesinde DURUYOR — tanım kaybolmadı, yalnızca seçilemiyor.
        var yonetim = await servis.ListeAsync(new SayfaIstegi { Boyut = 200 }, false);
        Assert.Contains(yonetim.Veriler, t => t.Id == tip.Id);
    }

    /// <summary>
    /// KULLANILMIŞ tip SİLİNEMEZ.
    /// </summary>
    /// <remarks>
    /// Silmek, açılmış görevlerin "hangi hizmet standardına göre ölçüldü?"
    /// sorusunun cevabını kaybettirirdi. Doğru yol kullanımdan kaldırmak.
    /// </remarks>
    [Fact]
    public async Task Kullanilmis_tip_SILINEMEZ()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var servis = TipServisi();
        var tip = await servis.OlusturAsync(TipKaydi());

        using (var b = _ortam.Baglam())
        {
            b.Gorevler.Add(new Application.Models.WorkTask
            {
                TakipNo = "GRV-TEST-" + Guid.NewGuid().ToString("N")[..8],
                Baslik = "Tip kullanımda",
                GorevTipiId = tip.Id,
                BirimId = 1,
            });
            await b.SaveChangesAsync();
        }

        var hata = await Assert.ThrowsAsync<BusinessRuleException>(() => servis.SilAsync(tip.Id));
        Assert.Contains("KULLANIMDAN KALDIRIN", hata.Message);

        // Hiç kullanılmamış tip silinebilir.
        var bosTip = await servis.OlusturAsync(TipKaydi());
        await servis.SilAsync(bosTip.Id);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => servis.GetirAsync(bosTip.Id));
    }

    // ── ekip ───────────────────────────────────────────────────────────

    /// <summary>Ekip lideri, ekibin ÜYESİ olmalı — dışarıdan biri ekibi yönetemez.</summary>
    [Fact]
    public async Task Ekip_lideri_uye_olmali()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var servis = EkipServisi();

        await Assert.ThrowsAsync<BusinessRuleException>(() => servis.OlusturAsync(new EkipKayitDto
        {
            Ad = "Budama Ekibi " + Guid.NewGuid().ToString("N")[..6],
            LiderId = 3,
            UyeIdler = [1, 2],
        }));
    }

    /// <summary>
    /// Ekibe atamada bildirim ÖNCE LİDERE gider.
    /// </summary>
    /// <remarks>
    /// Kullanıcının tarifi: "ekip varsa ekip başına" — iş dağıtımını lider
    /// yapar. Herkese bildirmek, bir işi beş kişinin birden sahiplendiğini
    /// sanmasına yol açardı.
    /// </remarks>
    [Fact]
    public async Task Ekipte_bildirim_ONCE_lidere_gider()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var servis = EkipServisi();
        var ekip = await servis.OlusturAsync(new EkipKayitDto
        {
            Ad = "Budama Ekibi " + Guid.NewGuid().ToString("N")[..6],
            LiderId = 2,
            UyeIdler = [1, 2, 3],
        });

        Assert.Equal([2L], await servis.BildirimHedefleriAsync(ekip.Id));
    }

    /// <summary>
    /// LİDERSİZ ekipte bildirim HERKESE gider.
    /// </summary>
    /// <remarks>
    /// Kimseye bildirmemek atamayı görünmez kılardı — göreve verildiğini
    /// kimsenin bilmediği bir ekip, atanmamış bir görevle aynı şey.
    /// </remarks>
    [Fact]
    public async Task Lidersiz_ekipte_bildirim_HERKESE_gider()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var servis = EkipServisi();
        var ekip = await servis.OlusturAsync(new EkipKayitDto
        {
            Ad = "Lidersiz Ekip " + Guid.NewGuid().ToString("N")[..6],
            UyeIdler = [1, 2, 3],
        });

        var hedefler = await servis.BildirimHedefleriAsync(ekip.Id);
        Assert.Equal([1L, 2L, 3L], hedefler.Order());
    }

    /// <summary>Başka birimin ekibi 403 değil BULUNAMADI döner.</summary>
    [Fact]
    public async Task Baska_birimin_ekibi_BULUNAMADI_doner()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var ekip = await EkipServisi(birimId: 1).OlusturAsync(new EkipKayitDto
        {
            Ad = "Birim 1 Ekibi " + Guid.NewGuid().ToString("N")[..6],
            UyeIdler = [1],
        });

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            EkipServisi(birimId: 2).GetirAsync(ekip.Id));
    }

    /// <summary>Üye listesi TAM LİSTE — gövdede olmayan üye çıkarılır.</summary>
    [Fact]
    public async Task Uye_listesi_TAM_LISTE()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var servis = EkipServisi();
        var ad = "Ekip " + Guid.NewGuid().ToString("N")[..6];

        var ekip = await servis.OlusturAsync(new EkipKayitDto { Ad = ad, UyeIdler = [1, 2, 3] });
        Assert.Equal(3, ekip.UyeSayisi);

        var guncel = await servis.GuncelleAsync(ekip.Id, new EkipKayitDto { Ad = ad, UyeIdler = [1] });
        Assert.Equal(1, guncel.UyeSayisi);
        Assert.Equal(1, guncel.Uyeler[0].KullaniciId);
    }

    /// <summary>
    /// AÇIK GÖREVİ olan ekip SİLİNEMEZ.
    /// </summary>
    /// <remarks>
    /// Silmek o görevleri sahipsiz bırakırdı: atama satırı artık var olmayan
    /// bir ekibi gösterirdi. Kapanmış görevlerin ataması tarihî kayıt;
    /// onlar engel değil.
    /// </remarks>
    [Fact]
    public async Task Acik_gorevi_olan_ekip_SILINEMEZ()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var servis = EkipServisi();
        var ekip = await servis.OlusturAsync(new EkipKayitDto
        {
            Ad = "Yüklü Ekip " + Guid.NewGuid().ToString("N")[..6],
            UyeIdler = [1],
        });

        long gorevId;
        using (var b = _ortam.Baglam())
        {
            var gorev = new Application.Models.WorkTask
            {
                TakipNo = "GRV-TEST-" + Guid.NewGuid().ToString("N")[..8],
                Baslik = "Ekibe atanmış iş",
                BirimId = 1,
                Durum = GorevDurumu.Basladi,
            };
            b.Gorevler.Add(gorev);
            await b.SaveChangesAsync();
            gorevId = gorev.Id;

            b.GorevAtamalari.Add(new Application.Models.WorkTaskAssignment
            {
                GorevId = gorevId, EkipId = ekip.Id,
            });
            await b.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<BusinessRuleException>(() => servis.SilAsync(ekip.Id));

        // Görev KAPANINCA ekip silinebilir hâle gelir.
        using (var b = _ortam.Baglam())
        {
            await b.Gorevler.Where(g => g.Id == gorevId)
                .ExecuteUpdateAsync(s => s.SetProperty(g => g.Durum, GorevDurumu.Tamamlandi));
        }

        await servis.SilAsync(ekip.Id);
        await Assert.ThrowsAsync<EntityNotFoundException>(() => servis.GetirAsync(ekip.Id));
    }

    /// <summary>Bu testlerin konusu yetki değil TANIM; izin kapısı açık tutuluyor.</summary>
    private sealed class HerSeyeIzinli : IIzinServisi
    {
        public Task<IReadOnlySet<string>> IzinleriAsync(long kullaniciId) =>
            Task.FromResult<IReadOnlySet<string>>(Izinler.Adlar.ToHashSet());

        public Task<bool> VarMiAsync(long kullaniciId, string izin) => Task.FromResult(true);
        public void Dusur(long kullaniciId) { }
        public Task RolDegistiAsync(long rolId) => Task.CompletedTask;
    }
}
