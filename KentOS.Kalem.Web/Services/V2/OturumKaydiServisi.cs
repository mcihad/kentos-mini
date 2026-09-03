using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto.V2.Ortak;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Data;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>Denetim listesinde bir oturum kaydı.</summary>
public class OturumKaydiDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("kullaniciId")] public long? KullaniciId { get; set; }
    [JsonPropertyName("kullaniciAdi")] public string KullaniciAdi { get; set; } = string.Empty;

    /// <summary>Kullanıcının görünen adı — kayıt anındaki değil, güncel hâli.</summary>
    [JsonPropertyName("adSoyad")] public string? AdSoyad { get; set; }

    [JsonPropertyName("olay")] public string Olay { get; set; } = string.Empty;
    [JsonPropertyName("basarili")] public bool Basarili { get; set; }
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("ipAdresi")] public string? IpAdresi { get; set; }
    [JsonPropertyName("istemci")] public string? Istemci { get; set; }
    [JsonPropertyName("tarih")] public DateTime Tarih { get; set; }
}

/// <summary>Oturum kaydı listesi süzgeci.</summary>
public class OturumKaydiSuzgeci : SayfaIstegi
{
    [JsonPropertyName("kullaniciId")] public long? KullaniciId { get; set; }

    /// <summary>Yalnızca başarısız denemeler.</summary>
    [JsonPropertyName("basarili")] public bool? Basarili { get; set; }

    [JsonPropertyName("baslangic")] public DateTime? Baslangic { get; set; }
    [JsonPropertyName("bitis")] public DateTime? Bitis { get; set; }

    /// <summary>
    /// IP adresi — tam ya da ön ek ("192.168." gibi).
    /// </summary>
    /// <remarks>
    /// Denetim kaydında en sık sorulan soru "bu adresten kimler girdi";
    /// ön ek eşleşmesi bir ağ bloğunu tek seferde süzmeyi sağlıyor.
    /// </remarks>
    [JsonPropertyName("ipAdresi")] public string? IpAdresi { get; set; }
}

public interface IOturumKaydiServisi
{
    Task KaydetAsync(
        long? kullaniciId, string kullaniciAdi, OturumOlayi olay,
        bool basarili, string? aciklama = null);

    Task<SayfaliSonuc<OturumKaydiDto>> ListeAsync(OturumKaydiSuzgeci suzgec);
}

