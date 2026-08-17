using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Services.V2;

public interface IHataKaydiServisi
{
    /// <summary>Hatayı kaydeder; aynı hata daha önce görüldüyse sayacı artırır.</summary>
    Task KaydetAsync(Exception hata, HttpContext baglam, string izKimligi, int durumKodu);

    Task<SayfaliSonuc<HataOzetDto>> ListeAsync(HataSuzgeci suzgec);
    Task<HataDetayDto> DetayAsync(long id);
    Task<HataDetayDto> NotKaydetAsync(long id, string? notlar, bool cozuldu, string kullaniciAdi);
    Task SilAsync(long id);
    Task<int> CozulenleriSilAsync();
}

/// <summary>
/// Sunucu hatalarının kaydı ve takibi.
/// </summary>
/// <remarks>
/// <para>
/// Sistem iki yıldır canlıda ve bugüne kadar hatalar YALNIZCA konsol
/// günlüğüne düşüyordu: sunucu yeniden başlayınca kayboluyor, kullanıcı
/// "hata aldım" dediğinde geriye bakacak hiçbir şey kalmıyordu.
/// </para>
/// <para>
/// <b>Kayıt asla isteği bozmaz.</b> Bu servisin içindeki her hata yutulur;
/// hata kaydedememek yüzünden kullanıcının işleminin çökmesi, çözmeye
/// çalıştığımız sorunun daha kötüsü olurdu.
/// </para>
/// </remarks>
public class HataKaydiServisi(
    AppDbContext _context,
    IServiceScopeFactory _kapsakFabrikasi,
    ILogger<HataKaydiServisi> _logger) : IHataKaydiServisi
{
    /// <summary>Gövdenin saklanacak en fazla uzunluğu.</summary>
    /// <remarks>
    /// Dosya yüklemeli isteklerde gövde megabaytlarca olabiliyor; hepsini
    /// saklamak veritabanını şişirir ve teşhise bir şey katmaz.
    /// </remarks>
    private const int EnFazlaGovde = 8_000;

    private const int EnFazlaYigin = 16_000;

    /// <summary>Günlüğe ASLA yazılmayacak başlıklar.</summary>
    private static readonly string[] GizliBasliklar =
        ["authorization", "cookie", "set-cookie", "x-api-key"];

    /// <summary>Gövdede maskelenecek alan adları.</summary>
    private static readonly string[] GizliAlanlar =
        ["parola", "password", "sifre", "yeniParola", "newPassword", "token", "jeton"];

    public async Task KaydetAsync(
        Exception hata, HttpContext baglam, string izKimligi, int durumKodu)
    {
        try
        {
            // AYRI BAĞLAM ZORUNLU: hata çoğu zaman `SaveChangesAsync` sırasında
            // oluşuyor ve o anda istek kapsamındaki bağlam BOZUK bir varlığı
            // izliyor. Aynı bağlamda kaydetmeye çalışmak, aynı hatalı INSERT'ü
            // yeniden denemek demek — hata kaydı da hatayla düşüyordu.
            using var kapsak = _kapsakFabrikasi.CreateScope();
            var db = kapsak.ServiceProvider.GetRequiredService<AppDbContext>();

            var (dosya, satir) = KaynakKonumu(hata);
            var parmakizi = Parmakizi(hata, dosya, satir);

            var mevcut = await db.SistemHatalari
                .FirstOrDefaultAsync(h => h.Parmakizi == parmakizi);

            if (mevcut is not null)
            {
                // Aynı hata için YENİ SATIR AÇILMAZ: döngüye giren tek bir hata
                // listeyi binlerce satırla doldurup diğerlerini görünmez kılardı.
                mevcut.Adet++;
                mevcut.SonGorulme = DateTime.Now;
                mevcut.IzKimligi = izKimligi;

                // Kayıt yeniden görüldüyse "çözüldü" işareti artık doğru değil.
                if (mevcut.Cozuldu)
                {
                    mevcut.Cozuldu = false;
                    mevcut.CozulmeTarihi = null;
                }

                await db.SaveChangesAsync();
                return;
            }

            db.SistemHatalari.Add(new SistemHatasi
            {
                Parmakizi = parmakizi,
                Tur = hata.GetType().FullName ?? hata.GetType().Name,
                Mesaj = Kirp(hata.Message, 2_000) ?? string.Empty,
                IcMesaj = Kirp(hata.InnerException?.Message, 2_000),
                YiginIzi = Kirp(hata.ToString(), EnFazlaYigin),
                Dosya = dosya,
                Satir = satir,
                DurumKodu = durumKodu,

                Yol = baglam.Request.Path.Value,
                Yontem = baglam.Request.Method,
                SorguDizesi = Kirp(baglam.Request.QueryString.Value, 1_000),
                Basliklar = BasliklariTopla(baglam),
                Govde = await GovdeyiOkuAsync(baglam),

                KullaniciId = KullaniciId(baglam),
                KullaniciAdi = baglam.User?.Identity?.Name,
                BirimId = BirimId(baglam),
                IpAdresi = IpAdresi(baglam),
                Istemci = Kirp(baglam.Request.Headers.UserAgent.ToString(), 500),
                IzKimligi = izKimligi,
            });

            await db.SaveChangesAsync();
        }
        catch (Exception kayitHatasi)
        {
            // Kayıt başarısız olsa bile istek akışı BOZULMAZ.
            _logger.LogWarning(kayitHatasi, "Sistem hatası kaydedilemedi.");
        }
    }

    // ───────────────────────────────────────────────────────── okuma

    public async Task<SayfaliSonuc<HataOzetDto>> ListeAsync(HataSuzgeci suzgec)
    {
        var ara = suzgec.TemizArama;

        var sorgu = _context.SistemHatalari
            .AsNoTracking()
            .Where(h => suzgec.Cozuldu == null || h.Cozuldu == suzgec.Cozuldu)
            .Where(h => ara == null
                || EF.Functions.ILike(h.Mesaj, $"%{ara}%")
                || EF.Functions.ILike(h.Tur, $"%{ara}%")
                || (h.Yol != null && EF.Functions.ILike(h.Yol, $"%{ara}%")))
            .OrderByDescending(h => h.SonGorulme);

        var toplam = await sorgu.LongCountAsync();
        var satirlar = await sorgu.Skip(suzgec.Atla).Take(suzgec.Boyut)
            .Select(h => new HataOzetDto
            {
                Id = h.Id,
                Tur = h.Tur,
                Mesaj = h.Mesaj,
                Yol = h.Yol,
                Yontem = h.Yontem,
                DurumKodu = h.DurumKodu,
                Adet = h.Adet,
                IlkGorulme = h.IlkGorulme,
                SonGorulme = h.SonGorulme,
                KullaniciAdi = h.KullaniciAdi,
                Cozuldu = h.Cozuldu,
            })
            .ToListAsync();

        return SayfaliSonuc<HataOzetDto>.Olustur(satirlar, toplam, suzgec);
    }

    public async Task<HataDetayDto> DetayAsync(long id)
    {
        var h = await _context.SistemHatalari.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli hata kaydı bulunamadı.");

        return Detay(h);
    }

    public async Task<HataDetayDto> NotKaydetAsync(
        long id, string? notlar, bool cozuldu, string kullaniciAdi)
    {
        var h = await _context.SistemHatalari.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli hata kaydı bulunamadı.");

        h.Notlar = notlar;

        if (cozuldu && !h.Cozuldu)
        {
            h.Cozuldu = true;
            h.CozulmeTarihi = DateTime.Now;
            h.CozenKullanici = kullaniciAdi;
        }
        else if (!cozuldu && h.Cozuldu)
        {
            h.Cozuldu = false;
            h.CozulmeTarihi = null;
            h.CozenKullanici = null;
        }

        await _context.SaveChangesAsync();
        return Detay(h);
    }

    public async Task SilAsync(long id)
    {
        var h = await _context.SistemHatalari.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli hata kaydı bulunamadı.");

        _context.SistemHatalari.Remove(h);
        await _context.SaveChangesAsync();
    }

    public Task<int> CozulenleriSilAsync() =>
        _context.SistemHatalari.Where(h => h.Cozuldu).ExecuteDeleteAsync();

    // ───────────────────────────────────────────────────────── yardımcı

    private static HataDetayDto Detay(SistemHatasi h) => new()
    {
        Id = h.Id,
        Parmakizi = h.Parmakizi,
        Tur = h.Tur,
        Mesaj = h.Mesaj,
        IcMesaj = h.IcMesaj,
        YiginIzi = h.YiginIzi,
        Dosya = h.Dosya,
        Satir = h.Satir,
        DurumKodu = h.DurumKodu,
        Yol = h.Yol,
        Yontem = h.Yontem,
        SorguDizesi = h.SorguDizesi,
        Govde = h.Govde,
        Basliklar = h.Basliklar,
        KullaniciId = h.KullaniciId,
        KullaniciAdi = h.KullaniciAdi,
        BirimId = h.BirimId,
        IpAdresi = h.IpAdresi,
        Istemci = h.Istemci,
        IzKimligi = h.IzKimligi,
        Adet = h.Adet,
        IlkGorulme = h.IlkGorulme,
        SonGorulme = h.SonGorulme,
        Cozuldu = h.Cozuldu,
        CozulmeTarihi = h.CozulmeTarihi,
        CozenKullanici = h.CozenKullanici,
        Notlar = h.Notlar,
    };

    /// <summary>
    /// Aynı hatayı tanıyan kararlı anahtar.
    /// </summary>
    /// <remarks>
    /// Mesajın kendisi ANAHTARA GİRMEZ: içinde değişken değerler oluyor
    /// ("42 kimlikli kayıt bulunamadı"), her kayıt ayrı satır açardı. Tür +
    /// atıldığı konum yeterince ayırt edici.
    /// </remarks>
    private static string Parmakizi(Exception hata, string? dosya, int? satir)
    {
        var kaynak = $"{hata.GetType().FullName}|{dosya}|{satir}|{IlkKendiKarem(hata)}";
        var bayt = SHA256.HashData(Encoding.UTF8.GetBytes(kaynak));
        return Convert.ToHexString(bayt)[..32];
    }

    /// <summary>Yığındaki İLK kendi kodumuza ait kare.</summary>
    private static string IlkKendiKarem(Exception hata) =>
        (hata.StackTrace ?? string.Empty)
            .Split('\n')
            .Select(s => s.Trim())
            .FirstOrDefault(s => s.Contains("KentOS.Mini", StringComparison.Ordinal))
        ?? string.Empty;

    /// <summary>Hatanın atıldığı dosya ve satır — yığın izinden çıkarılır.</summary>
    private static (string? Dosya, int? Satir) KaynakKonumu(Exception hata)
    {
        var iz = new System.Diagnostics.StackTrace(hata, fNeedFileInfo: true);

        foreach (var kare in iz.GetFrames())
        {
            var dosya = kare.GetFileName();
            if (string.IsNullOrEmpty(dosya)) continue;

            // Tam yol geliştirme makinesinin dizin yapısını sızdırır; yalnızca
            // depo içindeki göreli kısım saklanır.
            var i = dosya.IndexOf("KentOS.Mini", StringComparison.Ordinal);
            return (i >= 0 ? dosya[i..] : Path.GetFileName(dosya), kare.GetFileLineNumber());
        }

        return (null, null);
    }

    private static string? BasliklariTopla(HttpContext baglam)
    {
        var satirlar = baglam.Request.Headers
            .Where(b => !GizliBasliklar.Contains(b.Key.ToLowerInvariant()))
            .Select(b => $"{b.Key}: {b.Value}")
            .ToList();

        // Kimlik başlıkları SAKLANMAZ ama varlıkları teşhiste işe yarıyor.
        if (baglam.Request.Headers.ContainsKey("Authorization"))
        {
            satirlar.Add("Authorization: (var, maskelendi)");
        }

        return Kirp(string.Join('\n', satirlar), 4_000);
    }

    /// <summary>
    /// İstek gövdesini okur.
    /// </summary>
    /// <remarks>
    /// Gövde ancak <c>EnableBuffering</c> açıksa okunabilir (bkz. Program.cs);
    /// akış zaten tüketilmiş olduğu için başa sarılır. Çok parçalı istekler
    /// (dosya yükleme) atlanır: içeriği ikili ve megabaytlarca.
    /// </remarks>
    private static async Task<string?> GovdeyiOkuAsync(HttpContext baglam)
    {
        try
        {
            var istek = baglam.Request;
            if (!istek.Body.CanSeek) return null;
            if (istek.ContentType?.Contains("multipart/", StringComparison.OrdinalIgnoreCase) == true)
            {
                return "(çok parçalı istek — gövde saklanmadı)";
            }

            istek.Body.Position = 0;
            using var okuyucu = new StreamReader(istek.Body, leaveOpen: true);
            var ham = await okuyucu.ReadToEndAsync();
            istek.Body.Position = 0;

            return Kirp(Maskele(ham), EnFazlaGovde);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Gövdedeki parola/jeton alanlarını maskeler.</summary>
    private static string Maskele(string govde)
    {
        foreach (var alan in GizliAlanlar)
        {
            govde = System.Text.RegularExpressions.Regex.Replace(
                govde,
                $"(\"{alan}\"\\s*:\\s*)\"[^\"]*\"",
                "$1\"***\"",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return govde;
    }

    private static string? Kirp(string? metin, int enFazla)
    {
        if (string.IsNullOrEmpty(metin)) return metin;
        return metin.Length <= enFazla ? metin : metin[..enFazla] + "\n… (kırpıldı)";
    }

    private static long? KullaniciId(HttpContext baglam)
    {
        var ham = baglam.User?.FindFirst("UserId")?.Value;
        return long.TryParse(ham, out var d) ? d : null;
    }

    private static long? BirimId(HttpContext baglam)
    {
        var ham = baglam.User?.FindFirst("BirimId")?.Value;
        return long.TryParse(ham, out var d) ? d : null;
    }

    /// <summary>Vekil sunucu arkasındaysa gerçek istemci adresi.</summary>
    private static string? IpAdresi(HttpContext baglam)
    {
        var iletilen = baglam.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(iletilen))
        {
            return iletilen.Split(',')[0].Trim();
        }
        return baglam.Connection.RemoteIpAddress?.ToString();
    }
}
