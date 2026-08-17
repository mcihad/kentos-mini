using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Storage;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>Gönderim listesi satırı.</summary>
public class GonderimOzetDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("konu")] public string Konu { get; set; } = string.Empty;
    [JsonPropertyName("dosyaAdi")] public string DosyaAdi { get; set; } = string.Empty;
    [JsonPropertyName("boyut")] public long Boyut { get; set; }
    [JsonPropertyName("tarih")] public DateTime Tarih { get; set; }

    [JsonPropertyName("gonderenId")] public long GonderenId { get; set; }
    [JsonPropertyName("gonderenAd")] public string? GonderenAd { get; set; }
    [JsonPropertyName("aliciId")] public long AliciId { get; set; }
    [JsonPropertyName("aliciAd")] public string? AliciAd { get; set; }

    /// <summary>Oturum sahibi gönderen mi (aksi hâlde alıcı).</summary>
    [JsonPropertyName("benGonderdim")] public bool BenGonderdim { get; set; }

    [JsonPropertyName("okundu")] public bool Okundu { get; set; }
    [JsonPropertyName("notSayisi")] public int NotSayisi { get; set; }

    /// <summary>Son notun özeti — listede bağlam verir.</summary>
    [JsonPropertyName("sonNot")] public string? SonNot { get; set; }
    [JsonPropertyName("sonNotTarihi")] public DateTime? SonNotTarihi { get; set; }
}

/// <summary>Gönderim detayı — dosya + yazışma.</summary>
/// <remarks>
/// Diskteki dosya adı DIŞARI VERİLMEZ: dosya yalnızca
/// <c>GET /api/v2/gonderim/{id}/dosya</c> üzerinden, görünürlük süzgecinden
/// geçerek iner.
/// </remarks>
public class GonderimDetayDto : GonderimOzetDto
{
    [JsonPropertyName("icerikTuru")] public string? IcerikTuru { get; set; }
    [JsonPropertyName("notlar")] public List<GonderimNotuDto> Notlar { get; set; } = [];
}

public class GonderimNotuDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("yazanId")] public long YazanId { get; set; }
    [JsonPropertyName("yazanAd")] public string? YazanAd { get; set; }
    [JsonPropertyName("benimMi")] public bool BenimMi { get; set; }
    [JsonPropertyName("metin")] public string Metin { get; set; } = string.Empty;
    [JsonPropertyName("tarih")] public DateTime Tarih { get; set; }
}

/// <summary>Gönderim listesi süzgeci.</summary>
public class GonderimSuzgeci : SayfaIstegi
{
    /// <summary>`gelen` · `giden` · `tumu` (varsayılan).</summary>
    [JsonPropertyName("kutu")] public string Kutu { get; set; } = "tumu";
    [JsonPropertyName("okundu")] public bool? Okundu { get; set; }
}

public interface IDosyaGonderimiServisi
{
    Task<SayfaliSonuc<GonderimOzetDto>> ListeAsync(long kullaniciId, GonderimSuzgeci suzgec);
    Task<GonderimDetayDto> DetayAsync(long kullaniciId, long gonderimId);

    /// <summary>
    /// Dosyayı indirmek için akış açar — görünürlük süzgecinden geçerek.
    /// </summary>
    /// <returns>Akış, indirilecek ad ve içerik türü.</returns>
    Task<(Stream Akis, string Ad, string Tur)> DosyaAsync(long kullaniciId, long gonderimId);
    Task<GonderimDetayDto> GonderAsync(long gonderenId, long aliciId, string konu, string? not, IFormFile dosya);
    Task<GonderimNotuDto> NotEkleAsync(long kullaniciId, long gonderimId, string metin);
    Task<int> OkunmamisSayisiAsync(long kullaniciId);
    Task SilAsync(long kullaniciId, long gonderimId);
}

