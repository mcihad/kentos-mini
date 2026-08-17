using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Options;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Web.Services.V2;

// ══════════════════════════════════════════════════════════════════ DTO'lar

/// <summary>
/// İstemcilere verilen kurum bilgisi.
/// </summary>
/// <remarks>
/// <para>
/// <b>Anonim erişilebilir.</b> Giriş ekranının da amblemi, adı ve rengi
/// göstermesi gerekiyor; oturum açılmadan önce okunabilmeli. İçinde gizli
/// hiçbir şey yok: hepsi zaten sayfanın görünen yüzü.
/// </para>
/// <para>
/// Firebase alanları da burada. Gizli değiller (tarayıcıya nasıl olsa
/// iniyorlar) ama KURUMA ÖZELLER; SPA'nın derlemesine gömülseydi her kurum
/// için ayrı bir ön yüz derlemesi gerekirdi.
/// </para>
/// </remarks>
public class KurumBilgisiDto
{
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("kisaAd")] public string KisaAd { get; set; } = string.Empty;
    [JsonPropertyName("gorunenAd")] public string GorunenAd { get; set; } = string.Empty;
    [JsonPropertyName("birim")] public string? Birim { get; set; }
    [JsonPropertyName("kunye")] public string? Kunye { get; set; }

    [JsonPropertyName("webSitesi")] public string? WebSitesi { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("eposta")] public string? Eposta { get; set; }

    [JsonPropertyName("uygulamaAdi")] public string UygulamaAdi { get; set; } = string.Empty;
    [JsonPropertyName("uygulamaKisaAdi")] public string UygulamaKisaAdi { get; set; } = string.Empty;
    [JsonPropertyName("uygulamaAciklamasi")] public string? UygulamaAciklamasi { get; set; }

    [JsonPropertyName("marka")] public MarkaDto Marka { get; set; } = new();
    [JsonPropertyName("bildirim")] public BildirimYapilandirmasiDto? Bildirim { get; set; }

    /// <summary>
    /// Vatandaş şikayet portalı açık mı.
    /// </summary>
    /// <remarks>
    /// Bu DTO anonim okunabiliyor ve bayrak da anonim görünüyor — çünkü
    /// zaten gözlenebilir: portal adresine giren herkes açık mı kapalı mı
    /// olduğunu bir saniyede öğrenir. Gizlemeye çalışmak, SPA'nın kapalı
    /// portalı düzgün bir "şu an kapalı" ekranıyla karşılamasını engellemekten
    /// başka bir işe yaramazdı.
    /// </remarks>
    [JsonPropertyName("vatandasBildirimi")] public bool VatandasBildirimi { get; set; }
}

/// <summary>Kurumsal kimlik çekirdeği — SPA tonları bunlardan türetir.</summary>
public class MarkaDto
{
    [JsonPropertyName("birincil")] public string? Birincil { get; set; }
    [JsonPropertyName("vurgu")] public string? Vurgu { get; set; }
    [JsonPropertyName("notr")] public string? Notr { get; set; }
    [JsonPropertyName("birincilKoyu")] public string? BirincilKoyu { get; set; }
    [JsonPropertyName("amblem")] public string? Amblem { get; set; }
    [JsonPropertyName("favicon")] public string? Favicon { get; set; }
    [JsonPropertyName("uygulamaIkonu")] public string? UygulamaIkonu { get; set; }
}

/// <summary>
/// Web push için tarayıcıya gereken Firebase alanları. Yapılandırma eksikse
/// <c>null</c> döner ve SPA bildirim kurulumunu hiç denemez.
/// </summary>
public class BildirimYapilandirmasiDto
{
    [JsonPropertyName("apiKey")] public string ApiKey { get; set; } = string.Empty;
    [JsonPropertyName("authDomain")] public string AuthDomain { get; set; } = string.Empty;
    [JsonPropertyName("projectId")] public string ProjectId { get; set; } = string.Empty;
    [JsonPropertyName("storageBucket")] public string StorageBucket { get; set; } = string.Empty;
    [JsonPropertyName("messagingSenderId")] public string MessagingSenderId { get; set; } = string.Empty;
    [JsonPropertyName("appId")] public string AppId { get; set; } = string.Empty;
    [JsonPropertyName("vapidPublicKey")] public string VapidPublicKey { get; set; } = string.Empty;
}

