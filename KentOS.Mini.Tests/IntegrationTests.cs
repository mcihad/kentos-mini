using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Web.Services;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// Docker Postgres ('workcollab_test' ayrı DB) üzerinde gerçek Npgsql sağlayıcısıyla
/// entegrasyon testleri. Uygulama veritabanına dokunulmaz.
/// </summary>
[Collection("Postgres")]
public class IntegrationTests : IClassFixture<PostgresTestFixture>
{
    private readonly PostgresTestFixture _fixture;

    public IntegrationTests(PostgresTestFixture fixture) => _fixture = fixture;

    // Postgres'e ulaşılamıyorsa testi dinamik olarak ATLA (xunit DynamicSkipToken mekanizması).
    private void SkipIfNoDb()
    {
        if (!_fixture.CanConnect)
            throw Xunit.Sdk.SkipException.ForSkip(_fixture.SkipReason ?? "Postgres kullanılamıyor");
    }

    private static Ajanda YeniAjanda(string baslik) => new()
    {
        Baslik = baslik,
        Aciklama = "Entegrasyon açıklaması",
        BaslangicTarihi = new DateTime(2026, 3, 1, 10, 0, 0),
        OlusturmaTarihi = new DateTime(2026, 3, 1, 10, 0, 0),
        Status = AjandaStatus.Pending,
        IsDeleted = false
    };

    [Fact]
    public void SoftDelete_Global_Query_Filter_Silineni_Gizlemeli_IgnoreQueryFilters_Gostermeli()
    {
        SkipIfNoDb();

        long id;
        // 1) Ekle ve kaydet.
        using (var ctx = _fixture.CreateContext())
        {
            var ajanda = YeniAjanda("Soft-delete testi");
            ctx.Ajandalar.Add(ajanda);
            ctx.SaveChanges();
            id = ajanda.Id;
        }

        // 2) IsDeleted=true yap ve kaydet.
        using (var ctx = _fixture.CreateContext())
        {
            var ajanda = ctx.Ajandalar.Single(a => a.Id == id);
            ajanda.IsDeleted = true;
            ctx.SaveChanges();
        }

        // 3) Yeni context: varsayılan sorgu global query filter nedeniyle GETİRMEMELİ.
        using (var ctx = _fixture.CreateContext())
        {
            var varsayilan = ctx.Ajandalar.FirstOrDefault(a => a.Id == id);
            Assert.Null(varsayilan);

            // IgnoreQueryFilters ile GELMELİ.
            var filtreYok = ctx.Ajandalar.IgnoreQueryFilters().FirstOrDefault(a => a.Id == id);
            Assert.NotNull(filtreYok);
            Assert.True(filtreYok!.IsDeleted);
        }
    }

    [Fact]
    public void Audit_Gercek_Npgsql_Change_Tracking_Ile_Dogru_Diff_Dondurmeli()
    {
        SkipIfNoDb();

        using var ctx = _fixture.CreateContext();
        var ajanda = YeniAjanda("Audit testi - eski");
        ctx.Ajandalar.Add(ajanda);
        ctx.SaveChanges();

        // Alan değiştir (henüz kaydetmeden change-tracking üzerinden diff üretilir).
        ajanda.Baslik = "Audit testi - yeni";
        ajanda.Aciklama = "Güncellenmiş açıklama";

        var degisiklikler = AuditHelper.DegisenAlanlar(ctx.Entry(ajanda));

        Assert.Contains("Başlık: Audit testi - eski → Audit testi - yeni", degisiklikler);
        Assert.Contains("Açıklama: Entegrasyon açıklaması → Güncellenmiş açıklama", degisiklikler);
        // Değişmeyen alanlar diff'te olmamalı.
        Assert.DoesNotContain(degisiklikler, s => s.StartsWith("Konum:"));
    }
}
