using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Mapping;
using KentOS.Mini.Web.Options;
using KentOS.Mini.Web.Services;
using KentOS.Mini.Web.Storage;

namespace KentOS.Mini.Tests;

/// <summary>
/// Postgres'e dokunan tüm test sınıflarının koleksiyon adı.
/// </summary>
/// <remarks>
/// <para>
/// xUnit farklı test SINIFLARINI varsayılan olarak paralel koşturur;
/// <see cref="SunucuTestOrtami"/> kurucusu ise şemayı sıfırlayıp yeniden
/// yaratıyor. Aynı anda iki sınıf koşarsa biri diğerinin şemasını siler.
/// </para>
/// <para>
/// Sonuç sinsi: testler tek tek koşunca geçer, hep birlikte koşunca RASTGELE
/// sınıflar <c>relation "birimler" does not exist</c> ile düşer — yani hatayı
/// testin kendisi değil, o sırada şemayı silen komşu sınıf üretir. Bu ad
/// zaten kullanılıyordu; sabit hâline getirildi ki yeni bir sınıf yazarken
/// elle yazılıp unutulmasın.
/// </para>
/// </remarks>
public static class SunucuKoleksiyonu
{
    public const string Ad = "SeriPostgres";
}

/// <summary>
/// GERÇEK <see cref="AppDbContext"/> ile çalışan test ortamı.
///
/// Neden minimal TestDbContext yerine gerçek context: gizli etkinlik ve tekrar
/// serisi mantığı katılımcı tablosu, seri tablosu, Identity kullanıcıları ve
/// global soft-delete filtresinin BİRLİKTE davranışına dayanıyor. Bunların
/// hiçbiri kırpılmış bir modelde doğrulanamaz.
///
/// Uygulamanın kendi veritabanına ASLA dokunulmaz: ayrı bir <c>workcollab_seri_test</c>
/// veritabanı yaratılır, testler bitince silinir. Postgres yoksa testler atlanır.
/// </summary>
public class SunucuTestOrtami : IDisposable
{
    // Süper kullanıcı KULLANILMAZ: konteyner başka projelerle paylaşılıyor.
    // `workcollab` rolü yalnızca kendi veritabanlarının sahibi ve veritabanı
    // yaratma yetkisi yok — bkz. SemayiSifirla.
    private static readonly string BaglantiMetni =
        Environment.GetEnvironmentVariable("WORKCOLLAB_SERI_TEST_DB")
        ?? "Host=localhost;Port=5432;Database=workcollab_seri_test;Username=workcollab;Password=workcollab";

    public DbContextOptions<AppDbContext> Ayarlar { get; }
    public bool BaglanabildiMi { get; }
    public string? AtlamaNedeni { get; }
    public IMapper Mapper { get; }

    public SunucuTestOrtami()
    {
        Ayarlar = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(BaglantiMetni)
            .UseSnakeCaseNamingConvention()
            .Options;

        var config = new TypeAdapterConfig();
        MapsterConfig.Register(config);
        Mapper = new Mapper(config);

        try
        {
            using var ctx = new AppDbContext(Ayarlar);
            SemayiSifirla(ctx);
            ctx.Database.EnsureCreated();
            BaglanabildiMi = true;
        }
        catch (Exception ex)
        {
            BaglanabildiMi = false;
            AtlamaNedeni = "Postgres'e baglanilamadi (localhost:5432/workcollab_seri_test): " + ex.Message;
        }
    }

    public AppDbContext Baglam() => new(Ayarlar);