/// <summary>Kurum bilgisi düzenleme isteği.</summary>
public class KurumGuncellemeIstegi
{
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("kisaAd")] public string? KisaAd { get; set; }
    [JsonPropertyName("gorunenAd")] public string? GorunenAd { get; set; }
    [JsonPropertyName("birim")] public string? Birim { get; set; }
    [JsonPropertyName("kunye")] public string? Kunye { get; set; }

    [JsonPropertyName("webSitesi")] public string? WebSitesi { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("eposta")] public string? Eposta { get; set; }

    [JsonPropertyName("uygulamaAdi")] public string? UygulamaAdi { get; set; }
    [JsonPropertyName("uygulamaKisaAdi")] public string? UygulamaKisaAdi { get; set; }
    [JsonPropertyName("uygulamaAciklamasi")] public string? UygulamaAciklamasi { get; set; }

    [JsonPropertyName("markaBirincil")] public string? MarkaBirincil { get; set; }
    [JsonPropertyName("markaVurgu")] public string? MarkaVurgu { get; set; }
    [JsonPropertyName("markaNotr")] public string? MarkaNotr { get; set; }
    [JsonPropertyName("markaBirincilKoyu")] public string? MarkaBirincilKoyu { get; set; }

    [JsonPropertyName("amblem")] public string? Amblem { get; set; }
    [JsonPropertyName("favicon")] public string? Favicon { get; set; }
    [JsonPropertyName("uygulamaIkonu")] public string? UygulamaIkonu { get; set; }
    [JsonPropertyName("ciktiAmblemi")] public string? CiktiAmblemi { get; set; }

    /// <summary>Vatandaş şikayet portalını aç/kapat.</summary>
    [JsonPropertyName("vatandasBildirimi")] public bool VatandasBildirimi { get; set; }
}

// ═══════════════════════════════════════════════════════════════════ servis

public interface IInstitutionService
{
    /// <summary>Kurum kaydını verir; yoksa <c>.env</c> değerlerinden oluşturur.</summary>
    Task<Institution> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>İstemcilere verilecek biçim.</summary>
    Task<KurumBilgisiDto> GetPublicAsync(CancellationToken cancellationToken = default);

    /// <summary>Kurum bilgisini günceller ve önbelleği düşürür.</summary>
    Task<KurumBilgisiDto> UpdateAsync(KurumGuncellemeIstegi istek, CancellationToken cancellationToken = default);
}

