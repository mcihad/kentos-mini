using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.V2.Form;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Options;
using KentOS.Mini.Web.Storage;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>Vatandaş yüzeyi ve yanıt yönetimi.</summary>
public interface IFormYanitServisi
{
    // ── vatandaş (anonim) ──
    Task<FormPortalDto> PortalFormuAsync(string guid, CancellationToken iptal = default);
    Task<FormYanitSonucuDto> GonderAsync(
        string guid, FormYanitIstegiDto istek, string? ip, string? tarayici,
        CancellationToken iptal = default);
    Task<FormTaslakSonucuDto> TaslakKaydetAsync(
        string guid, FormYanitIstegiDto istek, CancellationToken iptal = default);
    Task<FormYanitIstegiDto?> TaslakGetirAsync(
        string guid, string anahtar, CancellationToken iptal = default);

    /// <summary>
    /// Form alanına dosya yükler — TASLAK yanıt satırı açar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Ayrı uç, gönderimle birlikte değil.</b> Zorunlu bir dosya alanı
    /// doğrulamaya giriyor ve 12 MB'lık bir gövde doğrulamada düşerse her
    /// şey yeniden yüklenirdi. Çiçek teslim akışındaki "tek çağrı" kararı
    /// burada geçerli değil: orada ayrı uç, fotoğrafın KODSUZ
    /// yüklenebilmesi demekti; burada kapı zaten adresteki erişim anahtarı.
    /// </para>
    /// <para>
    /// Taslak satırı <b>yanıt sayacını ARTIRMAZ</b>: yoksa yüz dosya
    /// yükleyen biri formu kotasından kapatırdı.
    /// </para>
    /// </remarks>
    Task<FormDosyaSonucuDto> DosyaYukleAsync(
        string guid, string alanKimligi, string? surdurmeAnahtari,
        Stream icerik, string? dosyaAdi, string? icerikTipi, CancellationToken iptal = default);

    /// <summary>Yetkili tarafın dosya indirmesi.</summary>
    Task<(Stream Akis, string Ad, string Tip)> DosyaIndirAsync(
        long formId, long yanitId, long dosyaId, CancellationToken iptal = default);

    // ── yetkili ──
    Task<SayfaliSonuc<FormYanitOzetDto>> ListeAsync(
        long formId, FormYanitSuzgecDto suzgec, CancellationToken iptal = default);
    Task<FormYanitDetayDto> GetirAsync(long formId, long yanitId, CancellationToken iptal = default);
    Task<FormOzetRaporuDto> OzetAsync(long formId, CancellationToken iptal = default);
    Task SilAsync(long formId, long yanitId, CancellationToken iptal = default);
}