/// <summary>
/// Kullanıcı oturum denetim kaydı.
///
/// <para>
/// Kayıt yazmak <b>asıl işlemi asla engellemez</b>: denetim tablosuna
/// yazılamadı diye giriş reddedilirse, tek bir veritabanı sorunu tüm
/// kullanıcıları sistemin dışında bırakır. Hata loglanır, akış sürer.
/// </para>
/// </summary>
public class OturumKaydiServisi(
    AppDbContext _context,
    IHttpContextAccessor _http,
    ILogger<OturumKaydiServisi> _logger) : IOturumKaydiServisi
{
    public async Task KaydetAsync(
        long? kullaniciId, string kullaniciAdi, OturumOlayi olay,
        bool basarili, string? aciklama = null)
    {
        try
        {
            var istek = _http.HttpContext?.Request;

            _context.OturumKayitlari.Add(new KullaniciOturumKaydi
            {
                KullaniciId = kullaniciId,
                // Kullanıcı adı 64 karakterle sınırlı; uzun bir deneme
                // veritabanı hatası vermesin diye kırpılır.
                KullaniciAdi = Kirp(kullaniciAdi, 64),
                Olay = olay,
                Basarili = basarili,
                Aciklama = Kirp(aciklama, 256),
                IpAdresi = IpCoz(),
                Istemci = Kirp(istek?.Headers.UserAgent.ToString(), 256),
                Tarih = DateTime.Now,
            });

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Denetim kaydı yazılamadı; giriş/çıkış AKIŞI ETKİLENMEZ.
            _logger.LogError(ex,
                "Oturum denetim kaydı yazılamadı: {KullaniciAdi} / {Olay}", kullaniciAdi, olay);
        }
    }

    public async Task<SayfaliSonuc<OturumKaydiDto>> ListeAsync(OturumKaydiSuzgeci suzgec)
    {
        var sorgu = _context.OturumKayitlari.AsNoTracking().AsQueryable();

        if (suzgec.KullaniciId is { } kid) sorgu = sorgu.Where(k => k.KullaniciId == kid);
        if (suzgec.Basarili is { } b) sorgu = sorgu.Where(k => k.Basarili == b);
        if (suzgec.Baslangic is { } bas) sorgu = sorgu.Where(k => k.Tarih >= bas.Date);
        if (suzgec.Bitis is { } bit) sorgu = sorgu.Where(k => k.Tarih < bit.Date.AddDays(1));

        // Ön ek eşleşmesi: "192.168." bütün bir ağ bloğunu süzer.
        if (!string.IsNullOrWhiteSpace(suzgec.IpAdresi))
        {
            var ip = suzgec.IpAdresi.Trim();
            sorgu = sorgu.Where(k => k.IpAdresi != null && EF.Functions.ILike(k.IpAdresi, ip + "%"));
        }

        if (suzgec.TemizArama is { } ara)
        {
            var kalip = $"%{ara}%";
            sorgu = sorgu.Where(k =>
                EF.Functions.ILike(k.KullaniciAdi, kalip) ||
                (k.IpAdresi != null && EF.Functions.ILike(k.IpAdresi, kalip)));
        }

        var toplam = await sorgu.LongCountAsync();

        var sayfa = await sorgu
            .OrderByDescending(k => k.Tarih)
            .Skip(suzgec.Atla)
            .Take(suzgec.Boyut)
            .ToListAsync();

        // Görünen adları TEK sorguda çöz — satır başına sorgu, 50 satırda
        // 50 gidiş-dönüş demek olurdu.
        var idler = sayfa.Where(k => k.KullaniciId != null).Select(k => k.KullaniciId!.Value).Distinct().ToList();
        var adlar = await _context.Users
            .Where(u => idler.Contains(u.Id))
            .Select(u => new { u.Id, u.Ad, u.Soyad })
            .ToDictionaryAsync(u => u.Id, u => $"{u.Ad} {u.Soyad}".Trim());

        var veriler = sayfa.Select(k => new OturumKaydiDto
        {
            Id = k.Id,
            KullaniciId = k.KullaniciId,
            KullaniciAdi = k.KullaniciAdi,
            AdSoyad = k.KullaniciId is { } id ? adlar.GetValueOrDefault(id) : null,
            Olay = k.Olay == OturumOlayi.Giris ? "Giriş" : "Çıkış",
            Basarili = k.Basarili,
            Aciklama = k.Aciklama,
            IpAdresi = k.IpAdresi,
            Istemci = k.Istemci,
            Tarih = k.Tarih,
        }).ToList();

        return SayfaliSonuc<OturumKaydiDto>.Olustur(veriler, toplam, suzgec);
    }

    /// <summary>
    /// İstemci IP'si.
    /// </summary>
    /// <remarks>
    /// Uygulama ters vekil arkasında çalışıyor; <c>RemoteIpAddress</c> o
    /// durumda hep vekilin adresini verir ve denetim kaydı işe yaramaz olur.
    /// Önce <c>X-Forwarded-For</c>'un İLK değeri okunur (zincirin en soldaki
    /// öğesi gerçek istemcidir).
    /// </remarks>
    private string? IpCoz()
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return null;

        var iletilen = ctx.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(iletilen))
        {
            return Kirp(iletilen.Split(',')[0].Trim(), 64);
        }

        return Kirp(ctx.Connection.RemoteIpAddress?.ToString(), 64);
    }

    private static string Kirp(string? metin, int uzunluk) =>
        string.IsNullOrEmpty(metin) ? string.Empty
        : metin.Length <= uzunluk ? metin : metin[..uzunluk];
}
