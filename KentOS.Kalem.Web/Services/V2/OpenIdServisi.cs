using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using KentOS.Kalem.Application.Dto.V2.OpenId;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Exceptions;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Web.Options;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>
/// KURUMSAL KİMLİK SAĞLAYICI ile giriş.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden elle yazıldı.</b> ASP.NET'in <c>AddOpenIdConnect</c> handler'ı
/// açılışta yapılandırılıyor; buradaki ayar ise <b>veritabanında</b> ve
/// yetkili tarafından çalışma anında değiştiriliyor. Handler'ı çalışırken
/// yeniden yapılandırmak, ayarı <c>.env</c>'e taşımaktan daha kırılgan
/// olurdu. Akışın kendisi (keşif → yetkilendirme → kod → jeton) üç HTTP
/// çağrısı; kütüphane getirisi düşük.
/// </para>
/// <para>
/// <b>PKCE zorunlu.</b> Gizli istemci olsak da kod yakalama saldırısına
/// karşı tek satırlık maliyeti var ve bazı sağlayıcılar (Azure AD, Keycloak
/// yeni sürümler) zaten şart koşuyor.
/// </para>
/// <para>
/// <b>Jeton uygulamanın KENDİ jetonu.</b> Sağlayıcının <c>id_token</c>'ı
/// yalnızca kimliği kanıtlamak için kullanılıyor, sonra atılıyor: yetkiler
/// bu sistemde ve rol/izin bilgisi sağlayıcıda yok. Böylece sağlayıcıyla
/// girenle parolayla giren aynı jetonu taşıyor ve geri kalan her şey
/// (izin önbelleği, mobil, süre) tek yoldan geçiyor.
/// </para>
/// </remarks>
public sealed class OpenIdServisi(
    AppDbContext _context,
    UserManager<AppUser> _userManager,
    IOturumServisi _oturumServisi,
    IHttpClientFactory _istemciFabrikasi,
    IMemoryCache _onbellek,
    IAdresCozucu _adresCozucu,
    ILogger<OpenIdServisi> _gunluk) : IOpenIdService
{
    /// <summary>Sağlayıcıya kaydedilecek dönüş yolu — tek yerde.</summary>
    private const string DonusYolu = "/api/v2/openid/geri-donus";

    /// <summary>Keşif belgesi önbellek süresi.</summary>
    /// <remarks>
    /// Her girişte keşif belgesini indirmek, sağlayıcı yavaşladığında giriş
    /// ekranını da yavaşlatıyor. On dakika, sağlayıcı adres değiştirdiğinde
    /// makul bir gecikme.
    /// </remarks>
    private static readonly TimeSpan KesifOmru = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Yetkilendirme isteğinin ömrü.
    /// </summary>
    /// <remarks>
    /// Kullanıcı sağlayıcıda parola girip döneceği için cömert; ama sonsuz
    /// değil — kullanılmayan bir <c>state</c> sonsuza kadar geçerli
    /// kalmamalı.
    /// </remarks>
    private static readonly TimeSpan DurumOmru = TimeSpan.FromMinutes(10);

    // ─────────────────────────────────────────────────────────────── ayar

    private async Task<OpenIdSettings?> KayitAsync() =>
        await _context.OpenIdAyarlari.FirstOrDefaultAsync(a => a.Id == OpenIdSettings.TekilId);

    /// <summary>
    /// Sağlayıcıya gidecek dönüş adresi — <b>istekten</b> türetilir.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uygulama birden çok alan adından yayınlanabiliyor. Sabit bir taban
    /// adres, kullanıcının BULUNDUĞU adres ile sağlayıcıya bildirilen adres
    /// arasında uyuşmazlık yaratıyor ve sağlayıcı isteği
    /// <c>redirect_uri mismatch</c> ile reddediyor.
    /// </para>
    /// <para>
    /// <b>Buradaki host injection riskini sağlayıcının kendisi kapatıyor:</b>
    /// gönderilen <c>redirect_uri</c> sağlayıcıdaki kayıtlı adreslerden biri
    /// değilse istek zaten reddediliyor. Yani sahte bir <c>Host</c>
    /// başlığıyla kullanıcıyı başka bir yere yollamak mümkün değil.
    /// </para>
    /// </remarks>
    private string TamDonusAdresi() => _adresCozucu.Mutlak(DonusYolu);

    public async Task<OpenIdAyarDto> AyarAsync()
    {
        var k = await KayitAsync();

        return new OpenIdAyarDto
        {
            Etkin = k?.Etkin ?? false,
            GorunenAd = k?.GorunenAd,
            Yetkili = k?.Yetkili,
            IstemciId = k?.IstemciId,
            // Sırrın KENDİSİ değil, varlığı.
            SirTanimli = !string.IsNullOrWhiteSpace(k?.IstemciSirri),
            Kapsamlar = k?.Kapsamlar ?? "openid profile email",
            KullaniciAdiTalebi = k?.KullaniciAdiTalebi ?? "preferred_username",
            OtomatikKullaniciOlustur = k?.OtomatikKullaniciOlustur ?? false,
            DonusAdresi = TamDonusAdresi(),
        };
    }

    public async Task<OpenIdAyarDto> KaydetAsync(OpenIdAyarIstegi istek)
    {
        var k = await KayitAsync();

        if (k is null)
        {
            k = new OpenIdSettings { Id = OpenIdSettings.TekilId };
            _context.OpenIdAyarlari.Add(k);
        }

        /*
          ETKİNLEŞTİRMEDEN ÖNCE ZORUNLU ALANLAR.

          Eksik yapılandırmayla "açık" işaretlemek, giriş ekranına çalışmayan
          bir düğme koymak demek: kullanıcı basıyor, sağlayıcıya gidiyor ve
          "invalid_client" ile geri dönemeyeceği bir sayfada kalıyor.
          Kapatmak serbest — kapalıyken alanların eksik olması sorun değil.
        */
        if (istek.Etkin)
        {
            var sirVar = !string.IsNullOrWhiteSpace(istek.IstemciSirri)
                || !string.IsNullOrWhiteSpace(k.IstemciSirri);

            if (string.IsNullOrWhiteSpace(istek.Yetkili)
                || string.IsNullOrWhiteSpace(istek.IstemciId)
                || !sirVar)
            {
                throw new BusinessRuleException(
                    "Sağlayıcı adresi, istemci kimliği ve istemci sırrı dolu olmadan "
                    + "kimlik sağlayıcı açılamaz.");
            }
        }

        k.Etkin = istek.Etkin;
        k.GorunenAd = istek.GorunenAd?.Trim();
        k.Yetkili = istek.Yetkili?.Trim().TrimEnd('/');
        k.IstemciId = istek.IstemciId?.Trim();
        k.Kapsamlar = string.IsNullOrWhiteSpace(istek.Kapsamlar)
            ? "openid profile email" : istek.Kapsamlar.Trim();
        k.KullaniciAdiTalebi = string.IsNullOrWhiteSpace(istek.KullaniciAdiTalebi)
            ? "preferred_username" : istek.KullaniciAdiTalebi.Trim();
        k.OtomatikKullaniciOlustur = istek.OtomatikKullaniciOlustur;
        k.GuncellemeTarihi = DateTime.Now;

        // BOŞ SIR = DEĞİŞTİRME. Okuma ucu sırrı dönmüyor, yani ekran onu
        // forma dolduramıyor; boş geleni "sil" saymak, ayarı açıp kaydeden
        // herkesin girişi bozması demekti.
        if (!string.IsNullOrWhiteSpace(istek.IstemciSirri))
        {
            k.IstemciSirri = istek.IstemciSirri.Trim();
        }

        await _context.SaveChangesAsync();
        _onbellek.Remove(KesifAnahtari(k.Yetkili));

        return await AyarAsync();
    }

    // ───────────────────────────────────────────────────────────── keşif

    private static string KesifAnahtari(string? yetkili) => $"openid-kesif:{yetkili}";

    /// <summary>
    /// Keşif belgesi. Ulaşılamazsa <c>null</c> — <b>istisna atmaz</b>.
    /// </summary>
    /// <remarks>
    /// Giriş ekranı bunu her açılışta soruyor; sağlayıcı kapalıyken giriş
    /// ekranının da hata vermesi, parolayla girmeyi de imkânsız kılardı.
    /// Sağlayıcı erişilemezse düğme çıkmaz, o kadar.
    /// </remarks>
    private async Task<JsonElement?> KesifAsync(OpenIdSettings k)
    {
        if (string.IsNullOrWhiteSpace(k.Yetkili)) return null;

        if (_onbellek.TryGetValue<JsonElement>(KesifAnahtari(k.Yetkili), out var onbellekli))
        {
            return onbellekli;
        }

        try
        {
            var istemci = _istemciFabrikasi.CreateClient();
            // Kısa zaman aşımı: giriş ekranı sağlayıcıyı beklemez.
            istemci.Timeout = TimeSpan.FromSeconds(5);

            var belge = await istemci.GetFromJsonAsync<JsonElement>(
                $"{k.Yetkili}/.well-known/openid-configuration");

            _onbellek.Set(KesifAnahtari(k.Yetkili), belge, KesifOmru);
            return belge;
        }
        catch (Exception h)
        {
            _gunluk.LogWarning(h, "OpenID keşif belgesi okunamadı: {Yetkili}", k.Yetkili);
            return null;
        }
    }

    private static string? Alan(JsonElement? belge, string ad) =>
        belge is { } b && b.TryGetProperty(ad, out var d) ? d.GetString() : null;

    public async Task<OpenIdGirisDto> GirisDurumuAsync()
    {
        var k = await KayitAsync();

        if (k is null || !k.Etkin
            || string.IsNullOrWhiteSpace(k.Yetkili)
            || string.IsNullOrWhiteSpace(k.IstemciId))
        {
            return new OpenIdGirisDto { Kullanilabilir = false };
        }

        // ERİŞİLEBİLİR Mİ — kullanıcının isteği tam olarak buydu: ayar
        // yapılmış VE sağlayıcıya ulaşılabiliyorsa düğme çıksın.
        var yetkilendirme = Alan(await KesifAsync(k), "authorization_endpoint");

        return new OpenIdGirisDto
        {
            Kullanilabilir = !string.IsNullOrWhiteSpace(yetkilendirme),
            GorunenAd = string.IsNullOrWhiteSpace(k.GorunenAd) ? "Kurum hesabı" : k.GorunenAd,
        };
    }

    public async Task<OpenIdSinamaDto> SinaAsync()
    {
        var k = await KayitAsync();

        if (k is null || string.IsNullOrWhiteSpace(k.Yetkili))
        {
            return new OpenIdSinamaDto { Basarili = false, Mesaj = "Sağlayıcı adresi girilmemiş." };
        }

        // Sınama önbelleği ATLAR: "sına" düğmesine basan kişi ŞU ANKİ durumu
        // soruyor; on dakikalık bir kayıt ona yanlış cevap verirdi.
        _onbellek.Remove(KesifAnahtari(k.Yetkili));

        var belge = await KesifAsync(k);
        var yetkilendirme = Alan(belge, "authorization_endpoint");

        if (string.IsNullOrWhiteSpace(yetkilendirme))
        {
            return new OpenIdSinamaDto
            {
                Basarili = false,
                Mesaj = "Sağlayıcıya ulaşılamadı ya da adres bir OpenID sağlayıcısı değil. "
                    + $"Denenen adres: {k.Yetkili}/.well-known/openid-configuration",
            };
        }

        return new OpenIdSinamaDto
        {
            Basarili = true,
            Mesaj = "Sağlayıcıya ulaşıldı.",
            YetkilendirmeAdresi = yetkilendirme,
        };
    }

    // ───────────────────────────────────────────────────────── giriş akışı

    private sealed record BekleyenIstek(string Dogrulayici, string DonusYolu);

    private static string RastgeleMetin(int bayt = 32) =>
        Base64Url(RandomNumberGenerator.GetBytes(bayt));

    private static string Base64Url(byte[] veri) =>
        Convert.ToBase64String(veri).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public async Task<string> YetkilendirmeAdresiAsync(string? donusYolu)
    {
        var k = await KayitAsync()
            ?? throw new BusinessRuleException("Kimlik sağlayıcı yapılandırılmamış.");

        if (!k.Etkin) throw new BusinessRuleException("Kimlik sağlayıcı ile giriş kapalı.");

        var yetkilendirme = Alan(await KesifAsync(k), "authorization_endpoint")
            ?? throw new BusinessRuleException("Kimlik sağlayıcıya ulaşılamadı.");

        var durum = RastgeleMetin();
        var dogrulayici = RastgeleMetin(64);

        // PKCE S256: doğrulayıcının SHA-256 özeti gidiyor, kendisi sunucuda
        // kalıyor. Yakalanan bir yetkilendirme kodu tek başına işe yaramaz.
        var meydanOkuma = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(dogrulayici)));

        _onbellek.Set(
            $"openid-durum:{durum}",
            new BekleyenIstek(dogrulayici, GuvenliDonusYolu(donusYolu)),
            DurumOmru);

        var sorgu = new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = k.IstemciId,
            ["redirect_uri"] = TamDonusAdresi(),
            ["scope"] = k.Kapsamlar ?? "openid profile email",
            ["state"] = durum,
            ["code_challenge"] = meydanOkuma,
            ["code_challenge_method"] = "S256",
        };

        var dize = string.Join('&', sorgu
            .Where(a => !string.IsNullOrEmpty(a.Value))
            .Select(a => $"{Uri.EscapeDataString(a.Key)}={Uri.EscapeDataString(a.Value!)}"));

        return $"{yetkilendirme}?{dize}";
    }

    /// <summary>
    /// AÇIK YÖNLENDİRME KAPISI.
    /// </summary>
    /// <remarks>
    /// Dönüş yolu sorgu dizesinden geliyor ve doğrudan kullanılsaydı
    /// <c>?donus=https://saldirgan.example</c> ile kullanıcı, giriş
    /// yaptıktan HEMEN SONRA saldırganın sayfasına gönderilebilirdi —
    /// üstelik jeton adres parçasında. Yalnızca <c>/</c> ile başlayan ve
    /// <c>//</c> içermeyen (protokole yakın) yollar kabul ediliyor.
    /// </remarks>
    public static string GuvenliDonusYolu(string? yol) =>
        !string.IsNullOrWhiteSpace(yol)
        && yol.StartsWith('/')
        && !yol.StartsWith("//", StringComparison.Ordinal)
            ? yol
            : "/";

    public async Task<(string Jeton, DateTime? GecerlilikSonu, string DonusYolu)> GeriDonusAsync(
        string kod, string durum)
    {
        if (!_onbellek.TryGetValue<BekleyenIstek>($"openid-durum:{durum}", out var bekleyen)
            || bekleyen is null)
        {
            // Süresi geçmiş ya da hiç üretilmemiş `state`. CSRF koruması bu.
            throw new BusinessRuleException(
                "Giriş isteği geçersiz ya da zaman aşımına uğradı. Tekrar deneyin.");
        }

        _onbellek.Remove($"openid-durum:{durum}");

        var k = await KayitAsync()
            ?? throw new BusinessRuleException("Kimlik sağlayıcı yapılandırılmamış.");

        var belge = await KesifAsync(k);
        var jetonUcu = Alan(belge, "token_endpoint")
            ?? throw new BusinessRuleException("Kimlik sağlayıcıya ulaşılamadı.");

        var istemci = _istemciFabrikasi.CreateClient();
        istemci.Timeout = TimeSpan.FromSeconds(10);

        var yanit = await istemci.PostAsync(jetonUcu, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = kod,
                ["redirect_uri"] = TamDonusAdresi(),
                ["client_id"] = k.IstemciId ?? string.Empty,
                ["client_secret"] = k.IstemciSirri ?? string.Empty,
                ["code_verifier"] = bekleyen.Dogrulayici,
            }));

        if (!yanit.IsSuccessStatusCode)
        {
            /*
              SAĞLAYICININ GÖVDESİ KULLANICIYA VERİLMEZ.

              İçinde istemci kimliği ve yapılandırma ayrıntısı olabiliyor.
              Günlüğe tam hâli düşüyor; kullanıcı anlaşılır bir cümle görüyor.
            */
            _gunluk.LogWarning("OpenID jeton değişimi başarısız: {Kod} · {Govde}",
                yanit.StatusCode, await yanit.Content.ReadAsStringAsync());

            throw new BusinessRuleException(
                "Kimlik sağlayıcı girişi doğrulamadı. Sistem yöneticinize başvurun.");
        }

        var jetonYaniti = await yanit.Content.ReadFromJsonAsync<JsonElement>();

        var kimlikJetonu = jetonYaniti.TryGetProperty("id_token", out var i) ? i.GetString() : null;
        if (string.IsNullOrWhiteSpace(kimlikJetonu))
        {
            throw new BusinessRuleException("Kimlik sağlayıcı kimlik jetonu döndürmedi.");
        }

        var talepler = KimlikJetonuTalepleri(kimlikJetonu);
        var talepAdi = k.KullaniciAdiTalebi ?? "preferred_username";

        var kullaniciAdi = Alan(talepler, talepAdi)
            ?? Alan(talepler, "preferred_username")
            ?? Alan(talepler, "email");

        if (string.IsNullOrWhiteSpace(kullaniciAdi))
        {
            throw new BusinessRuleException(
                $"Kimlik sağlayıcı '{talepAdi}' bilgisini göndermedi. "
                + "Ayarlardaki eşleşme alanını kontrol edin.");
        }

        var kullanici = await _userManager.FindByNameAsync(kullaniciAdi)
            ?? await _userManager.FindByEmailAsync(kullaniciAdi);

        if (kullanici is null)
        {
            if (!k.OtomatikKullaniciOlustur)
            {
                throw new BusinessRuleException(
                    $"'{kullaniciAdi}' bu sistemde tanımlı değil. Yöneticinizden kullanıcı "
                    + "tanımlanmasını isteyin.");
            }

            kullanici = new AppUser
            {
                UserName = kullaniciAdi,
                Email = Alan(talepler, "email"),
                Ad = Alan(talepler, "given_name"),
                Soyad = Alan(talepler, "family_name"),
                EmailConfirmed = true,
            };

            var sonuc = await _userManager.CreateAsync(kullanici);
            if (!sonuc.Succeeded)
            {
                throw new BusinessRuleException(
                    "Kullanıcı oluşturulamadı: "
                    + string.Join(", ", sonuc.Errors.Select(e => e.Description)));
            }
        }

        /*
          KİLİTLİ HESAP SAĞLAYICIYLA DA GİREMEZ.

          Kilit bu sistemin kararı; sağlayıcı onu bilmiyor. Denetlenmeseydi
          parolayla kilitlenen hesap, sağlayıcı düğmesiyle girmeye devam
          ederdi — yani kilit hiçbir şey ifade etmezdi.
        */
        if (await _userManager.IsLockedOutAsync(kullanici))
        {
            throw new BusinessRuleException("Hesabınız geçici olarak kilitli. Daha sonra deneyin.");
        }

        /*
          JETON PAROLA GİRİŞİYLE AYNI YOLDAN ÜRETİLİYOR.

          Talep listesi (rol, birim, kullanıcı kimliği) ve oturum kaydı
          `OturumServisi.JetonUretAsync` içinde tek yerde. Burada kopya bir
          talep listesi kursaydık, sisteme yeni bir talep eklendiğinde
          sağlayıcıyla giren kullanıcı sessizce eksik yetkiyle dolaşırdı.
        */
        var girisSonucu = await _oturumServisi.JetonUretAsync(kullanici, OturumOlayi.Giris);
        return (girisSonucu.Jeton!, girisSonucu.GecerlilikSonu, bekleyen.DonusYolu);
    }

    /// <summary>
    /// Kimlik jetonunun gövdesini okur.
    /// </summary>
    /// <remarks>
    /// <b>İmza burada doğrulanmıyor ve bu güvenli</b>: jeton, sağlayıcının
    /// jeton ucundan <b>doğrudan bize</b>, TLS üzerinden ve istemci sırrıyla
    /// kimlik kanıtlayarak geldi — araya giren yok. İmza doğrulaması, jeton
    /// güvenilmeyen bir taraftan (ör. tarayıcıdan) gelseydi şart olurdu.
    /// </remarks>
    private static JsonElement? KimlikJetonuTalepleri(string kimlikJetonu)
    {
        var parcalar = kimlikJetonu.Split('.');
        if (parcalar.Length < 2) return null;

        var govde = parcalar[1].Replace('-', '+').Replace('_', '/');
        govde = govde.PadRight(govde.Length + (4 - govde.Length % 4) % 4, '=');

        return JsonSerializer.Deserialize<JsonElement>(Convert.FromBase64String(govde));
    }
}
