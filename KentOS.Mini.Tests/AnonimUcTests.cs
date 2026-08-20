using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using KentOS.Mini.Application.Dto.V2.Cicek;
using KentOS.Mini.Web.AuthPolicies;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// GİRİŞ YAPMADAN ULAŞILABİLEN YÜZEYİ kilitler.
/// </summary>
/// <remarks>
/// <para>
/// <c>IzinAttribute</c> elle yazılmış bir yetkilendirme filtresidir ve uzun
/// süre <c>[AllowAnonymous]</c>'u <b>hiç okumuyordu</b>: sınıf düzeyinde
/// <c>[Izin(...)]</c> taşıyan bir controller'da metoda anonim demek işe
/// yaramıyor, istek yine 401 alıyordu. Ölçülen sonuç, çiçekçiye SMS ile
/// giden teslim bağlantısının hiç açılmamasıydı.
/// </para>
/// <para>
/// Düzeltme filtreyi <b>sistem genelinde</b> etkiliyor, o yüzden bedeli de
/// sistem genelinde ölçülmeli: bundan sonra bir uca <c>[AllowAnonymous]</c>
/// yazmak onu GERÇEKTEN herkese açar. Aşağıdaki liste o yüzden var — yeni
/// bir anonim uç eklemek artık bilinçli bir karar ve bu testten geçiyor.
/// </para>
/// </remarks>
public class AnonimUcTests
{
    /// <summary>
    /// Giriş yapılmadan çağrılabilen uçlar — her biri gerekçesiyle.
    /// </summary>
    private static readonly HashSet<string> AnonimYuzey =
    [
        // Giriş ucunun kendisi; jeton buradan alınıyor.
        "OturumController.GirisAsync",

        // Kurum bilgisi: giriş EKRANI da amblemi, kurum adını ve marka
        // rengini göstermek zorunda. Yanıtta gizli bir şey yok.
        "InstitutionController.KurumAsync",

        // PWA manifesti: tarayıcı bunu jeton göndermeden ister. Statik dosya
        // olsaydı da anonim olurdu; sunucuda üretilmesinin sebebi kurum adı.
        "ManifestController.ManifestAsync",

        // VATANDAŞ PORTALI — uygulamanın tek anonim YAZMA yüzeyi ve kuralı
        // bilerek kıran tek yer (bkz. BildirimPortalController). Kendi
        // korumaları var: kurum bayrağı kapalıyken 404, IP ve numara başına
        // hız sınırı, telefon doğrulaması, gizli alana yükleme.
        "BildirimPortalController.BildirAsync",
        "BildirimPortalController.DogrulaAsync",
        "BildirimPortalController.FotografAsync",
        "BildirimPortalController.KodAsync",

        // KİMLİK SAĞLAYICI ile giriş yolu: çağıran henüz giriş YAPMAMIŞ.
        // Üçü de yalnızca giriş akışını yürütüyor:
        //  - giris-durumu: "düğme çizilsin mi" + düğme metni. Yetkili adres,
        //    istemci kimliği ve kapsamlar bu yanıttan SIZMAZ.
        //  - baslat / geri-donus: yönlendirme akışının kendisi; ikisi de
        //    giriş hız sınırına bağlı ve `state` ile korunuyor.
        "OpenIdController.GirisDurumuAsync",
        "OpenIdController.BaslatAsync",
        "OpenIdController.GeriDonusAsync",

        // Çiçekçi kurumun kullanıcısı değil: hesabı, rolü, jetonu yok.
        // Yetki belirteci bağlantıdaki GUID; yanıt da ona göre daraltıldı.
        "CicekController.TeslimKartiAsync",
        "CicekController.TeslimEtAsync",
    ];

