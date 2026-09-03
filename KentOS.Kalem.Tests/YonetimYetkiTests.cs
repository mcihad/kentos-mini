using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Kalem.Application.Dto.V2.Yonetim;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Exceptions;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Tests;

/// <summary>
/// Yönetim uçlarının rol yükseltme (privilege escalation) kapısını kapattığını
/// kanıtlar.
///
/// <para>
/// v1'de <c>Sistem</c> / <c>BaskanOzel</c> kısıtı yalnızca görünümdeydi:
/// açılır listeye bu roller basılmıyordu ama <c>POST</c> gelen listeyi
/// denetlemiyordu. Elle hazırlanmış tek bir istekle herhangi bir Admin
/// kendini <c>Sistem</c> yapabilirdi. Buradaki testler kuralın <b>sunucuda</b>
/// olduğunu kilitler.
/// </para>
///
/// <para>
/// Gerçek Postgres gerekir: <c>ExecuteUpdateAsync</c> ve Identity mağazaları
/// InMemory sağlayıcıda ya çalışmıyor ya da farklı davranıyor.
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class YonetimYetkiTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>, IDisposable
{
    private readonly SunucuTestOrtami _ortam = ortam;
    private ServiceProvider? _saglayici;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    /// <summary>Ortamın veritabanı üzerinde Identity'li bir servis kabı kurar.</summary>
    private YonetimServisi Servis()
    {
        var hizmetler = new ServiceCollection();
        hizmetler.AddLogging();
        // Parola sıfırlama jetonu üreteci veri koruma sağlayıcısına dayanır;
        // uygulamada web ana bilgisayarı kuruyor, testte açıkça eklenmeli.
        hizmetler.AddDataProtection();

        // Ortamın hazır ayarlarını kullan: aynı veritabanı, aynı snake_case
        // sözleşmesi. Bağlantı metnini burada yeniden yazmak, ortam değişkeniyle
        // yönlendirme yapıldığında iki testin farklı veritabanlarına gitmesine
        // yol açardı.
        hizmetler.AddScoped(_ => new AppDbContext(_ortam.Ayarlar));

        hizmetler.AddIdentityCore<AppUser>(o =>
        {
            o.Password.RequireDigit = false;
            o.Password.RequireUppercase = false;
            o.Password.RequireNonAlphanumeric = false;
            o.Password.RequiredLength = 4;
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        _saglayici = hizmetler.BuildServiceProvider();

        return new YonetimServisi(
            _saglayici.GetRequiredService<AppDbContext>(),
            _saglayici.GetRequiredService<UserManager<AppUser>>(),
            _saglayici.GetRequiredService<RoleManager<AppRole>>(),
            new SahteMesajServisi(),
            NullLogger<YonetimServisi>.Instance);
    }

    private async Task RolVarEtAsync(params string[] roller)
    {
        var yonetici = _saglayici!.GetRequiredService<RoleManager<AppRole>>();
        foreach (var rol in roller)
        {
            if (!await yonetici.RoleExistsAsync(rol))
            {
                await yonetici.CreateAsync(new AppRole { Name = rol });
            }
        }
    }

    private static string Ad(string onek) => onek + Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task Sistem_yetkisi_olmayan_Sistem_rolu_ATAYAMAZ()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();
        var servis = Servis();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => servis.KullaniciOlusturAsync(new KullaniciOlusturIstegi
            {
                KullaniciAdi = Ad("yukseltme"),
                Parola = "Gecici123.",
                Roller = [UserRoles.Sistem],
                SmsGonder = false,
            }, sistemYetkisi: false));
    }

    [Fact]
    public async Task Sistem_yetkisi_olmayan_BaskanOzel_rolu_ATAYAMAZ()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();
        var servis = Servis();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => servis.KullaniciOlusturAsync(new KullaniciOlusturIstegi
            {
                KullaniciAdi = Ad("ozel"),
                Parola = "Gecici123.",
                Roller = [UserRoles.BaskanOzel],
                SmsGonder = false,
            }, sistemYetkisi: false));
    }

    [Fact]
    public async Task Sistem_yetkisi_olan_atayabilir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();
        var servis = Servis();
        await RolVarEtAsync(UserRoles.Sistem);

        var sonuc = await servis.KullaniciOlusturAsync(new KullaniciOlusturIstegi
        {
            KullaniciAdi = Ad("sis"),
            Parola = "Gecici123.",
            Roller = [UserRoles.Sistem],
            SmsGonder = false,
        }, sistemYetkisi: true);

        Assert.Contains(UserRoles.Sistem, sonuc.Roller);
    }

    [Fact]
    public async Task Sistem_yetkisi_olmayan_korumali_rolu_KALDIRAMAZ()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();
        var servis = Servis();
        await RolVarEtAsync(UserRoles.Sistem, UserRoles.Kullanici);

        var ad = Ad("kur");
        var kullanici = await servis.KullaniciOlusturAsync(new KullaniciOlusturIstegi
        {
            KullaniciAdi = ad,
            Parola = "Gecici123.",
            Roller = [UserRoles.Sistem],
            SmsGonder = false,
        }, sistemYetkisi: true);

        // Yalnızca eklemeyi denetlemek yetmez: korumalı rolü SÖKMEK de yetki
        // ister. Aksi hâlde bir Admin, sistem yöneticisini kilitleyip dışarıda
        // bırakabilirdi.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => servis.KullaniciGuncelleAsync(kullanici.Id, new KullaniciGuncelleIstegi
            {
                KullaniciAdi = ad,
                Roller = [UserRoles.Kullanici],
            }, sistemYetkisi: false));
    }

    [Fact]
    public async Task Rol_degisikligi_yalnizca_FARKI_uygular()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();
        var servis = Servis();
        await RolVarEtAsync(UserRoles.Kullanici, UserRoles.Sekreter, UserRoles.Yonetici);

        var ad = Ad("fark");
        var kullanici = await servis.KullaniciOlusturAsync(new KullaniciOlusturIstegi
        {
            KullaniciAdi = ad,
            Parola = "Gecici123.",
            Roller = [UserRoles.Kullanici, UserRoles.Sekreter],
            SmsGonder = false,
        }, sistemYetkisi: false);

        var guncel = await servis.KullaniciGuncelleAsync(kullanici.Id, new KullaniciGuncelleIstegi
        {
            KullaniciAdi = ad,
            Roller = [UserRoles.Kullanici, UserRoles.Yonetici],
        }, sistemYetkisi: false);

        Assert.Contains(UserRoles.Kullanici, guncel.Roller);
        Assert.Contains(UserRoles.Yonetici, guncel.Roller);
        Assert.DoesNotContain(UserRoles.Sekreter, guncel.Roller);
    }

    [Fact]
    public async Task Ozet_jetonlari_DISARI_VERMEZ()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        using (var db = _ortam.Baglam())
        {
            await db.Users.Where(u => u.Id == 1).ExecuteUpdateAsync(s => s
                .SetProperty(u => u.FcmToken, "GIZLI-MOBIL-JETON")
                .SetProperty(u => u.WebFcmToken, "GIZLI-WEB-JETON"));
        }

        var ozet = await Servis().KullaniciAsync(1);

        // Jeton, sahibinin cihazına bildirim göndermeye yeter. Yönetim
        // listesinde görünmesi için hiçbir sebep yok — v1 formu basıyordu.
        var json = System.Text.Json.JsonSerializer.Serialize(ozet);
        Assert.DoesNotContain("GIZLI-MOBIL-JETON", json);
        Assert.DoesNotContain("GIZLI-WEB-JETON", json);

        // Ama "cihaz bağlı mı" bilgisi görünür.
        Assert.True(ozet.MobilBagli);
        Assert.True(ozet.WebBagli);
    }

    [Fact]
    public async Task Birim_kendi_altina_TASINAMAZ()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();
        var servis = Servis();

        var ust = await servis.BirimOlusturAsync(new BirimIstegi { Ad = Ad("Üst"), Yetkili = "A" });
        var alt = await servis.BirimOlusturAsync(new BirimIstegi
        {
            Ad = Ad("Alt"),
            Yetkili = "B",
            UstBirimId = ust.Id,
        });

        // Döngü kurulursa ağacı gezen her kod sonsuz döngüye girer.
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => servis.BirimGuncelleAsync(ust.Id, new BirimIstegi
            {
                Ad = ust.Ad,
                Yetkili = "A",
                UstBirimId = alt.Id,
            }));
    }

    [Fact]
    public async Task Kullanicisi_olan_birim_SILINEMEZ()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        // Ortamın kullanıcıları 1 numaralı birime bağlı.
        await Assert.ThrowsAsync<BusinessRuleException>(() => Servis().BirimSilAsync(1));
    }

    // ═════════════════════════════════════════════ birim detayı ve rol üyeliği

    [Fact]
    public async Task Birim_detayi_kullanicilari_ve_sayaclari_dondurur()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var detay = await Servis().BirimDetayAsync(1);

        Assert.Equal(1, detay.Id);
        // Ortam 1/2/3 numaralı kullanıcıları 1. birime bağlıyor.
        Assert.True(detay.KullaniciSayisi >= 3);
        Assert.Equal(detay.KullaniciSayisi, detay.Kullanicilar.Count);
        Assert.All(detay.Kullanicilar, k => Assert.False(string.IsNullOrWhiteSpace(k.KullaniciAdi)));
    }

    [Fact]
    public async Task Birim_detayi_alt_birim_sayisini_bildirir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();
        var servis = Servis();

        var ust = await servis.BirimOlusturAsync(new BirimIstegi { Ad = Ad("Üst"), Yetkili = "A" });
        await servis.BirimOlusturAsync(new BirimIstegi { Ad = Ad("Alt"), Yetkili = "B", UstBirimId = ust.Id });

        var detay = await servis.BirimDetayAsync(ust.Id);

        Assert.Equal(1, detay.AltBirimSayisi);
        Assert.Equal(0, detay.KullaniciSayisi);
    }

    [Fact]
    public async Task Olmayan_birimin_detayi_bulunamadi_atar()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        await Assert.ThrowsAsync<EntityNotFoundException>(() => Servis().BirimDetayAsync(999_999));
    }

    [Fact]
    public async Task Rol_kullanicilari_yalnizca_o_roldekileri_dondurur()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();
        var servis = Servis();
        await RolVarEtAsync(UserRoles.Kullanici, UserRoles.Sekreter);

        var sekreterAdi = Ad("sek");
        await servis.KullaniciOlusturAsync(new KullaniciOlusturIstegi
        {
            KullaniciAdi = sekreterAdi,
            Parola = "Gecici123.",
            Roller = [UserRoles.Sekreter],
            SmsGonder = false,
        }, sistemYetkisi: false);

        var digerAdi = Ad("diger");
        await servis.KullaniciOlusturAsync(new KullaniciOlusturIstegi
        {
            KullaniciAdi = digerAdi,
            Parola = "Gecici123.",
            Roller = [UserRoles.Kullanici],
            SmsGonder = false,
        }, sistemYetkisi: false);

        var uyeler = await servis.RolKullanicilariAsync(UserRoles.Sekreter);

        Assert.Contains(uyeler, u => u.KullaniciAdi == sekreterAdi);
        Assert.DoesNotContain(uyeler, u => u.KullaniciAdi == digerAdi);
    }

    [Fact]
    public async Task Role_kullanici_eklenip_cikarilabilir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();
        var servis = Servis();
        await RolVarEtAsync(UserRoles.Kullanici, UserRoles.Sekreter);

        var kullanici = await servis.KullaniciOlusturAsync(new KullaniciOlusturIstegi
        {
            KullaniciAdi = Ad("uye"),
            Parola = "Gecici123.",
            Roller = [UserRoles.Kullanici],
            SmsGonder = false,
        }, sistemYetkisi: false);

        // İsteyen: 1 numaralı kullanıcı (ortamın yöneticisi).
        await servis.RoleKullaniciEkleAsync(UserRoles.Sekreter, kullanici.Id, isteyenId: 1);
        Assert.Contains(
            await servis.RolKullanicilariAsync(UserRoles.Sekreter),
            u => u.Id == kullanici.Id);

        await servis.RoldenKullaniciCikarAsync(UserRoles.Sekreter, kullanici.Id, isteyenId: 1);
        Assert.DoesNotContain(
            await servis.RolKullanicilariAsync(UserRoles.Sekreter),
            u => u.Id == kullanici.Id);
    }

    [Fact]
    public async Task Kullanicinin_SON_rolu_cikarilamaz()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();
        var servis = Servis();
        await RolVarEtAsync(UserRoles.Kullanici);

        var kullanici = await servis.KullaniciOlusturAsync(new KullaniciOlusturIstegi
        {
            KullaniciAdi = Ad("tek"),
            Parola = "Gecici123.",
            Roller = [UserRoles.Kullanici],
            SmsGonder = false,
        }, sistemYetkisi: false);

        // Rolsüz kullanıcı giriş yapıyor ama hiçbir politikadan geçemiyor:
        // boş bir kabuk görüyor ve "sistem bozuldu" diye geri dönüyor.
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => servis.RoldenKullaniciCikarAsync(UserRoles.Kullanici, kullanici.Id, isteyenId: 1));
    }

    [Fact]
    public async Task Korumali_role_Sistem_yetkisi_olmayan_kullanici_EKLEYEMEZ()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();
        var servis = Servis();
        await RolVarEtAsync(UserRoles.Kullanici, UserRoles.Sistem);

        var sistemsiz = await servis.KullaniciOlusturAsync(new KullaniciOlusturIstegi
        {
            KullaniciAdi = Ad("sistemsiz"),
            Parola = "Gecici123.",
            Roller = [UserRoles.Kullanici],
            SmsGonder = false,
        }, sistemYetkisi: false);

        var hedef = await servis.KullaniciOlusturAsync(new KullaniciOlusturIstegi
        {
            KullaniciAdi = Ad("hedef"),
            Parola = "Gecici123.",
            Roller = [UserRoles.Kullanici],
            SmsGonder = false,
        }, sistemYetkisi: false);

        // Rol detay ekranı düğmeyi gizliyor; kural yine de SUNUCUDA duruyor.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => servis.RoleKullaniciEkleAsync(UserRoles.Sistem, hedef.Id, isteyenId: sistemsiz.Id));
    }

    [Fact]
    public async Task Olmayan_rolun_kullanicilari_bulunamadi_atar()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => Servis().RolKullanicilariAsync("BoyleBirRolYok"));
    }

    public void Dispose()
    {
        _saglayici?.Dispose();
        GC.SuppressFinalize(this);
    }
}
