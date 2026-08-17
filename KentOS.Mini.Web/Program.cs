using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Services;
using KentOS.Mini.Web.Data;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.AspNetCore.Identity;
using KentOS.Mini.Application.Models;
using Mapster;
using System.Security.Claims;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.AuthPolicies;
using System.Globalization;
using FirebaseAdmin;
using KentOS.Mini.Web.Filters;
using KentOS.Mini.Web.Middleware;
using FluentValidation;
using KentOS.Mini.Web.Services.V2;
using KentOS.Mini.Web.Validation.V2;
using KentOS.Mini.Web.Configuration;
using KentOS.Mini.Web.Options;
using KentOS.Mini.Web.Storage;

// ═══════════════════════════════════════════════════════════════════════════
//  `.env` — HER ŞEYDEN ÖNCE
// ═══════════════════════════════════════════════════════════════════════════
//  Kuruma özel ne varsa (ad, alan adı, renk, SMS hesabı, veritabanı, depolama)
//  `.env` dosyasından gelir. Amaç tek cümle: uygulamayı başka bir belediyeye
//  verdiğimizde YALNIZCA bu dosyayı değiştirerek ayağa kaldırabilmek.
//
//  Biçim .NET'in kendi kuralı: `Bolum__Alt=deger` → `Bolum:Alt` yapılandırma
//  anahtarı. Böylece aynı ayar hem `.env` ile hem de IIS/Docker ortam
//  değişkeniyle verilebilir; okuma yolu tek.
//
//  SIRA: `CreateBuilder` ortam değişkenlerini okuyor, bu yüzden yükleme ondan
//  ÖNCE olmak zorunda. Sonra çağrılsaydı dosya sessizce etkisiz kalırdı.
var envDosyasi = EnvironmentFile.Load(Directory.GetCurrentDirectory())
                 ?? EnvironmentFile.Load(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

if (envDosyasi is not null)
{
    Console.WriteLine($"Yapılandırma dosyası yüklendi: {envDosyasi}");
}
else
{
    Console.WriteLine(
        "BİLGİ: .env bulunamadı; ayarlar appsettings.json ve ortam değişkenlerinden okunacak. " +
        "Örnek dosya: .env.example");
}

builder.Services.AddApplicationOptions(builder.Configuration);

// ProblemDetails "type" bağlantılarının tabanı — koda alan adı yazılmaz.
KentOS.Mini.Web.Filters.HataTurleri.Kur(
    OptionsRegistration.Read<ApplicationOptions>(
        builder.Configuration, ApplicationOptions.SectionName).BaseUrl);

// Ayarların bir kısmına builder kurulurken (DI'dan önce) ihtiyaç var.
var jwtAyari = OptionsRegistration.Read<JwtOptions>(builder.Configuration, JwtOptions.SectionName);
var kurumAyari = OptionsRegistration.Read<InstitutionOptions>(builder.Configuration, InstitutionOptions.SectionName);
var uygulamaAyari = OptionsRegistration.Read<ApplicationOptions>(builder.Configuration, ApplicationOptions.SectionName);
var firebaseAyari = OptionsRegistration.Read<FirebaseOptions>(builder.Configuration, FirebaseOptions.SectionName);

if (string.IsNullOrWhiteSpace(jwtAyari.Secret))
{
    // Sessizce boş anahtarla açılmak, giriş yapan herkesin doğrulanamayan
    // jeton almasına yol açardı — açılışta durmak daha ucuz.
    throw new InvalidOperationException(
        "JWT imza anahtarı tanımsız. .env dosyasına JWT__SECRET yazın (en az 32 karakter).");
}

// Configure Turkish culture support
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "tr-TR", "en-US" };
    options.SetDefaultCulture("tr-TR")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});

// Configure URL encoding to handle Turkish characters properly
builder.Services.Configure<RouteOptions>(options =>
{
    options.AppendTrailingSlash = false;
    options.LowercaseUrls = false; // Important: Keep URLs case-sensitive for Turkish characters
    options.LowercaseQueryStrings = false;
});

