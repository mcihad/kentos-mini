using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Data;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>
/// SLA İŞÇİSİ — süresi aşan işleri ÜST YÖNETİCİYE bildirir.
/// </summary>
/// <remarks>
/// <para>
/// Saatte bir çalışır. <c>TekrarUfkuWorker</c> kalıbı: gecikmeli ilk
/// çalışma, her turda kendi kapsamı, hatayı yalnızca loglar.
/// </para>
///
/// <para>
/// <b>AYNI GÖREV İÇİN AYNI EŞİK İKİ KEZ BİLDİRİLMEZ.</b> Bekçi ayrı bir
/// "bildirildi mi" kolonu değil, ZAMAN ÇİZELGESİNİN KENDİSİ: bildirim zaten
/// oraya yazılıyor ve iki kayıt arasında tutarsızlık ihtimali kalmıyor.
/// Kolon açsaydık çizelgede bildirim görünüp kolon boş kalabilir ya da tersi
/// olabilirdi.
/// </para>
///
/// <para>
/// <b>Çok örnekli dağıtımda çift tetiklemeye karşı Postgres danışma
/// kilidi.</b> Uygulama iki kopya hâlinde çalışırsa ikisi de aynı saatte
/// uyanır ve her gecikmeyi iki kez bildirirdi. <c>pg_try_advisory_lock</c>
/// hemen dönüyor: kilidi alamayan kopya o turu atlıyor, beklemiyor.
/// </para>
///
/// <para>
/// Bildirim <b>üst birime</b> gidiyor, görevi yürüten kişiye değil: personel
/// zaten işin başında ve ona gecikmeyi hatırlatmak bir yaptırım değil
/// gürültü. Süre aşımının muhatabı yönetimdir.
/// </para>
/// </remarks>
public interface ISlaTarayici
{
    /// <summary>
    /// Süresi aşan işleri tarar ve bildirir. KAÇ görev bildirildiğini döner.
    /// </summary>
    /// <remarks>
    /// İşçiden AYRI bir servis: zamanlama ile kuralın kendisi farklı şeyler
    /// ve kural test edilebilir olmalı. İşçinin içinde kalsaydı "aynı görev
    /// iki kez bildirilmiyor" davranışını doğrulamanın tek yolu bir saat
    /// beklemek olurdu.
    /// </remarks>
    Task<int> TaraAsync(CancellationToken iptal = default);
}

/// <summary>Süre aşımı taraması — işçinin çağırdığı asıl kural.</summary>
public class SlaTarayici(
    AppDbContext _context,
    IMessageService _mesajlar,
    ILogger<SlaTarayici> _kayit) : ISlaTarayici
{
    /// <summary>
    /// Bir turda en fazla kaç görev bildirilir.
    /// </summary>
    /// <remarks>
    /// Sınır yoksa ilk çalıştırmada birikmiş bütün gecikmeler tek seferde
    /// yüzlerce bildirim üretir ve kimse hiçbirini okumaz.
    /// </remarks>
    private const int TurBasinaSinir = 200;

    public async Task<int> TaraAsync(CancellationToken iptal = default)
    {
        var simdi = DateTime.Now;

        var asanlar = await _context.Gorevler
            .AsNoTracking()
            .Where(g => g.SlaBitis != null && g.SlaBitis < simdi)
            .Where(g => g.Durum != GorevDurumu.Tamamlandi && g.Durum != GorevDurumu.Iptal)
            .OrderBy(g => g.SlaBitis)
            .Take(TurBasinaSinir)
            .Select(g => new { g.Id, g.TakipNo, g.Baslik, g.BirimId, g.SlaBitis })
            .ToListAsync(iptal);

        if (asanlar.Count == 0) return 0;

        // İKİ KEZ BİLDİRME BEKÇİSİ: çizelgede `SlaUyarisi` olayı varsa atla.
        var idler = asanlar.Select(g => g.Id).ToList();

        var bildirilmisler = await _context.IsOlaylari
            .AsNoTracking()
            .Where(o => o.VarlikTuru == IsVarligi.Gorev
                     && idler.Contains(o.VarlikId)
                     && o.Tip == GorevOlayTipi.SlaUyarisi)
            .Select(o => o.VarlikId)
            .ToListAsync(iptal);

        var yeniler = asanlar.Where(g => !bildirilmisler.Contains(g.Id)).ToList();
        if (yeniler.Count == 0) return 0;

        foreach (var g in yeniler)
        {
            if (iptal.IsCancellationRequested) break;

            var hedefler = await UstYoneticilerAsync(_context, g.BirimId, iptal);

            if (hedefler.Count > 0)
            {
                try
                {
                    var veri = new TokenDataDto(
                        NotificationEntity.Gorev, (int)g.Id, NotificationAction.None);

                    var gecikme = (int)(simdi - g.SlaBitis!.Value).TotalHours;

                    await _mesajlar.CreateForUsersAsync(
                        hedefler,
                        "Süre aşımı",
                        $"{g.TakipNo} — {g.Baslik} ({gecikme} saat gecikti)",
                        SendMessageType.PushNotification,
                        NotifikasyonTip.TaskOnOverdue,
                        veri.ToJson());
                }
                catch (Exception hata)
                {
                    _kayit.LogWarning(hata, "SLA bildirimi yazılamadı: {TakipNo}", g.TakipNo);
                }
            }

            /*
              OLAY, BİLDİRİM GÖNDERİLEMESE DE YAZILIYOR.

              Aksi hâlde hedefi olmayan bir görev (birimde yönetici yok) her
              turda yeniden denenir ve sonsuza kadar sorgu üretirdi. Kayıt
              "bu aşım görüldü" demek; bildirimin gitmesi ayrı bir şey.
            */
            _context.IsOlaylari.Add(new Application.Models.WorkEvent
            {
                VarlikTuru = IsVarligi.Gorev,
                VarlikId = g.Id,
                Tip = GorevOlayTipi.SlaUyarisi,
                Aciklama = $"Süre aşıldı ({g.SlaBitis:dd.MM.yyyy HH:mm}).",
                Kullanici = "Sistem",
                BirimId = g.BirimId,
                Tarih = simdi,
            });
        }

        await _context.SaveChangesAsync(iptal);
        return yeniler.Count;
    }

    /// <summary>
    /// Süre aşımının muhatabı: ÜST BİRİMDEKİ onay yetkilileri.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Üst birim yoksa (kök birim) kendi biriminin yetkilileri. Hiçbir hedef
    /// bulunamazsa liste boş dönüyor ve olay yine de yazılıyor.
    /// </para>
    /// <para>
    /// Hedef <c>gorev.onayla</c> iznine göre çözülüyor: birimin
    /// <c>Yetkili</c> alanı bir METİN, kullanıcı kimliği değil.
    /// </para>
    /// </remarks>
    private static async Task<List<long>> UstYoneticilerAsync(
        AppDbContext baglam, long birimId, CancellationToken iptal)
    {
        var ustId = await baglam.Birimler
            .AsNoTracking()
            .Where(b => b.Id == birimId)
            .Select(b => b.UstBirimId)
            .FirstOrDefaultAsync(iptal);

        var hedefBirim = ustId ?? birimId;

        return await (
            from ur in baglam.UserRoles
            join ri in baglam.RolIzinleri on ur.RoleId equals ri.RolId
            join iz in baglam.Izinler on ri.IzinAd equals iz.Ad
            join k in baglam.Users on ur.UserId equals k.Id
            where iz.Ad == Izinler.GorevOnayla && iz.Kullanimda && k.BirimId == hedefBirim
            select ur.UserId
        ).Distinct().ToListAsync(iptal);
    }
}

