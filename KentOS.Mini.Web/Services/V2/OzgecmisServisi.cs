using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Storage;
using System.Text.Json.Serialization;
using Telefon_ = KentOS.Mini.Application.Dto.V2.Ortak.Telefon;

namespace KentOS.Mini.Web.Services.V2;

// ══════════════════════════════════════════════════════════════════ DTO'lar

/// <summary>Havuz listesindeki tek satır.</summary>
public class OzgecmisOzetDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("adSoyad")] public string AdSoyad { get; set; } = string.Empty;
    [JsonPropertyName("telefon")] public string? Telefon { get; set; }
    [JsonPropertyName("eposta")] public string? Eposta { get; set; }

    [JsonPropertyName("meslekId")] public long? MeslekId { get; set; }
    [JsonPropertyName("meslekAd")] public string? MeslekAd { get; set; }
    [JsonPropertyName("mahalleId")] public long? MahalleId { get; set; }
    [JsonPropertyName("mahalleAd")] public string? MahalleAd { get; set; }

    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }

    [JsonPropertyName("dosyaAdi")] public string DosyaAdi { get; set; } = string.Empty;
    [JsonPropertyName("boyut")] public long Boyut { get; set; }
    [JsonPropertyName("icerikTuru")] public string? IcerikTuru { get; set; }

    /// <summary>Doluysa kayıt bir talepten geldi.</summary>
    [JsonPropertyName("talepId")] public long? TalepId { get; set; }
    [JsonPropertyName("talepKonusu")] public string? TalepKonusu { get; set; }

    /// <summary><c>Havuz</c> · <c>Talep</c> — listede rozet olarak gösterilir.</summary>
    [JsonPropertyName("kaynakAd")] public string KaynakAd => TalepId is null ? "Havuz" : "Talep";

    [JsonPropertyName("birimId")] public long? BirimId { get; set; }
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }

    [JsonPropertyName("olusturan")] public string? Olusturan { get; set; }
    [JsonPropertyName("olusturmaTarihi")] public DateTime OlusturmaTarihi { get; set; }

    /// <summary>Kaç kişiye yönlendirildi.</summary>
    [JsonPropertyName("paylasimSayisi")] public int PaylasimSayisi { get; set; }
}

public class OzgecmisPaylasimDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("paylasanAd")] public string? PaylasanAd { get; set; }
    [JsonPropertyName("aliciId")] public long AliciId { get; set; }
    [JsonPropertyName("aliciAd")] public string? AliciAd { get; set; }
    [JsonPropertyName("not")] public string? Not { get; set; }
    [JsonPropertyName("tarih")] public DateTime Tarih { get; set; }
    [JsonPropertyName("goruntulemeTarihi")] public DateTime? GoruntulemeTarihi { get; set; }
}

public class OzgecmisDetayDto : OzgecmisOzetDto
{
    [JsonPropertyName("adres")] public string? Adres { get; set; }
    [JsonPropertyName("guncelleyen")] public string? Guncelleyen { get; set; }
    [JsonPropertyName("guncellemeTarihi")] public DateTime? GuncellemeTarihi { get; set; }
    [JsonPropertyName("paylasimlar")] public List<OzgecmisPaylasimDto> Paylasimlar { get; set; } = [];
}

/// <summary>Havuz süzgeci — hepsi VE ile birleşir.</summary>
public class OzgecmisSuzgeci : SayfaIstegi
{
    /// <summary>Ad, telefon, e-posta, meslek ve açıklamada arar.</summary>
    [JsonPropertyName("ara")] public string? Ara { get; set; }

    [JsonPropertyName("meslekId")] public long? MeslekId { get; set; }
    [JsonPropertyName("mahalleId")] public long? MahalleId { get; set; }
    [JsonPropertyName("birimId")] public long? BirimId { get; set; }

    /// <summary><c>havuz</c> · <c>talep</c> — boşsa ikisi de.</summary>
    [JsonPropertyName("kaynak")] public string? Kaynak { get; set; }