/// <summary>
/// Kullanıcıdan kullanıcıya dosya gönderimi.
///
/// <para>
/// <b>Görünürlük tek kapıdan geçer</b> (<see cref="GorunurOlanlar"/>): bir
/// gönderimi yalnızca gönderen ve alıcı görebilir. <b>Rol bypass'ı yoktur</b> —
/// Admin bile başkalarının gönderimlerini göremez. Bu, gizli etkinlik
/// kuralıyla aynı felsefe; sistemin en kolay sızıntı noktası "yönetici her
/// şeyi görsün" kolaylığıdır.
/// </para>
///
/// <para>
/// Gönderim BAŞLATMAK <c>DosyaGonderebilir</c> yetkisi ister; <b>almak
/// istemez</b> — yoksa gönderilen dosya kimseye ulaşmazdı.
/// </para>
/// </summary>
public class DosyaGonderimiServisi(
    AppDbContext _context,
    IFileStorage _depo,
    IMessageService _mesajServisi,
    ILogger<DosyaGonderimiServisi> _logger) : IDosyaGonderimiServisi
{
    /// <summary>
    /// Gönderilen dosyaların tutulduğu dizin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Varsayılan <c>wwwroot/uploads/gonderim</c>. Bu klasörün seçilmesi bir
    /// KURULUM kararıdır: uygulama havuzu kimliğine <c>wwwroot/uploads</c> için
    /// yazma hakkı zaten verilmiş (etkinlik fotoğrafları ve talep dosyaları iki
    /// yıldır oraya yazılıyor). Ayrı bir klasör, her yayında elle izin vermeyi
    /// gerektirir ve unutulduğunda özellik sessizce çalışmaz.
    /// </para>
    /// <para>
    /// <b>Ama <c>wwwroot</c> altı kimlik doğrulanmadan servis edilir.</b> Bu
    /// yüzden <see cref="Middleware.GonderimDosyaKorumasi"/> ara katmanı
    /// <c>/uploads/gonderim</c> altına gelen HTTP isteklerini 404'ler; dosyaya
    /// tek giriş <c>GET /api/v2/gonderim/{id}/dosya</c>, o da gönderen/alıcı
    /// süzgecinden geçer.
    /// </para>
    /// <para>
    /// Belgeleri yayın klasörünün dışında tutmak isteyen kurulumlar
    /// <c>STORAGE__SENDDIRECTORY</c> ile başka bir yol verebilir (örn.
    /// <c>D:\workcollab-veri\gonderim</c>); o zaman dosyalar sürüm
    /// değiştirdiğinde yerinde kalır. Nesne deposu seçiliyse yol kavramı
    /// tamamen kalkar, dosya <c>gonderim/</c> öneki altında durur — her iki
    /// durumda da <see cref="StorageArea.Private"/> alanı kullanılır ve içerik
    /// hiçbir zaman statik sunulmaz.
    /// </para>
    /// </remarks>
    private const long EnBuyukDosya = 25 * 1024 * 1024; // 25 MB

    /// <remarks>
    /// Yürütülebilir dosyalar `wwwroot` altında statik sunuluyor; birinin
    /// indirip çalıştırması an meselesi.
    /// </remarks>
    private static readonly string[] YasakUzantilar =
        [".exe", ".dll", ".bat", ".cmd", ".sh", ".ps1", ".js", ".jar", ".msi", ".scr", ".com", ".vbs"];

    /// <summary>
    /// GÖRÜNÜRLÜK KAPISI — her sorgu buradan başlamalı.
    /// </summary>
    /// <remarks>
    /// Bu çağrıyı unutan bir sorgu, sistemdeki BÜTÜN dosya gönderimlerini
    /// herkese açar. Hata sessizdir: derleme geçer, ekran çalışır.
    /// </remarks>
    private IQueryable<DosyaGonderimi> GorunurOlanlar(long kullaniciId) =>
        _context.DosyaGonderimleri
            .AsNoTracking()
            .Where(g => g.GonderenId == kullaniciId || g.AliciId == kullaniciId);

    public Task<SayfaliSonuc<GonderimOzetDto>> ListeAsync(long kullaniciId, GonderimSuzgeci suzgec)
    {
        var sorgu = GorunurOlanlar(kullaniciId);

        sorgu = suzgec.Kutu switch
        {
            "gelen" => sorgu.Where(g => g.AliciId == kullaniciId),
            "giden" => sorgu.Where(g => g.GonderenId == kullaniciId),
            _ => sorgu,
        };

        // "Okunmadı" yalnızca ALICI için anlamlı; gönderenin kendi gönderisi
        // hep okunmuş sayılır.
        if (suzgec.Okundu is { } okundu)
        {
            sorgu = okundu
                ? sorgu.Where(g => g.OkunmaTarihi != null || g.GonderenId == kullaniciId)
                : sorgu.Where(g => g.OkunmaTarihi == null && g.AliciId == kullaniciId);
        }

        if (suzgec.TemizArama is { } ara)
        {
            var k = $"%{ara}%";
            sorgu = sorgu.Where(g =>
                EF.Functions.ILike(g.Konu, k) ||
                EF.Functions.ILike(g.DosyaAdi, k));
        }

        return sorgu
            .OrderByDescending(g => g.OlusturmaTarihi)
            .Select(g => new GonderimOzetDto
            {
                Id = g.Id,
                Konu = g.Konu,
                DosyaAdi = g.DosyaAdi,
                Boyut = g.Boyut,
                Tarih = g.OlusturmaTarihi,
                GonderenId = g.GonderenId,
                GonderenAd = g.Gonderen == null ? null : (g.Gonderen.Ad + " " + g.Gonderen.Soyad).Trim(),
                AliciId = g.AliciId,
                AliciAd = g.Alici == null ? null : (g.Alici.Ad + " " + g.Alici.Soyad).Trim(),
                BenGonderdim = g.GonderenId == kullaniciId,
                Okundu = g.OkunmaTarihi != null || g.GonderenId == kullaniciId,
                NotSayisi = g.Notlar.Count,
                SonNot = g.Notlar.OrderByDescending(n => n.OlusturmaTarihi)
                    .Select(n => n.Metin).FirstOrDefault(),
                SonNotTarihi = g.Notlar.OrderByDescending(n => n.OlusturmaTarihi)
                    .Select(n => (DateTime?)n.OlusturmaTarihi).FirstOrDefault(),
            })
            .SayfalaAsync(suzgec);
    }

    /// <summary>Depodaki anahtar — saklanan değer yalnızca dosya adıdır.</summary>
    private static string Anahtar(string diskAdi) => Path.GetFileName(diskAdi);

    public async Task<(Stream Akis, string Ad, string Tur)> DosyaAsync(
        long kullaniciId, long gonderimId)
    {
        // Aynı görünürlük süzgeci: gönderen ve alıcı dışında kimse inemez.
        var g = await GorunurOlanlar(kullaniciId).FirstOrDefaultAsync(x => x.Id == gonderimId)
            ?? throw new EntityNotFoundException($"{gonderimId} kimlikli gönderim bulunamadı.");

        var akis = await _depo.OpenReadAsync(StorageArea.Private, Anahtar(g.DosyaYolu))
            ?? throw new EntityNotFoundException("Dosya sunucuda bulunamadı.");

        return (
            akis,
            g.DosyaAdi,
            string.IsNullOrWhiteSpace(g.IcerikTuru) ? "application/octet-stream" : g.IcerikTuru);
    }

    public async Task<GonderimDetayDto> DetayAsync(long kullaniciId, long gonderimId)
    {
        var g = await GorunurOlanlar(kullaniciId)
            .Include(x => x.Gonderen)
            .Include(x => x.Alici)
            .Include(x => x.Notlar).ThenInclude(n => n.Yazan)
            .FirstOrDefaultAsync(x => x.Id == gonderimId)
            // "Yetkin yok" yerine "bulunamadı": başkasının gönderiminin VAR
            // olduğunu doğrulamak bile bilgi sızdırır.
            ?? throw new EntityNotFoundException($"{gonderimId} kimlikli gönderim bulunamadı.");

        // Alıcı ilk kez açtığında okundu işaretle.
        if (g.AliciId == kullaniciId && g.OkunmaTarihi is null)
        {
            await _context.DosyaGonderimleri
                .Where(x => x.Id == gonderimId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.OkunmaTarihi, DateTime.Now));
            g.OkunmaTarihi = DateTime.Now;
        }

        return new GonderimDetayDto
        {
            Id = g.Id,
            Konu = g.Konu,
            DosyaAdi = g.DosyaAdi,
            IcerikTuru = g.IcerikTuru,
            Boyut = g.Boyut,
            Tarih = g.OlusturmaTarihi,
            GonderenId = g.GonderenId,
            GonderenAd = AdCoz(g.Gonderen),
            AliciId = g.AliciId,
            AliciAd = AdCoz(g.Alici),
            BenGonderdim = g.GonderenId == kullaniciId,
            Okundu = g.OkunmaTarihi != null || g.GonderenId == kullaniciId,
            NotSayisi = g.Notlar.Count,
            Notlar = g.Notlar
                .OrderBy(n => n.OlusturmaTarihi)
                .Select(n => new GonderimNotuDto
                {
                    Id = n.Id,
                    YazanId = n.YazanId,
                    YazanAd = AdCoz(n.Yazan),
                    BenimMi = n.YazanId == kullaniciId,
                    Metin = n.Metin,
                    Tarih = n.OlusturmaTarihi,
                })
                .ToList(),
        };
    }

    public async Task<GonderimDetayDto> GonderAsync(
        long gonderenId, long aliciId, string konu, string? not, IFormFile dosya)
    {
        if (aliciId == gonderenId)
        {
            throw new BusinessRuleException("Kendinize dosya gönderemezsiniz.");
        }

        if (!await _context.Users.AnyAsync(u => u.Id == aliciId))
        {
            throw new EntityNotFoundException("Alıcı kullanıcı bulunamadı.");
        }

        if (dosya is null || dosya.Length == 0)
        {
            throw new BusinessRuleException("Gönderilecek dosya seçilmedi.");
        }

        if (dosya.Length > EnBuyukDosya)
        {
            throw new BusinessRuleException("Dosya 25 MB'tan büyük olamaz.");
        }

        var uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
        if (YasakUzantilar.Contains(uzanti))
        {
            throw new BusinessRuleException($"\"{uzanti}\" uzantılı dosyalar gönderilemez.");
        }

        // Dizin `wwwroot` DIŞINDA: oradaki her şey statik dosya ara katmanıyla
        // kimlik doğrulaması olmadan servis ediliyor. Gönderilen belgeyi
        // yalnızca iki tarafın görebilmesi bu özelliğin varlık sebebi; adresi
        // tahmin edilemez bir dosya adı erişim denetimi değildir (adres
        // geçmişe, günlüklere ve referrer başlığına düşer). İndirme
        // `GET /api/v2/gonderim/{id}/dosya` üzerinden, görünürlük süzgeciyle.
        // Depodaki ad SUNUCUDA üretilir; istemciden gelen ad hiç kullanılmaz,
        // böylece `../` içeren bir ad dizin dışına yazamaz.
        var diskAdi = $"{Guid.NewGuid()}{uzanti}";
        await using (var akis = dosya.OpenReadStream())
        {
            await _depo.SaveAsync(StorageArea.Private, diskAdi, akis, dosya.ContentType);
        }

        var kayit = new DosyaGonderimi
        {
            GonderenId = gonderenId,
            AliciId = aliciId,
            Konu = konu.Trim(),
            DosyaAdi = Path.GetFileName(dosya.FileName),
            DosyaYolu = diskAdi,
            IcerikTuru = dosya.ContentType,
            Boyut = dosya.Length,
        };

        if (!string.IsNullOrWhiteSpace(not))
        {
            kayit.Notlar.Add(new DosyaGonderimiNotu
            {
                YazanId = gonderenId,
                Metin = not.Trim(),
            });
        }

        _context.DosyaGonderimleri.Add(kayit);
        await _context.SaveChangesAsync();

        await BildirAsync(kayit.Id, aliciId, gonderenId, "Yeni dosya gönderimi", konu);
        _logger.LogInformation("Dosya gönderimi {Id}: {Gonderen} → {Alici}", kayit.Id, gonderenId, aliciId);

        return await DetayAsync(gonderenId, kayit.Id);
    }

    public async Task<GonderimNotuDto> NotEkleAsync(long kullaniciId, long gonderimId, string metin)
    {
        var g = await GorunurOlanlar(kullaniciId).FirstOrDefaultAsync(x => x.Id == gonderimId)
            ?? throw new EntityNotFoundException($"{gonderimId} kimlikli gönderim bulunamadı.");

        var not = new DosyaGonderimiNotu
        {
            GonderimId = gonderimId,
            YazanId = kullaniciId,
            Metin = metin.Trim(),
        };

        _context.DosyaGonderimiNotlari.Add(not);
        await _context.SaveChangesAsync();

        // Bildirim KARŞI tarafa gider; kendi notun için bildirim almak saçma.
        var karsiTaraf = g.GonderenId == kullaniciId ? g.AliciId : g.GonderenId;
        await BildirAsync(gonderimId, karsiTaraf, kullaniciId, "Dosya gönderimine yeni not", g.Konu);

        var yazan = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == kullaniciId);

        return new GonderimNotuDto
        {
            Id = not.Id,
            YazanId = kullaniciId,
            YazanAd = AdCoz(yazan),
            BenimMi = true,
            Metin = not.Metin,
            Tarih = not.OlusturmaTarihi,
        };
    }

    public Task<int> OkunmamisSayisiAsync(long kullaniciId) =>
        _context.DosyaGonderimleri
            .Where(g => g.AliciId == kullaniciId && g.OkunmaTarihi == null)
            .CountAsync();

    public async Task SilAsync(long kullaniciId, long gonderimId)
    {
        // Yalnızca GÖNDEREN silebilir: alıcının kendisine gelen bir belgeyi
        // gönderenin bilgisi olmadan yok etmesi, "gönderdim" ile "almadım"
        // arasında çözülemez bir anlaşmazlık yaratır.
        var g = await _context.DosyaGonderimleri
            .FirstOrDefaultAsync(x => x.Id == gonderimId && x.GonderenId == kullaniciId)
            ?? throw new EntityNotFoundException(
                $"{gonderimId} kimlikli gönderim bulunamadı ya da silme yetkiniz yok.");

        try
        {
            await _depo.DeleteAsync(StorageArea.Private, Anahtar(g.DosyaYolu));
        }
        catch (Exception ex)
        {
            // Yetim dosya zararsız; silinemeyen kayıt kullanıcıyı çıkışsız bırakır.
            _logger.LogWarning(ex, "Gönderim dosyası depodan silinemedi: {Yol}", g.DosyaYolu);
        }

        _context.DosyaGonderimleri.Remove(g);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Karşı tarafa bildirim.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>action: None</c> BİLİNÇLİ: mobil uygulamanın yayındaki sürümleri
    /// <c>entity</c> değerini tanımadığında sessizce <c>Talep</c>'e düşüyor
    /// (<c>NotificationEntity.fromString</c> içindeki <c>orElse</c>) ve yanlış
    /// ekranı açardı. <c>None</c> ile eski istemci hiçbir yere gitmez; web
    /// istemcisi ise varlık adına bakarak doğru yere yönlendirir.
    /// </para>
    /// </remarks>
    private async Task BildirAsync(long gonderimId, long hedefKullaniciId, long kimden, string baslik, string konu)
    {
        var gonderenAdi = await _context.Users
            .Where(u => u.Id == kimden)
            .Select(u => (u.Ad + " " + u.Soyad).Trim())
            .FirstOrDefaultAsync();

        // `OpenDetails`: bildirime tıklayan doğrudan gönderimin detayına gitmeli.
        //
        // Sahadaki ESKİ mobil sürümler `Dosya` varlığını tanımıyor ve
        // `NotificationEntity.fromString` bilinmeyeni `talep`e düşürüyordu;
        // mobil taraf `bilinmeyen` değerine çevrildi, o sürümler artık
        // yanlış ekrana gitmek yerine hiçbir yere gitmiyor. `None` göndermek
        // çare değildi: mobil yönlendirme eylemi hiç okumuyor, buna karşılık
        // web tarafında yönlendirmeyi tamamen kapatıyordu.
        var veri = new TokenDataDto(
            NotificationEntity.Dosya, (int)gonderimId, NotificationAction.OpenDetails);

        await _mesajServisi.CreateForUsersAsync(
            [hedefKullaniciId],
            baslik,
            string.IsNullOrWhiteSpace(gonderenAdi) ? konu : $"{gonderenAdi} · {konu}",
            SendMessageType.PushNotification,
            NotifikasyonTip.Always,
            veri.ToJson());
    }

    private static string? AdCoz(AppUser? u) =>
        u is null ? null : $"{u.Ad} {u.Soyad}".Trim() is { Length: > 0 } ad ? ad : u.UserName;
}
