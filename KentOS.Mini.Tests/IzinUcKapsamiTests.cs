using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.AuthPolicies;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// v2'deki HER ucun bir izin kapısı olduğunu denetler.
/// </summary>
/// <remarks>
/// <para>
/// Politika tabanlı <c>[Authorize]</c> satırları v2'den kaldırıldı; yetki
/// artık izinden geliyor. <b>İşaretlenmemiş bir uç, giriş yapan HERKESE
/// açıktır</b> ve bu sessizdir: uç çalışır, kimse bir şey fark etmez.
/// </para>
/// <para>
/// Yeni bir uç yazan kişi izni unutursa bu test düşer — kod incelemesinde
/// gözden kaçabilecek tek şey tam olarak budur.
/// </para>
/// </remarks>
public class IzinUcKapsamiTests
{
    private static IEnumerable<Type> V2Controllerlari() =>
        typeof(KentOS.Mini.Web.Controllers.V2.V2ControllerBase).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "KentOS.Mini.Web.Controllers.V2")
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"));

    private static IEnumerable<MethodInfo> Uclar(Type controller) =>
        controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());

    /// <summary>Uç (ya da controller'ı) anonim mi?</summary>
    /// <remarks>
    /// Sınıf düzeyi de bakılıyor: form portalı ve vatandaş portalı
    /// controller'ın tamamını anonim ilan ediyor.
    /// </remarks>
    private static bool AnonimMi(MethodInfo uc) =>
        uc.GetCustomAttributes<AllowAnonymousAttribute>().Any()
        || uc.DeclaringType!.GetCustomAttributes<AllowAnonymousAttribute>().Any();

    private static bool IzinKapisiVar(MethodInfo uc) =>
        uc.GetCustomAttributes<IzinAttribute>().Any()
        || uc.DeclaringType!.GetCustomAttributes<IzinAttribute>().Any();

    /// <summary>
    /// Kimlik doğrulaması istemeyen, bilerek herkese açık uçlar.
    /// </summary>
    private static readonly HashSet<string> AcikUclar =
    [
        // Giriş yapılmadan çağrılır; `[AllowAnonymous]` taşır.
        "OturumController.GirisAsync",

        // Oturum sahibinin KENDİ bilgisi ve kendi tercihleri: yetki istemez,
        // aksi hâlde izni kısılmış bir kullanıcı uygulamaya hiç giremezdi.
        "OturumController.BenAsync",
        "OturumController.CikisAsync",
        "OturumController.ParolaDegistirAsync",
        "OturumController.TercihlerAsync",
        "OturumController.TercihKaydetAsync",

        // Referans listeleri (durum, tip, mahalle, meslek…): her formda
        // gerekiyor ve tek başına hiçbir veri sızdırmıyor.
        "AyarController",

        // Push jetonu kaydı — cihaz bildirimi almak yetki istemez.
        "BildirimController",

        // Kurum bilgisi ve kurumsal kimlik. GİRİŞ EKRANI da amblemi, kurum
        // adını ve marka rengini göstermek zorunda; oturum açılmadan önce
        // okunabilmeli. Yanıtta gizli hiçbir şey yok — hepsi sayfanın görünen
        // yüzü. Aynı controller'ın YAZMA ucu `sistem.kurum` istiyor.
        "InstitutionController.KurumAsync",

        /*
          VATANDAŞ PORTALI — uygulamanın TEK anonim yazma yüzeyi.

          Kimliği doğrulanmamış bir vatandaştan bildirim almanın başka yolu
          yok; izin kapısı koymak ucu hiç kullanılamaz yapardı. Kuralın
          kırılması karşılığında konan korumalar controller'ın kendi
          belgesinde tek tek yazılı: IP başına hız sınırı, numara başına ayrı
          sınır, karma saklanan ve deneme sınırlı doğrulama kodu, imzalı ve
          kısa ömürlü bilet, yalnızca takip numarası dönen yanıt.

          BURAYA YENİ UÇ EKLEMEK BİLİNÇLİ BİR KARARDIR. Controller yalnızca
          YAZIYOR; okuma ucu yok ve olmamalı — takip numarasıyla sorgulama
          eklenirse numarayı bilen herkese o vatandaşın adını, telefonunu ve
          adresini açardı.
        */
        "BildirimPortalController",
    ];

    private static bool BilerekAcik(MethodInfo uc)
    {
        var tam = $"{uc.DeclaringType!.Name}.{uc.Name}";
        return AcikUclar.Contains(tam) || AcikUclar.Contains(uc.DeclaringType.Name);
    }

    [Fact]
    public void v2_uclarinin_TAMAMI_izin_kapisindan_geciyor()
    {
        var korumasiz = new List<string>();

        foreach (var c in V2Controllerlari())
        {
            foreach (var uc in Uclar(c))
            {
                // GİRİŞ İSTEMEYEN uç bu testin yetki alanı dışında: orada
                // kapı izin değil (çağıranın hesabı yok). Boşluk kalmıyor —
                // anonim yüzeyin tamamı `AnonimUcTests` içinde ad ad kilitli
                // ve yeni bir anonim uç oradaki listeyi düşürüyor.
                if (AnonimMi(uc)) continue;

                if (BilerekAcik(uc) || IzinKapisiVar(uc)) continue;
                korumasiz.Add($"{c.Name}.{uc.Name}");
            }
        }

        Assert.True(korumasiz.Count == 0,
            "İzin kapısı olmayan uç(lar) — giriş yapan HERKESE açık:\n  "
            + string.Join("\n  ", korumasiz));
    }

    [Fact]
    public void Yazma_uclari_GORUNTULEME_izniyle_yetinmiyor()
    {
        // Sınıf düzeyindeki izin çoğu yerde "…goruntule". Silme/ekleme gibi
        // uçlar KENDİ iznini ayrıca ilan etmeli; yoksa görüntüleme yetkisi
        // olan herkes silebilirdi.
        var eksik = new List<string>();

        foreach (var c in V2Controllerlari())
        {
            var sinifIzinleri = c.GetCustomAttributes<IzinAttribute>().ToList();
            if (sinifIzinleri.Count == 0) continue;

            foreach (var uc in Uclar(c))
            {
                if (BilerekAcik(uc)) continue;

                // GİRİŞ İSTEMEYEN uç bu testin yetki alanı dışında: orada kapı
                // izin değil (çağıranın hesabı yok). Boşluk kalmıyor — anonim
                // yüzeyin tamamı `AnonimUcTests` içinde ad ad kilitli ve yeni
                // bir anonim uç eklemek oradaki listeyi düşürüyor.
                if (AnonimMi(uc)) continue;

                var fiiller = uc.GetCustomAttributes<HttpMethodAttribute>()
                    .SelectMany(a => a.HttpMethods)
                    .ToList();

                var yaziyor = fiiller.Any(f =>
                    f is "POST" or "PUT" or "DELETE" or "PATCH");
                if (!yaziyor) continue;

                // Yalnızca okuma amaçlı POST'lar var (arama, tarih aralığı);
                // onlar sınıf iznine güvenebilir.
                if (uc.Name.Contains("Ara") || uc.Name.Contains("Aralik")
                    || uc.Name.Contains("GuneGore")) continue;

                if (!uc.GetCustomAttributes<IzinAttribute>().Any())
                {
                    eksik.Add($"{c.Name}.{uc.Name} [{string.Join(",", fiiller)}]");
                }
            }
        }

        Assert.True(eksik.Count == 0,
            "Yazma ucu KENDİ iznini ilan etmiyor, görüntüleme izniyle çalışır:\n  "
            + string.Join("\n  ", eksik));
    }

    [Fact]
    public void Isaretlenen_her_izin_KATALOGDA_var()
    {
        // `[Izin("uydurma.ad")]` derlenir ama hiçbir role verilemez; uç
        // sessizce HERKESE KAPALI olur.
        var bilinmeyen = new List<string>();

        foreach (var c in V2Controllerlari())
        {
            var hepsi = c.GetCustomAttributes<IzinAttribute>()
                .Concat(Uclar(c).SelectMany(u => u.GetCustomAttributes<IzinAttribute>()));

            foreach (var a in hepsi)
            {
                var alan = typeof(IzinAttribute)
                    .GetField("_izinler", BindingFlags.NonPublic | BindingFlags.Instance)!;
                foreach (var izin in (string[])alan.GetValue(a)!)
                {
                    if (!Izinler.Gecerli(izin)) bilinmeyen.Add($"{c.Name}: {izin}");
                }
            }
        }

        Assert.True(bilinmeyen.Count == 0,
            "Katalogda olmayan izin işaretlenmiş:\n  " + string.Join("\n  ", bilinmeyen));
    }
}
