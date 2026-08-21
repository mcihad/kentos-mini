using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.V2.Form;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Services;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>Dinamik form yönetimi — yetkili yüzeyi.</summary>
public interface IFormServisi
{
    Task<SayfaliSonuc<FormOzetDto>> ListeAsync(FormSuzgecDto suzgec, CancellationToken iptal = default);
    Task<FormDetayDto> GetirAsync(long id, CancellationToken iptal = default);
    Task<FormDetayDto> OlusturAsync(FormKayitDto istek, CancellationToken iptal = default);
    Task<FormDetayDto> GuncelleAsync(long id, FormKayitDto istek, CancellationToken iptal = default);

    /// <summary>Çalışılan tanımı dondurup yayına alır.</summary>
    Task<FormDetayDto> YayinlaAsync(long id, CancellationToken iptal = default);

    /// <summary>Yanıt kabulünü açar/kapatır (yayından kaldırmadan).</summary>
    Task<FormDetayDto> DurumDegistirAsync(long id, FormDurumu durum, CancellationToken iptal = default);

    /// <summary>Formu tanımıyla birlikte kopyalar.</summary>
    Task<FormDetayDto> KopyalaAsync(long id, CancellationToken iptal = default);

    Task SilAsync(long id, CancellationToken iptal = default);
}

