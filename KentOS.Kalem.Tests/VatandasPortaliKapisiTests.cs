using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Controllers.V2;
using KentOS.Kalem.Web.Filters;
using KentOS.Kalem.Web.Services.V2;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// VATANDAŞ PORTALI KAPISI — bayrak kapalıyken uçlar var olmamalı.
/// </summary>
/// <remarks>
/// <para>
/// Bu testin varlık sebebi şu: portalı "kapatmak" ekranı gizlemekle
/// karıştırılabilir. SPA rotayı gizlese bile uçlar açık kaldığı sürece portal
/// AÇIKTIR — <c>curl</c> ile yazmaya devam edilir. Kapının sunucuda olduğunu
/// kilitleyen tek şey burası.
/// </para>
/// <para>
/// Ayrıca <b>varsayılanın kapalı</b> olduğu doğrulanıyor: yeni bir kurulum,
/// kimsenin haberi olmadan anonim yazmaya açık gelmemeli.
/// </para>
/// </remarks>
public class VatandasPortaliKapisiTests
{
    /// <summary>Yalnızca bayrağı taşıyan sahte kurum servisi.</summary>
    private sealed class SahteKurum(bool acik) : IInstitutionService
    {
        public Task<Institution> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new Institution { Name = "Test", CitizenReportEnabled = acik });

        public Task<KurumBilgisiDto> GetPublicAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<KurumBilgisiDto> UpdateAsync(
            KurumGuncellemeIstegi istek, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static ActionExecutingContext Baglam()
    {
        var http = new DefaultHttpContext();

        var eylem = new ActionContext(
            http, new Microsoft.AspNetCore.Routing.RouteData(), new ActionDescriptor());

        return new ActionExecutingContext(
            eylem, [], new Dictionary<string, object?>(), controller: null!);
    }

    [Fact]
    public async Task Bayrak_kapaliyken_uc_404_donuyor()
    {
        var baglam = Baglam();
        var calisti = false;

        await new VatandasPortaliFiltresi(new SahteKurum(acik: false))
            .OnActionExecutionAsync(baglam, () =>
            {
                calisti = true;
                return Task.FromResult<ActionExecutedContext>(null!);
            });

        Assert.IsType<NotFoundResult>(baglam.Result);

        // Eylemin ÇALIŞMAMASI 404 dönmesinden daha önemli: kısa devre
        // olmasaydı satır yazılır, sonra 404 dönerdi.
        Assert.False(calisti, "Kapı kapalıyken eylem hiç çalışmamalı.");
    }

    [Fact]
    public async Task Bayrak_acikken_uc_calisiyor()
    {
        var baglam = Baglam();
        var calisti = false;

        await new VatandasPortaliFiltresi(new SahteKurum(acik: true))
            .OnActionExecutionAsync(baglam, () =>
            {
                calisti = true;
                return Task.FromResult<ActionExecutedContext>(null!);
            });

        Assert.True(calisti);
        Assert.Null(baglam.Result);
    }

    [Fact]
    public void Yeni_kurulumda_portal_KAPALI()
    {
        // Varsayılanın kapalı olması bir tercih değil, güvenlik kararı:
        // anonim yazma yüzeyi kimsenin haberi olmadan açık gelmemeli.
        Assert.False(new Institution().CitizenReportEnabled);
    }

    [Fact]
    public void Portal_controllerinda_kapi_ve_hiz_siniri_ikisi_de_var()
    {
        /*
          İKİSİ BİRDEN ŞART ve ikisi de öznitelik olduğu için sessizce
          silinebilir. Kapı olmadan portal kapatılamaz; hız sınırı olmadan
          açık portal bir SMS musluğuna döner.
        */
        var tur = typeof(BildirimPortalController);

        var kapi = tur.GetCustomAttributes(typeof(ServiceFilterAttribute), inherit: true)
            .Cast<ServiceFilterAttribute>()
            .FirstOrDefault(f => f.ServiceType == typeof(VatandasPortaliFiltresi));

        Assert.True(kapi is not null, "BildirimPortalController üzerinde VatandasPortaliFiltresi yok.");

        /*
          SIRA DA KİLİTLİ.

          Varsayılan sırada kapalı portal, bozuk bir gövdeye 400 dönüyordu ve
          bu "burada bir uç var" demekti. 400'ü döndüren şey bizim
          filtrelerimiz değil, `[ApiController]`'ın `-2000`'de çalışan kendi
          model doğrulaması; kapının ondan önde olması gerekiyor. Tarayıcıyla
          ölçüldü, sonra buraya kilitlendi.
        */
        Assert.True(
            kapi!.Order < -2000,
            $"Kapı model doğrulamasından (Order = -2000) sonra çalışıyor: {kapi.Order}. " +
            "Kapalı portal bozuk gövdeye 404 değil 400 döner ve varlığını ele verir.");

        var sinir = tur.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
            .Cast<EnableRateLimitingAttribute>()
            .Any(a => a.PolicyName == HizSiniri.VatandasPortali);

        Assert.True(sinir, "BildirimPortalController üzerinde hız sınırı politikası yok.");
    }
}
