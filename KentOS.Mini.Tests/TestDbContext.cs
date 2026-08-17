using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Models;

namespace KentOS.Mini.Tests;

/// <summary>
/// Yalnızca Ajanda + AjandaNot içeren minimal test DbContext'i.
/// Gerçek AppDbContext bir IdentityDbContext olup çok sayıda entity ve
/// Npgsql'e özgü yapılandırma içerdiğinden, EnsureCreated'ı güvenilir kılmak için
/// yalnızca test edilen model alanlarını (aynı Ajanda modeli + global query filter)
/// barındıran bu minimal context kullanılır. Diğer navigasyonlar Ignore edilir.
/// Hem EF InMemory (unit) hem gerçek Npgsql (entegrasyon) ile çalışır.
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
        // Gerçek AppDbContext ile aynı legacy timestamp davranışı (Local/Unspecified DateTime kabulü).
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
    }

    public DbSet<Ajanda> Ajandalar { get; set; } = null!;
    public DbSet<AjandaNot> AjandaNotlar { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Ajanda>(e =>
        {
            e.HasKey(x => x.Id);
            // Gerçek AppDbContext'teki soft-delete global query filter ile aynı.
            e.HasQueryFilter(x => !x.IsDeleted);

            // Modeldeki [Required] öznitelikleri gerçek AppDbContext'te FK yapılandırması
            // (IsRequired(false)) ile gevşetiliyor; minimal context'te bu navigasyonlar
            // Ignore edildiği için FK skalarlarını burada nullable olarak işaretliyoruz.
            e.Property(x => x.DurumId).IsRequired(false);
            e.Property(x => x.RandevuTipId).IsRequired(false);

            // Test kapsamı dışındaki navigasyonları modelden çıkar.
            e.Ignore(x => x.Birim);
            e.Ignore(x => x.Cicek);
            e.Ignore(x => x.Durum);
            e.Ignore(x => x.RandevuTip);
            e.Ignore(x => x.TekrarDetaylari);
            e.Ignore(x => x.Photos);
            e.Ignore(x => x.AjandaNotlar);
        });

        builder.Entity<AjandaNot>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.Ajanda);
        });
    }
}
