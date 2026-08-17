using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Storage;

namespace KentOS.Mini.Web.Services.V2;

public interface IDosyaServisi
{
    Task<List<AjandaPhotoDto>> EtkinlikFotografiYukleAsync(long etkinlikId, IFormFileCollection dosyalar);
    Task EtkinlikFotografiSilAsync(long fotografId);
    Task<RandevuDosyaDto> TalepDosyasiYukleAsync(long talepId, IFormFile dosya, string? aciklama);
    Task TalepDosyasiSilAsync(long dosyaId);

    /// <summary>Talep dosyasını BİRİM süzgecinden geçirerek indirir.</summary>
    Task<(Stream Akis, string Ad, string Tur)> TalepDosyasiAsync(long dosyaId);
}

/// <summary>
/// Etkinlik fotoğrafı ve talep dosyası yükleme.
///
/// <para>
/// Eski arayüzde bu mantık <c>AjandaController.UploadPhotos</c> ve
/// <c>RandevuController.UploadFile</c> içine gömülüydü. Buraya taşınması
/// aynı zamanda birkaç sessiz sorunu kapatıyor (aşağıda).
/// </para>
/// </summary>
public class DosyaServisi(
    AppDbContext _context,
    IFileStorage _depo,
    ICurrentUserService _mevcutKullanici,
    IAjandaService _ajandaService,
    IMessageService _mesajServisi,
    ILogger<DosyaServisi> _logger) : IDosyaServisi
{
    /// <summary>
    /// Yüklemelerin anahtar öneki. Veritabanında saklanan yolla birebir aynı
    /// tutulur (<c>/uploads/randevu/…</c>), böylece yerel diskten nesne
    /// deposuna geçerken tek bir kayıt bile güncellenmez.
    /// </summary>
    private const string EtkinlikKoku = "uploads/ajanda";
    private const string TalepKoku = "uploads/randevu";

    private const long EnBuyukFotograf = 5 * 1024 * 1024;   // 5 MB
    private const long EnBuyukDosya = 20 * 1024 * 1024;     // 20 MB

    /// <summary>
    /// İzin verilen fotoğraf türleri.
    /// </summary>
    /// <remarks>
    /// Yalnızca MIME türüne bakmak yetmez — istemci onu serbestçe uydurabilir.
    /// Uzantı de denetlenir ve dosya adı sunucuda ÜRETİLİR (istemciden gelen
    /// ad hiç kullanılmaz), böylece `../` içeren bir ad dizin dışına yazamaz.
    /// </remarks>
    private static readonly string[] FotografTurleri = ["image/jpeg", "image/png", "image/webp"];
    private static readonly string[] FotografUzantilari = [".jpg", ".jpeg", ".png", ".webp"];

    private static readonly string[] YasakUzantilar =
        [".exe", ".dll", ".bat", ".cmd", ".sh", ".ps1", ".js", ".jar", ".msi", ".scr", ".com"];

    // ─────────────────────────────────────────── etkinlik fotoğrafı

    public async Task<List<AjandaPhotoDto>> EtkinlikFotografiYukleAsync(
        long etkinlikId, IFormFileCollection dosyalar)
    {
        if (dosyalar.Count == 0)
        {
            throw new BusinessRuleException("Yüklenecek dosya seçilmedi.");
        }

        // Görünürlük kontrolü: gizli bir etkinliğe, onu göremeyen biri
        // fotoğraf yükleyememeli. Eski uç bunu HİÇ kontrol etmiyordu.
        _ = await _ajandaService.GetAsync(etkinlikId);

        var eklenenler = new List<AjandaPhoto>();
        var reddedilen = 0;

        foreach (var d in dosyalar)
        {
            var uzanti = Path.GetExtension(d.FileName).ToLowerInvariant();

            if (d.Length == 0 || d.Length > EnBuyukFotograf ||
                !FotografTurleri.Contains(d.ContentType) ||
                !FotografUzantilari.Contains(uzanti))
            {
                reddedilen++;
                continue;
            }

            var ad = $"{Guid.NewGuid()}{uzanti}";
            await using (var akis = d.OpenReadStream())
            {
                await _depo.SaveAsync(StorageArea.Public, $"{EtkinlikKoku}/{ad}", akis, d.ContentType);
            }

            var kayit = new AjandaPhoto
            {
                AjandaId = etkinlikId,
                Filename = ad,
                ContentType = d.ContentType,
            };
            _context.AjandaPhotos.Add(kayit);
            eklenenler.Add(kayit);
        }

        if (eklenenler.Count == 0)
        {
            // Eski uç sessizce `success: true` dönüyordu; kullanıcı hiçbir şey
            // yüklenmediğini ancak sayfayı yenileyince fark ediyordu.
            throw new BusinessRuleException(
                "Hiçbir dosya yüklenemedi. Yalnızca 5 MB'a kadar JPEG, PNG veya WEBP kabul edilir.");
        }

        await _context.SaveChangesAsync();

        if (reddedilen > 0)
        {
            _logger.LogInformation(
                "{Etkinlik} etkinliğine {Kabul} fotoğraf yüklendi, {Ret} dosya reddedildi.",
                etkinlikId, eklenenler.Count, reddedilen);
        }

        await FotografBildirimiGonderAsync(etkinlikId, eklenenler.Count);

        return eklenenler
            .Select(f => new AjandaPhotoDto
            {
                Id = f.Id,
                Filename = f.Filename,
                ContentType = f.ContentType,
                AjandaId = f.AjandaId,
            })
            .ToList();
    }

    public async Task EtkinlikFotografiSilAsync(long fotografId)
    {
        var foto = await _context.AjandaPhotos.FirstOrDefaultAsync(f => f.Id == fotografId)
            ?? throw new EntityNotFoundException($"{fotografId} kimlikli fotoğraf bulunamadı.");

        // Görünürlük kontrolü — gizli etkinliğin fotoğrafını yabancı silemesin.
        _ = await _ajandaService.GetAsync(foto.AjandaId);

        await _depo.DeleteAsync(StorageArea.Public, $"{EtkinlikKoku}/{foto.Filename}");

        _context.AjandaPhotos.Remove(foto);
        await _context.SaveChangesAsync();
    }

    // ────────────────────────────────────────────── talep dosyası

    public async Task<RandevuDosyaDto> TalepDosyasiYukleAsync(
        long talepId, IFormFile dosya, string? aciklama)
    {
        if (dosya is null || dosya.Length == 0)
        {
            throw new BusinessRuleException("Yüklenecek dosya seçilmedi.");
        }

        if (dosya.Length > EnBuyukDosya)
        {
            throw new BusinessRuleException("Dosya 20 MB'tan büyük olamaz.");
        }

        var uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
        if (YasakUzantilar.Contains(uzanti))
        {
            // Yürütülebilir dosyalar `wwwroot` altında statik olarak
            // sunuluyor; birinin indirip çalıştırması an meselesi.
            throw new BusinessRuleException($"\"{uzanti}\" uzantılı dosyalar yüklenemez.");
        }

        if (!await _context.Randevular.IgnoreQueryFilters().AnyAsync(r => r.Id == talepId))
        {
            throw new EntityNotFoundException($"{talepId} kimlikli talep bulunamadı.");
        }

        var ad = $"{Guid.NewGuid()}{uzanti}";
        await using (var akis = dosya.OpenReadStream())
        {
            await _depo.SaveAsync(StorageArea.Public, $"{TalepKoku}/{ad}", akis, dosya.ContentType);
        }

        var kayit = new RandevuDosya
        {
            RandevuId = talepId,
            // Görünen ad kullanıcının verdiği ad, DİSKTEKİ ad üretilen GUID.
            Ad = Path.GetFileName(dosya.FileName),
            Aciklama = aciklama,
            Path = $"/{TalepKoku}/{ad}",
            ContentType = dosya.ContentType,
            Size = dosya.Length,
            OlusturmaTarih = DateTime.Now,
        };

        _context.Dosyalar.Add(kayit);
        await _context.SaveChangesAsync();

        return new RandevuDosyaDto
        {
            Id = kayit.Id,
            Ad = kayit.Ad,
            Aciklama = kayit.Aciklama,
            Path = kayit.Path,
            ContentType = kayit.ContentType,
            Size = kayit.Size,
            OlusturmaTarih = kayit.OlusturmaTarih,
        };
    }

    public async Task TalepDosyasiSilAsync(long dosyaId)
    {
        var dosya = await _context.Dosyalar.FirstOrDefaultAsync(d => d.Id == dosyaId)
            ?? throw new EntityNotFoundException($"{dosyaId} kimlikli dosya bulunamadı.");

        if (!string.IsNullOrWhiteSpace(dosya.Path))
        {
            // Yol veritabanından geliyor; yıllar içinde elle yazılmış bozuk bir
            // değer silme işlemini patlatmamalı — kayıt yine de silinebilmeli.
            try
            {
                await _depo.DeleteAsync(StorageArea.Public, dosya.Path);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Geçersiz dosya yolu silinemedi: {Yol}", dosya.Path);
            }
        }

        _context.Dosyalar.Remove(dosya);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Talep dosyasını indirir — <b>kimlik denetimli</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dosyalar bugün <c>wwwroot/uploads</c> altında ve statik dosya ara
    /// katmanıyla <b>kimlik doğrulanmadan</b> servis ediliyor. Bu yol v1'in
    /// ve eski MVC arayüzünün bağlı olduğu davranış, dolayısıyla kapatılmıyor;
    /// ama yeni istemcilerin oraya bağlanması için de bir sebep yok.
    /// </para>
    /// <para>
    /// Süzgeç <b>talebin birimi</b>: bir kullanıcı yalnızca kendi biriminin
    /// talebine ait dosyayı indirebilir. Kayıt yoksa ya da başka birimdeyse
    /// aynı "bulunamadı" hatası döner — başkasının dosyasının VAR olduğunu
    /// doğrulamak bile bilgi sızdırır.
    /// </para>
    /// </remarks>
    public async Task<(Stream Akis, string Ad, string Tur)> TalepDosyasiAsync(long dosyaId)
    {
        var birimId = _mevcutKullanici.GetCurrentBirimId();

        var dosya = await _context.Dosyalar
            .AsNoTracking()
            .Include(d => d.Randevu)
            .IgnoreQueryFilters()   // arşivlenmiş talebin dosyası da inebilmeli
            .FirstOrDefaultAsync(d => d.Id == dosyaId && d.Randevu != null
                                   && d.Randevu.BirimId == birimId)
            ?? throw new EntityNotFoundException($"{dosyaId} kimlikli dosya bulunamadı.");

        if (string.IsNullOrWhiteSpace(dosya.Path))
        {
            throw new EntityNotFoundException("Dosyanın sunucuda bir yolu yok.");
        }

        var akis = await _depo.OpenReadAsync(StorageArea.Public, dosya.Path)
            ?? throw new EntityNotFoundException("Dosya sunucuda bulunamadı.");

        return (
            akis,
            dosya.Ad,
            string.IsNullOrWhiteSpace(dosya.ContentType)
                ? "application/octet-stream"
                : dosya.ContentType);
    }

    // ──────────────────────────────────────────────────── yardımcı

    private async Task FotografBildirimiGonderAsync(long etkinlikId, int adet)
    {
        var etkinlik = await _ajandaService.GetAsync(etkinlikId);

        // Gizli etkinliğin fotoğrafı BİRİME duyurulmaz — birim bildirimi
        // etkinliğin varlığını görmemesi gereken kişilere sızdırır.
        if (etkinlik.Gizli) return;

        var kim = await _mevcutKullanici.GetFullNameAsync();
        var mesaj = $"{kim} tarafından {DateTime.Now:dd MMMM yyyy HH:mm} tarihinde " +
                    $"{etkinlik.Baslik} konulu etkinliğe {adet} adet fotoğraf yüklendi.";

        var veri = new TokenDataDto(NotificationEntity.Ajanda, (int)etkinlikId, NotificationAction.OpenImages);

        await _mesajServisi.CreateForAllPersonAsync(
            _mevcutKullanici.GetCurrentBirimId(),
            "Yeni Fotoğraf Yüklendi",
            mesaj,
            SendMessageType.PushNotification,
            NotifikasyonTip.AgendaOnImageUpload,
            veri.ToJson());
    }
}
