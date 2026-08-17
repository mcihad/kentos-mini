using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Services;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Tests;

/// <summary>
/// Mobil + web jetonlarının bir arada çalıştığını ve gizlilik açığı
/// doğurmadığını kanıtlar.
///
/// <para>
/// Buradaki en kritik test <see cref="Jeton_baska_kullanicida_ise_ondan_alinir"/>:
/// FCM web jetonu tarayıcı profiline bağlıdır, kullanıcıya değil. Ortak bir
/// bilgisayarda A çıkış yapmadan B giriş yaparsa jeton iki kullanıcıda birden
/// kalabilir; o durumda A'nın <b>gizli etkinlik</b> bildirimleri B'nin ekranına
/// düşerdi.
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class WebPushJetonTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;

    /// <summary>
    /// `ExecuteUpdate` InMemory sağlayıcıda desteklenmiyor; jeton yönetimi
    /// testleri gerçek Postgres ister. Üretim kodunu test kolaylığı için
    /// yavaşlatmak (satırları belleğe çekmek) yerine test gerçek veritabanına
    /// bakar — atomik tek SQL cümlesi, "geri çalma" yarışında da doğru olan bu.
    /// </summary>
    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    private async Task<(AppUser, AppUser)> GercekIkiKullaniciAsync()
    {
        // Ortamın kendi kullanıcıları kullanılır (id 1 "ekleyen", id 2 "katilimci").
        // Yeni kullanıcı eklemek, tohumun açık kimlik yazması yüzünden kimlik
        // dizisiyle çakışıyor.
        await _ortam.TemelVerileriKurAsync();

        using var db = _ortam.Baglam();
        await db.Users
            .Where(u => u.Id == 1 || u.Id == 2)
            .ExecuteUpdateAsync(su => su
                .SetProperty(u => u.FcmToken, (string?)null)
                .SetProperty(u => u.WebFcmToken, (string?)null));

        var a = (await db.Users.FindAsync(1L))!;
        var b = (await db.Users.FindAsync(2L))!;
        return (a, b);
    }

    private static AppDbContext Baglam() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"push-{Guid.NewGuid()}")
            .Options);

    private static async Task<(AppUser, AppUser)> IkiKullaniciAsync(AppDbContext db)
    {
        var a = new AppUser { Id = 1, UserName = "a", BirimId = 1 };
        var b = new AppUser { Id = 2, UserName = "b", BirimId = 1 };
        db.Users.AddRange(a, b);
        await db.SaveChangesAsync();
        return (a, b);
    }

    // --------------------------------------------------------- jeton yönetimi

    [Fact]
    public async Task Web_jetonu_mobil_jetonun_uzerine_yazmaz()
    {
        PostgresYoksaAtla();
        var (a, _) = await GercekIkiKullaniciAsync();

        using var db = _ortam.Baglam();
        (await db.Users.FindAsync(a.Id))!.FcmToken = "MOBIL-1";
        await db.SaveChangesAsync();

        var servis = new WebBildirimServisi(db, NullLogger<WebBildirimServisi>.Instance);
        await servis.JetonKaydetAsync(a.Id, "WEB-1");

        // TAZE bağlam: servis ExecuteUpdate ile doğrudan veritabanına yazıyor,
        // eski bağlamın izlediği nesne güncellenmez.
        using var taze = _ortam.Baglam();
        var k = await taze.Users.FindAsync(a.Id);
        Assert.Equal("MOBIL-1", k!.FcmToken);
        Assert.Equal("WEB-1", k.WebFcmToken);
    }

    [Fact]
    public async Task Jeton_baska_kullanicida_ise_ondan_alinir()
    {
        PostgresYoksaAtla();
        var (a, b) = await GercekIkiKullaniciAsync();

        using var db = _ortam.Baglam();
        (await db.Users.FindAsync(a.Id))!.WebFcmToken = "PAYLASILAN";
        await db.SaveChangesAsync();

        var servis = new WebBildirimServisi(db, NullLogger<WebBildirimServisi>.Instance);
        await servis.JetonKaydetAsync(b.Id, "PAYLASILAN");

        using var taze = _ortam.Baglam();
        var tazeA = await taze.Users.FindAsync(a.Id);
        var tazeB = await taze.Users.FindAsync(b.Id);

        // Bir jeton en fazla TEK kullanıcıya ait olabilir — son giren kazanır.
        Assert.Null(tazeA!.WebFcmToken);
        Assert.Equal("PAYLASILAN", tazeB!.WebFcmToken);
    }

    [Fact]
    public async Task Jeton_silme_yalnizca_eslesiyorsa_calisir()
    {
        PostgresYoksaAtla();
        var (a, _) = await GercekIkiKullaniciAsync();

        using var db = _ortam.Baglam();
        (await db.Users.FindAsync(a.Id))!.WebFcmToken = "GUNCEL";
        await db.SaveChangesAsync();

        var servis = new WebBildirimServisi(db, NullLogger<WebBildirimServisi>.Instance);

        // Eski bir sekmeden gelen gecikmiş çıkış isteği güncel jetonu silmemeli.
        await servis.JetonSilAsync(a.Id, "ESKI");
        using (var t1 = _ortam.Baglam())
        {
            Assert.Equal("GUNCEL", (await t1.Users.FindAsync(a.Id))!.WebFcmToken);
        }

        await servis.JetonSilAsync(a.Id, "GUNCEL");
        using var t2 = _ortam.Baglam();
        Assert.Null((await t2.Users.FindAsync(a.Id))!.WebFcmToken);
    }

    // ------------------------------------------------------------- fan-out

    private static MessageService MesajServisi(AppDbContext db) =>
        new(db, new HerZamanBildirimAlanKullanici(), NullLogger<IMessageService>.Instance);

    [Fact]
    public async Task Iki_jetonu_olan_kullaniciya_iki_satir_uretilir()
    {
        using var db = Baglam();
        var (a, _) = await IkiKullaniciAsync(db);
        a.FcmToken = "MOBIL";
        a.WebFcmToken = "WEB";
        await db.SaveChangesAsync();

        await MesajServisi(db).CreateAsync(
            a.Id, "yoksayilir", "Başlık", "İçerik",
            SendMessageType.PushNotification, NotifikasyonTip.Always, null);

        var satirlar = await db.Messages.Where(m => m.UserId == a.Id).ToListAsync();
        Assert.Equal(2, satirlar.Count);
        Assert.Contains(satirlar, m => m.Token == "MOBIL");
        Assert.Contains(satirlar, m => m.Token == "WEB");
    }

    [Fact]
    public async Task Tek_jetonu_olan_kullaniciya_tek_satir_uretilir()
    {
        using var db = Baglam();
        var (a, _) = await IkiKullaniciAsync(db);
        a.FcmToken = "SADECE-MOBIL";
        await db.SaveChangesAsync();

        await MesajServisi(db).CreateAsync(
            a.Id, "yoksayilir", "B", "I",
            SendMessageType.PushNotification, NotifikasyonTip.Always, null);

        var satirlar = await db.Messages.Where(m => m.UserId == a.Id).ToListAsync();
        Assert.Single(satirlar);
        Assert.Equal("SADECE-MOBIL", satirlar[0].Token);
    }

    [Fact]
    public async Task Jetonu_olmayan_kullaniciya_satir_uretilmez()
    {
        using var db = Baglam();
        var (a, _) = await IkiKullaniciAsync(db);

        await MesajServisi(db).CreateAsync(
            a.Id, "yoksayilir", "B", "I",
            SendMessageType.PushNotification, NotifikasyonTip.Always, null);

        // ÖNCEDEN: null jetonla bir satır üretilip kuyrukta takılıyordu.
        Assert.Empty(await db.Messages.Where(m => m.UserId == a.Id).ToListAsync());
    }

    [Fact]
    public async Task SMS_yolunda_verilen_numara_aynen_kullanilir()
    {
        using var db = Baglam();
        var (a, _) = await IkiKullaniciAsync(db);
        a.FcmToken = "MOBIL";
        await db.SaveChangesAsync();

        await MesajServisi(db).CreateAsync(
            a.Id, "05551112233", "B", "I",
            SendMessageType.SMS, NotifikasyonTip.Always, null);

        var satirlar = await db.Messages.Where(m => m.UserId == a.Id).ToListAsync();
        Assert.Single(satirlar);
        // SMS'te `token` bir TELEFON NUMARASIDIR, jeton değil.
        Assert.Equal("05551112233", satirlar[0].Token);
    }

    [Fact]
    public async Task Gizli_etkinlik_bildiriminde_her_katilimci_tum_cihazlarindan_alir()
    {
        using var db = Baglam();
        var (a, b) = await IkiKullaniciAsync(db);
        a.FcmToken = "A-MOBIL";
        a.WebFcmToken = "A-WEB";
        b.WebFcmToken = "B-WEB";
        await db.SaveChangesAsync();

        await MesajServisi(db).CreateForUsersAsync(
            [a.Id, b.Id], "🔒 Gizli · Toplantı", "İçerik",
            SendMessageType.PushNotification, NotifikasyonTip.Always, null);

        var hepsi = await db.Messages.ToListAsync();
        Assert.Equal(3, hepsi.Count);
        Assert.Equal(2, hepsi.Count(m => m.UserId == a.Id));
        Assert.Single(hepsi.Where(m => m.UserId == b.Id));
    }

    // ─────────────────────────────────────────────────── mobil jeton (v2)

    [Fact]
    public async Task Mobil_jeton_kaydedilir_ve_web_jetonuna_dokunmaz()
    {
        PostgresYoksaAtla();
        var (a, _) = await GercekIkiKullaniciAsync();

        using var db = _ortam.Baglam();
        (await db.Users.FindAsync(a.Id))!.WebFcmToken = "WEB-1";
        await db.SaveChangesAsync();

        var servis = new WebBildirimServisi(db, NullLogger<WebBildirimServisi>.Instance);
        await servis.MobilJetonKaydetAsync(a.Id, "MOBIL-1");

        using var taze = _ortam.Baglam();
        var k = await taze.Users.FindAsync(a.Id);

        // İki sütun BAĞIMSIZ: kullanıcının hem telefonu hem tarayıcısı bildirim
        // alabilmeli. Tek sütun olsaydı son kaydolan cihaz diğerini susturur.
        Assert.Equal("MOBIL-1", k!.FcmToken);
        Assert.Equal("WEB-1", k.WebFcmToken);
    }

    [Fact]
    public async Task Mobil_jeton_baska_kullanicida_ise_ondan_ALINIR()
    {
        PostgresYoksaAtla();
        var (a, b) = await GercekIkiKullaniciAsync();

        using var db = _ortam.Baglam();
        (await db.Users.FindAsync(a.Id))!.FcmToken = "PAYLASILAN-CIHAZ";
        await db.SaveChangesAsync();

        var servis = new WebBildirimServisi(db, NullLogger<WebBildirimServisi>.Instance);
        await servis.MobilJetonKaydetAsync(b.Id, "PAYLASILAN-CIHAZ");

        using var taze = _ortam.Baglam();

        // FCM jetonu CİHAZA bağlıdır, kullanıcıya değil. Ortak bir telefonda A
        // çıkış yapmadan B giriş yaparsa bu adım olmadan jeton iki kullanıcıda
        // birden kalır ve A'nın GİZLİ etkinlik bildirimleri B'nin telefonuna
        // düşer. v1'in ucu bu adımı atlıyor.
        Assert.Null((await taze.Users.FindAsync(a.Id))!.FcmToken);
        Assert.Equal("PAYLASILAN-CIHAZ", (await taze.Users.FindAsync(b.Id))!.FcmToken);
    }

    [Fact]
    public async Task Mobil_jeton_silme_ESLESME_kontrollu()
    {
        PostgresYoksaAtla();
        var (a, _) = await GercekIkiKullaniciAsync();

        using var db = _ortam.Baglam();
        (await db.Users.FindAsync(a.Id))!.FcmToken = "GUNCEL";
        await db.SaveChangesAsync();

        var servis = new WebBildirimServisi(db, NullLogger<WebBildirimServisi>.Instance);

        // Eski bir oturumdan gelen gecikmiş çıkış isteği, yeni kaydedilmiş
        // geçerli jetonu SİLMEMELİ — yoksa kullanıcı sebepsiz bildirim almaz.
        await servis.MobilJetonSilAsync(a.Id, "ESKI");

        using var taze = _ortam.Baglam();
        Assert.Equal("GUNCEL", (await taze.Users.FindAsync(a.Id))!.FcmToken);

        await servis.MobilJetonSilAsync(a.Id, "GUNCEL");

        using var sonrasi = _ortam.Baglam();
        Assert.Null((await sonrasi.Users.FindAsync(a.Id))!.FcmToken);
    }

    /// <summary>Bildirim tercihi kontrolünü devre dışı bırakan yerine geçen.</summary>
    private sealed class HerZamanBildirimAlanKullanici : IUserService
    {
        public Task<bool> HasReceiveNotification(long userId, NotifikasyonTip tip) =>
            Task.FromResult(true);

        public Task<Application.Dto.UserDto> Get() => throw new NotSupportedException();
        public Task<Application.Dto.UserSettingDto> GetSetting() => throw new NotSupportedException();
        public Task<Application.Dto.UserSettingDto> UpdateSetting(Application.Dto.UserSettingDto s) => throw new NotSupportedException();
        public Task<Application.Dto.LoginResponseDto> LoginAsync(Application.Dto.LoginDto d) => throw new NotSupportedException();
        public Task<Application.Dto.PasswordChangeResponseDto> PasswordChange(Application.Dto.PasswordChangeDto d) => throw new NotSupportedException();
        public void LogoutAsync() => throw new NotSupportedException();
    }
}