/// <summary>
/// Kurum bilgilerini veritabanından okur ve önbelleğe alır.
/// </summary>
/// <remarks>
/// <para>
/// <b>Önbellek şart:</b> bu kayıt her sayfa açılışında, her PDF üretiminde ve
/// giriş ekranında okunuyor. Önbelleksiz, sistemin en sık çalışan sorgusu
/// olurdu. Yazma işlemi önbelleği hemen düşürür, dolayısıyla eskimiş veri
/// görünmez.
/// </para>
/// <para>
/// <b>Tohumlama okuma sırasında yapılır</b>, ayrı bir seeder adımında değil:
/// tablo boşken ilk okuyan satırı açar. Böylece hem sıfırdan kurulum hem de
/// var olan bir veritabanına yapılan yükseltme aynı yoldan geçer.
/// </para>
/// </remarks>
public class InstitutionService(
    AppDbContext _context,
    IMemoryCache _onbellek,
    ICurrentUserService _mevcutKullanici,
    InstitutionOptions _kurumAyari,
    BrandOptions _markaAyari,
    ApplicationOptions _uygulamaAyari,
    FirebaseOptions _firebaseAyari,
    ILogger<InstitutionService> _logger) : IInstitutionService
{
    /// <summary>Önbellek anahtarı.</summary>
    public const string OnbellekAnahtari = "KurumBilgisi";

    public async Task<Institution> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_onbellek.TryGetValue(OnbellekAnahtari, out Institution? onbellekli) && onbellekli is not null)
        {
            return onbellekli;
        }

        var kayit = await _context.KurumBilgileri
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == Institution.TekilId, cancellationToken);

        kayit ??= await TohumlaAsync(cancellationToken);

        _onbellek.Set(OnbellekAnahtari, kayit, TimeSpan.FromMinutes(30));
        return kayit;
    }

    public async Task<KurumBilgisiDto> GetPublicAsync(CancellationToken cancellationToken = default) =>
        Cevir(await GetAsync(cancellationToken));

    public async Task<KurumBilgisiDto> UpdateAsync(
        KurumGuncellemeIstegi istek, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(istek.Ad))
        {
            throw new Exceptions.BusinessRuleException("Kurum adı boş olamaz.");
        }

        var kayit = await _context.KurumBilgileri
            .FirstOrDefaultAsync(k => k.Id == Institution.TekilId, cancellationToken);

        if (kayit is null)
        {
            kayit = new Institution { Id = Institution.TekilId };
            _context.KurumBilgileri.Add(kayit);
        }

        kayit.Name = istek.Ad.Trim();
        kayit.ShortName = Kirp(istek.KisaAd);
        kayit.DisplayName = Kirp(istek.GorunenAd);
        kayit.Department = Kirp(istek.Birim);
        kayit.FooterNote = Kirp(istek.Kunye);

        kayit.Website = Kirp(istek.WebSitesi);
        kayit.Address = Kirp(istek.Adres);
        kayit.Phone = Kirp(istek.Telefon);
        kayit.Email = Kirp(istek.Eposta);

        kayit.ApplicationName = Kirp(istek.UygulamaAdi);
        kayit.ApplicationShortName = Kirp(istek.UygulamaKisaAdi);
        kayit.ApplicationDescription = Kirp(istek.UygulamaAciklamasi);

        kayit.BrandPrimary = Kirp(istek.MarkaBirincil);
        kayit.BrandAccent = Kirp(istek.MarkaVurgu);
        kayit.BrandNeutral = Kirp(istek.MarkaNotr);
        kayit.BrandPrimaryDark = Kirp(istek.MarkaBirincilKoyu);

        kayit.Logo = Kirp(istek.Amblem);
        kayit.Favicon = Kirp(istek.Favicon);
        kayit.AppIcon = Kirp(istek.UygulamaIkonu);
        kayit.PrintLogo = Kirp(istek.CiktiAmblemi);

        /*
          PORTALIN AÇILIP KAPANMASI KAYDA GEÇİYOR.

          Diğer alanlar bir görünüm tercihi; bu bayrak bir GÜVENLİK kararı ve
          "kim ne zaman açtı" sorusunun sonradan sorulacağı tek alan.
          `Guncelleyen`/`GuncellemeTarihi` bunu ancak son değişiklik için
          söylüyor, bu yüzden ayrıca loglanıyor.
        */
        if (kayit.CitizenReportEnabled != istek.VatandasBildirimi)
        {
            _logger.LogWarning(
                "Vatandaş bildirim portalı {Durum}: {Kullanici}",
                istek.VatandasBildirimi ? "AÇILDI" : "KAPATILDI",
                await _mevcutKullanici.GetFullNameAsync());
        }

        kayit.CitizenReportEnabled = istek.VatandasBildirimi;

        kayit.GuncellemeTarihi = DateTime.Now;
        kayit.Guncelleyen = await _mevcutKullanici.GetFullNameAsync();

        await _context.SaveChangesAsync(cancellationToken);

        // Önbellek HEMEN düşürülür: yönetici kaydettikten sonra sayfayı
        // yenilediğinde eski adı görmemeli.
        _onbellek.Remove(OnbellekAnahtari);

        _logger.LogInformation("Kurum bilgileri güncellendi: {Ad}", kayit.Name);
        return Cevir(kayit);
    }

    /// <summary>
    /// Tablo boşken ilk satırı <c>.env</c> değerlerinden açar.
    /// </summary>
    /// <remarks>
    /// Yazma başarısız olursa (salt okunur veritabanı, eşzamanlı ikinci örnek)
    /// kayıt KAYDEDİLMEDEN döndürülür — uygulama ayakta kalır ve kurum bilgisi
    /// yine de görünür. Bu durumda sonraki istek yeniden dener.
    /// </remarks>
    private async Task<Institution> TohumlaAsync(CancellationToken cancellationToken)
    {
        var kayit = new Institution
        {
            Id = Institution.TekilId,
            Name = _kurumAyari.Name,
            ShortName = Kirp(_kurumAyari.ShortName),
            DisplayName = Kirp(_kurumAyari.DisplayName),
            Department = Kirp(_kurumAyari.Department),
            FooterNote = Kirp(_kurumAyari.FooterNote),
            Website = Kirp(_kurumAyari.Website),
            Address = Kirp(_kurumAyari.Address),
            Phone = Kirp(_kurumAyari.Phone),
            Email = Kirp(_kurumAyari.Email),
            ApplicationName = Kirp(_uygulamaAyari.Name),
            ApplicationShortName = Kirp(_uygulamaAyari.ShortName),
            ApplicationDescription = Kirp(_uygulamaAyari.Description),
            BrandPrimary = Kirp(_markaAyari.Primary),
            BrandAccent = Kirp(_markaAyari.Accent),
            BrandNeutral = Kirp(_markaAyari.Neutral),
            BrandPrimaryDark = Kirp(_markaAyari.PrimaryDark),
            Logo = Kirp(_markaAyari.Logo),
            Favicon = Kirp(_markaAyari.Favicon),
            AppIcon = Kirp(_markaAyari.AppIcon),
            PrintLogo = Kirp(_markaAyari.PrintLogo),
        };

        try
        {
            _context.KurumBilgileri.Add(kayit);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogWarning(
                "Kurum bilgileri tablosu boştu; ilk kayıt .env değerlerinden oluşturuldu ({Ad}).",
                kayit.Name);
        }
        catch (Exception ex)
        {
            _context.Entry(kayit).State = EntityState.Detached;
            _logger.LogError(ex,
                "Kurum bilgisi kaydedilemedi; .env değerleri geçici olarak kullanılıyor.");
        }

        return kayit;
    }

    private KurumBilgisiDto Cevir(Institution k) => new()
    {
        Ad = k.Name,
        KisaAd = k.ResolvedShortName,
        GorunenAd = k.ResolvedDisplayName,
        Birim = k.Department,
        Kunye = k.FooterNote,
        WebSitesi = k.Website,
        Adres = k.Address,
        Telefon = k.Phone,
        Eposta = k.Email,
        UygulamaAdi = string.IsNullOrWhiteSpace(k.ApplicationName)
            ? _uygulamaAyari.Name
            : k.ApplicationName,
        UygulamaKisaAdi = string.IsNullOrWhiteSpace(k.ApplicationShortName)
            ? (string.IsNullOrWhiteSpace(k.ApplicationName) ? _uygulamaAyari.ResolvedShortName : k.ApplicationName)
            : k.ApplicationShortName,
        UygulamaAciklamasi = k.ApplicationDescription,
        VatandasBildirimi = k.CitizenReportEnabled,
        Marka = new MarkaDto
        {
            Birincil = k.BrandPrimary,
            Vurgu = k.BrandAccent,
            Notr = k.BrandNeutral,
            BirincilKoyu = k.BrandPrimaryDark,
            Amblem = k.Logo,
            Favicon = k.Favicon,
            UygulamaIkonu = k.AppIcon,
        },
        // Bildirim yapılandırması VERİTABANINDA DEĞİL: Firebase projesi
        // altyapıya ait ve kimlik dosyasıyla birlikte yürüyor. Arayüzden
        // düzenlenebilir olması da anlamsız — yanlış değer bildirimi tamamen
        // durdurur.
        Bildirim = _firebaseAyari.IsWebPushConfigured
            ? new BildirimYapilandirmasiDto
            {
                ApiKey = _firebaseAyari.ApiKey,
                AuthDomain = _firebaseAyari.AuthDomain,
                ProjectId = _firebaseAyari.ProjectId,
                StorageBucket = _firebaseAyari.StorageBucket,
                MessagingSenderId = _firebaseAyari.MessagingSenderId,
                AppId = _firebaseAyari.AppId,
                VapidPublicKey = _firebaseAyari.VapidPublicKey,
            }
            : null,
    };

    private static string? Kirp(string? deger) =>
        string.IsNullOrWhiteSpace(deger) ? null : deger.Trim();
}
