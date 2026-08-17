using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Web.Data;

namespace KentOS.Mini.Tests;

/// <summary>
/// Migration zincirinin BAŞTAN SONA uygulanabildiğini kanıtlar.
///
/// <para>
/// NEDEN GEREKLİ: Diğer bütün veritabanı testleri <c>EnsureCreated()</c>
/// kullanıyor — bu, şemayı doğrudan modelden kurar ve migration dosyalarına
/// hiç bakmaz. Yani bozuk bir migration bütün testler yeşilken depoya girebilir.
/// </para>
///
/// <para>
/// Bu tehlikeli, çünkü uygulama <c>Database:AutoMigrate = true</c> ile açılışta
/// migration uyguluyor ve başarısız olursa <b>fail-fast</b> davranıyor: bozuk bir
/// migration üretimde uygulamanın hiç açılmaması demek. Bu test, o senaryoyu
/// dağıtımdan önce yakalar.
/// </para>
/// </summary>
public class MigrasyonZinciriTests : IDisposable
{
    private static readonly string BaglantiMetni =
        Environment.GetEnvironmentVariable("WORKCOLLAB_MIGRASYON_TEST_DB")
        ?? "Host=localhost;Port=5432;Database=workcollab_migrasyon_test;Username=workcollab;Password=workcollab";

    private readonly DbContextOptions<AppDbContext> _ayarlar;
    private readonly bool _baglanabildi;
    private readonly string? _atlamaSebebi;

    public MigrasyonZinciriTests()
    {
        _ayarlar = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(BaglantiMetni)
            .UseSnakeCaseNamingConvention()
            .Options;

        try
        {
            using var ctx = new AppDbContext(_ayarlar);
            SemayiSifirla(ctx);
            _baglanabildi = true;
        }
        catch (Exception ex)
        {
            _baglanabildi = false;
            _atlamaSebebi = $"Postgres'e bağlanılamadı ({BaglantiMetni}): {ex.Message}";
        }
    }

    [Fact]
    public void Tum_migrationlar_bos_veritabanina_uygulanabilir()
    {
        // Postgres yoksa testi dinamik olarak atla (depodaki mevcut kalıp).
        if (!_baglanabildi)
            throw Xunit.Sdk.SkipException.ForSkip(_atlamaSebebi ?? "Postgres kullanılamıyor");

        using var ctx = new AppDbContext(_ayarlar);

        // Asıl sınav: uygulamanın açılışta yaptığı şeyin aynısı.
        ctx.Database.Migrate();

        // Uygulandıktan sonra bekleyen migration KALMAMALI. Kalıyorsa model ile
        // migration'lar arasında fark var demektir ve bir sonraki `dotnet ef
        // migrations add` beklenmedik bir fark üretir.
        var bekleyen = ctx.Database.GetPendingMigrations().ToList();
        Assert.True(
            bekleyen.Count == 0,
            $"Migration'lar uygulandıktan sonra hâlâ bekleyen var: {string.Join(", ", bekleyen)}");

        // Zincir gerçekten çalıştı mı — çekirdek tablolar yerinde mi?
        var uygulanan = ctx.Database.GetAppliedMigrations().ToList();
        Assert.NotEmpty(uygulanan);
        Assert.Contains(uygulanan, m => m.EndsWith("_InitialMigration"));
    }

    /// <summary>
    /// Veritabanını DÜŞÜRMEDEN içini boşaltır.
    ///
    /// <para>
    /// <c>EnsureDeleted()</c> kullanılmıyor: o, veritabanını düşürür ve
    /// <c>Migrate()</c> yeniden yaratmak için <c>CREATEDB</c> yetkisi ister.
    /// Bu Postgres konteyneri başka projelerle paylaşıldığı için test rolüne
    /// veritabanı yaratma yetkisi VERMİYORUZ — rol yalnızca kendi
    /// veritabanlarının sahibi. Şemayı sıfırlamak aynı sonucu, yetki
    /// yükseltmeden verir.
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
        if (!_baglanabildi) return;
        try
        {
            using var ctx = new AppDbContext(_ayarlar);
            SemayiSifirla(ctx);
        }
        catch
        {
            // Temizlik başarısızlığı testi düşürmez.
        }
        GC.SuppressFinalize(this);
    }
}