// Add services to the container.
// MVC GÖRÜNÜMLERİ YOK: eski arayüz kaldırıldı, uygulama tek sayfa uygulaması
// + API'den ibaret. `AddControllersWithViews` Razor motorunu ve view arama
// altyapısını da kuruyordu; hiçbiri kullanılmıyor.
builder.Services.AddControllers(opt =>
{
    opt.Filters.Add<EntityNotFoundExceptionFilter>();
}).AddJsonOptions(opt =>
{
    opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});


// Bağlantı havuzu SINIRLI: PostgreSQL örneği paylaşımlı ve Npgsql'in
// varsayılanı (100) sunucunun tüm yuvalarını tek başına kaplayabiliyor.
// Ayrıntı ve ölçüm: Data/BaglantiAyari.cs.
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(BaglantiAyari.Tamamla(builder.Configuration["ConnectionStrings:DefaultConnection"]))
        .UseSnakeCaseNamingConvention();
});

//builder.Services.AddDatabaseDeveloperPageExceptionFilter();


builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    // Brute-force koruması: 10 hatalı denemeden sonra hesabı 5 dk kilitle.
    // Hesap bazlı kilitleme, paylaşımlı IP'lerde (belediye içi ağ) meşru
    // kullanıcıları etkilemez — IP rate-limit'in aksine güvenli tercih.
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 10;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    // NOT: Parola kuralları CANLI kullanıcı/seed akışını bozmamak için değiştirilmedi.
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();


//builder.Services.AddControllers();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
        .AddCookie(options =>
        {
            options.Cookie.IsEssential = true;
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
            options.SlidingExpiration = true;
        })
        .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Sıfır tolerans, sunucu/istemci saat sapmasına aşırı hassastı
                    // (aralıklı "token süresi doldu"/giriş sorunları). 2 dk tolerans.
                    ClockSkew = TimeSpan.FromMinutes(2),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtAyari.ValidIssuer,
                    ValidAudience = jwtAyari.ValidAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAyari.Secret))
                };
            });

//add claims for ajanda only Admin,Sekreter,Yonetici
builder.Services.AddDefaultPolicies();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    // Künye KURUMDAN gelir; koda kurum adı yazılmaz. Adres alanları geçersiz
    // ya da boşsa Swagger üretimi patlıyordu — bu yüzden `Uri` dönüşümü
    // toleranslı yapılıyor.
    static Uri? AdresCoz(string? deger) =>
        Uri.TryCreate(deger, UriKind.Absolute, out var u) ? u : null;

    opt.SwaggerDoc("v1", new()
    {
        Title = $"{uygulamaAyari.Name} API",
        Version = "v1",
        Contact = new OpenApiContact
        {
            Email = string.IsNullOrWhiteSpace(uygulamaAyari.SupportEmail)
                ? kurumAyari.Email
                : uygulamaAyari.SupportEmail,
            Name = kurumAyari.Name,
            Url = AdresCoz(uygulamaAyari.SupportUrl) ?? AdresCoz(kurumAyari.Website)
        },
        Description = "Mobil uygulamanın sözleşmesi. Rota, alan adı ve dönüş şekli DEĞİŞTİRİLMEZ.",
        TermsOfService = AdresCoz(kurumAyari.Website)
    });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Token ile yetkilendirme. Örnek: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    opt.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

    // İki ayrı döküman: v1 mobil uygulamanın sözleşmesi, v2 yeni web arayüzünün.
    // SPA'nın TypeScript tipleri v2 dökümanından üretiliyor.
    opt.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = $"{uygulamaAyari.Name} API v2 (web arayüzü)",
        Version = "v2",
        Description = "Yeni tek sayfa uygulamasının kullandığı API. v1 mobil " +
                      "uygulamanın sözleşmesidir ve değiştirilmez."
    });
    opt.DocInclusionPredicate((dokuman, api) =>
    {
        var v2Mi = api.RelativePath?.StartsWith("api/v2", StringComparison.OrdinalIgnoreCase) == true;
        return dokuman == "v2" ? v2Mi : !v2Mi;
    });
});

