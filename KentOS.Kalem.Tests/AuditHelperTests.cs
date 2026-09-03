using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Services;
using Xunit;

namespace KentOS.Kalem.Tests;

public class AuditHelperTests
{
    private static TestDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    private static Ajanda YeniAjanda() => new()
    {
        Baslik = "İlk Başlık",
        Aciklama = "İlk açıklama",
        BaslangicTarihi = new DateTime(2026, 1, 1, 9, 0, 0),
        Status = AjandaStatus.Pending,
        IsDeleted = false
    };

    [Fact]
    public void DegisenAlanlar_Degisen_Alani_Etiket_Ile_Dondurmeli()
    {
        using var ctx = CreateInMemoryContext();
        var ajanda = YeniAjanda();
        ctx.Ajandalar.Add(ajanda);
        ctx.SaveChanges();

        // Bir alanı değiştir.
        ajanda.Baslik = "Yeni Başlık";

        var degisiklikler = AuditHelper.DegisenAlanlar(ctx.Entry(ajanda));

        // "Etiket: eski → yeni" biçimi; Ajanda.Baslik'in [Display] etiketi "Başlık".
        Assert.Contains("Başlık: İlk Başlık → Yeni Başlık", degisiklikler);
    }

    [Fact]
    public void DegisenAlanlar_Degismeyen_Alanlari_Icermemeli()
    {
        using var ctx = CreateInMemoryContext();
        var ajanda = YeniAjanda();
        ctx.Ajandalar.Add(ajanda);
        ctx.SaveChanges();

        ajanda.Baslik = "Sadece başlık değişti";

        var degisiklikler = AuditHelper.DegisenAlanlar(ctx.Entry(ajanda));

        // Değişmeyen "Açıklama" alanı listede olmamalı.
        Assert.DoesNotContain(degisiklikler, s => s.StartsWith("Açıklama:"));
        Assert.Single(degisiklikler);
    }

    [Fact]
    public void DegisenAlanlar_Gizli_Alanlari_Icermemeli()
    {
        using var ctx = CreateInMemoryContext();
        var ajanda = YeniAjanda();
        ctx.Ajandalar.Add(ajanda);
        ctx.SaveChanges();

        // Görünür bir alan + gizli metadata alanları değiştir.
        ajanda.Baslik = "Değişti";
        ajanda.GuncellemeTarihi = DateTime.Now;   // gizli alan
        ajanda.KullaniciId = "user-42";           // gizli alan (GizliAlanlar: KullaniciId)

        var degisiklikler = AuditHelper.DegisenAlanlar(ctx.Entry(ajanda));

        Assert.DoesNotContain(degisiklikler, s => s.StartsWith("Id:"));
        Assert.DoesNotContain(degisiklikler, s => s.Contains("Güncelleme Tarihi"));
        Assert.DoesNotContain(degisiklikler, s => s.Contains("Kullanıcı Id"));
        // Yalnızca görünür alan (Başlık) raporlanmalı.
        Assert.Single(degisiklikler);
        Assert.StartsWith("Başlık:", degisiklikler[0]);
    }

    [Fact]
    public void DegisenAlanlar_Degisiklik_Yoksa_Bos_Liste_Dondurmeli()
    {
        using var ctx = CreateInMemoryContext();
        var ajanda = YeniAjanda();
        ctx.Ajandalar.Add(ajanda);
        ctx.SaveChanges();

        // Hiçbir değişiklik yapılmadı (Unchanged durumu).
        var degisiklikler = AuditHelper.DegisenAlanlar(ctx.Entry(ajanda));

        Assert.Empty(degisiklikler);
    }
}