    /// <summary>
    /// Etkinlik kaydı için ZORUNLU referans verileri (birim, etkinlik tipi, durum)
    /// ve test kullanıcıları. <c>ajandalar.randevu_tip_id</c> ile <c>durum_id</c>
    /// veritabanında NOT NULL olduğu için bunlar olmadan hiçbir etkinlik yazılamaz.
    /// </summary>
    public async Task TemelVerileriKurAsync()
    {
        using var b = Baglam();

        if (!await b.Birimler.AnyAsync(x => x.Id == 1))
        {
            // birimler tablosunda yetkili/unvan NOT NULL.
            b.Birimler.Add(new Birim { Id = 1, Ad = "Test Birimi", Yetkili = "Test Yetkili", Unvan = "Müdür" });
            b.Birimler.Add(new Birim { Id = 2, Ad = "Diğer Birim", Yetkili = "Diğer Yetkili", Unvan = "Müdür" });
        }
        if (!await b.RandevuTipleri.AnyAsync(x => x.Id == 1))
        {
            b.RandevuTipleri.Add(new RandevuTip { Id = 1, Ad = "Toplantı" });
        }
        if (!await b.AjandaDurumlar.AnyAsync(x => x.Id == 1))
        {
            b.AjandaDurumlar.Add(new AjandaDurum { Id = 1, Ad = "Planlandı", Renk = "#0d6efd" });
        }

        // Kullanıcılar: 1 = ekleyen, 2 = katılımcı, 3 = aynı birimden yabancı,
        // 4 = başka birimden kullanıcı.
        foreach (var (id, kadi, birimId) in new[]
                 {
                     (1L, "ekleyen", 1L), (2L, "katilimci", 1L), (3L, "yabanci", 1L), (4L, "digerbirim", 2L)
                 })
        {
            if (!await b.Users.AnyAsync(u => u.Id == id))
            {
                b.Users.Add(new AppUser
                {
                    Id = id,
                    UserName = kadi,
                    NormalizedUserName = kadi.ToUpperInvariant(),
                    Email = kadi + "@test.local",
                    NormalizedEmail = (kadi + "@test.local").ToUpperInvariant(),
                    Ad = kadi,
                    Soyad = "Test",
                    Unvan = "Uzman",
                    BirimId = birimId,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });
            }
        }

        await b.SaveChangesAsync();

        DizileriEsitle(b);
    }

    /// <summary>
    /// Açık kimlikle eklenen tohum satırlarından sonra kimlik dizilerini ileri
    /// sarar.
    ///
    /// <para>
    /// <c>Id = 1</c> yazmak diziyi ilerletmez; sonraki <c>INSERT</c> yine 1
    /// üretmeye çalışır ve <c>duplicate key</c> ile düşer. Hata testin
    /// kendisinde değil, testin doğrulamaya çalıştığı kuralın ÖNCESİNDE
    /// patladığı için yanıltıcıdır — bu yüzden burada, tek yerde çözülüyor.
    /// </para>
    /// </summary>
    private static void DizileriEsitle(AppDbContext b)
    {
        // Tablo adları modelden okunur, elle yazılmaz: Identity tablolarının
        // adı yapılandırmaya göre değişiyor ve sabit bir liste sessizce
        // "relation does not exist" ile düşerdi.
        foreach (var tur in new[] { typeof(Birim), typeof(AppUser), typeof(AppRole), typeof(RandevuTip), typeof(AjandaDurum) })
        {
            var tablo = b.Model.FindEntityType(tur)?.GetTableName();
            if (string.IsNullOrWhiteSpace(tablo)) continue;

            // EF1002: tablo adı MODELDEN geliyor, dış girdiden değil.
#pragma warning disable EF1002
            b.Database.ExecuteSqlRaw(
                $"""
                 SELECT CASE WHEN pg_get_serial_sequence('"{tablo}"', 'id') IS NULL THEN 0
                        ELSE setval(pg_get_serial_sequence('"{tablo}"', 'id'),
                                    GREATEST(COALESCE((SELECT MAX(id) FROM "{tablo}"), 0), 1)) END
                 """);
#pragma warning restore EF1002
        }
    }

    /// <summary>
    /// Veritabanını DÜŞÜRMEDEN içini boşaltır.
    ///
    /// <para>
    /// <c>EnsureDeleted()</c> KULLANILMIYOR: veritabanını düşürüp yeniden
    /// yaratmak <c>CREATEDB</c> yetkisi ister. Bu Postgres konteyneri başka
    /// projelerle (kentos, turbopos, turbohesap) paylaşıldığı için test rolüne
    /// veritabanı yaratma yetkisi vermiyoruz. Şemayı sıfırlamak aynı sonucu,
    /// yetki yükseltmeden verir.
    /// </para>
    /// </summary>
    private static void SemayiSifirla(AppDbContext ctx)
    {
        ctx.Database.OpenConnection();
        ctx.Database.ExecuteSqlRaw("DROP SCHEMA IF EXISTS public CASCADE; CREATE SCHEMA public;");
        ctx.Database.CloseConnection();
    }

