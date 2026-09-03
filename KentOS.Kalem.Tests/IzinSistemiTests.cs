using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.AuthPolicies;
using KentOS.Kalem.Web.Data;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// Rol → izin sistemi.
///
/// <para>
/// Önceki hâlde yetki dağılımı <c>PolicyRegistrar.cs</c> içinde sabit rol
/// listeleriydi: her yeni yetki bir kod değişikliği ve yayın demekti. Sistem
/// bunu iki kez zorlamış, <c>AppUser</c>'a iki ayrı <c>bool</c> sütun
/// eklenmişti.
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class IzinSistemiTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    private async Task TemizleAsync()
    {
        using var b = _ortam.Baglam();
        await b.Database.ExecuteSqlRawAsync("DELETE FROM rol_izinleri;");
        await b.Database.ExecuteSqlRawAsync("DELETE FROM izinler;");
        await b.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserRoles\";");
        await b.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetRoles\";");
        await _ortam.TemelVerileriKurAsync();
    }

    private static IzinServisi Servis(AppDbContext b) =>
        new(b, new MemoryCache(new MemoryCacheOptions()));

    /// <summary>Kodda tanımlı rolleri yazar — tohumun dağıtacağı bir şey olsun.</summary>
    private async Task SistemRolleriKurAsync()
    {
        using var b = _ortam.Baglam();
        b.Roles.AddRange(UserRoles.GetRoles().Select(r => new AppRole
        {
            Name = r,
            NormalizedName = r.ToUpperInvariant(),
        }));
        await b.SaveChangesAsync();
    }

    /// <summary>Rol yaratır ve verilen izinleri bağlar.</summary>
    private async Task<long> RolKurAsync(string ad, params string[] izinler)
    {
        using var b = _ortam.Baglam();
        var rol = new AppRole { Name = ad, NormalizedName = ad.ToUpperInvariant() };
        b.Roles.Add(rol);
        await b.SaveChangesAsync();

        b.RolIzinleri.AddRange(izinler.Select(i => new RolIzin { RolId = rol.Id, IzinAd = i }));
        await b.SaveChangesAsync();
        return rol.Id;
    }

    private async Task RoleAtaAsync(long kullaniciId, long rolId)
    {
        using var b = _ortam.Baglam();
        b.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<long>
        {
            UserId = kullaniciId,
            RoleId = rolId,
        });
        await b.SaveChangesAsync();
    }

    // ═════════════════════════════════════════════════════════ katalog

    [Fact]
    public async Task Katalog_KODDAN_tohumlanir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        using var b = _ortam.Baglam();
        await IzinTohumu.UygulaAsync(b);

        using var kontrol = _ortam.Baglam();
        var sayi = await kontrol.Izinler.CountAsync();

        // Yeni bir izin tanımlayıp uygulamayı başlatmak yeterli olmalı; elle
        // SQL yazmak, izin eklemeyi yine bir yayın işine çevirirdi.
        Assert.Equal(Izinler.Katalog.Count, sayi);
    }

    [Fact]
    public async Task Tohum_iki_kez_calisinca_KOPYALAMAZ()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        using (var b = _ortam.Baglam()) await IzinTohumu.UygulaAsync(b);
        using (var b = _ortam.Baglam()) await IzinTohumu.UygulaAsync(b);

        using var kontrol = _ortam.Baglam();
        // Her açılışta çalışıyor; ikinci çalıştırma kataloğu ikiye katlasaydı
        // yönetim ekranı aynı izni tekrar tekrar gösterirdi.
        Assert.Equal(Izinler.Katalog.Count, await kontrol.Izinler.CountAsync());
    }

    [Fact]
    public async Task Koddan_kaldirilan_izin_SILINMEZ_isaretlenir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        using (var b = _ortam.Baglam())
        {
            b.Izinler.Add(new Izin
            {
                Ad = "eski.izin", Grup = "Eski", Baslik = "Eski", Aciklama = "Kaldırıldı",
            });
            await b.SaveChangesAsync();
        }

        using (var b = _ortam.Baglam()) await IzinTohumu.UygulaAsync(b);

        using var kontrol = _ortam.Baglam();
        var eski = await kontrol.Izinler.FirstAsync(i => i.Ad == "eski.izin");

        // Silmek, o izne bağlı rol kayıtlarını da götürür ve yetkinin fark
        // edilmeden değişmesi demektir.
        Assert.False(eski.Kullanimda);
    }

    [Fact]
    public async Task Ilk_dagilim_ROLUN_IZNI_VARSA_dokunmaz()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        await SistemRolleriKurAsync();

        using (var b = _ortam.Baglam()) await IzinTohumu.UygulaAsync(b);

        // Yönetici Sekreter'in iznini kıstı.
        long sekreterId;
        using (var b = _ortam.Baglam())
        {
            sekreterId = await b.Roles.Where(r => r.Name == UserRoles.Sekreter)
                .Select(r => r.Id).FirstAsync();
            var hepsi = await b.RolIzinleri.Where(x => x.RolId == sekreterId).ToListAsync();
            b.RolIzinleri.RemoveRange(hepsi.Skip(1));
            await b.SaveChangesAsync();
        }

        using (var b = _ortam.Baglam()) await IzinTohumu.UygulaAsync(b);

        using var kontrol = _ortam.Baglam();
        var kalan = await kontrol.RolIzinleri.CountAsync(x => x.RolId == sekreterId);

        // Yeniden başlatmak eski hâline döndürseydi yetki yönetimi işe
        // yaramazdı: yönetici kısıyor, sunucu geri açıyor.
        Assert.Equal(1, kalan);
    }

    [Fact]
    public async Task Admin_HATA_KAYITLARINI_goremez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        await SistemRolleriKurAsync();

        using (var b = _ortam.Baglam()) await IzinTohumu.UygulaAsync(b);

        using var kontrol = _ortam.Baglam();
        var adminId = await kontrol.Roles.Where(r => r.Name == UserRoles.Admin)
            .Select(r => r.Id).FirstAsync();

        var varMi = await kontrol.RolIzinleri
            .AnyAsync(x => x.RolId == adminId && x.IzinAd == Izinler.SistemHata);

        // Kayıtlarda istek gövdeleri, IP adresleri ve yığın izleri var. Bu
        // kısıt izin sisteminden ÖNCE de vardı ve öyle kalmalı.
        Assert.False(varMi);
    }

    // ═══════════════════════════════════════════════════════ çözümleme

    [Fact]
    public async Task Kullanicinin_izinleri_ROLLERINDEN_cozulur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using (var b = _ortam.Baglam()) await IzinTohumu.UygulaAsync(b);

        var rolId = await RolKurAsync("TalepPersoneli",
            Izinler.TalepGoruntule, Izinler.TalepAjandayaEkle);
        await RoleAtaAsync(1, rolId);

        using var b2 = _ortam.Baglam();
        var izinler = await Servis(b2).IzinleriAsync(1);

        Assert.Contains(Izinler.TalepGoruntule, izinler);
        Assert.Contains(Izinler.TalepAjandayaEkle, izinler);
        // "Başkan onaylar, personel ekler": ekleme var, onaylama YOK.
        Assert.DoesNotContain(Izinler.TalepDurumDegistir, izinler);
    }

    [Fact]
    public async Task Iki_rol_izinleri_BIRLESIR()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using (var b = _ortam.Baglam()) await IzinTohumu.UygulaAsync(b);

        await RoleAtaAsync(1, await RolKurAsync("A", Izinler.TalepGoruntule));
        await RoleAtaAsync(1, await RolKurAsync("B", Izinler.AjandaSil));

        using var b2 = _ortam.Baglam();
        var izinler = await Servis(b2).IzinleriAsync(1);

        Assert.Contains(Izinler.TalepGoruntule, izinler);
        Assert.Contains(Izinler.AjandaSil, izinler);
    }

    [Fact]
    public async Task KULLANIMDAN_KALKMIS_izin_cozume_girmez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using (var b = _ortam.Baglam()) await IzinTohumu.UygulaAsync(b);

        var rolId = await RolKurAsync("Eski", Izinler.TalepGoruntule);
        await RoleAtaAsync(1, rolId);

        using (var b = _ortam.Baglam())
        {
            var izin = await b.Izinler.FirstAsync(i => i.Ad == Izinler.TalepGoruntule);
            izin.Kullanimda = false;
            await b.SaveChangesAsync();
        }

        using var b2 = _ortam.Baglam();
        var izinler = await Servis(b2).IzinleriAsync(1);

        // Bağ kayıtta duruyor (silmiyoruz) ama artık yetki VERMİYOR; aksi
        // hâlde koddan kaldırılan bir izin sessizce çalışmaya devam ederdi.
        Assert.Empty(izinler);
    }

    [Fact]
    public async Task Rolsuz_kullanicinin_HIC_izni_yok()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using (var b = _ortam.Baglam()) await IzinTohumu.UygulaAsync(b);

        using var b2 = _ortam.Baglam();
        Assert.Empty(await Servis(b2).IzinleriAsync(3));
    }

    // ═══════════════════════════════════════════════════════ önbellek

    [Fact]
    public async Task Rol_izni_degisince_onbellek_DUSER()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        using (var b = _ortam.Baglam()) await IzinTohumu.UygulaAsync(b);

        var rolId = await RolKurAsync("Deneme", Izinler.TalepGoruntule);
        await RoleAtaAsync(1, rolId);

        using var b2 = _ortam.Baglam();
        var servis = Servis(b2);

        Assert.Contains(Izinler.TalepGoruntule, await servis.IzinleriAsync(1));

        using (var b = _ortam.Baglam())
        {
            b.RolIzinleri.RemoveRange(b.RolIzinleri.Where(x => x.RolId == rolId));
            await b.SaveChangesAsync();
        }

        // Önbellek düşmezse yetki 5 dakika daha çalışırdı. Jeton yerine
        // istek başına okumayı seçmemizin sebebi tam da bu gecikme.
        await servis.RolDegistiAsync(rolId);

        Assert.Empty(await servis.IzinleriAsync(1));
    }

    // ══════════════════════════════════════════════════════════ katalog

    [Fact]
    public void Katalogdaki_her_iznin_ACIKLAMASI_var()
    {
        // Rol kuran kişi izin ADINA bakarak karar veremiyor: "ajanda.havale"
        // adı, havalenin gizli etkinlikte çalışmadığını söylemiyor.
        foreach (var k in Izinler.Katalog)
        {
            Assert.False(string.IsNullOrWhiteSpace(k.Aciklama), k.Ad);
            Assert.False(string.IsNullOrWhiteSpace(k.Grup), k.Ad);
            Assert.False(string.IsNullOrWhiteSpace(k.Baslik), k.Ad);
        }
    }

    [Fact]
    public void Katalogda_MUKERRER_ad_yok()
    {
        // Aynı ad iki kez geçerse tohum çakışır ve ikinci kayıt sessizce
        // birincinin metinlerini ezerdi.
        Assert.Equal(Izinler.Katalog.Count, Izinler.Adlar.Distinct().Count());
    }

    [Fact]
    public void Uydurma_izin_adi_gecersiz()
    {
        Assert.True(Izinler.Gecerli(Izinler.AjandaSil));
        Assert.False(Izinler.Gecerli("uydurma.izin"));
    }
}
