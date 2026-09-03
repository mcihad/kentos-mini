using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Dto.V2.Ortak;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Exceptions;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>Bildirim merkezindeki bir kayıt.</summary>
public class BildirimDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("baslik")] public string Baslik { get; set; } = string.Empty;
    [JsonPropertyName("icerik")] public string Icerik { get; set; } = string.Empty;
    [JsonPropertyName("tarih")] public DateTime Tarih { get; set; }
    [JsonPropertyName("okundu")] public bool Okundu { get; set; }

    /// <summary>Gizli etkinlik bildirimi — başlığı `🔒 Gizli · ` ile başlar.</summary>
    [JsonPropertyName("gizli")] public bool Gizli { get; set; }

    /// <summary>
    /// Yönlendirme hedefi — mobil ile AYNI sözleşme.
    /// </summary>
    /// <remarks>
    /// Sunucu `Data` alanında ham JSON tutuyor (<c>{entity,id,action}</c>).
    /// İstemcinin onu ayrıştırması gerekmesin diye burada çözülür; bozuk
    /// JSON'lar da burada elenir, arayüzde "tıkladım bir şey olmadı" durumu
    /// oluşmaz.
    /// </remarks>
    [JsonPropertyName("varlik")] public string? Varlik { get; set; }
    [JsonPropertyName("varlikId")] public long? VarlikId { get; set; }
    [JsonPropertyName("eylem")] public string? Eylem { get; set; }
}

public interface IBildirimMerkeziServisi
{
    Task<SayfaliSonuc<BildirimDto>> ListeAsync(
        long kullaniciId, SayfaIstegi istek, bool yalnizcaOkunmamis, bool tumGecmis = false);
    Task<int> OkunmamisSayisiAsync(long kullaniciId);
    Task OkunduIsaretleAsync(long kullaniciId, long bildirimId);
    Task<int> TumunuOkunduIsaretleAsync(long kullaniciId);
    Task SilAsync(long kullaniciId, long bildirimId);
    Task<int> OkunanlariSilAsync(long kullaniciId);
}

/// <summary>
/// Uygulama içi bildirim merkezi.
///
/// <para>
/// Kaynak, FCM giden kutusu olan <c>Messages</c> tablosudur — ayrı bir
/// bildirim tablosu AÇILMADI: aynı bildirimi iki yere yazmak, birinin
/// yazılıp diğerinin yazılmadığı durumları kaçınılmaz kılar.
/// </para>
///
/// <para>
/// Tarayıcı bildirimleri kullanıcı tek tek silene kadar işletim sisteminin
/// bildirim merkezinde birikiyordu. Uygulama içi merkez bunu çözer:
/// kullanıcı buradan hepsini bir kerede okundu işaretleyebiliyor ve
/// service worker açık bir sekme varken sistem bildirimi ÜRETMİYOR.
/// </para>
/// </summary>
public class BildirimMerkeziServisi(AppDbContext _context) : IBildirimMerkeziServisi
{
    /// <summary>
    /// Bildirim merkezine giren kayıtlar.
    /// </summary>
    /// <remarks>
    /// Yalnızca push bildirimleri: SMS satırları da aynı tabloda duruyor ama
    /// onlar vatandaşa/çiçekçiye giden mesajlar, kullanıcının bildirimi değil.
    /// Bir kullanıcının iki cihazı varsa aynı bildirim iki satır üretiyor;
    /// merkez bunları başlık+içerik+dakika üçlüsüyle TEKİLLEŞTİRİR.
    /// </remarks>
    /// <summary>
    /// Bildirim merkezinin varsayılan ZAMAN PENCERESİ (gün).
    /// </summary>
    /// <remarks>
    /// GERÇEK SORUN: <c>Messages</c> iki yıllık giden kutusu. Yeni web
    /// arayüzüne ilk giren kullanıcı, hepsi geçmiş işlere ait <b>on binden
    /// fazla</b> bildirim görüyordu ve rozet asla anlamlı bir sayı
    /// göstermiyordu.
    ///
    /// Veri SİLİNMEDİ ve sessizce okundu işaretlenmedi — ikisi de kullanıcının
    /// geçmişini kaybettirirdi. Bunun yerine zil ve rozet son 30 güne bakar;
    /// tüm geçmiş "Tüm bildirimler" sayfasından sayfalanarak okunur.
    /// </remarks>
    public const int VarsayilanGecmisGunu = 30;

    private IQueryable<Message> TemelSorgu(long kullaniciId, bool tumGecmis = false)
    {
        var sorgu = _context.Messages
            .AsNoTracking()
            .Where(m => m.UserId == kullaniciId
                     && m.MessageType == SendMessageType.PushNotification);

        if (!tumGecmis)
        {
            var sinir = DateTime.Now.AddDays(-VarsayilanGecmisGunu);
            sorgu = sorgu.Where(m => m.CreatedAt >= sinir);
        }

        return sorgu;
    }