    public void Dispose()
    {
        if (!BaglanabildiMi)
        {
            return;
        }

        try
        {
            using var ctx = new AppDbContext(Ayarlar);
            SemayiSifirla(ctx);
        }
        catch
        {
            // Temizlik hatası testlerin sonucunu etkilemesin.
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>Sabit kimlik döndüren <see cref="ICurrentUserService"/> yerine geçen.</summary>
public class SahteKullaniciServisi(long kullaniciId, string kullaniciAdi, long birimId) : ICurrentUserService
{
    public long KullaniciId { get; set; } = kullaniciId;
    public string KullaniciAdi { get; set; } = kullaniciAdi;
    public long BirimId { get; set; } = birimId;

    /// <summary>Rapor başlıklarında kullanılan birim adı.</summary>
    public string? BirimAd { get; set; } = "Test Birimi";

    /// <summary>
    /// Gizli etkinlik oluşturma yetkisi.
    /// </summary>
    /// <remarks>
    /// Varsayılan <c>true</c>: mevcut testlerin çoğu gizli etkinliğin
    /// GÖRÜNÜRLÜK kurallarını sınıyor, oluşturma yetkisini değil. Yetkinin
    /// kendisi <see cref="GizliEtkinlikYetkisiTests"/> içinde bu bayrak
    /// kapatılarak sınanıyor.
    /// </remarks>
    public bool GizliEtkinlikEkleyebilir { get; set; } = true;

    /// <summary>
    /// Ajandanın yalnızca basına açık kısmını mı görüyor?
    /// </summary>
    /// <remarks>
    /// Varsayılan <c>false</c>: testlerin çoğu tam görüntüleme yetkisiyle
    /// çalışıyor. Daraltma <see cref="BasinAjandasiTests"/> içinde açılarak
    /// sınanıyor.
    /// </remarks>
    public bool YalnizcaBasin { get; set; }

    public string GetUsername() => KullaniciAdi;
    public long GetCurrentBirimId() => BirimId;
    public Task<bool> YalnizcaBasinMiAsync() => Task.FromResult(YalnizcaBasin);
    public Task<long?> GetUserIdAsync() => Task.FromResult<long?>(KullaniciId);
    public Task<AppUser> GetCurrentAsync() => Task.FromResult(new AppUser
    {
        Id = KullaniciId,
        UserName = KullaniciAdi,
        BirimId = BirimId,
        GizliEtkinlikEkleyebilir = GizliEtkinlikEkleyebilir,
    });
    public Task<string?> GetCurrentBirimAdiAsync() => Task.FromResult(BirimAd);

    public Task<string> GetFullNameAsync() => Task.FromResult(KullaniciAdi + " Test");
    public Task<string> GetFullNameAndUnvan() => Task.FromResult(KullaniciAdi + " Test - Uzman");
}

/// <summary>Gönderilen bildirimleri KAYDEDEN sahte mesaj servisi.</summary>
public class SahteMesajServisi : IMessageService
{
    public record BirimBildirimi(long BirimId, string Baslik, string Icerik, string? Data = null);
    public record KisiBildirimi(List<long> KullaniciIdler, string Baslik, string Icerik, string? Data = null);

    public List<BirimBildirimi> BirimeGidenler { get; } = [];
    public List<KisiBildirimi> KisilereGidenler { get; } = [];
    public List<long> TekKisiyeGidenler { get; } = [];

    /// <summary>
    /// Tek kişiye giden mesajın TAMAMI — alıcı, numara ve METİN.
    /// </summary>
    /// <remarks>
    /// <see cref="TekKisiyeGidenler"/> yalnızca kullanıcı kimliğini tutuyor ve
    /// SMS'te asıl doğrulanması gereken şey METİN: yer tutucular (`{adSoyad}`,
    /// `{saat}`) doldu mu, kalıntı kaldı mı. Vatandaşa yanlış saat göndermek
    /// geri alınamaz.
    /// </remarks>
    public record TekMesaj(long KullaniciId, string Jeton, string Baslik, string Icerik);

    public List<TekMesaj> TekKisiyeGidenMesajlar { get; } = [];

    public Task CreateAsync(long userId, string token, string title, string content, SendMessageType type, NotifikasyonTip tip, string? data)
    {
        TekKisiyeGidenler.Add(userId);
        TekKisiyeGidenMesajlar.Add(new TekMesaj(userId, token, title, content));
        return Task.CompletedTask;
    }

    public Message BuildMessage(long userId, string token, string title, string content, SendMessageType type, NotifikasyonTip tip, string? data)
        => new() { UserId = userId, Token = token, Title = title, Content = content, MessageType = type, Data = data };

    public Task CreateForAllPersonAsync(long departmentId, string title, string content, SendMessageType type, NotifikasyonTip tip, string? data)
    {
        BirimeGidenler.Add(new BirimBildirimi(departmentId, title, content, data));
        return Task.CompletedTask;
    }

    public Task CreateForUsersAsync(IEnumerable<long> userIds, string title, string content, SendMessageType type, NotifikasyonTip tip, string? data)
    {
        KisilereGidenler.Add(new KisiBildirimi(userIds.ToList(), title, content, data));
        return Task.CompletedTask;
    }

    public Task<Message> GetAsync(long id) => Task.FromResult(new Message());
    public Task DeleteAsync(long id) => Task.CompletedTask;
    public Task UpdateAsync(Message message) => Task.CompletedTask;
    public Task<IEnumerable<Message>> GetWaitingMessagesAsync() => Task.FromResult<IEnumerable<Message>>([]);
}

/// <summary>Test servislerini bir arada kuran fabrika.</summary>
public static class TestServisFabrikasi
{
    public static (AjandaService ajanda, AjandaSeriService seri, SahteMesajServisi mesaj) Kur(
        AppDbContext baglam, ICurrentUserService kullanici, IMapper mapper)
    {
        var mesaj = new SahteMesajServisi();
        var olay = new AjandaOlayService(baglam, kullanici, NullLogger<AjandaOlayService>.Instance);
        var seri = new AjandaSeriService(baglam, kullanici, mesaj, olay, mapper, NullLogger<AjandaSeriService>.Instance);
        var ajanda = new AjandaService(baglam, kullanici, new ApplicationOptions(), mesaj, olay,
            new SahteDepo(), seri, mapper);

        return (ajanda, seri, mesaj);
    }

    /// <summary>Yalnızca dosya yükleme yolunda kullanılan barındırma ortamı yerine geçen.</summary>
    public sealed class SahteOrtam : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "workcollab-test-wwwroot");
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ApplicationName { get; set; } = "Testler";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Test";
    }

    /// <summary>
    /// Bellek içi dosya deposu.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Diske yazmak yerine sözlükte tutar. Bu, testleri hem hızlandırıyor hem
    /// de yan etkisiz kılıyor: eskiden yükleme testleri geçici klasöre gerçek
    /// dosya bırakıyordu ve arta kalanlar bir sonraki koşumu etkileyebiliyordu.
    /// </para>
    /// <para>
    /// <see cref="Yazilanlar"/> testlerden okunabilir; "dosya gerçekten
    /// yazıldı mı, hangi anahtarla?" sorusunu doğrulamak için.
    /// </para>
    /// </remarks>
    public sealed class SahteDepo : IFileStorage
    {
        /// <summary>Yazılan içerikler: "alan|anahtar" → baytlar.</summary>
        public Dictionary<string, byte[]> Yazilanlar { get; } = new(StringComparer.Ordinal);

        public string ProviderName => "Test";
        public bool IsRemote => false;

        private static string Anahtar(StorageArea alan, string anahtar) =>
            $"{alan}|{StorageKey.Normalize(anahtar)}";

        public async Task SaveAsync(StorageArea area, string key, Stream content, string? contentType,
                                    CancellationToken cancellationToken = default)
        {
            using var tampon = new MemoryStream();
            await content.CopyToAsync(tampon, cancellationToken);
            Yazilanlar[Anahtar(area, key)] = tampon.ToArray();
        }

        public Task SaveAsync(StorageArea area, string key, byte[] content, string? contentType,
                              CancellationToken cancellationToken = default)
        {
            Yazilanlar[Anahtar(area, key)] = content;
            return Task.CompletedTask;
        }

        public Task<Stream?> OpenReadAsync(StorageArea area, string key,
                                           CancellationToken cancellationToken = default) =>
            Task.FromResult(Yazilanlar.TryGetValue(Anahtar(area, key), out var icerik)
                ? (Stream?)new MemoryStream(icerik, writable: false)
                : null);

        public Task<byte[]?> ReadAllBytesAsync(StorageArea area, string key,
                                               CancellationToken cancellationToken = default) =>
            Task.FromResult(Yazilanlar.TryGetValue(Anahtar(area, key), out var icerik) ? icerik : null);

        public Task<bool> ExistsAsync(StorageArea area, string key,
                                      CancellationToken cancellationToken = default) =>
            Task.FromResult(Yazilanlar.ContainsKey(Anahtar(area, key)));

        public Task DeleteAsync(StorageArea area, string key,
                                CancellationToken cancellationToken = default)
        {
            Yazilanlar.Remove(Anahtar(area, key));
            return Task.CompletedTask;
        }
    }
}
