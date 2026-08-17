using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Services;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Web.Services.V2;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KentOS.Mini.Tests;

/// <summary>
/// Giriş akışının davranışını kilitler.
///
/// <para>
/// NEDEN: Akış <c>AccountApiController.Login</c> içinden <see cref="OturumServisi"/>'ne
/// TAŞINDI ve artık hem v1 (mobil) hem v2 (web) bunu çağırıyor. Taşımanın
/// davranışı değiştirmediğini kanıtlamak zorundayız — burası iki yıldır sahadaki
/// uygulamanın giriş yolu.
/// </para>
///
/// <para>
/// Özellikle korunan iki karar: (1) "kullanıcı yok" ile "şifre yanlış" AYNI
/// mesajı döndürür (kullanıcı adı tespitini engeller), (2) hesap kilitliyse
/// parola hiç kontrol edilmez.
/// </para>
/// </summary>
public class OturumServisiTests : IDisposable
{
    private readonly ServiceProvider _saglayici;
    private readonly UserManager<AppUser> _userManager;
    private readonly IOturumServisi _servis;

    public OturumServisiTests()
    {
        var hizmetler = new ServiceCollection();

        hizmetler.AddLogging();
        hizmetler.AddDbContext<AppDbContext>(o =>
            o.UseInMemoryDatabase($"oturum-{Guid.NewGuid()}"));

        hizmetler.AddIdentity<AppUser, AppRole>(o =>
        {
            o.Lockout.AllowedForNewUsers = true;
            o.Lockout.MaxFailedAccessAttempts = 10;
            o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            o.Password.RequireDigit = false;
            o.Password.RequireUppercase = false;
            o.Password.RequireNonAlphanumeric = false;
            o.Password.RequiredLength = 4;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        hizmetler.AddSingleton<IJwtService>(new SahteJwtServisi());
        // Denetim kaydı yazan sahte servis: giriş akışının kaydı yazması
        // ZORUNLU ama testin konusu o değil.
        hizmetler.AddSingleton<IOturumKaydiServisi, SahteOturumKaydi>();
        hizmetler.AddScoped<IBirimService, SahteBirimServisi>();
        hizmetler.AddScoped<IOturumServisi, OturumServisi>();

        _saglayici = hizmetler.BuildServiceProvider();
        _userManager = _saglayici.GetRequiredService<UserManager<AppUser>>();
        _servis = _saglayici.GetRequiredService<IOturumServisi>();
    }

    private async Task<AppUser> KullaniciKurAsync(string kadi = "test", string parola = "Parola1.")
    {
        var k = new AppUser { UserName = kadi, Email = $"{kadi}@test.local", BirimId = 1 };
        var sonuc = await _userManager.CreateAsync(k, parola);
        Assert.True(sonuc.Succeeded, string.Join(", ", sonuc.Errors.Select(e => e.Description)));
        return k;
    }

    [Fact]
    public async Task Dogru_parola_ile_jeton_uretir()
    {
        await KullaniciKurAsync();

        var sonuc = await _servis.GirisYapAsync("test", "Parola1.");

        Assert.Equal(GirisSonucTuru.Basarili, sonuc.Tur);
        Assert.False(string.IsNullOrWhiteSpace(sonuc.Jeton));
        Assert.NotNull(sonuc.GecerlilikSonu);
    }

    [Fact]
    public async Task Olmayan_kullanici_ve_yanlis_parola_AYNI_mesaji_dondurur()
    {
        await KullaniciKurAsync();

        var olmayan = await _servis.GirisYapAsync("boyle-biri-yok", "herhangi");
        var yanlisParola = await _servis.GirisYapAsync("test", "yanlis-parola");

        // Bu eşitlik bir güvenlik kararıdır: farklı mesaj vermek, saldırganın
        // geçerli kullanıcı adlarını tespit etmesine (enumeration) yarar.
        Assert.Equal(GirisSonucTuru.KimlikHatali, olmayan.Tur);
        Assert.Equal(GirisSonucTuru.KimlikHatali, yanlisParola.Tur);
        Assert.Equal(olmayan.Mesaj, yanlisParola.Mesaj);
        Assert.Equal("Kullanıcı adı veya şifre hatalı", olmayan.Mesaj);
    }

    [Fact]
    public async Task Yanlis_parola_hatali_deneme_sayacini_artirir()
    {
        var k = await KullaniciKurAsync();

        await _servis.GirisYapAsync("test", "yanlis");
        await _servis.GirisYapAsync("test", "yanlis");

        var taze = await _userManager.FindByNameAsync("test");
        Assert.Equal(2, await _userManager.GetAccessFailedCountAsync(taze!));
        Assert.Equal(k.Id, taze!.Id);
    }

    [Fact]
    public async Task Basarili_giris_hatali_deneme_sayacini_sifirlar()
    {
        await KullaniciKurAsync();

        await _servis.GirisYapAsync("test", "yanlis");
        await _servis.GirisYapAsync("test", "yanlis");
        await _servis.GirisYapAsync("test", "Parola1.");

        var taze = await _userManager.FindByNameAsync("test");
        Assert.Equal(0, await _userManager.GetAccessFailedCountAsync(taze!));
    }

    [Fact]
    public async Task Kilitli_hesapta_parola_dogru_olsa_bile_giris_reddedilir()
    {
        var k = await KullaniciKurAsync();
        await _userManager.SetLockoutEndDateAsync(k, DateTimeOffset.UtcNow.AddMinutes(5));

        var sonuc = await _servis.GirisYapAsync("test", "Parola1.");

        Assert.Equal(GirisSonucTuru.Kilitli, sonuc.Tur);
        Assert.Null(sonuc.Jeton);
        Assert.Contains("kilitlendi", sonuc.Mesaj);
    }

    [Fact]
    public async Task Jeton_beklenen_claimleri_tasir()
    {
        var k = await KullaniciKurAsync();

        // Rol önce var olmalı; Identity olmayan role atamayı reddediyor.
        var roleManager = _saglayici.GetRequiredService<RoleManager<AppRole>>();
        await roleManager.CreateAsync(new AppRole { Name = "Sekreter" });
        await _userManager.AddToRoleAsync(k, "Sekreter");

        var sonuc = await _servis.GirisYapAsync("test", "Parola1.");
        var jeton = new JwtSecurityTokenHandler().ReadJwtToken(sonuc.Jeton);

        // Mobil ve web istemcileri bu claim'lere göre davranıyor.
        Assert.Contains(jeton.Claims, c => c.Type == ClaimTypes.Name && c.Value == "test");
        Assert.Contains(jeton.Claims, c => c.Type == "UserId");
        Assert.Contains(jeton.Claims, c => c.Type == "BirimId");
        Assert.Contains(jeton.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Sekreter");
        Assert.Contains(jeton.Claims, c => c.Type == "jti");
    }

    /// <summary>
    /// Birimi olmayan kullanıcı da giriş yapabilmeli.
    /// </summary>
    /// <remarks>
    /// Yönetim formunda birim "Seçilmedi" bırakılabiliyor ve akış eskiden
    /// birim kaydını arayıp bulamayınca <c>EntityNotFoundException</c>
    /// fırlatıyordu: parolası doğru olan kullanıcı "Kayıt bulunamadı" ile
    /// dışarıda kalıyordu. Birimi SİLİNMİŞ kullanıcı da aynı duruma düşerdi.
    /// </remarks>
    [Fact]
    public async Task Birimsiz_kullanici_giris_yapabilir()
    {
        var k = new AppUser { UserName = "birimsiz", Email = "birimsiz@test.local", BirimId = null };
        await _userManager.CreateAsync(k, "Parola1.");

        var sonuc = await _servis.GirisYapAsync("birimsiz", "Parola1.");

        Assert.Equal(GirisSonucTuru.Basarili, sonuc.Tur);

        var jeton = new JwtSecurityTokenHandler().ReadJwtToken(sonuc.Jeton);
        Assert.Contains(jeton.Claims, c => c.Type == "BirimId" && c.Value == "0");
    }

    public void Dispose()
    {
        _saglayici.Dispose();
        GC.SuppressFinalize(this);
    }

    // --- test yerine geçenler -------------------------------------------------

    private sealed class SahteJwtServisi : IJwtService
    {
        private static readonly SymmetricSecurityKey Anahtar =
            new(Encoding.UTF8.GetBytes("test-icin-en-az-otuz-iki-karakterlik-anahtar"));

        public JwtSecurityToken GenerateToken(List<Claim> claims) => new(
            issuer: "test",
            audience: "test",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: new SigningCredentials(Anahtar, SecurityAlgorithms.HmacSha256));

        public bool IsTokenExpired(string token) => false;
        public bool IsTokenValid(string token) => true;
        public List<string> GetRoles(string token) => [];
    }

    private sealed class SahteBirimServisi : IBirimService
    {
        private static Birim Ornek(long id) =>
            new() { Id = id == 0 ? 1 : id, Ad = "Test Birimi", Yetkili = "Y", Unvan = "U" };

        public Task<long> GetCurrentBirimIdAsync() => Task.FromResult(1L);
        public Task<Birim> GetCurrentAsync() => Task.FromResult(Ornek(1));
        public Task<Birim> GetAsync(long id) => Task.FromResult(Ornek(id));
        public Task<IEnumerable<BirimDto>> GetAltBirimlerAsync() =>
            Task.FromResult(Enumerable.Empty<BirimDto>());
        public Task<IEnumerable<BirimDto>> GetBirimlerAsync() =>
            Task.FromResult(Enumerable.Empty<BirimDto>());
        public Task<BirimDto> GetUstBirimAsync() => Task.FromResult(new BirimDto());
    }

    /// <summary>
    /// Denetim kaydını BELLEKTE tutan yerine geçen.
    /// </summary>
    /// <remarks>
    /// Kayıt yazmak giriş akışını asla engellememeli; bu sahte hiçbir zaman
    /// hata fırlatmaz. Kayıtların gerçekten yazıldığı ayrı bir testte
    /// doğrulanıyor.
    /// </remarks>
    private sealed class SahteOturumKaydi : IOturumKaydiServisi
    {
        public List<(string KullaniciAdi, OturumOlayi Olay, bool Basarili)> Kayitlar { get; } = [];

        public Task KaydetAsync(long? kullaniciId, string kullaniciAdi, OturumOlayi olay,
            bool basarili, string? aciklama = null)
        {
            Kayitlar.Add((kullaniciAdi, olay, basarili));
            return Task.CompletedTask;
        }

        public Task<Application.Dto.V2.Ortak.SayfaliSonuc<OturumKaydiDto>> ListeAsync(
            OturumKaydiSuzgeci suzgec) => throw new NotSupportedException();
    }
}