    [JsonPropertyName("baslangic")] public DateTime? Baslangic { get; set; }
    [JsonPropertyName("bitis")] public DateTime? Bitis { get; set; }

    /// <summary>Yalnızca bana paylaşılanlar.</summary>
    [JsonPropertyName("banaPaylasilan")] public bool? BanaPaylasilan { get; set; }
}

public class OzgecmisIstegi
{
    [JsonPropertyName("adSoyad")] public string AdSoyad { get; set; } = string.Empty;

    private string? _telefon;
    [JsonPropertyName("telefon")]
    public string? Telefon
    {
        get => _telefon;
        set => _telefon = Telefon_.Duzelt(value);
    }

    [JsonPropertyName("eposta")] public string? Eposta { get; set; }
    [JsonPropertyName("meslekId")] public long? MeslekId { get; set; }
    [JsonPropertyName("meslekAd")] public string? MeslekAd { get; set; }
    [JsonPropertyName("mahalleId")] public long? MahalleId { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }

    /// <summary>Var olan bir talebe bağlamak için.</summary>
    [JsonPropertyName("talepId")] public long? TalepId { get; set; }
}

public class PaylasimIstegi
{
    [JsonPropertyName("aliciIdler")] public List<long> AliciIdler { get; set; } = [];
    [JsonPropertyName("not")] public string? Not { get; set; }
}

/// <summary>Yüklenen dosyanın ham hâli — controller'dan servise.</summary>
public record YuklenenDosya(
    [property: JsonPropertyName("ad")] string Ad,
    [property: JsonPropertyName("icerikTuru")] string? IcerikTuru,
    [property: JsonPropertyName("icerik")] byte[] Icerik);

public interface IOzgecmisServisi
{
    Task<SayfaliSonuc<OzgecmisOzetDto>> ListeAsync(OzgecmisSuzgeci suzgec);
    Task<OzgecmisDetayDto> DetayAsync(long id);
    Task<OzgecmisDetayDto> OlusturAsync(OzgecmisIstegi istek, YuklenenDosya dosya);
    Task<OzgecmisDetayDto> GuncelleAsync(long id, OzgecmisIstegi istek, YuklenenDosya? dosya);
    Task SilAsync(long id);
    Task<(byte[] Icerik, string DosyaAdi, string IcerikTuru)> DosyaAsync(long id);
    Task<int> PaylasAsync(long id, PaylasimIstegi istek);

    /// <summary>Talebe yüklenen özgeçmişi havuza yansıtır.</summary>
    Task TalepOzgecmisiniYansitAsync(Randevu randevu, string diskAdi, string dosyaAdi,
        long boyut, string? icerikTuru);
}

// ══════════════════════════════════════════════════════════════ uygulama