// Firebase yönetici SDK'sı.
//
// ÖNCEDEN koşulsuzdu ve iki şekilde patlıyordu: (1) kimlik dosyası yoksa
// uygulama HİÇ açılmıyordu, (2) aynı süreçte ikinci kez çağrılınca fırlıyordu —
// bu yüzden `WebApplicationFactory` ile entegrasyon/sözleşme testi yazmak
// imkânsızdı. Artık bir kez ve dosya varsa kuruluyor; yoksa uyarı loglanıyor
// ve bildirim gönderimi devre dışı kalıyor (uygulama ayakta kalır).
//
// Dosyanın ADI DA kuruma özel: `FIREBASE__CREDENTIALSPATH` ile veriliyor.
// Göreli yol verilirse önce uygulama kökü, sonra çalışma dizini denenir.
var firebaseKimlikYolu = FirebaseKimligiBul(firebaseAyari.CredentialsPath);

static string? FirebaseKimligiBul(string ayar)
{
    if (string.IsNullOrWhiteSpace(ayar)) return null;
    if (Path.IsPathRooted(ayar)) return ayar;

    foreach (var kok in new[] { AppDomain.CurrentDomain.BaseDirectory, Directory.GetCurrentDirectory() })
    {
        var aday = Path.Combine(kok, ayar);
        if (File.Exists(aday)) return aday;
    }

    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ayar);
}

if (FirebaseApp.DefaultInstance is null)
{
    if (firebaseKimlikYolu is not null && File.Exists(firebaseKimlikYolu))
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(firebaseKimlikYolu)
        });
    }
    else
    {
        Console.Error.WriteLine(
            "UYARI: Firebase kimlik dosyası bulunamadı " +
            $"({firebaseKimlikYolu ?? "FIREBASE__CREDENTIALSPATH tanımsız"}). " +
            "Push bildirimleri gönderilmeyecek.");
    }
}

// Mapster (AutoMapper yerine — MIT lisanslı, ücretsiz, güvenlik açığı yok).
// Eşlemeler tamamen isim-konvansiyonu bazlı olduğu için ekstra CreateMap gerekmez;
// aynı global config hem in-memory Map hem de ProjectToType için kullanılır.
var mapsterConfig = TypeAdapterConfig.GlobalSettings;
// Döngüsel navigasyonlar (Ajanda ⇄ AjandaNot/Cicek/AjandaPhoto) Mapster'da
// sonsuz özyinelemeye ve süreç çökmesine yol açıyordu — bkz. MapsterConfig.
KentOS.Mini.Web.Mapping.MapsterConfig.Register(mapsterConfig);
builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<MapsterMapper.IMapper, MapsterMapper.ServiceMapper>();
builder.Services.AddMemoryCache();