public class SlaWorker(
    IServiceScopeFactory _kapsamlar,
    ILogger<SlaWorker> _kayit) : BackgroundService
{
    /// <summary>Tur aralığı.</summary>
    private static readonly TimeSpan Aralik = TimeSpan.FromHours(1);

    /// <summary>
    /// İlk tur gecikmesi.
    /// </summary>
    /// <remarks>
    /// Açılışta hemen çalışmak, göç ve tohumlama sürerken veritabanına
    /// yüklenmek demek. İki dakika, uygulamanın ayağa kalkması için yeterli.
    /// </remarks>
    private static readonly TimeSpan IlkGecikme = TimeSpan.FromMinutes(2);

    /// <summary>Danışma kilidi anahtarı — bu işçiye özel sabit.</summary>
    private const long KilitAnahtari = 947_213_005;

    protected override async Task ExecuteAsync(CancellationToken durdur)
    {
        try
        {
            await Task.Delay(IlkGecikme, durdur);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!durdur.IsCancellationRequested)
        {
            try
            {
                await TurAsync(durdur);
            }
            catch (OperationCanceledException) when (durdur.IsCancellationRequested)
            {
                return;
            }
            catch (Exception hata)
            {
                // İşçi ÖLMEZ: bir turdaki hata bütün SLA takibini
                // durdurmamalı. Sonraki tur yeniden dener.
                _kayit.LogError(hata, "SLA turu başarısız.");
            }

            try
            {
                await Task.Delay(Aralik, durdur);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task TurAsync(CancellationToken durdur)
    {
        using var kapsam = _kapsamlar.CreateScope();
        var baglam = kapsam.ServiceProvider.GetRequiredService<AppDbContext>();

        // ÇOK ÖRNEKLİ DAĞITIM: kilidi alamayan kopya turu atlar.
        var kilit = await baglam.Database
            .SqlQuery<bool>($"SELECT pg_try_advisory_lock({KilitAnahtari}) AS \"Value\"")
            .FirstAsync(durdur);

        if (!kilit)
        {
            _kayit.LogInformation("SLA turu atlandı: kilit başka bir örnekte.");
            return;
        }

        try
        {
            var tarayici = kapsam.ServiceProvider.GetRequiredService<ISlaTarayici>();
            var sayi = await tarayici.TaraAsync(durdur);

            if (sayi > 0)
                _kayit.LogInformation("SLA turu: {Sayi} görev için süre aşımı bildirildi.", sayi);
        }
        finally
        {
            await baglam.Database
                .ExecuteSqlAsync($"SELECT pg_advisory_unlock({KilitAnahtari})", durdur);
        }
    }
}