    public async Task<SayfaliSonuc<BildirimDto>> ListeAsync(
        long kullaniciId, SayfaIstegi istek, bool yalnizcaOkunmamis, bool tumGecmis = false)
    {
        var sorgu = TemelSorgu(kullaniciId, tumGecmis);
        if (yalnizcaOkunmamis) sorgu = sorgu.Where(m => !m.Okundu);

        if (istek.TemizArama is { } ara)
        {
            var k = $"%{ara}%";
            sorgu = sorgu.Where(m =>
                EF.Functions.ILike(m.Title, k) || EF.Functions.ILike(m.Content, k));
        }

        // Sayfayı çekmeden önce tekilleştirmek gerekiyor; aksi hâlde iki
        // cihazlı kullanıcıda "50 kayıt" istenip 25 farklı bildirim dönerdi.
        // Gruplama veritabanında yapılır: aynı dakikadaki aynı metin tektir.
        var gruplu = sorgu
            .GroupBy(m => new { m.Title, m.Content, m.Data, Dakika = new DateTime(
                m.CreatedAt.Year, m.CreatedAt.Month, m.CreatedAt.Day,
                m.CreatedAt.Hour, m.CreatedAt.Minute, 0) })
            .Select(g => new
            {
                // Temsilci satır: en küçük kimlik. "Okundu" işareti gruptaki
                // HERHANGİ biri okunduysa okunmuş sayılır.
                Id = g.Min(x => x.Id),
                g.Key.Title,
                g.Key.Content,
                g.Key.Data,
                Tarih = g.Max(x => x.CreatedAt),
                Okundu = g.Max(x => x.Okundu ? 1 : 0) == 1,
            });

        var toplam = await gruplu.LongCountAsync();

        var sayfa = await gruplu
            .OrderByDescending(x => x.Tarih)
            .Skip(istek.Atla)
            .Take(istek.Boyut)
            .ToListAsync();

        var veriler = sayfa.Select(x =>
        {
            var (varlik, varlikId, eylem) = YonlendirmeCoz(x.Data);
            return new BildirimDto
            {
                Id = x.Id,
                Baslik = x.Title,
                Icerik = x.Content,
                Tarih = x.Tarih,
                Okundu = x.Okundu,
                Gizli = x.Title.StartsWith("🔒", StringComparison.Ordinal),
                Varlik = varlik,
                VarlikId = varlikId,
                Eylem = eylem,
            };
        }).ToList();

        return SayfaliSonuc<BildirimDto>.Olustur(veriler, toplam, istek);
    }

    /// <remarks>
    /// Rozet YALNIZCA zaman penceresindeki okunmamışları sayar; iki yıllık
    /// birikmiş geçmiş "99+" olarak donup anlamını yitiriyordu.
    /// </remarks>
    public Task<int> OkunmamisSayisiAsync(long kullaniciId) =>
        TemelSorgu(kullaniciId)
            .Where(m => !m.Okundu)
            // Tekilleştirilmiş sayı: iki cihazlı kullanıcıda rozet iki katı
            // görünmesin.
            .Select(m => new { m.Title, m.Content, m.CreatedAt.Date, m.CreatedAt.Hour, m.CreatedAt.Minute })
            .Distinct()
            .CountAsync();

    public async Task OkunduIsaretleAsync(long kullaniciId, long bildirimId)
    {
        // Temsilci kaydı bul, sonra AYNI bildirimi taşıyan tüm cihaz
        // satırlarını birlikte işaretle — biri okununca diğeri okunmamış
        // kalırsa rozet asla sıfırlanmaz.
        var kayit = await _context.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == bildirimId && m.UserId == kullaniciId)
            ?? throw new EntityNotFoundException($"{bildirimId} kimlikli bildirim bulunamadı.");

        var altSinir = kayit.CreatedAt.AddMinutes(-1);
        var ustSinir = kayit.CreatedAt.AddMinutes(1);

        await _context.Messages
            .Where(m => m.UserId == kullaniciId
                     && m.Title == kayit.Title
                     && m.Content == kayit.Content
                     && m.CreatedAt >= altSinir && m.CreatedAt <= ustSinir
                     && !m.Okundu)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Okundu, true)
                .SetProperty(m => m.OkunmaTarihi, DateTime.Now));
    }

    public Task<int> TumunuOkunduIsaretleAsync(long kullaniciId) =>
        _context.Messages
            .Where(m => m.UserId == kullaniciId
                     && m.MessageType == SendMessageType.PushNotification
                     && !m.Okundu)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Okundu, true)
                .SetProperty(m => m.OkunmaTarihi, DateTime.Now));

    public async Task SilAsync(long kullaniciId, long bildirimId)
    {
        var silinen = await _context.Messages
            .Where(m => m.Id == bildirimId && m.UserId == kullaniciId)
            .ExecuteDeleteAsync();

        if (silinen == 0)
        {
            throw new EntityNotFoundException($"{bildirimId} kimlikli bildirim bulunamadı.");
        }
    }

    /// <summary>
    /// Okunmuş bildirimleri siler.
    /// </summary>
    /// <remarks>
    /// Yalnızca GÖNDERİLMİŞ olanlar silinir: <c>IsSuccess == false</c> ve
    /// <c>RetryCount &lt; 3</c> olan satırlar hâlâ <c>FirebaseWorker</c>'ın
    /// kuyruğunda; silmek gönderilmemiş bir bildirimi yok etmek olurdu.
    /// </remarks>
    public Task<int> OkunanlariSilAsync(long kullaniciId) =>
        _context.Messages
            .Where(m => m.UserId == kullaniciId
                     && m.MessageType == SendMessageType.PushNotification
                     && m.Okundu
                     && (m.IsSuccess || m.RetryCount >= 3))
            .ExecuteDeleteAsync();

    /// <summary>`fcmData` JSON'unu çözer; bozuksa sessizce boş döner.</summary>
    private static (string? Varlik, long? Id, string? Eylem) YonlendirmeCoz(string? ham)
    {
        if (string.IsNullOrWhiteSpace(ham)) return (null, null, null);

        try
        {
            var d = JsonSerializer.Deserialize<TokenDataDto>(ham);
            if (d is null) return (null, null, null);

            return (d.Entity.ToString(), d.Id, d.Action.ToString());
        }
        catch (JsonException)
        {
            // Eski kayıtlarda farklı biçimler var; yönlendirme yapılamaz ama
            // bildirimin kendisi yine de gösterilir.
            return (null, null, null);
        }
    }
}