// İzin çözümü: jetona GİRMEZ, her istekte okunur (kullanıcı başına 5 dk
// önbellekli). JWT 900 dakika ve iptal listesi yok — izne jetonda taşımak,
// geri alınan bir yetkinin 15 saat daha çalışması demekti.
builder.Services.AddScoped<IIzinServisi, IzinServisi>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAjandaService, AjandaService>();
// Tekrarlanan etkinlikler (RRULE serileri). Tekrarlar gerçek etkinlik satırı
// olarak üretildiği için mevcut listeleme/arama/takvim yolları değişmez.
builder.Services.AddScoped<IAjandaSeriService, AjandaSeriService>();
// Salt-okunur istatistik servisi (mobil arşiv grafikleri). Ayrı uç; mevcut
// akışlara dokunmaz, yazma/bildirim yapmaz.
builder.Services.AddScoped<IAjandaIstatistikService, AjandaIstatistikService>();
builder.Services.AddScoped<ITalepIstatistikServisi, TalepIstatistikServisi>();
// Etkinlik zaman çizelgesi. Yazma hataları yutulur; mevcut akışları etkilemez.
builder.Services.AddScoped<IAjandaOlayService, AjandaOlayService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IBirimService, BirimService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRandevuService, RandevuService>();
builder.Services.AddHttpClient("sms"); // SMSService için havuzlanmış HttpClient
builder.Services.AddSingleton<ISMSService, SMSService>();
builder.Services.AddScoped<ICicekciService, CicekciService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IOneriService, OneriService>();
builder.Services.AddScoped<IAnalizService, AnalizService>();
// ---------------------------------------------------------------- /api/v2
// v2, v1'in YANINA eklenir; v1'in davranışına dokunulmaz. Filtreler global
// değil, yalnızca V2ControllerBase üzerinden takılır.
// Kurum bilgisi VERİTABANINDAN okunur (tek satır), `.env` yalnızca ilk
// tohumlamayı yapar. Böylece kurum adı/amblem/renk arayüzden düzenlenebilir.
builder.Services.AddScoped<IInstitutionService, InstitutionService>();
// İŞ TAKİP — ortak altyapı.
//
// `IBirimAgaci` özyinelemeli CTE ile alt ağacı TEK sorguda okur (eski
// `GetDescendants` her seviye için ayrı sorgu atıyor). `IEtkinBirim`
// "alt birim adına iş yapma" vekâletini çözer ve başlığı doğrular.
builder.Services.AddScoped<IBirimAgaci, BirimAgaci>();
builder.Services.AddScoped<IEtkinBirim, EtkinBirim>();
builder.Services.AddScoped<IIsEkServisi, IsEkServisi>();
builder.Services.AddScoped<IIsYorumServisi, IsYorumServisi>();
builder.Services.AddScoped<IIsOlayServisi, IsOlayServisi>();

// İŞ TAKİP — görev çekirdeği.
builder.Services.AddScoped<IGorevTipiServisi, GorevTipiServisi>();
builder.Services.AddScoped<IEkipServisi, EkipServisi>();
builder.Services.AddScoped<IGorevServisi, GorevServisi>();
builder.Services.AddScoped<IProjeServisi, ProjeServisi>();

builder.Services.AddScoped<IHataKaydiServisi, HataKaydiServisi>();
builder.Services.AddScoped<IOturumServisi, OturumServisi>();
builder.Services.AddScoped<IWebBildirimServisi, WebBildirimServisi>();
builder.Services.AddScoped<ITakvimSorguServisi, TakvimSorguServisi>();
builder.Services.AddScoped<IYonetimServisi, YonetimServisi>();
builder.Services.AddScoped<IReferansServisi, ReferansServisi>();
builder.Services.AddScoped<ITalepSorguServisi, TalepSorguServisi>();
builder.Services.AddScoped<IDosyaServisi, DosyaServisi>();
builder.Services.AddScoped<IDisaAktarmaServisi, DisaAktarmaServisi>();
builder.Services.AddScoped<IHaritaServisi, HaritaServisi>();
builder.Services.AddScoped<IBildirimMerkeziServisi, BildirimMerkeziServisi>();
builder.Services.AddScoped<IOturumKaydiServisi, OturumKaydiServisi>();
builder.Services.AddScoped<IProtokolServisi, ProtokolServisi>();
builder.Services.AddScoped<IDavetServisi, DavetServisi>();
builder.Services.AddScoped<IHalkGunuServisi, HalkGunuServisi>();
builder.Services.AddScoped<IHalkGunuIslemServisi, HalkGunuIslemServisi>();
builder.Services.AddScoped<IHalkGunuCiktiServisi, HalkGunuCiktiServisi>();
builder.Services.AddScoped<ICicekciDetayServisi, CicekciDetayServisi>();
builder.Services.AddScoped<IIsimKartiServisi, IsimKartiServisi>();
builder.Services.AddScoped<IOzgecmisServisi, OzgecmisServisi>();
builder.Services.AddScoped<IDavetCiktiServisi, DavetCiktiServisi>();
builder.Services.AddScoped<IDosyaGonderimiServisi, DosyaGonderimiServisi>();
builder.Services.AddScoped<V2HataFiltresi>();
builder.Services.AddScoped<V2DogrulamaFiltresi>();
builder.Services.AddValidatorsFromAssemblyContaining<GirisIstegiDogrulayici>(ServiceLifetime.Scoped);