/// <inheritdoc cref="IFormYanitServisi"/>
public sealed class FormYanitServisi(
    AppDbContext _context,
    IFormServisi _formServisi,
    IInstitutionService _kurum,
    IFileStorage _depo,
    JwtOptions _jwtAyari) : IFormYanitServisi
{
    /// <summary>Aynı IP'nin bir forma bir saatte gönderebileceği yanıt.</summary>
    /// <remarks>
    /// IP başına hız sınırı ara katmanda zaten var; bu, FORM BAŞINA ikinci
    /// bir tavan. Kurumun tamamı tek NAT arkasında olabildiği için cömert,
    /// ama bir botun tek formu binlerce kez doldurmasını kesiyor.
    /// </remarks>
    private const int SaatlikIpSiniri = 20;

    // ═══════════════════════════════════════════════ vatandaş yüzeyi

    public async Task<FormPortalDto> PortalFormuAsync(string guid, CancellationToken iptal = default)
    {
        var form = await FormuBulAsync(guid, iptal);

        // TASLAK FORM HİÇ VAR OLMAMIŞ GİBİ davranır: 404. "Var ama
        // yayınlanmadı" demek, hazırlanan formların adresini denemeye değer
        // kılardı.
        if (form.Durum == FormDurumu.Taslak || form.YayinSurumId is null)
        {
            throw new EntityNotFoundException("Form bulunamadı.");
        }

        var surum = await _context.FormSurumleri.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == form.YayinSurumId, iptal)
            ?? throw new EntityNotFoundException("Form bulunamadı.");

        var (aliyor, sebep) = FormServisi.YanitDurumu(form);
        var tanim = FormServisi.TanimiCoz(surum.Tanim);
        var kurum = await _kurum.GetAsync(iptal);

        return new FormPortalDto
        {
            Baslik = form.Baslik,
            Aciklama = form.Aciklama,
            KurumAdi = kurum?.DisplayName ?? kurum?.Name,
            Erisim = form.Erisim,
            Tanim = tanim,
            SurumNo = surum.SurumNo,
            YanitAliyor = aliyor,
            KapaliSebebi = sebep,
            KaydetDevamEt = tanim.Ayarlar.KaydetDevamEt,
        };
    }

    public async Task<FormYanitSonucuDto> GonderAsync(
        string guid, FormYanitIstegiDto istek, string? ip, string? tarayici,
        CancellationToken iptal = default)
    {
        var form = await FormuBulAsync(guid, iptal);

        /*
          BOT TUZAĞI — sessizce başarılı görünüp atılır.

          Hata dönseydi bot tuzağın varlığını öğrenir ve bir dahaki sefere
          o alanı boş bırakırdı. Sahte bir takip numarası dönmek, tuzağı
          görünmez tutuyor.
        */
        if (!string.IsNullOrWhiteSpace(istek.Website))
        {
            return new FormYanitSonucuDto
            {
                TakipNo = FormServisi.TakipNoUret(),
                TesekkurMetni = form.TesekkurMetni,
            };
        }

        var (aliyor, sebep) = FormServisi.YanitDurumu(form);
        if (!aliyor) throw new BusinessRuleException(sebep ?? "Bu form yanıt kabul etmiyor.");

        var surum = await _context.FormSurumleri.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == form.YayinSurumId, iptal)
            ?? throw new EntityNotFoundException("Form bulunamadı.");

        var tanim = FormServisi.TanimiCoz(surum.Tanim);

        // ── kimlik kuralları ──
        var telefonSade = Telefon.Duzelt(istek.Telefon) is { } t && t.Length > 0
            ? new string(t.Where(char.IsDigit).ToArray())
            : null;

        if (form.Erisim == FormErisimi.TelefonDogrulamali
            && string.IsNullOrWhiteSpace(telefonSade))
        {
            throw new BusinessRuleException("Bu form telefon numarası ister.");
        }

        if (form.TekYanit && telefonSade is not null)
        {
            var varMi = await _context.FormYanitlari.AnyAsync(
                y => y.FormId == form.Id
                    && y.TelefonSade == telefonSade
                    && y.Durum == FormYanitDurumu.Gonderildi, iptal);

            if (varMi) throw new BusinessRuleException("Bu forma zaten yanıt verdiniz.");
        }

        // ── IP tavanı ──
        var ipOzeti = IpOzetle(ip);
        if (ipOzeti is not null)
        {
            var esik = DateTime.Now.AddHours(-1);
            var sayi = await _context.FormYanitlari.CountAsync(
                y => y.FormId == form.Id && y.IpOzeti == ipOzeti && y.GonderimTarihi > esik, iptal);

            if (sayi >= SaatlikIpSiniri)
            {
                throw new BusinessRuleException(
                    "Kısa sürede çok fazla yanıt gönderildi. Lütfen daha sonra deneyin.");
            }
        }

        // ── DOĞRULAMA: istemci hiç yokmuş gibi ──
        var sonuc = FormDogrulayici.Dogrula(tanim, istek.Cevaplar);
        if (!sonuc.Gecerli) throw AlanHatasi(sonuc.Hatalar);

        // Yarım kayıt varsa onu tamamla, yenisini açma.
        var yanit = istek.SurdurmeAnahtari is { Length: > 0 } anahtar
            ? await _context.FormYanitlari.FirstOrDefaultAsync(
                y => y.FormId == form.Id
                    && y.SurdurmeAnahtari == anahtar
                    && y.Durum == FormYanitDurumu.Taslak, iptal)
            : null;

        if (yanit is null)
        {
            yanit = new FormResponse
            {
                FormId = form.Id,
                TakipNo = FormServisi.TakipNoUret(),
            };
            _context.FormYanitlari.Add(yanit);
        }

        yanit.SurumId = surum.Id;
        yanit.Cevaplar = JsonSerializer.Serialize(sonuc.TemizCevaplar, FormServisi.JsonAyari);
        yanit.Durum = FormYanitDurumu.Gonderildi;
        yanit.GonderimTarihi = DateTime.Now;
        yanit.AdSoyad = istek.AdSoyad?.Trim();
        yanit.Telefon = istek.Telefon?.Trim();
        yanit.TelefonSade = telefonSade;
        yanit.Eposta = istek.Eposta?.Trim();
        yanit.IpOzeti = ipOzeti;
        yanit.Tarayici = tarayici?[..Math.Min(tarayici.Length, 250)];
        yanit.SurdurmeAnahtari = null;

        /*
          SAYAÇ ATOMİK ARTIRILIR.

          `form.YanitSayisi++` yapıp kaydetseydik iki eşzamanlı gönderim
          aynı değeri okuyup aynı değeri yazar ve sayaç geride kalırdı —
          yanıt sınırı da bu yüzden aşılabilirdi.
        */
        await _context.SaveChangesAsync(iptal);

        await _context.Formlar
            .Where(f => f.Id == form.Id)
            .ExecuteUpdateAsync(a => a.SetProperty(f => f.YanitSayisi, f => f.YanitSayisi + 1), iptal);

        return new FormYanitSonucuDto
        {
            TakipNo = yanit.TakipNo,
            TesekkurMetni = form.TesekkurMetni,
            TesekkurAdresi = form.TesekkurAdresi,
            Ozet = form.YanitOzetiGorunur ? OzetCikar(tanim, sonuc.TemizCevaplar) : null,
        };
    }

    public async Task<FormTaslakSonucuDto> TaslakKaydetAsync(
        string guid, FormYanitIstegiDto istek, CancellationToken iptal = default)
    {
        var form = await FormuBulAsync(guid, iptal);

        var (aliyor, sebep) = FormServisi.YanitDurumu(form);
        if (!aliyor) throw new BusinessRuleException(sebep ?? "Bu form yanıt kabul etmiyor.");

        var surum = await _context.FormSurumleri.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == form.YayinSurumId, iptal)
            ?? throw new EntityNotFoundException("Form bulunamadı.");

        var tanim = FormServisi.TanimiCoz(surum.Tanim);

        // Taslakta ZORUNLULUK aranmaz ama biçim aranır: bozuk veri
        // saklanırsa gönderim anında değil, günler sonra patlar.
        var sonuc = FormDogrulayici.Dogrula(tanim, istek.Cevaplar, taslakMi: true);
        if (!sonuc.Gecerli) throw AlanHatasi(sonuc.Hatalar);

        var yanit = istek.SurdurmeAnahtari is { Length: > 0 } a
            ? await _context.FormYanitlari.FirstOrDefaultAsync(
                y => y.FormId == form.Id && y.SurdurmeAnahtari == a
                    && y.Durum == FormYanitDurumu.Taslak, iptal)
            : null;

        if (yanit is null)
        {
            yanit = new FormResponse
            {
                FormId = form.Id,
                SurumId = surum.Id,
                TakipNo = FormServisi.TakipNoUret(),
                // Takip numarası KISA ve okunabilir; sürdürme anahtarı ise
                // tahmin edilemez olmalı — numarayı bilen biri başkasının
                // yarım formunu açamamalı.
                SurdurmeAnahtari = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),
            };
            _context.FormYanitlari.Add(yanit);
        }

        yanit.Cevaplar = JsonSerializer.Serialize(sonuc.TemizCevaplar, FormServisi.JsonAyari);
        await _context.SaveChangesAsync(iptal);

        return new FormTaslakSonucuDto { SurdurmeAnahtari = yanit.SurdurmeAnahtari! };
    }

    public async Task<FormYanitIstegiDto?> TaslakGetirAsync(
        string guid, string anahtar, CancellationToken iptal = default)
    {
        var form = await FormuBulAsync(guid, iptal);

        var yanit = await _context.FormYanitlari.AsNoTracking()
            .FirstOrDefaultAsync(y => y.FormId == form.Id
                && y.SurdurmeAnahtari == anahtar
                && y.Durum == FormYanitDurumu.Taslak, iptal);

        if (yanit is null) return null;

        return new FormYanitIstegiDto
        {
            Cevaplar = CevaplariCoz(yanit.Cevaplar),
            AdSoyad = yanit.AdSoyad,
            Telefon = yanit.Telefon,
            Eposta = yanit.Eposta,
            SurdurmeAnahtari = anahtar,
        };
    }

    /// <summary>Form dosyası üst sınırı ve izinli uzantılar.</summary>
    /// <remarks>
    /// Uç anonim: sınırsız yükleme diski doldurabilirdi. Uzantı listesi
    /// alanın kendi ayarından daraltılabiliyor ama bu tavan her zaman
    /// geçerli — çalışabilir uzantılar (`.html`, `.svg`…) hiçbir formda
    /// açılamıyor.
    /// </remarks>
    private static readonly string[] IzinliUzantilar =
    [
        ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".heic",
        ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv", ".zip",
    ];

    private const long DosyaSiniri = 12 * 1024 * 1024;

    public async Task<FormDosyaSonucuDto> DosyaYukleAsync(
        string guid, string alanKimligi, string? surdurmeAnahtari,
        Stream icerik, string? dosyaAdi, string? icerikTipi, CancellationToken iptal = default)
    {
        var form = await FormuBulAsync(guid, iptal);

        var (aliyor, sebep) = FormServisi.YanitDurumu(form);
        if (!aliyor) throw new BusinessRuleException(sebep ?? "Bu form yanıt kabul etmiyor.");

        var surum = await _context.FormSurumleri.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == form.YayinSurumId, iptal)
            ?? throw new EntityNotFoundException("Form bulunamadı.");

        var tanim = FormServisi.TanimiCoz(surum.Tanim);

        var alan = FormDogrulayici.TumAlanlar(tanim)
            .FirstOrDefault(a => a.Kimlik == alanKimligi
                && a.Tip == Application.Enums.FormAlanTipi.Dosya)
            ?? throw new BusinessRuleException("Bu formda böyle bir dosya alanı yok.");

        var uzanti = Path.GetExtension(dosyaAdi ?? string.Empty).ToLowerInvariant();

        // Alanın kendi listesi varsa ONA da uymalı; tavan her hâlükârda bizim.
        var alanListesi = alan.Dogrulama?.DosyaUzantilari;

        if (!IzinliUzantilar.Contains(uzanti)
            || (alanListesi is { Count: > 0 } && !alanListesi.Contains(uzanti)))
        {
            throw new BusinessRuleException(
                "Bu dosya türü yüklenemez. İzin verilenler: "
                + string.Join(", ", alanListesi is { Count: > 0 } ? alanListesi : IzinliUzantilar));
        }

        using var bellek = new MemoryStream();
        await icerik.CopyToAsync(bellek, iptal);

        var tavan = alan.Dogrulama?.EnCokDosyaMb is { } mb && mb > 0
            ? Math.Min(DosyaSiniri, mb * 1024L * 1024L)
            : DosyaSiniri;

        if (bellek.Length == 0) throw new BusinessRuleException("Dosya okunamadı.");
        if (bellek.Length > tavan)
        {
            throw new BusinessRuleException($"Dosya çok büyük (en fazla {tavan / 1024 / 1024} MB).");
        }

        // TASLAK SATIR: dosyanın bağlanacağı bir yanıt gerekiyor. Nullable
        // bir yabancı anahtar + ikinci bir durum makinesi yerine zaten var
        // olan `Taslak` durumu kullanılıyor — sürdürme özelliğini de
        // bedavaya veriyor.
        var yanit = surdurmeAnahtari is { Length: > 0 } a
            ? await _context.FormYanitlari.FirstOrDefaultAsync(
                y => y.FormId == form.Id && y.SurdurmeAnahtari == a
                    && y.Durum == FormYanitDurumu.Taslak, iptal)
            : null;

        if (yanit is null)
        {
            yanit = new FormResponse
            {
                FormId = form.Id,
                SurumId = surum.Id,
                TakipNo = FormServisi.TakipNoUret(),
                SurdurmeAnahtari = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),
            };
            _context.FormYanitlari.Add(yanit);
            await _context.SaveChangesAsync(iptal);
        }

        /*
          GİZLİ ALANDA saklanır. Vatandaşın yüklediği belge kimlik fotokopisi
          olabiliyor; `wwwroot/uploads` altındaki her şey kimlik doğrulanmadan
          servis ediliyor ve orası bu iş için yanlış yer.

          Ad GUID'den türetiliyor: gelen ad yol ayracı taşıyabiliyor.
        */
        var depoAnahtari = $"form/{form.Id}/{Guid.NewGuid():N}{uzanti}";
        await _depo.SaveAsync(StorageArea.Private, depoAnahtari, bellek.ToArray(), icerikTipi);

        var kayit = new FormResponseFile
        {
            YanitId = yanit.Id,
            AlanKimligi = alanKimligi,
            Ad = Path.GetFileName(dosyaAdi ?? "belge") ?? "belge",
            Anahtar = depoAnahtari,
            IcerikTipi = icerikTipi,
            Boyut = bellek.Length,
        };

        _context.FormYanitDosyalari.Add(kayit);
        await _context.SaveChangesAsync(iptal);

        return new FormDosyaSonucuDto
        {
            DosyaId = kayit.Id,
            Ad = kayit.Ad,
            Boyut = kayit.Boyut,
            SurdurmeAnahtari = yanit.SurdurmeAnahtari!,
        };
    }

    public async Task<(Stream Akis, string Ad, string Tip)> DosyaIndirAsync(
        long formId, long yanitId, long dosyaId, CancellationToken iptal = default)
    {
        await ((FormServisi)_formServisi).ErisebilirMiAsync(formId, iptal);

        var d = await _context.FormYanitDosyalari.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dosyaId && x.YanitId == yanitId, iptal)
            ?? throw new EntityNotFoundException("Dosya bulunamadı.");

        // Yanıtın gerçekten BU forma ait olduğu ayrıca doğrulanıyor: yanıt
        // kimliği tahmin edilerek başka bir formun dosyasına ulaşılmamalı.
        var sahiplik = await _context.FormYanitlari.AnyAsync(
            y => y.Id == yanitId && y.FormId == formId, iptal);

        if (!sahiplik) throw new EntityNotFoundException("Dosya bulunamadı.");

        var akis = await _depo.OpenReadAsync(StorageArea.Private, d.Anahtar, iptal)
            ?? throw new EntityNotFoundException("Dosya bulunamadı.");

        return (akis, d.Ad, d.IcerikTipi ?? "application/octet-stream");
    }

    // ═══════════════════════════════════════════════ yetkili yüzeyi

    public async Task<SayfaliSonuc<FormYanitOzetDto>> ListeAsync(
        long formId, FormYanitSuzgecDto suzgec, CancellationToken iptal = default)
    {
        await ((FormServisi)_formServisi).ErisebilirMiAsync(formId, iptal);

        var sorgu = _context.FormYanitlari.AsNoTracking()
            .Where(y => y.FormId == formId);

        sorgu = suzgec.Durum is { } d
            ? sorgu.Where(y => y.Durum == d)
            // Taslaklar varsayılan listede YOK: yarım kalmış bir form
            // "gelen yanıt" değil ve sayıyı yanıltıyor.
            : sorgu.Where(y => y.Durum == FormYanitDurumu.Gonderildi);

        if (suzgec.Baslangic is { } b) sorgu = sorgu.Where(y => y.GonderimTarihi >= b);
        if (suzgec.Bitis is { } s) sorgu = sorgu.Where(y => y.GonderimTarihi < s.AddDays(1));

        if (!string.IsNullOrWhiteSpace(suzgec.Arama))
        {
            var a = suzgec.Arama.Trim();
            sorgu = sorgu.Where(y => y.TakipNo == a.ToUpperInvariant()
                || (y.AdSoyad != null && EF.Functions.ILike(y.AdSoyad, $"%{a}%"))
                || (y.Telefon != null && EF.Functions.ILike(y.Telefon, $"%{a}%")));
        }

        /*
          "ŞU ALANA ŞU CEVABI VERENLER" — JSONB üzerinde.

          `cevaplar @> '{"alan":"deger"}'` GIN indeksini kullanıyor; tam
          tarama değil. Bunu ilişkisel bir "yanıt kalemi" tablosuyla yapmak
          her okumada pivot demekti.
        */
        if (!string.IsNullOrWhiteSpace(suzgec.AlanKimligi)
            && !string.IsNullOrWhiteSpace(suzgec.AlanDegeri))
        {
            /*
              ARANAN ŞEKİL SARMALAYICIYLA AYNI OLMALI.

              Saklanan belge `{"puan":{"deger":4}}`; düz
              `{"puan":"4"}` aramak HİÇ EŞLEŞMEZ — üstelik sessizce, sıfır
              sonuçla. Ölçüldü: süzgeç eklendiğinde bütün sorgular boş
              dönüyordu.

              TİP DE ÖNEMLİ: JSONB'de `4` ile `"4"` farklı değerler.
              Sayıya çevrilebilen bir arama metni sayı olarak, çevrilemeyen
              metin olarak aranıyor. İkisini birden denemek yerine tek
              denemek yeterli: alanın tipi tanımda belli ve doğrulayıcı
              sayıyı sayı olarak yazıyor.
            */
            object aranan = decimal.TryParse(
                suzgec.AlanDegeri, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var sayi)
                ? sayi
                : suzgec.AlanDegeri;

            var arananJson = JsonSerializer.Serialize(
                new Dictionary<string, Dictionary<string, object>>
                {
                    [suzgec.AlanKimligi] = new() { [FormDogrulayici.DegerAlani] = aranan },
                });

            sorgu = sorgu.Where(y => EF.Functions.JsonContains(y.Cevaplar, arananJson));
        }

        var toplam = await sorgu.LongCountAsync(iptal);

        var kayitlar = await sorgu
            .OrderByDescending(y => y.GonderimTarihi ?? y.BaslamaTarihi)
            .Skip((suzgec.Sayfa - 1) * suzgec.Boyut)
            .Take(suzgec.Boyut)
            .Select(y => new
            {
                y.Id, y.TakipNo, y.Durum, y.AdSoyad, y.Telefon, y.Eposta,
                y.GonderimTarihi, y.Cevaplar,
                SurumNo = y.Surum!.SurumNo,
            })
            .ToListAsync(iptal);

        /*
          TANIM BİR KEZ yükleniyor, yanıt başına değil.

          Önizlemede seçenek KİMLİĞİ ("hiz: orta") yazmak kullanıcıya hiçbir
          şey söylemiyor; etikete çevirmek için tanım gerekiyor. Yanıt başına
          yüklemek 25 satırlık bir sayfada 25 sorgu demekti.
        */
        var form = await _context.Formlar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == formId, iptal);

        var surumId = form?.YayinSurumId;
        var tanimJson = surumId is { } sid
            ? await _context.FormSurumleri.AsNoTracking()
                .Where(v => v.Id == sid).Select(v => v.Tanim).FirstOrDefaultAsync(iptal)
            : null;

        var tanim = FormServisi.TanimiCoz(tanimJson);

        var veriler = kayitlar.Select(y => new FormYanitOzetDto
        {
            Id = y.Id,
            TakipNo = y.TakipNo,
            Durum = y.Durum,
            AdSoyad = y.AdSoyad,
            Telefon = y.Telefon,
            Eposta = y.Eposta,
            GonderimTarihi = y.GonderimTarihi,
            SurumNo = y.SurumNo,
            Onizleme = Onizleme(y.Cevaplar, tanim),
        }).ToList();

        return SayfaliSonuc<FormYanitOzetDto>.Olustur(veriler, toplam, suzgec);
    }

    public async Task<FormYanitDetayDto> GetirAsync(
        long formId, long yanitId, CancellationToken iptal = default)
    {
        await ((FormServisi)_formServisi).ErisebilirMiAsync(formId, iptal);

        var yanit = await _context.FormYanitlari.AsNoTracking()
            .Include(y => y.Surum)
            .Include(y => y.Dosyalar)
            .FirstOrDefaultAsync(y => y.Id == yanitId && y.FormId == formId, iptal)
            ?? throw new EntityNotFoundException("Yanıt bulunamadı.");

        return new FormYanitDetayDto
        {
            Id = yanit.Id,
            TakipNo = yanit.TakipNo,
            Durum = yanit.Durum,
            AdSoyad = yanit.AdSoyad,
            Telefon = yanit.Telefon,
            Eposta = yanit.Eposta,
            GonderimTarihi = yanit.GonderimTarihi,
            SurumNo = yanit.Surum?.SurumNo ?? 0,

            // YANITIN VERİLDİĞİ sürümün tanımı — güncel olan değil. Soru
            // metni sonradan değişmiş olabilir ve cevap, vatandaşın gördüğü
            // metnin altında okunmalı.
            Tanim = FormServisi.TanimiCoz(yanit.Surum?.Tanim),
            Cevaplar = CevaplariCoz(yanit.Cevaplar),
            Dosyalar = yanit.Dosyalar.Select(d => new FormYanitDosyasiDto
            {
                Id = d.Id, AlanKimligi = d.AlanKimligi, Ad = d.Ad,
                Boyut = d.Boyut, IcerikTipi = d.IcerikTipi,
            }).ToList(),
        };
    }

    public async Task<FormOzetRaporuDto> OzetAsync(long formId, CancellationToken iptal = default)
    {
        var form = await ((FormServisi)_formServisi).ErisebilirMiAsync(formId, iptal);

        var surum = form.YayinSurumId is { } id
            ? await _context.FormSurumleri.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, iptal)
            : null;

        var tanim = FormServisi.TanimiCoz(surum?.Tanim);

        var yanitlar = await _context.FormYanitlari.AsNoTracking()
            .Where(y => y.FormId == formId && y.Durum == FormYanitDurumu.Gonderildi)
            .Select(y => new { y.Cevaplar, y.GonderimTarihi })
            .ToListAsync(iptal);

        var cozulmus = yanitlar.Select(y => CevaplariCoz(y.Cevaplar)).ToList();

        var rapor = new FormOzetRaporuDto
        {
            FormId = formId,
            Baslik = form.Baslik,
            ToplamYanit = yanitlar.Count,
            IlkYanit = yanitlar.Min(y => y.GonderimTarihi),
            SonYanit = yanitlar.Max(y => y.GonderimTarihi),
        };

        foreach (var alan in FormDogrulayici.TumAlanlar(tanim))
        {
            if (FormDogrulayici.BlokMu(alan.Tip)) continue;
            rapor.Alanlar.Add(AlanOzeti(alan, cozulmus));
        }

        return rapor;
    }

    public async Task SilAsync(long formId, long yanitId, CancellationToken iptal = default)
    {
        await ((FormServisi)_formServisi).ErisebilirMiAsync(formId, iptal);

        var yanit = await _context.FormYanitlari
            .FirstOrDefaultAsync(y => y.Id == yanitId && y.FormId == formId, iptal)
            ?? throw new EntityNotFoundException("Yanıt bulunamadı.");

        // GEÇERSİZ İŞARETLENİR, SİLİNMEZ. "Kaç kişi yanıtladı" ile "kaçı
        // sayıldı" ayrı bilgi; sert silme ikisini de kaybettirirdi.
        yanit.Durum = FormYanitDurumu.Gecersiz;
        await _context.SaveChangesAsync(iptal);

        await _context.Formlar
            .Where(f => f.Id == formId && f.YanitSayisi > 0)
            .ExecuteUpdateAsync(a => a.SetProperty(f => f.YanitSayisi, f => f.YanitSayisi - 1), iptal);
    }

    // ═══════════════════════════════════════════════ yardımcılar

    /// <summary>
    /// Alan hatalarını tek bir iş kuralı hatasına indirger.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ayrı bir istisna tipi ve filtre kolu açmak yerine mevcut
    /// <see cref="BusinessRuleException"/> kullanılıyor: v2 hata gövdesi
    /// zaten RFC 7807 ve istemci <c>detail</c> alanını okuyor. Yeni bir
    /// istisna tipi, <c>V2HataFiltresi</c>'ne yeni bir kol ve istemciye
    /// yeni bir hata şekli demekti.
    /// </para>
    /// <para>
    /// <b>Alan kimlikleri mesajda taşınıyor</b> ki oynatıcı hatayı doğru
    /// alanın altına yazabilsin; ilk alanın adı öne alınıyor çünkü kullanıcı
    /// sayfanın neresine bakacağını bilmeli.
    /// </para>
    /// </remarks>
    private static BusinessRuleException AlanHatasi(Dictionary<string, string> hatalar)
    {
        var ozet = string.Join(" · ", hatalar.Select(h => $"{h.Key}: {h.Value}"));
        return new BusinessRuleException($"Form doğrulanamadı — {ozet}");
    }

    private async Task<Form> FormuBulAsync(string guid, CancellationToken iptal) =>
        await _context.Formlar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.ErisimAnahtari == guid && !f.Silindi, iptal)
        ?? throw new EntityNotFoundException("Form bulunamadı.");

    /// <summary>
    /// IP'nin TUZLANMIŞ özeti — ham adres saklanmaz.
    /// </summary>
    /// <remarks>
    /// Ham IP kişisel veri ve saklanmasının tek gerekçesi kötüye kullanımı
    /// ayırt etmek; bunun için özet yetiyor. Tuz olarak JWT imza anahtarı
    /// kullanılıyor: kuruma özel, zaten gizli ve ayrı bir sır yönetmeyi
    /// gerektirmiyor. Tuzsuz bir özet, IPv4 uzayı küçük olduğu için kaba
    /// kuvvetle geri çevrilebilirdi.
    /// </remarks>
    private string? IpOzetle(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;

        var veri = Encoding.UTF8.GetBytes($"{_jwtAyari.Secret}|form-ip|{ip}");
        return Convert.ToHexString(SHA256.HashData(veri))[..32];
    }

    internal static Dictionary<string, object?> CevaplariCoz(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, FormServisi.JsonAyari)
                ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>
    /// Liste satırındaki tek satırlık özet — ETİKETLİ.
    /// </summary>
    /// <remarks>
    /// Alan sırası tanımdan geliyor, JSONB'nin anahtar sırasından değil:
    /// önizlemede görünen ilk üç cevap, formun ilk üç sorusu olmalı.
    /// </remarks>
    private static string? Onizleme(string json, FormTanimiDto tanim)
    {
        var c = CevaplariCoz(json);
        if (c.Count == 0) return null;

        var parcalar = new List<string>();

        foreach (var alan in FormDogrulayici.TumAlanlar(tanim))
        {
            if (parcalar.Count >= 3) break;
            if (FormDogrulayici.BlokMu(alan.Tip)) continue;
            if (!c.TryGetValue(alan.Kimlik, out var sarmal)) continue;

            var metin = EtiketliDeger(alan, sarmal);
            if (metin.Length > 0) parcalar.Add(metin);
        }

        // Tanım boşsa (bozuk sürüm) ham değerlere düş: boş bir satır
        // göstermektense okunabilir olmayan bir şey göstermek yeğ.
        if (parcalar.Count == 0)
        {
            parcalar.AddRange(c.Values
                .Select(v => MetneCevir(FormDogrulayici.Normalize(FormDogrulayici.Deger(v))))
                .Where(x => x.Length > 0).Take(3));
        }

        var birlesik = string.Join(" · ", parcalar);
        return birlesik.Length > 120 ? birlesik[..120] + "…" : birlesik;
    }

    private static string MetneCevir(object? d) => d switch
    {
        null => string.Empty,
        List<string> l => string.Join(", ", l),
        Dictionary<string, object?> m => string.Join(", ", m.Select(x => $"{x.Key}: {MetneCevir(x.Value)}")),
        bool b => b ? "Evet" : "Hayır",
        _ => d.ToString() ?? string.Empty,
    };

    private static List<FormCevapOzetiDto> OzetCikar(
        FormTanimiDto tanim, Dictionary<string, object?> cevaplar)
    {
        var satirlar = new List<FormCevapOzetiDto>();

        foreach (var alan in FormDogrulayici.TumAlanlar(tanim))
        {
            if (FormDogrulayici.BlokMu(alan.Tip)) continue;
            if (!cevaplar.TryGetValue(alan.Kimlik, out var deger)) continue;

            satirlar.Add(new FormCevapOzetiDto
            {
                Etiket = alan.Etiket,
                Deger = EtiketliDeger(alan, deger),
            });
        }

        return satirlar;
    }

    /// <summary>
    /// Ham değeri kullanıcının gördüğü etikete çevirir.
    /// </summary>
    /// <remarks>
    /// Seçim alanlarında JSONB'de seçenek KİMLİĞİ duruyor; sonuç sayfasında
    /// "sec_3" yazmak vatandaşa hiçbir şey söylemez.
    /// </remarks>
    private static string EtiketliDeger(FormAlaniDto alan, object? sarmal)
    {
        /*
          SARMALAYICI BURADA AÇILIR.

          Cevap `{ "deger": …, "metin": … }` şeklinde saklanıyor.
          Açılmasaydı sonuç sayfası "deger: Ayşe Yılmaz" yazardı — ölçüldü.
        */
        var d = FormDogrulayici.Normalize(FormDogrulayici.Deger(sarmal));
        var serbest = FormDogrulayici.SerbestMetin(sarmal);

        var sozlukler = (alan.Secenekler ?? []).Concat(alan.Sutunlar ?? [])
            .ToDictionary(x => x.Kimlik, x => x.Etiket);

        string Cevir(string k) => sozlukler.TryGetValue(k, out var e) ? e : k;

        var metin = d switch
        {
            null => string.Empty,
            List<string> l => string.Join(", ", l.Select(Cevir)),

            // Matris: satır etiketleriyle. İç değerler sarmalı DEĞİL,
            // doğrudan seçenek kimliği — bu yüzden `Cevir` ile çevriliyor.
            Dictionary<string, object?> m => string.Join(" · ", m.Select(x =>
            {
                var satirEtiketi = (alan.Satirlar ?? [])
                    .FirstOrDefault(s => s.Kimlik == x.Key)?.Etiket ?? x.Key;
                var ic = FormDogrulayici.Normalize(x.Value);
                var icMetin = ic is List<string> ll
                    ? string.Join(", ", ll.Select(Cevir))
                    : Cevir(ic?.ToString() ?? string.Empty);
                return $"{satirEtiketi}: {icMetin}";
            })),

            bool b => b ? "Evet" : "Hayır",
            string s => Cevir(s),
            _ => d.ToString() ?? string.Empty,
        };

        // "Diğer" seçildiyse serbest metin PARANTEZ İÇİNDE: kullanıcı ne
        // seçtiğini de ne yazdığını da tek satırda görmeli.
        return serbest is { Length: > 0 } ? $"{metin} ({serbest})" : metin;
    }

    private static FormAlanOzetiDto AlanOzeti(
        FormAlaniDto alan, List<Dictionary<string, object?>> yanitlar)
    {
        var ozet = new FormAlanOzetiDto
        {
            AlanKimligi = alan.Kimlik,
            Etiket = alan.Etiket,
            Tip = alan.Tip,
        };

        var degerler = yanitlar
            .Where(y => y.ContainsKey(alan.Kimlik))
            .Select(y => FormDogrulayici.Normalize(FormDogrulayici.Deger(y[alan.Kimlik])))
            .Where(v => v is not null)
            .ToList();

        ozet.YanitSayisi = degerler.Count;
        if (degerler.Count == 0) return ozet;

        var secimli = alan.Tip is FormAlanTipi.TekSecim or FormAlanTipi.CokSecim
            or FormAlanTipi.AcilirListe or FormAlanTipi.CokluAcilirListe
            or FormAlanTipi.EvetHayir;

        if (secimli)
        {
            var sayac = new Dictionary<string, int>();

            foreach (var d in degerler)
            {
                foreach (var k in d is List<string> l ? l : [MetneCevir(d)])
                {
                    sayac[k] = sayac.GetValueOrDefault(k) + 1;
                }
            }

            var etiketler = (alan.Secenekler ?? []).ToDictionary(x => x.Kimlik, x => x.Etiket);

            ozet.Dagilim = sayac
                .OrderByDescending(x => x.Value)
                .Select(x => new FormDagilimDto
                {
                    Etiket = etiketler.TryGetValue(x.Key, out var e) ? e : x.Key,
                    Adet = x.Value,
                    Yuzde = Math.Round(x.Value * 100.0 / degerler.Count, 1),
                })
                .ToList();

            return ozet;
        }

        if (alan.Tip is FormAlanTipi.Sayi or FormAlanTipi.Olcek
            or FormAlanTipi.Nps or FormAlanTipi.Yildiz)
        {
            var sayilar = degerler.Select(d => d switch
            {
                decimal m => (double?)m,
                double db => db,
                int i => i,
                string s when double.TryParse(s, out var r) => r,
                _ => null,
            }).Where(x => x is not null).Select(x => x!.Value).ToList();

            if (sayilar.Count > 0) ozet.Ortalama = Math.Round(sayilar.Average(), 2);
            return ozet;
        }

        // Metin tiplerinde son birkaç cevap: dağılım anlamsız, örnek yararlı.
        ozet.Ornekler = degerler.TakeLast(5).Select(MetneCevir)
            .Where(s => s.Length > 0).ToList();

        return ozet;
    }
}
