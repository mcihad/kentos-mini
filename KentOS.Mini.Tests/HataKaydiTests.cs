using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Services.V2;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// Sunucu hata kayıtları.
///
/// <para>
/// Sistem iki yıldır canlıda ve hatalar yalnızca konsol günlüğüne düşüyordu:
/// sunucu yeniden başlayınca kayboluyor, kullanıcı "hata aldım" dediğinde
/// geriye bakacak hiçbir şey kalmıyordu.
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class HataKaydiTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>, IDisposable
{
    private readonly SunucuTestOrtami _ortam = ortam;
    private ServiceProvider? _saglayici;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    private HataKaydiServisi Servis()
    {
        var hizmetler = new ServiceCollection();
        hizmetler.AddLogging();
        hizmetler.AddScoped(_ => new AppDbContext(_ortam.Ayarlar));
        _saglayici = hizmetler.BuildServiceProvider();

        return new HataKaydiServisi(
            _saglayici.GetRequiredService<AppDbContext>(),
            _saglayici.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<HataKaydiServisi>.Instance);
    }

    private async Task TemizleAsync()
    {
        using var db = _ortam.Baglam();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE sistem_hatalari RESTART IDENTITY CASCADE;");
    }

    /// <summary>Gerçekten fırlatılmış bir istisna — yığın izi dolu olsun.</summary>
    private static Exception Firlat(string mesaj)
    {
        try
        {
            throw new InvalidOperationException(mesaj);
        }
        catch (Exception e)
        {
            return e;
        }
    }

    /// <summary>
    /// BAŞKA bir satırdan fırlatılmış istisna.
    /// </summary>
    /// <remarks>
    /// Parmakizi mesajı BİLEREK dışlıyor: mesajda değişken değerler oluyor
    /// ("42 kimlikli kayıt bulunamadı") ve her biri ayrı satır açardı. İki
    /// farklı hatayı ayırmak için gerçekten farklı bir fırlatma yeri gerekiyor.
    /// </remarks>
    private static Exception BaskaYerdenFirlat(string mesaj)
    {
        try
        {
            throw new ArgumentException(mesaj);
        }
        catch (Exception e)
        {
            return e;
        }
    }

    private static DefaultHttpContext Baglam(
        string yol = "/api/v2/etkinlik",
        string yontem = "POST",
        string? govde = null,
        string? jeton = "Bearer GIZLI-JETON")
    {
        var b = new DefaultHttpContext();
        b.Request.Path = yol;
        b.Request.Method = yontem;
        b.Request.Headers.UserAgent = "curl/8.7.1";
        if (jeton is not null) b.Request.Headers.Authorization = jeton;
        b.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.7");

        if (govde is not null)
        {
            var bayt = System.Text.Encoding.UTF8.GetBytes(govde);
            b.Request.Body = new MemoryStream(bayt);
            b.Request.ContentType = "application/json";
        }

        return b;
    }

    [Fact]
    public async Task Hata_kaydedilir_ve_kunye_dolar()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        await Servis().KaydetAsync(
            Firlat("Bir şeyler ters gitti"), Baglam(), "IZ-1", 500);

        using var db = _ortam.Baglam();
        var h = await db.SistemHatalari.SingleAsync();

        Assert.Equal("Bir şeyler ters gitti", h.Mesaj);
        Assert.Contains("InvalidOperationException", h.Tur);
        Assert.Equal("/api/v2/etkinlik", h.Yol);
        Assert.Equal("POST", h.Yontem);
        Assert.Equal(500, h.DurumKodu);
        Assert.Equal("10.0.0.7", h.IpAdresi);
        Assert.Equal("IZ-1", h.IzKimligi);
        Assert.Equal(1, h.Adet);
        Assert.False(string.IsNullOrWhiteSpace(h.YiginIzi));
    }

    [Fact]
    public async Task Ayni_hata_YENI_SATIR_ACMAZ_sayac_artar()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var servis = Servis();
        await servis.KaydetAsync(Firlat("Tekrarlayan"), Baglam(), "IZ-1", 500);
        await servis.KaydetAsync(Firlat("Tekrarlayan"), Baglam(), "IZ-2", 500);
        await servis.KaydetAsync(Firlat("Tekrarlayan"), Baglam(), "IZ-3", 500);

        using var db = _ortam.Baglam();
        var h = await db.SistemHatalari.SingleAsync();

        // Döngüye giren tek bir hata, listeyi binlerce satırla doldurup
        // diğerlerini görünmez kılardı.
        Assert.Equal(3, h.Adet);
        Assert.Equal("IZ-3", h.IzKimligi);
    }

    [Fact]
    public async Task Yeniden_gorulen_hata_COZULDU_isaretini_kaldirir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var servis = Servis();
        await servis.KaydetAsync(Firlat("Geri gelen"), Baglam(), "IZ-1", 500);

        using (var db = _ortam.Baglam())
        {
            var h = await db.SistemHatalari.SingleAsync();
            h.Cozuldu = true;
            h.CozulmeTarihi = DateTime.Now;
            await db.SaveChangesAsync();
        }

        await servis.KaydetAsync(Firlat("Geri gelen"), Baglam(), "IZ-2", 500);

        using var kontrol = _ortam.Baglam();
        var son = await kontrol.SistemHatalari.SingleAsync();

        // "Çözüldü" diye işaretlenmiş bir hata yeniden oluşuyorsa işaret
        // yanlıştır; listede çözülmüş görünmesi sorunu gizlerdi.
        Assert.False(son.Cozuldu);
        Assert.Null(son.CozulmeTarihi);
    }

    [Fact]
    public async Task Yetkilendirme_basligi_ve_parola_MASKELENIR()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        await Servis().KaydetAsync(
            Firlat("Gizli veri"),
            Baglam(govde: """{"kullaniciAdi":"admin","parola":"Admin123.","jeton":"abc"}"""),
            "IZ-1", 500);

        using var db = _ortam.Baglam();
        var h = await db.SistemHatalari.SingleAsync();

        // Kayıt ekranı Sistem rolüne açık ama parola hiçbir gözün önüne
        // gelmemeli; kayıt yedeklere de giriyor.
        Assert.DoesNotContain("Admin123.", h.Govde);
        Assert.DoesNotContain("GIZLI-JETON", h.Basliklar);

        // Alanın VARLIĞI teşhiste işe yarıyor, değeri değil.
        Assert.Contains("admin", h.Govde);
        Assert.Contains("maskelendi", h.Basliklar);
    }

    [Fact]
    public async Task Cok_parcali_istek_govdesi_SAKLANMAZ()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var baglam = Baglam(govde: "----sinir----ikili-veri");
        baglam.Request.ContentType = "multipart/form-data; boundary=----sinir----";

        await Servis().KaydetAsync(Firlat("Yükleme hatası"), baglam, "IZ-1", 500);

        using var db = _ortam.Baglam();
        var h = await db.SistemHatalari.SingleAsync();

        // Dosya yükleme gövdeleri ikili ve megabaytlarca; saklamak
        // veritabanını şişirir ve teşhise bir şey katmaz.
        Assert.Contains("çok parçalı", h.Govde);
        Assert.DoesNotContain("ikili-veri", h.Govde);
    }

    [Fact]
    public async Task AI_raporu_teshis_icin_gereken_her_seyi_tasir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var servis = Servis();
        await servis.KaydetAsync(
            Firlat("Kayıt bulunamadı"),
            Baglam(govde: """{"id":42}"""),
            "IZ-9", 500);

        long id;
        using (var db = _ortam.Baglam())
        {
            id = (await db.SistemHatalari.SingleAsync()).Id;
        }

        var rapor = (await servis.DetayAsync(id)).AiRaporu;

        // Rapor doğrudan bir ajana yapıştırılacak; ham döküm değil, bağlamlı
        // bir istek olmalı.
        Assert.Contains("Sunucu hatası", rapor);
        Assert.Contains("Kayıt bulunamadı", rapor);
        Assert.Contains("POST /api/v2/etkinlik", rapor);
        Assert.Contains("\"id\":42", rapor);
        Assert.Contains("Yığın izi", rapor);
        Assert.Contains("Senden beklenen", rapor);

        // Teknoloji bağlamı olmadan ajan yanlış katmanda arar.
        Assert.Contains("ASP.NET Core", rapor);
        Assert.Contains("PostgreSQL", rapor);
    }

    [Fact]
    public async Task Not_ve_cozum_durumu_kaydedilir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var servis = Servis();
        await servis.KaydetAsync(Firlat("Not denemesi"), Baglam(), "IZ-1", 500);

        long id;
        using (var db = _ortam.Baglam())
        {
            id = (await db.SistemHatalari.SingleAsync()).Id;
        }

        var sonuc = await servis.NotKaydetAsync(id, "Kök neden: eksik FK.", true, "sistem");

        Assert.True(sonuc.Cozuldu);
        Assert.Equal("sistem", sonuc.CozenKullanici);
        Assert.NotNull(sonuc.CozulmeTarihi);
        Assert.Equal("Kök neden: eksik FK.", sonuc.Notlar);
    }

    [Fact]
    public async Task Toplu_silme_YALNIZCA_cozulenleri_siler()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var servis = Servis();
        // İki AYRI fırlatma yeri: aynı satırdan atılan iki istisna tek kayda
        // toplanır (parmakizi mesajı dışlıyor).
        await servis.KaydetAsync(Firlat("Çözülen"), Baglam(yol: "/api/v2/a"), "I1", 500);
        await servis.KaydetAsync(
            BaskaYerdenFirlat("Duran"), Baglam(yol: "/api/v2/b"), "I2", 500);

        using (var db = _ortam.Baglam())
        {
            var ilk = await db.SistemHatalari.OrderBy(h => h.Id).FirstAsync();
            ilk.Cozuldu = true;
            await db.SaveChangesAsync();
        }

        var silinen = await servis.CozulenleriSilAsync();

        // Çözülmemiş bir kaydı toplu silmek, üzerinde çalışılan bir sorunun
        // izini kaybetmek demek.
        Assert.Equal(1, silinen);

        using var kontrol = _ortam.Baglam();
        var kalan = await kontrol.SistemHatalari.SingleAsync();
        Assert.Equal("Duran", kalan.Mesaj);
    }

    public void Dispose()
    {
        _saglayici?.Dispose();
        GC.SuppressFinalize(this);
    }
}