// Dosya deposu: STORAGE__PROVIDER=Local (varsayılan, mevcut davranış) ya da
// STORAGE__PROVIDER=S3 (MinIO/AWS S3 uyumlu nesne deposu).
builder.Services.AddFileStorage(builder.Configuration);

builder.Services.AddHostedService<FirebaseWorker>();
// Sonsuz tekrar serilerinin üretim ufkunu günde bir ileriye taşır.
builder.Services.AddHostedService<TekrarUfkuWorker>();

// Reverse-proxy / IIS arkasında gerçek istemci IP'si ve https scheme'i doğru
// algılansın (aksi halde HTTPS yönlendirmesi ve uzak IP yanlış olabilir).
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Kart fontları QuestPDF'e AÇILIŞTA kaydedilir: ilk PDF isteği sırasında
// kaydetmek, eşzamanlı iki istekte yarışa açık bir başlangıç demekti.
KentOS.Mini.Web.Services.V2.KartTasarimlari.FontlariKaydet(
    AppContext.BaseDirectory);

var app = builder.Build();

// ---------------------------------------------------------------------------
//  Yazılabilir dizinler — AÇILIŞTA denetlenir.
//
//  IIS'te uygulama havuzu kimliğinin (örn. `IIS AppPool\WorkCollab`) yayın
//  klasörüne yazma izni varsayılan olarak YOKTUR. İzin eksikse hata, ilk dosya
//  yüklemeye çalışan kullanıcıda ve "500" olarak ortaya çıkıyordu; sebebi de
//  günlüklere bakmadan anlaşılmıyordu. Açılışta bir kez deneyip UYARI yazmak,
//  sorunu yayın anında görünür kılıyor.
//
//  Uygulama BAŞLATILIR: yazma izni olmayan bir kurulumda okuma işlevlerinin
//  tamamı çalışmaya devam etmeli.
//
//  NESNE DEPOSU seçiliyse bu denetim atlanır ve onun yerine kovanın varlığı
//  denetlenir — dosyalar diske hiç yazılmıyor.
// ---------------------------------------------------------------------------
{
    var baslangicGunlugu = app.Services.GetRequiredService<ILogger<Program>>();
    var depoAyari = app.Services.GetRequiredService<StorageOptions>();

    await StorageRegistration.EnsureBucketAsync(app.Services, baslangicGunlugu);

    var yazilabilirDizinler = depoAyari.UsesObjectStorage
        ? []
        : new[]
        {
            Path.Combine(
                LocalFileStorage.ResolveWebRoot(app.Environment), depoAyari.UploadPath),
            LocalFileStorage.ResolvePrivateRoot(app.Environment, depoAyari),
        }.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    foreach (var dizin in yazilabilirDizinler)
    {
        try
        {
            Directory.CreateDirectory(dizin);

            // Var olmak yetmez, YAZILABİLİR olmalı: yalnızca `Exists` denetimi
            // salt okunur bir paylaşımda da başarılı olurdu.
            var deneme = Path.Combine(dizin, $".yazma-denemesi-{Guid.NewGuid():N}");
            await File.WriteAllTextAsync(deneme, "1");
            File.Delete(deneme);

            baslangicGunlugu.LogInformation("Yazılabilir dizin hazır: {Dizin}", dizin);
        }
        catch (Exception ex)
        {
            baslangicGunlugu.LogError(ex,
                "DİZİNE YAZILAMIYOR: {Dizin}. Dosya yükleme ve gönderimi çalışmayacak. " +
                "IIS'te uygulama havuzu kimliğine bu klasör için 'Değiştir' izni verin, " +
                "STORAGE__SENDDIRECTORY ile yazılabilir bir yol gösterin " +
                "ya da STORAGE__PROVIDER=S3 ile nesne deposuna geçin.",
                dizin);
        }
    }
}