/// <summary>
/// ÖZGEÇMİŞ HAVUZU.
/// </summary>
/// <remarks>
/// <para>
/// <b>Görünürlük birime bağlı değil.</b> Sistemin geri kalanında kayıt birim
/// süzgecinden geçer; burada geçmez, çünkü havuzun varlık sebebi tam tersi:
/// bir müdürlüğün elindeki özgeçmişi, işe alacak olan başka müdürlüğün de
/// görebilmesi. Kapı tek: <c>ozgecmis.goruntule</c> izni. Birim yine de
/// kaydediliyor ve süzgeç olarak sunuluyor — "bizim eklediklerimiz" sorusu
/// meşru, ama varsayılan kısıt değil.
/// </para>
/// <para>
/// <b>Talepten gelen kayıtlar aynı tabloda.</b> İki kaynağı sorgu anında
/// birleştirmek (UNION) sayfalamayı ve sıralamayı bozuyordu; talebe özgeçmiş
/// yüklendiğinde burada da bir satır açılır ve <c>RandevuId</c> ile nereden
/// geldiği belli olur. Dosya KOPYALANMAZ, ikisi aynı dosyayı gösterir.
/// </para>
/// </remarks>
public class OzgecmisServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici,
    IMessageService _mesaj,
    IFileStorage _depo) : IOzgecmisServisi
{
    /// <summary>
    /// Dosyalar talep özgeçmişleriyle AYNI klasörde.
    /// </summary>
    /// <remarks>
    /// Havuzdaki kayıt talepteki dosyanın aynısını gösteriyor; ayrı klasör,
    /// aynı dosyanın iki kopyasını ve hangisinin güncel olduğu sorusunu
    /// getirirdi. Klasör zaten yayında yazılabilir durumda.
    /// </remarks>
    private const string Kok = "uploads/ozgecmis";

    /// <summary>Depodaki anahtar — veritabanında yalnızca dosya adı saklanır.</summary>
    private static string Anahtar(string diskAdi) => $"{Kok}/{Path.GetFileName(diskAdi)}";

    // ── liste ──────────────────────────────────────────────────────────

    public async Task<SayfaliSonuc<OzgecmisOzetDto>> ListeAsync(OzgecmisSuzgeci suzgec)
    {
        var kullaniciId = await _kullanici.GetUserIdAsync();

        var sorgu = _context.Ozgecmisler
            .AsNoTracking()
            .Where(o => !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(suzgec.Ara))
        {
            var ara = suzgec.Ara.Trim();

            // Aranan metin de KAYIT GİBİ sadeleştirilir: kullanıcı
            // "+90 532 604 22 11" yazdığında ham rakamlar `905326042211`
            // oluyor ama sütunda `05326042211` var — ülke kodu eşleşmeyi
            // düşürüyordu.
            var rakam = Sadelestir(ara) ?? string.Empty;

            sorgu = sorgu.Where(o =>
                EF.Functions.ILike(o.AdSoyad, $"%{ara}%")
                || (o.Eposta != null && EF.Functions.ILike(o.Eposta, $"%{ara}%"))
                || (o.MeslekAd != null && EF.Functions.ILike(o.MeslekAd, $"%{ara}%"))
                || (o.Meslek != null && EF.Functions.ILike(o.Meslek.Ad, $"%{ara}%"))
                || (o.Aciklama != null && EF.Functions.ILike(o.Aciklama, $"%{ara}%"))
                // Telefon SADE sütunda aranır: kullanıcı boşluklu da yazsa,
                // bitişik de yazsa aynı kaydı bulmalı.
                || (rakam.Length >= 3 && o.TelefonSade != null
                    && EF.Functions.Like(o.TelefonSade, $"%{rakam}%")));
        }

        if (suzgec.MeslekId is { } meslek)
            sorgu = sorgu.Where(o => o.MeslekId == meslek);

        if (suzgec.MahalleId is { } mahalle)
            sorgu = sorgu.Where(o => o.MahalleId == mahalle);

        if (suzgec.BirimId is { } birim)
            sorgu = sorgu.Where(o => o.BirimId == birim);

        sorgu = suzgec.Kaynak?.ToLowerInvariant() switch
        {
            "havuz" => sorgu.Where(o => o.RandevuId == null),
            "talep" => sorgu.Where(o => o.RandevuId != null),
            _ => sorgu,
        };

        if (suzgec.Baslangic is { } bas)
            sorgu = sorgu.Where(o => o.OlusturmaTarihi >= bas.Date);

        if (suzgec.Bitis is { } bit)
            sorgu = sorgu.Where(o => o.OlusturmaTarihi < bit.Date.AddDays(1));

        if (suzgec.BanaPaylasilan == true && kullaniciId is { } benim)
            sorgu = sorgu.Where(o => o.Paylasimlar.Any(p => p.AliciId == benim));

        return await sorgu
            .OrderByDescending(o => o.OlusturmaTarihi)
            .Select(o => Ozet(o))
            .SayfalaAsync(suzgec);
    }

    /// <summary>Tek yerde tanımlı yansıtma — liste ve detay ayrışmasın.</summary>
    private static OzgecmisOzetDto Ozet(Ozgecmis o) => new()
    {
        Id = o.Id,
        AdSoyad = o.AdSoyad,
        Telefon = o.Telefon,
        Eposta = o.Eposta,
        MeslekId = o.MeslekId,
        MeslekAd = o.Meslek != null ? o.Meslek.Ad : o.MeslekAd,
        MahalleId = o.MahalleId,
        MahalleAd = o.Mahalle != null ? o.Mahalle.Ad : null,
        Aciklama = o.Aciklama,
        DosyaAdi = o.DosyaAdi,
        Boyut = o.Boyut,
        IcerikTuru = o.IcerikTuru,
        TalepId = o.RandevuId,
        TalepKonusu = o.Randevu != null ? o.Randevu.Konu : null,
        BirimId = o.BirimId,
        Olusturan = o.Olusturan,
        OlusturmaTarihi = o.OlusturmaTarihi,
        PaylasimSayisi = o.Paylasimlar.Count,
    };

    public async Task<OzgecmisDetayDto> DetayAsync(long id)
    {
        var o = await _context.Ozgecmisler
            .AsNoTracking()
            .Include(x => x.Meslek)
            .Include(x => x.Mahalle)
            .Include(x => x.Randevu)
            .Include(x => x.Paylasimlar)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new EntityNotFoundException($"Özgeçmiş bulunamadı. Id:{id}");

        var ozet = Ozet(o);
        var detay = new OzgecmisDetayDto
        {
            Id = ozet.Id,
            AdSoyad = ozet.AdSoyad,
            Telefon = ozet.Telefon,
            Eposta = ozet.Eposta,
            MeslekId = ozet.MeslekId,
            MeslekAd = ozet.MeslekAd,
            MahalleId = ozet.MahalleId,
            MahalleAd = ozet.MahalleAd,
            Aciklama = ozet.Aciklama,
            DosyaAdi = ozet.DosyaAdi,
            Boyut = ozet.Boyut,
            IcerikTuru = ozet.IcerikTuru,
            TalepId = ozet.TalepId,
            TalepKonusu = ozet.TalepKonusu,
            BirimId = ozet.BirimId,
            Olusturan = ozet.Olusturan,
            OlusturmaTarihi = ozet.OlusturmaTarihi,
            PaylasimSayisi = ozet.PaylasimSayisi,
            Adres = o.Adres,
            Guncelleyen = o.Guncelleyen,
            GuncellemeTarihi = o.GuncellemeTarihi,
            Paylasimlar = [.. o.Paylasimlar
                .OrderByDescending(p => p.Tarih)
                .Select(p => new OzgecmisPaylasimDto
                {
                    Id = p.Id,
                    PaylasanAd = p.PaylasanAd,
                    AliciId = p.AliciId,
                    AliciAd = p.AliciAd,
                    Not = p.Not,
                    Tarih = p.Tarih,
                    GoruntulemeTarihi = p.GoruntulemeTarihi,
                })],
        };

        // Bana paylaşılan kayıtta "gördü" damgası: paylaşan kişi
        // "gönderdim ama bakmadı" ile "hiç göndermedim"i ayırt edebilmeli.
        if (await _kullanici.GetUserIdAsync() is { } benim)
        {
            var benimki = await _context.OzgecmisPaylasimlari
                .Where(p => p.OzgecmisId == id && p.AliciId == benim && p.GoruntulemeTarihi == null)
                .ToListAsync();

            if (benimki.Count > 0)
            {
                foreach (var p in benimki) p.GoruntulemeTarihi = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        return detay;
    }

    // ── yazma ──────────────────────────────────────────────────────────

    public async Task<OzgecmisDetayDto> OlusturAsync(OzgecmisIstegi istek, YuklenenDosya dosya)
    {
        if (string.IsNullOrWhiteSpace(istek.AdSoyad))
            throw new BusinessRuleException("Ad soyad zorunludur.");

        var diskAdi = await DosyaYazAsync(dosya);

        var kayit = new Ozgecmis
        {
            DosyaAdi = dosya.Ad,
            DosyaYolu = diskAdi,
            Boyut = dosya.Icerik.LongLength,
            IcerikTuru = dosya.IcerikTuru,
            BirimId = _kullanici.GetCurrentBirimId(),
            Olusturan = _kullanici.GetUsername(),
            OlusturmaTarihi = DateTime.Now,
        };

        Uygula(kayit, istek);
        _context.Ozgecmisler.Add(kayit);
        await _context.SaveChangesAsync();

        return await DetayAsync(kayit.Id);
    }

    public async Task<OzgecmisDetayDto> GuncelleAsync(
        long id, OzgecmisIstegi istek, YuklenenDosya? dosya)
    {
        var kayit = await BulAsync(id);

        if (dosya is not null)
        {
            // Yeni dosya geldiyse ESKİSİ SİLİNMEZ: aynı dosya bir talebe de
            // bağlı olabilir ve orada hâlâ gösteriliyor.
            kayit.DosyaYolu = await DosyaYazAsync(dosya);
            kayit.DosyaAdi = dosya.Ad;
            kayit.Boyut = dosya.Icerik.LongLength;
            kayit.IcerikTuru = dosya.IcerikTuru;
        }

        Uygula(kayit, istek);
        kayit.Guncelleyen = _kullanici.GetUsername();
        kayit.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();
        return await DetayAsync(kayit.Id);
    }

    public async Task SilAsync(long id)
    {
        var kayit = await BulAsync(id);

        // YUMUŞAK silme ve dosya diskte kalır: yanlışlıkla silinen bir
        // özgeçmişi geri getirmenin başka yolu yok, üstelik dosya talebe de
        // bağlı olabilir.
        kayit.IsDeleted = true;
        kayit.Guncelleyen = _kullanici.GetUsername();
        kayit.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();
    }

    public async Task<(byte[] Icerik, string DosyaAdi, string IcerikTuru)> DosyaAsync(long id)
    {
        var kayit = await _context.Ozgecmisler
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted)
            ?? throw new EntityNotFoundException($"Özgeçmiş bulunamadı. Id:{id}");

        var icerik = await _depo.ReadAllBytesAsync(StorageArea.Public, Anahtar(kayit.DosyaYolu))
            ?? throw new EntityNotFoundException("Özgeçmiş dosyası sunucuda bulunamadı.");

        return (icerik,
                kayit.DosyaAdi,
                kayit.IcerikTuru ?? "application/octet-stream");
    }

    // ── paylaşım ───────────────────────────────────────────────────────

    public async Task<int> PaylasAsync(long id, PaylasimIstegi istek)
    {
        var kayit = await BulAsync(id);

        var alicilar = istek.AliciIdler.Distinct().Where(x => x > 0).ToList();
        if (alicilar.Count == 0)
            throw new BusinessRuleException("En az bir kişi seçilmelidir.");

        var paylasanId = await _kullanici.GetUserIdAsync() ?? 0;
        var paylasanAd = await _kullanici.GetFullNameAsync();

        var kisiler = await _context.Users
            .Where(k => alicilar.Contains(k.Id))
            .Select(k => new { k.Id, Ad = ((k.Ad ?? "") + " " + (k.Soyad ?? "")).Trim() })
            .ToListAsync();

        foreach (var alici in kisiler)
        {
            _context.OzgecmisPaylasimlari.Add(new OzgecmisPaylasim
            {
                OzgecmisId = kayit.Id,
                PaylasanId = paylasanId,
                PaylasanAd = paylasanAd,
                AliciId = alici.Id,
                AliciAd = alici.Ad,
                Not = istek.Not,
                Tarih = DateTime.Now,
            });

            // Eylem NONE: bu varlığı tanımayan eski mobil sürümler varlığı
            // sessizce `talep`e düşürüyor ve var olmayan bir talebi açmaya
            // çalışıyordu (dosya gönderiminde yaşandı).
            var veri = new TokenDataDto(
                NotificationEntity.Ozgecmis, (int)kayit.Id, NotificationAction.None);

            await _mesaj.CreateAsync(
                alici.Id,
                null,
                "Size bir özgeçmiş yönlendirildi",
                $"{paylasanAd}, {kayit.AdSoyad} adlı kişinin özgeçmişini paylaştı." +
                (string.IsNullOrWhiteSpace(istek.Not) ? "" : $" Not: {istek.Not}"),
                SendMessageType.PushNotification,
                NotifikasyonTip.ResumeOnShared,
                veri.ToJson());
        }

        await _context.SaveChangesAsync();
        return kisiler.Count;
    }

    // ── talep köprüsü ──────────────────────────────────────────────────

    /// <summary>
    /// Talebe yüklenen özgeçmişi havuza yansıtır.
    /// </summary>
    /// <remarks>
    /// Aynı talep için ikinci kez yükleme yapılırsa var olan satır GÜNCELLENİR;
    /// yeni satır açmak havuzda aynı kişiyi iki kez gösterirdi.
    /// </remarks>
    public async Task TalepOzgecmisiniYansitAsync(
        Randevu randevu, string diskAdi, string dosyaAdi, long boyut, string? icerikTuru)
    {
        var kayit = await _context.Ozgecmisler
            .FirstOrDefaultAsync(o => o.RandevuId == randevu.Id && !o.IsDeleted);

        var yeni = kayit is null;
        kayit ??= new Ozgecmis
        {
            RandevuId = randevu.Id,
            OlusturmaTarihi = DateTime.Now,
            Olusturan = _kullanici.GetUsername(),
        };

        kayit.AdSoyad = $"{randevu.Ad} {randevu.Soyad}".Trim();
        kayit.Telefon = randevu.Telefon;
        kayit.TelefonSade = Sadelestir(randevu.Telefon);
        kayit.Eposta = randevu.Email;
        kayit.MeslekAd = randevu.Meslek;
        kayit.MahalleId = randevu.MahalleId;
        kayit.Adres = randevu.Adres;
        kayit.Aciklama = randevu.Konu;
        kayit.BirimId = randevu.BirimId;
        kayit.DosyaAdi = dosyaAdi;
        kayit.DosyaYolu = diskAdi;
        kayit.Boyut = boyut;
        kayit.IcerikTuru = icerikTuru;

        if (!yeni)
        {
            kayit.Guncelleyen = _kullanici.GetUsername();
            kayit.GuncellemeTarihi = DateTime.Now;
        }
        else
        {
            _context.Ozgecmisler.Add(kayit);
        }

        await _context.SaveChangesAsync();
    }

    // ── yardımcılar ────────────────────────────────────────────────────

    private async Task<Ozgecmis> BulAsync(long id) =>
        await _context.Ozgecmisler.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted)
        ?? throw new EntityNotFoundException($"Özgeçmiş bulunamadı. Id:{id}");

    private void Uygula(Ozgecmis kayit, OzgecmisIstegi istek)
    {
        kayit.AdSoyad = istek.AdSoyad.Trim();
        kayit.Telefon = istek.Telefon;
        kayit.TelefonSade = Sadelestir(istek.Telefon);
        kayit.Eposta = istek.Eposta;
        kayit.MeslekId = istek.MeslekId;
        kayit.MeslekAd = istek.MeslekAd;
        kayit.MahalleId = istek.MahalleId;
        kayit.Adres = istek.Adres;
        kayit.Aciklama = istek.Aciklama;
        if (istek.TalepId is { } talep) kayit.RandevuId = talep;
    }

    private static string? Sadelestir(string? telefon)
    {
        var sade = Telefon_.Duzelt(telefon);
        if (sade is null) return null;
        var rakam = new string(sade.Where(char.IsDigit).ToArray());
        return rakam.Length == 0 ? null : rakam;
    }

    private async Task<string> DosyaYazAsync(YuklenenDosya dosya)
    {
        var uzanti = Path.GetExtension(dosya.Ad);
        var diskAdi = $"{Guid.NewGuid()}{uzanti}";
        await _depo.SaveAsync(StorageArea.Public, Anahtar(diskAdi), dosya.Icerik, dosya.IcerikTuru);
        return diskAdi;
    }
}
