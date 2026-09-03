using Microsoft.EntityFrameworkCore;

namespace KentOS.Kalem.Tests;

/// <summary>
/// Docker'daki Postgres üzerinde AYRI bir 'workcollab_test' veritabanı kullanır.
/// Uygulamanın kendi veritabanına ASLA dokunmaz. Kurulumda EnsureDeleted+EnsureCreated,
/// Dispose'ta EnsureDeleted ile test DB'sini yaratır/temizler.
/// Postgres'e ulaşılamazsa CanConnect=false olur ve testler Assert.Skip ile atlanır.
/// </summary>
public class PostgresTestFixture : IDisposable
{
    // AYRI test veritabanı — uygulama DB'si DEĞİL.
    //
    // SÜPER KULLANICI KULLANILMAZ. Bu Postgres konteyneri başka projelerle
    // paylaşılıyor (kentos, turbopos, turbohesap) ve aşağıda `EnsureDeleted`
    // çağrılıyor; `postgres` süper kullanıcısıyla bağlanmak, yanlış bir veritabanı
    // adının bütün bir projeyi silmesi anlamına gelirdi. `workcollab` rolü yalnızca
    // kendi veritabanlarının sahibidir.
    //
    // WORKCOLLAB_TEST_DB ortam değişkeniyle ezilebilir (CI için).
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("WORKCOLLAB_TEST_DB")
        ?? "Host=localhost;Port=5432;Database=workcollab_test;Username=workcollab;Password=workcollab";

    public DbContextOptions<TestDbContext> Options { get; }
    public bool CanConnect { get; }
    public string? SkipReason { get; }

    public PostgresTestFixture()
    {
        Options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        try
        {
            using var ctx = new TestDbContext(Options);
            SemayiSifirla(ctx);
            ctx.Database.EnsureCreated();
            CanConnect = true;
        }
        catch (Exception ex)
        {
            CanConnect = false;
            SkipReason = "Postgres'e baglanilamadi (localhost:5432/workcollab_test): " + ex.Message;
        }
    }

    public TestDbContext CreateContext() => new(Options);


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
    private static void SemayiSifirla(TestDbContext ctx)
    {
        ctx.Database.OpenConnection();
        ctx.Database.ExecuteSqlRaw("DROP SCHEMA IF EXISTS public CASCADE; CREATE SCHEMA public;");
        ctx.Database.CloseConnection();
    }

    public void Dispose()
    {
        if (!CanConnect) return;
        try
        {
            using var ctx = new TestDbContext(Options);
            SemayiSifirla(ctx);
        }
        catch
        {
            // Temizlik hatasını yut — testlerin sonucunu etkilememeli.
        }
        GC.SuppressFinalize(this);
    }
}