// ---------------------------------------------------------------------------
//  Veritabanı: bekleyen migration'ları uygulama açılışında OTOMATİK uygula.
//  Seeder'dan ÖNCE çalışmalı — seeder şemanın hazır olmasını varsayar.
//
//  • Kapatmak için appsettings: "Database:AutoMigrate": false
//  • Sunucu yeniden başlarken PostgreSQL henüz ayakta olmayabilir; geçici
//    bağlantı hataları için sınırlı sayıda yeniden deneme yapılır.
//  • Migration başarısız olursa uygulama BAŞLATILMAZ (fail-fast). Yarım şemayla
//    çalışmak, hatayı sessizce üretime taşımaktan daha risklidir.
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    var autoMigrate = serviceProvider.GetRequiredService<DatabaseOptions>().AutoMigrate;

    if (autoMigrate)
    {
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        const int maxDeneme = 5;

        for (var deneme = 1; deneme <= maxDeneme; deneme++)
        {
            try
            {
                var bekleyenler = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
                if (bekleyenler.Count == 0)
                {
                    logger.LogInformation("Veritabanı güncel — uygulanacak migration yok.");
                }
                else
                {
                    logger.LogWarning("{Adet} migration uygulanıyor: {Liste}",
                        bekleyenler.Count, string.Join(", ", bekleyenler));
                    await dbContext.Database.MigrateAsync();
                    logger.LogInformation("Migration'lar başarıyla uygulandı.");
                }
                break;
            }
            catch (Exception ex) when (deneme < maxDeneme)
            {
                var bekleme = TimeSpan.FromSeconds(deneme * 3);
                logger.LogWarning(ex,
                    "Migration denemesi {Deneme}/{Max} başarısız. {Saniye} sn sonra tekrar denenecek.",
                    deneme, maxDeneme, bekleme.TotalSeconds);
                await Task.Delay(bekleme);
            }
        }
    }
    else
    {
        logger.LogInformation("Database:AutoMigrate = false — migration atlandı.");
    }

    await DataSeeder.EnsureInitialData(serviceProvider);

    // Geliştirme verisi (birimler, örnek kullanıcılar, tekrar eden ve gizli
    // etkinlikler, talepler). ÜRETİMDE ÇALIŞMAZ — `DataSeeder`'dan ayrı
    // tutulmasının sebebi budur: o koşulsuz çalışıyor ve buraya konulan her
    // kayıt canlı veritabanına düşerdi.
    if (app.Environment.IsDevelopment())
    {
        await GelistirmeTohumu.UygulaAsync(serviceProvider, logger);
    }
}


// Proxy header'ları pipeline'ın başında değerlendirilmeli.
app.UseForwardedHeaders();

// Temel güvenlik header'ları (CSP, mevcut MVC görünümlerindeki inline script'leri
// bozabileceğinden bilinçli olarak eklenmedi — ayrı bir adımda değerlendirilecek).
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // `/Home/Error` MVC görünümüne gidiyordu; o katman kaldırıldı.
    // API hataları zaten `V2HataFiltresi` üzerinden RFC 7807 olarak dönüyor;
    // buradaki işleyici yalnızca ardışık düzenin geri kalanı için.
    app.UseExceptionHandler("/hata");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    // İki döküman: v1 (mobil sözleşmesi) ve v2 (yeni web arayüzü).
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1 — mobil");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "v2 — web");
});
app.UseHttpsRedirection();

// SIRA ÖNEMLİ: gönderilen belgeler wwwroot altında duruyor (izin sebebiyle);
// statik dosya ara katmanı onları kimlik doğrulamadan servis etmeden ÖNCE
// kapatılır. Bkz. Middleware/GonderimDosyaKorumasi.cs.
// Hata kaydının İSTEK GÖVDESİNİ okuyabilmesi için akış tamponlanır.
// Tamponsuz akış bir kez okunduğunda tükeniyor ve hata anında gövde
// alınamıyordu — oysa "hangi veriyle patladı" teşhisin yarısı.
app.Use(async (baglam, sonraki) =>
{
    if (baglam.Request.Path.StartsWithSegments("/api"))
    {
        baglam.Request.EnableBuffering();
    }
    await sonraki();
});