/// <inheritdoc cref="IFormServisi"/>
public sealed class FormServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici,
    IEtkinBirim _etkinBirim,
    IAdresCozucu _adresCozucu,
    IInstitutionService _kurum) : IFormServisi
{
    /// <summary>
    /// Portal bayrağı — istek başına BİR KEZ okunur.
    /// </summary>
    /// <remarks>
    /// Liste 25 satır dönüyor ve her satırın "yanıt alıyor mu" kararı bu
    /// bayrağa bakıyor; satır başına sormak 25 sorgu demekti. Kurum kaydı
    /// zaten önbellekli ama alan yine de tek yerde tutuluyor.
    /// </remarks>
    private bool? _portalAcik;

    private async Task<bool> PortalAcikAsync(CancellationToken iptal)
        => _portalAcik ??= (await _kurum.GetAsync(iptal)).FormPortalEnabled;

    /// <summary>
    /// Tanımın JSON serileştirme ayarı — TEK YERDE.
    /// </summary>
    /// <remarks>
    /// Yazma ve okuma aynı ayarı kullanmak zorunda. Ayrı ayrı kurulsaydı
    /// biri <c>camelCase</c>, öteki <c>PascalCase</c> yazar ve tanım
    /// sessizce boş okunurdu — hata vermeden, yalnızca form boş açılarak.
    /// </remarks>
    internal static readonly JsonSerializerOptions JsonAyari = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    // ── okuma ──────────────────────────────────────────────────────────

    public async Task<SayfaliSonuc<FormOzetDto>> ListeAsync(
        FormSuzgecDto suzgec, CancellationToken iptal = default)
    {
        var kapsam = await _etkinBirim.KapsamAsync(altBirimlerDahil: true, iptal);

        var sorgu = _context.Formlar
            .AsNoTracking()
            .Where(f => !f.Silindi)
            // BİRİM KAPISI: formu açan birim ve alt birimleri görür.
            // Birimi olmayan (eski) kayıtlar herkese açık kalmıyor;
            // görünmüyorlar — sahipsiz bir formu herkese göstermek,
            // yanıtlarını da herkese göstermek demekti.
            .Where(f => f.BirimId != null && kapsam.Contains(f.BirimId.Value));

        if (suzgec.Durum is { } d) sorgu = sorgu.Where(f => f.Durum == d);
        else sorgu = sorgu.Where(f => f.Durum != FormDurumu.Arsiv);

        if (!string.IsNullOrWhiteSpace(suzgec.Arama))
        {
            var a = suzgec.Arama.Trim();
            sorgu = sorgu.Where(f => EF.Functions.ILike(f.Baslik, $"%{a}%")
                || (f.Aciklama != null && EF.Functions.ILike(f.Aciklama, $"%{a}%")));
        }

        var toplam = await sorgu.LongCountAsync(iptal);

        var kayitlar = await sorgu
            .OrderByDescending(f => f.OlusturmaTarihi)
            .Skip((suzgec.Sayfa - 1) * suzgec.Boyut)
            .Take(suzgec.Boyut)
            .Select(f => new { Form = f, SurumNo = (int?)f.YayinSurumu!.SurumNo })
            .ToListAsync(iptal);

        var portalAcik = await PortalAcikAsync(iptal);
        var veriler = kayitlar.Select(x => Ozet(x.Form, x.SurumNo, portalAcik)).ToList();
        return SayfaliSonuc<FormOzetDto>.Olustur(veriler, toplam, suzgec);
    }

    public async Task<FormDetayDto> GetirAsync(long id, CancellationToken iptal = default)
    {
        var form = await ErisebilirMiAsync(id, iptal);

        // ÇALIŞILAN tanım en son sürümdür; yayındaki ondan eski olabilir.
        var sonSurum = await _context.FormSurumleri
            .AsNoTracking()
            .Where(s => s.FormId == id)
            .OrderByDescending(s => s.SurumNo)
            .FirstOrDefaultAsync(iptal);

        var yayin = form.YayinSurumId is { } ysid
            ? await _context.FormSurumleri.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == ysid, iptal)
            : null;

        var detay = new FormDetayDto
        {
            Tanim = TanimiCoz(sonSurum?.Tanim),
            YayinlanmamisDegisiklik = sonSurum is not null && sonSurum.Id != form.YayinSurumId,
            TesekkurMetni = form.TesekkurMetni,
            TesekkurAdresi = form.TesekkurAdresi,
            YanitOzetiGorunur = form.YanitOzetiGorunur,
            SonuclarHerkeseAcik = form.SonuclarHerkeseAcik,
            TekYanit = form.TekYanit,
        };

        Doldur(detay, form, yayin?.SurumNo, await PortalAcikAsync(iptal));
        return detay;
    }

    // ── yazma ──────────────────────────────────────────────────────────

    public async Task<FormDetayDto> OlusturAsync(FormKayitDto istek, CancellationToken iptal = default)
    {
        var birim = await _etkinBirim.IdAsync(iptal);
        if (birim <= 0) throw new BusinessRuleException("Form açmak için bir birime bağlı olmalısınız.");

        TarihleriDogrula(istek);
        TanimiDogrula(istek.Tanim);

        var form = new Form
        {
            Baslik = istek.Baslik.Trim(),
            Aciklama = istek.Aciklama,
            Erisim = istek.Erisim,
            BirimId = birim,
            BaslangicTarihi = istek.BaslangicTarihi,
            BitisTarihi = istek.BitisTarihi,
            YanitSiniri = istek.YanitSiniri,
            TekYanit = istek.TekYanit,
            TesekkurMetni = istek.TesekkurMetni,
            TesekkurAdresi = istek.TesekkurAdresi,
            YanitOzetiGorunur = istek.YanitOzetiGorunur,
            SonuclarHerkeseAcik = istek.SonuclarHerkeseAcik,
            OlusturanId = await _kullanici.GetUserIdAsync(),

            // Entity başlatıcısı YOK: orada üretilseydi her güncellemede
            // yeniden üretilir ve dağıtılmış bağlantılar sessizce ölürdü.
            ErisimAnahtari = AnahtarUret(),
            AnonimTuzu = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
        };

        _context.Formlar.Add(form);
        await _context.SaveChangesAsync(iptal);

        await SurumYazAsync(form, istek.Tanim, iptal);
        return await GetirAsync(form.Id, iptal);
    }

    public async Task<FormDetayDto> GuncelleAsync(
        long id, FormKayitDto istek, CancellationToken iptal = default)
    {
        var form = await ErisebilirMiAsync(id, iptal);

        TarihleriDogrula(istek);
        TanimiDogrula(istek.Tanim);

        /*
          ERİŞİM KİPİ YANIT ALINDIKTAN SONRA DEĞİŞMEZ.

          Anonim toplanmış yanıtların üstüne "telefon doğrulamalı" demek,
          var olmayan bir kimliği varmış gibi göstermek olurdu; tersi de
          toplanmış telefonların anonim sayılması demek.
        */
        if (form.YanitSayisi > 0 && form.Erisim != istek.Erisim)
        {
            throw new BusinessRuleException(
                "Yanıt alınmış bir formun erişim kipi değiştirilemez.");
        }

        form.Baslik = istek.Baslik.Trim();
        form.Aciklama = istek.Aciklama;
        form.Erisim = istek.Erisim;
        form.BaslangicTarihi = istek.BaslangicTarihi;
        form.BitisTarihi = istek.BitisTarihi;
        form.YanitSiniri = istek.YanitSiniri;
        form.TekYanit = istek.TekYanit;
        form.TesekkurMetni = istek.TesekkurMetni;
        form.TesekkurAdresi = istek.TesekkurAdresi;
        form.YanitOzetiGorunur = istek.YanitOzetiGorunur;
        form.SonuclarHerkeseAcik = istek.SonuclarHerkeseAcik;
        form.GuncellemeTarihi = DateTime.Now;

        await SurumYazAsync(form, istek.Tanim, iptal);
        return await GetirAsync(id, iptal);
    }

    /// <summary>
    /// Tanımı yeni bir sürüm olarak yazar.
    /// </summary>
    /// <remarks>
    /// <b>Yayınlanmamış sürüm ÜZERİNE yazılır, yenisi açılmaz.</b> Tasarımcı
    /// her tuş vuruşunda kaydediyor; her kayıt yeni sürüm açsaydı tek bir
    /// formun yüzlerce sürümü olur ve "hangi sürüme yanıt verildi" bilgisi
    /// anlamını yitirirdi. Yeni sürüm yalnızca YAYINLANMIŞ bir sürümün
    /// üstüne yazılmak istendiğinde doğuyor.
    /// </remarks>
    private async Task SurumYazAsync(Form form, FormTanimiDto tanim, CancellationToken iptal)
    {
        var json = JsonSerializer.Serialize(tanim, JsonAyari);

        var son = await _context.FormSurumleri
            .Where(s => s.FormId == form.Id)
            .OrderByDescending(s => s.SurumNo)
            .FirstOrDefaultAsync(iptal);

        if (son is not null && son.Id != form.YayinSurumId)
        {
            son.Tanim = json;
            son.OlusturmaTarihi = DateTime.Now;
        }
        else
        {
            _context.FormSurumleri.Add(new FormVersion
            {
                FormId = form.Id,
                SurumNo = (son?.SurumNo ?? 0) + 1,
                Tanim = json,
                OlusturanId = await _kullanici.GetUserIdAsync(),
            });
        }

        await _context.SaveChangesAsync(iptal);
    }

    public async Task<FormDetayDto> YayinlaAsync(long id, CancellationToken iptal = default)
    {
        var form = await ErisebilirMiAsync(id, iptal);

        var son = await _context.FormSurumleri
            .Where(s => s.FormId == id)
            .OrderByDescending(s => s.SurumNo)
            .FirstOrDefaultAsync(iptal)
            ?? throw new BusinessRuleException("Yayınlanacak bir tanım yok.");

        var tanim = TanimiCoz(son.Tanim);

        // BOŞ FORM YAYINLANMAZ. Vatandaşa boş bir sayfa göstermek, formu
        // hiç yayınlamamaktan kötü: bağlantı paylaşılıyor, açılıyor ve
        // yapacak bir şey olmuyor.
        if (!FormDogrulayici.TumAlanlar(tanim).Any(a => !FormDogrulayici.BlokMu(a.Tip)))
        {
            throw new BusinessRuleException("Formda en az bir soru olmalı.");
        }

        form.YayinSurumId = son.Id;
        form.Durum = FormDurumu.Yayinda;
        form.YayinTarihi ??= DateTime.Now;
        form.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync(iptal);
        return await GetirAsync(id, iptal);
    }

    public async Task<FormDetayDto> DurumDegistirAsync(
        long id, FormDurumu durum, CancellationToken iptal = default)
    {
        var form = await ErisebilirMiAsync(id, iptal);

        if (durum == FormDurumu.Yayinda && form.YayinSurumId is null)
        {
            throw new BusinessRuleException("Önce formu yayınlayın.");
        }

        form.Durum = durum;
        form.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync(iptal);
        return await GetirAsync(id, iptal);
    }

    public async Task<FormDetayDto> KopyalaAsync(long id, CancellationToken iptal = default)
    {
        var kaynak = await ErisebilirMiAsync(id, iptal);

        var son = await _context.FormSurumleri.AsNoTracking()
            .Where(s => s.FormId == id)
            .OrderByDescending(s => s.SurumNo)
            .FirstOrDefaultAsync(iptal);

        var kopya = new Form
        {
            Baslik = $"{kaynak.Baslik} (kopya)",
            Aciklama = kaynak.Aciklama,
            Erisim = kaynak.Erisim,
            BirimId = await _etkinBirim.IdAsync(iptal),
            YanitSiniri = kaynak.YanitSiniri,
            TekYanit = kaynak.TekYanit,
            TesekkurMetni = kaynak.TesekkurMetni,
            TesekkurAdresi = kaynak.TesekkurAdresi,
            YanitOzetiGorunur = kaynak.YanitOzetiGorunur,
            SonuclarHerkeseAcik = kaynak.SonuclarHerkeseAcik,
            OlusturanId = await _kullanici.GetUserIdAsync(),
            ErisimAnahtari = AnahtarUret(),
            AnonimTuzu = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),

            // TARİHLER KOPYALANMAZ: kaynağın süresi çoktan dolmuş olabilir
            // ve kopyayı açan kişi "neden yanıt alınmıyor" diye arardı.
        };

        _context.Formlar.Add(kopya);
        await _context.SaveChangesAsync(iptal);

        _context.FormSurumleri.Add(new FormVersion
        {
            FormId = kopya.Id,
            SurumNo = 1,
            Tanim = son?.Tanim ?? "{}",
            OlusturanId = kopya.OlusturanId,
        });

        await _context.SaveChangesAsync(iptal);
        return await GetirAsync(kopya.Id, iptal);
    }

    public async Task SilAsync(long id, CancellationToken iptal = default)
    {
        var form = await ErisebilirMiAsync(id, iptal);

        // YUMUŞAK SİLME: yanıtlar duruyor. Sert silme, toplanmış vatandaş
        // geri bildirimini tek tıkla yok etmek demekti.
        form.Silindi = true;
        form.Durum = FormDurumu.Arsiv;
        form.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync(iptal);
    }

    // ── ortak ──────────────────────────────────────────────────────────

    internal async Task<Form> ErisebilirMiAsync(long id, CancellationToken iptal)
    {
        var form = await _context.Formlar.FirstOrDefaultAsync(f => f.Id == id && !f.Silindi, iptal)
            ?? throw new EntityNotFoundException("Form bulunamadı.");

        var kapsam = await _etkinBirim.KapsamAsync(altBirimlerDahil: true, iptal);

        if (form.BirimId is null || !kapsam.Contains(form.BirimId.Value))
        {
            // 404, 403 DEĞİL: "bu form var ama senin değil" demek, başka
            // birimlerin kaç formu olduğunu saymayı mümkün kılardı.
            throw new EntityNotFoundException("Form bulunamadı.");
        }

        return form;
    }

    private static void TarihleriDogrula(FormKayitDto istek)
    {
        if (istek.BaslangicTarihi is { } b && istek.BitisTarihi is { } s && s < b)
        {
            throw new BusinessRuleException("Bitiş tarihi başlangıçtan önce olamaz.");
        }

        if (istek.YanitSiniri is { } y && y <= 0)
        {
            throw new BusinessRuleException("Yanıt sınırı sıfırdan büyük olmalı.");
        }
    }

    /// <summary>
    /// Tanımın kendi bütünlüğü — alan kimlikleri benzersiz mi, koşullar
    /// var olan alanı mı gösteriyor.
    /// </summary>
    /// <remarks>
    /// Tasarımcı bunları zaten üretmiyor ama tanım API'den de gelebiliyor.
    /// Yinelenen kimlik, iki sorunun aynı JSONB anahtarını paylaşması ve
    /// birinin cevabının ötekini ezmesi demek — sessiz veri kaybı.
    /// </remarks>
    private static void TanimiDogrula(FormTanimiDto tanim)
    {
        var alanlar = FormDogrulayici.TumAlanlar(tanim).ToList();

        var yinelenen = alanlar.GroupBy(a => a.Kimlik).FirstOrDefault(g => g.Count() > 1);
        if (yinelenen is not null)
        {
            throw new BusinessRuleException($"Aynı alan kimliği iki kez kullanılmış: {yinelenen.Key}");
        }

        if (alanlar.Any(a => string.IsNullOrWhiteSpace(a.Kimlik)))
        {
            throw new BusinessRuleException("Her alanın bir kimliği olmalı.");
        }

        /*
          KOŞUL YALNIZCA GERİYE BAKAR.

          Hedef alan, koşulu taşıyan alandan ÖNCE gelmek zorunda. İki
          kazancı var:

          - Döngü tespiti hiç yazılmıyor. Mapster döngüsü bu depoda bir kez
            `StackOverflowException` ile bütün API sürecini düşürdü; aynı
            sınıf hatayı yapı gereği imkânsız kılmak, testle yakalamaktan
            ucuz.
          - Sunucu doğrulaması formu TEK GEÇİŞTE yeniden oynatabiliyor;
            sıra garantisi olmadan "bu soru zorunlu muydu" kararı tanımsız.
        */
        var sira = alanlar.Select((a, i) => (a.Kimlik, i))
            .ToDictionary(x => x.Kimlik, x => x.i);

        void KosuluDogrula(FormKosuluDto? kosul, string sahip, int sahipSirasi)
        {
            foreach (var kural in kosul?.Kurallar ?? [])
            {
                if (!sira.TryGetValue(kural.AlanKimligi, out var hedef))
                {
                    throw new BusinessRuleException(
                        $"'{sahip}' alanının koşulu var olmayan bir alanı gösteriyor.");
                }

                if (hedef >= sahipSirasi)
                {
                    throw new BusinessRuleException(
                        $"'{sahip}' alanının koşulu KENDİSİNDEN SONRAKİ bir alana bakıyor. "
                        + "Koşullar yalnızca daha önce gelen bir soruya bağlanabilir.");
                }
            }

            if ((kosul?.Kurallar?.Count ?? 0) > 8)
            {
                throw new BusinessRuleException("Bir koşulda en fazla 8 kural olabilir.");
            }
        }

        for (var i = 0; i < alanlar.Count; i++)
        {
            KosuluDogrula(alanlar[i].Kosul, alanlar[i].Etiket, i);
        }

        // Grup koşulu, grubun İLK alanının sırasına göre denetlenir: gruptaki
        // hiçbir alan, grubu görünür kılan sorudan önce gelemez.
        foreach (var grup in (tanim.Adimlar ?? []).SelectMany(a => a.Gruplar ?? []))
        {
            if (grup.Kosul is null) continue;

            var ilk = (grup.Alanlar ?? []).Select(a => sira.GetValueOrDefault(a.Kimlik, int.MaxValue))
                .DefaultIfEmpty(int.MaxValue).Min();

            KosuluDogrula(grup.Kosul, grup.Baslik ?? "Grup", ilk);
        }

        if (alanlar.Count > 500)
        {
            throw new BusinessRuleException("Bir formda en fazla 500 alan olabilir.");
        }
    }

    internal static FormTanimiDto TanimiCoz(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new FormTanimiDto();

        try
        {
            return JsonSerializer.Deserialize<FormTanimiDto>(json, JsonAyari) ?? new FormTanimiDto();
        }
        catch (JsonException)
        {
            // Bozuk tanım formu açılamaz kılmamalı: boş bir ağaç dönüp
            // ekranın "soru yok" demesi, beyaz sayfadan iyi.
            return new FormTanimiDto();
        }
    }

    /// <summary>
    /// Vatandaş adresindeki erişim anahtarı.
    /// </summary>
    /// <remarks>
    /// URL'de geçtiği için base64url (<c>+/</c> yok); 128 bit, tahmin
    /// edilemez. Artan bir kimlik olsaydı yayınlanmamış formların adresini
    /// denemek ve kurumun kaç form açtığını saymak mümkün olurdu.
    /// </remarks>
    internal static string AnahtarUret() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Takip numarası — okunabilir ama tahmin edilemez.</summary>
    internal static string TakipNoUret()
    {
        // Karışan harfler (I/O) ve rakamlar (1/0) DIŞARIDA: numara telefonda
        // okunup elle yazılıyor.
        const string abece = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var harfler = new char[10];

        for (var i = 0; i < harfler.Length; i++)
        {
            harfler[i] = abece[RandomNumberGenerator.GetInt32(abece.Length)];
        }

        return new string(harfler);
    }

    // ── eşleme ─────────────────────────────────────────────────────────

    private FormOzetDto Ozet(Form f, int? surumNo, bool portalAcik)
    {
        var d = new FormOzetDto();
        Doldur(d, f, surumNo, portalAcik);
        return d;
    }

    private void Doldur(FormOzetDto d, Form f, int? surumNo, bool portalAcik)
    {
        var (aliyor, sebep) = YanitDurumu(f, portalAcik);

        d.Id = f.Id;
        d.ErisimAnahtari = f.ErisimAnahtari;
        d.Baslik = f.Baslik;
        d.Aciklama = f.Aciklama;
        d.Durum = f.Durum;
        d.DurumAd = DurumAdi(f.Durum);
        d.Erisim = f.Erisim;
        d.ErisimAd = ErisimAdi(f.Erisim);
        d.YanitSayisi = f.YanitSayisi;
        d.YanitSiniri = f.YanitSiniri;
        d.BaslangicTarihi = f.BaslangicTarihi;
        d.BitisTarihi = f.BitisTarihi;
        d.YanitAliyor = aliyor;
        d.KapaliSebebi = sebep;
        d.SurumNo = surumNo;
        d.BirimId = f.BirimId;
        d.OlusturmaTarihi = f.OlusturmaTarihi;
        d.YayinTarihi = f.YayinTarihi;

        // Adres İSTEKTEN türetiliyor: uygulama birden çok alan adından
        // yayınlanabiliyor ve sabit bir taban yanlış bağlantı üretirdi.
        //
        // Portal kapalıyken de veriliyor — gizlemek "adres yok" gibi
        // okunurdu; `kapaliSebebi` neden çalışmadığını zaten söylüyor.
        d.PaylasimAdresi = f.Durum == FormDurumu.Taslak
            ? null
            : _adresCozucu.Mutlak($"/form/{f.ErisimAnahtari}");

        d.PortalAcik = portalAcik;
    }

    /// <summary>
    /// ŞU AN yanıt alıyor mu — üç kural birlikte.
    /// </summary>
    /// <remarks>
    /// <b>Tek yerde.</b> İstemcide kurulsaydı web ve mobil aynı üç kuralı
    /// ayrı ayrı yazar, biri unutulduğunda ekran "açık" derken sunucu
    /// reddederdi.
    /// </remarks>
    internal static (bool Aliyor, string? Sebep) YanitDurumu(Form f, bool portalAcik = true)
    {
        /*
          PORTAL KAPALIYSA HİÇBİR FORM YANIT ALMAZ.

          Bayrak kapalıyken vatandaş ucu 404 dönüyor ama yönetim ekranı
          bunu bilmiyordu: yayınlanmış bir formun paylaşım adresi
          veriliyor, kopyalanıyor, açılıyor ve "Form bulunamadı" çıkıyordu.
          Sebebi hiçbir yerde yazmıyordu — ölçülmüş bir çıkmaz.
        */
        if (!portalAcik)
        {
            return (false, "Form portalı kapalı. Kurum Bilgileri ekranından açın.");
        }

        if (f.Durum == FormDurumu.Taslak) return (false, "Form henüz yayınlanmadı.");
        if (f.Durum == FormDurumu.Kapali) return (false, "Bu form yanıt kabul etmiyor.");
        if (f.Durum == FormDurumu.Arsiv) return (false, "Bu form arşivlendi.");

        var simdi = DateTime.Now;

        if (f.BaslangicTarihi is { } b && simdi < b)
            return (false, $"Bu form {b:dd.MM.yyyy HH:mm} tarihinde açılacak.");

        if (f.BitisTarihi is { } s && simdi > s)
            return (false, $"Bu formun süresi {s:dd.MM.yyyy HH:mm} tarihinde doldu.");

        if (f.YanitSiniri is { } y && f.YanitSayisi >= y)
            return (false, "Bu form için beklenen yanıt sayısına ulaşıldı.");

        return (true, null);
    }

    private static string DurumAdi(FormDurumu d) => d switch
    {
        FormDurumu.Taslak => "Taslak",
        FormDurumu.Yayinda => "Yayında",
        FormDurumu.Kapali => "Kapalı",
        FormDurumu.Arsiv => "Arşiv",
        _ => "Bilinmiyor",
    };

    private static string ErisimAdi(FormErisimi e) => e switch
    {
        FormErisimi.Anonim => "Herkese açık",
        FormErisimi.TelefonDogrulamali => "Telefon doğrulamalı",
        FormErisimi.Personel => "Yalnızca personel",
        _ => "Bilinmiyor",
    };
}
