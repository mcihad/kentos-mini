using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Tests;

/// <summary>
/// Testlerde <see cref="GorevServisi"/>'ye verilen KÜÇÜK servis sağlayıcısı.
/// </summary>
/// <remarks>
/// <para>
/// <c>GorevServisi</c> devir servisini kurucudan değil sağlayıcıdan çözüyor
/// (aksi hâlde dairesel bağımlılık olurdu). Testlerde gerçek bir kapsayıcı
/// kurmak yerine, yalnızca o tek bağımlılığı karşılayan bir sağlayıcı
/// veriliyor.
/// </para>
/// <para>
/// Devir servisi burada <b>hiçbir şey yapmıyor</b>: akış testlerinin konusu
/// görevin kendi kuralları ve devir ayrı bir testte
/// (<c>GelenKutusuTests</c>) ölçülüyor.
/// </para>
/// </remarks>
public static class TestKapsayici
{
    public static IServiceProvider Bos { get; } = Kur();

    private static IServiceProvider Kur()
    {
        var servisler = new ServiceCollection();
        servisler.AddSingleton<IGelenKutusuServisi, SessizGelenKutusu>();
        return servisler.BuildServiceProvider();
    }

    /// <summary>Devri hiç uygulamayan sahte.</summary>
    private sealed class SessizGelenKutusu : IGelenKutusuServisi
    {
        public Task DevirleriUygulaAsync(long gorevId, CancellationToken iptal = default) =>
            Task.CompletedTask;

        public Task<KentOS.Kalem.Application.Dto.V2.Ortak.SayfaliSonuc<
            KentOS.Kalem.Application.Dto.V2.IsTakip.GelenKutusuDto>> ListeAsync(
            KentOS.Kalem.Application.Dto.V2.Ortak.SayfaIstegi istek,
            KentOS.Kalem.Application.Enums.GelenKutusuDurumu? durum,
            bool altBirimlerDahil, CancellationToken iptal = default) =>
            throw new NotSupportedException();

        public Task<int> BekleyenSayisiAsync(CancellationToken iptal = default) =>
            Task.FromResult(0);

        public Task<KentOS.Kalem.Application.Dto.V2.IsTakip.GelenKutusuDto> KabulAsync(
            long id, KentOS.Kalem.Application.Dto.V2.IsTakip.GelenKutusuKabulDto istek,
            CancellationToken iptal = default) => throw new NotSupportedException();

        public Task<KentOS.Kalem.Application.Dto.V2.IsTakip.GelenKutusuDto> ReddetAsync(
            long id, string gerekce, CancellationToken iptal = default) =>
            throw new NotSupportedException();

        public Task<KentOS.Kalem.Application.Dto.V2.IsTakip.GelenKutusuDto> OkunduAsync(
            long id, CancellationToken iptal = default) => throw new NotSupportedException();
    }
}