    private static IEnumerable<(Type Tip, MethodInfo Uc)> TumUclar() =>
        typeof(KentOS.Mini.Web.Controllers.V2.V2ControllerBase).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"))
            .SelectMany(t => t
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any())
                .Select(m => (Tip: t, Uc: m)));

    [Fact]
    public void Anonim_yuzey_beklenen_uclarla_sinirli()
    {
        var anonim = TumUclar()
            .Where(x => x.Uc.GetCustomAttributes<AllowAnonymousAttribute>().Any()
                        || x.Tip.GetCustomAttributes<AllowAnonymousAttribute>().Any())
            .Select(x => $"{x.Tip.Name}.{x.Uc.Name}")
            .ToHashSet();

        var beklenmeyen = anonim.Except(AnonimYuzey).OrderBy(a => a).ToList();
        var kaybolan = AnonimYuzey.Except(anonim).OrderBy(a => a).ToList();

        Assert.True(
            beklenmeyen.Count == 0,
            "Giriş gerektirmeyen YENİ uç(lar) var. Kasıtlıysa gerekçesiyle "
            + $"AnonimYuzey listesine ekle: {string.Join(", ", beklenmeyen)}");

        Assert.True(
            kaybolan.Count == 0,
            $"Listedeki uç(lar) artık anonim değil ya da adı değişti: {string.Join(", ", kaybolan)}");
    }

    /// <summary>
    /// Filtrenin <c>[AllowAnonymous]</c>'ta ERKEN ÇIKTIĞINI kanıtlar.
    /// </summary>
    /// <remarks>
    /// Servis sağlayıcı bilerek <b>boş</b>: filtre erken çıkmazsa
    /// <c>ICurrentUserService</c>'i çözmeye çalışır ve test istisna ile
    /// düşer. Yani "çalıştı" değil, "kapıya hiç uğramadı" ölçülüyor.
    /// </remarks>
    [Fact]
    public async Task Izin_filtresi_AllowAnonymous_ucunda_erken_cikar()
    {
        var sonuc = await FiltreyiCalistir(anonimMi: true);
        Assert.Null(sonuc);
    }

    /// <summary>
    /// Bekçinin ATEŞ ETTİĞİNİ kanıtlar: <c>[AllowAnonymous]</c> yoksa filtre
    /// kapıyı işletmeye çalışır. Hiç ateş etmeyen bekçi, olmayan bekçidir.
    /// </summary>
    [Fact]
    public async Task Izin_filtresi_isaretsiz_ucta_kapiyi_isletir()
    {
        await Assert.ThrowsAnyAsync<Exception>(() => FiltreyiCalistir(anonimMi: false));
    }

    private static async Task<IActionResult?> FiltreyiCalistir(bool anonimMi)
    {
        var tanim = new ActionDescriptor();
        if (anonimMi) tanim.EndpointMetadata = [new AllowAnonymousAttribute()];

        var baglam = new AuthorizationFilterContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), tanim),
            []);

        await new IzinAttribute(KentOS.Mini.Application.Identity.Izinler.CicekGoruntule)
            .OnAuthorizationAsync(baglam);

        return baglam.Result;
    }

    /// <summary>
    /// Anonim kart yanıtı DOĞRULAMA KODUNU taşımaz.
    /// </summary>
    /// <remarks>
    /// Kart bağlantısı ile kod aynı SMS'te gidiyor ama teslim kapısı kod;
    /// kartın kendisi kodu gösterseydi kapı hiçbir şeyi korumazdı. Kurum içi
    /// <c>CicekKartDto</c> kodu taşımaya devam ediyor, bu DTO ondan ayrı
    /// tutulmasının tek sebebi de bu.
    /// </remarks>
    [Fact]
    public void Teslim_karti_dogrulama_kodunu_disari_vermez()
    {
        var sizan = typeof(CicekTeslimKartiDto)
            .GetProperties()
            .Where(p => p.Name.Contains("Kod", StringComparison.OrdinalIgnoreCase)
                        || p.Name.Contains("Dogrulama", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Name)
            .ToList();

        Assert.True(sizan.Count == 0, $"Anonim kart DTO'su kod sızdırıyor: {string.Join(", ", sizan)}");
    }
}