app.UseGonderimDosyaKorumasi();
// Nesne deposu seçiliyse eski `/uploads/...` adresleri buradan karşılanır —
// yayındaki mobil uygulama o adresleri kullanıyor. Yerel depoda hiç devreye
// girmez.
app.UseUzakDepoKopruSu();
app.UseStaticFiles();
app.UseCookiePolicy();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("tr"),
    SupportedCultures = new List<CultureInfo> { new CultureInfo("tr") },
    SupportedUICultures = new List<CultureInfo> { new CultureInfo("tr") }
});

// KÖK ARTIK YENİ UYGULAMAYA GİDER.
//
// ══════════════════════════════════════════════════════════════════════════
//  TEK ARAYÜZ: SPA ARTIK KÖKTE
// ══════════════════════════════════════════════════════════════════════════
//  Uygulama `/yeni` altında yaşıyordu ve `/` oraya yönlendiriliyordu. Artık
//  kök adresin KENDİSİ uygulama.
//
//  ESKİ MVC YÖNLENDİRMESİ KAPATILDI. Sebep teknik: iki rota SPA ekranlarıyla
//  birebir çakışıyordu — `/ajanda` MVC'nin `AjandaController`'ına, `/cicek`
//  `CicekController`'a düşüyordu. Varsayılan MVC rotası (`{controller}/...`)
//  kökten çalıştığı için SPA'nın o iki ekranı hiç açılamazdı.
//
//  DOSYALAR SİLİNMEDİ: `Controllers/`, `Views/` ve alanlar yerinde duruyor;
//  yalnızca uç noktaya bağlanmıyorlar. Geri almak, aşağıdaki blokları yeniden
//  etkinleştirmekten ibaret.
//
//  `MapControllers()` yukarıda duruyor ve DEĞİŞMEDİ: `/api/*` (v1 mobil
//  sözleşmesi dahil) öznitelik yönlendirmesiyle çalışıyor, bu bloktan
//  etkilenmiyor.
//
//  ── Eski MVC rotaları (devre dışı) ────────────────────────────────────
//  app.MapAreaControllerRoute("Baskan",  "Baskan",  "Baskan/{controller=Baskan}/{action=Index}/{id?}");
//  app.MapAreaControllerRoute("Sibeski", "Sibeski", "Sibeski/{controller=Home}/{action=Index}/{id?}");
//  app.MapAreaControllerRoute("System",  "System",  "System/{controller=Home}/{action=Index}/{id?}");
//  app.MapControllerRoute("default", "{controller=Modules}/{action=Index}/{id?}");

// Eski derin bağlantılar ve KURULU PWA'lar bir süre `/yeni/...` istemeye
// devam edecek (ana ekran kısayolu, yer imi, etkin service worker). Onları
// köke taşıyoruz; 302 çünkü 301 tarayıcıda süresiz önbelleğe alınıyor ve
// geri dönmek gerekirse kullanıcıların tarayıcısını temizletmek gerekirdi.
app.MapGet("/yeni/{*yol}", (string? yol) =>
        Results.Redirect("/" + (yol ?? string.Empty), permanent: false))
   .ExcludeFromDescription();

// SPA derin bağlantıları: uzantısız her istek index.html'e düşer.
//
// `:nonfile` kısıtı şart: onsuz `/uygulama/index-abc.js` ve `/uploads/x.pdf`
// gibi GERÇEK dosya istekleri de index.html alırdı. Uzantılı istekler statik
// dosya ara katmanına gider.
//
// `/api/*` bu kalıba girmiyor çünkü `MapControllers()` onları zaten
// eşleştiriyor ve fallback yalnızca EŞLEŞMEYEN isteklerde çalışır.
app.MapFallbackToFile("{*yol:nonfile}", "/index.html");

app.Run();
