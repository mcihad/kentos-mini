using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto.Analiz;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Data;
using System.Globalization;

namespace KentOS.Kalem.Web.Services;

/// <summary>
/// Birim bazlı <b>talep</b> istatistikleri.
/// </summary>
/// <remarks>
/// <para>
/// Etkinlik panosundan ayrı bir servis: ikisi farklı soruları cevaplıyor.
/// Etkinlik panosu "makamın günü nasıl geçiyor", bu ise "vatandaş neyi,
/// nereden, kim aracılığıyla istiyor" der. Asıl sebebi <b>mahalle</b> ve
/// <b>meslek</b> dağılımları: talebin nereden ve kimden geldiğini gösteren tek
/// iki alan bunlar ve hiçbir yerde toplanmıyorlardı.
/// </para>
/// <para>
/// <see cref="AjandaIstatistikService"/> ile aynı tasarım kararları geçerli:
/// yalnızca SELECT, <c>AsNoTracking</c>, TEK sorguyla küçük bir projeksiyon ve
/// bütün dağılımlar bellekte. Böylece veritabanına onlarca GROUP BY gitmez ve
/// karmaşık gruplamaların PostgreSQL'e çevrilememe riski kalmaz.
/// </para>
/// </remarks>
public class TalepIstatistikServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici) : ITalepIstatistikServisi
{
    private static readonly CultureInfo Tr = new("tr-TR");

    /// Bellekte hesap için gereken en küçük alan kümesi.
    private sealed record Satir(
        long Id,
        DateTime? Olusturma,
        DateTime? BaslangicTarih,
        bool Arsivlendi,
        bool AjandaDurum,
        bool OzgecmisDurum,
        int DosyaSayisi,
        string? Meslek,
        string? MahalleAd,
        string? TipAd,
        string? TipRenk,
        string? DurumAd,
        string? DurumRenk,
        string? BirimAd,
        string? Kullanici);

    public async Task<TalepIstatistikDto> PanoAsync(
        DateTime? baslangic = null, DateTime? bitis = null)
    {
        var birimId = _kullanici.GetCurrentBirimId();
        var bit = (bitis ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);
        var bas = (baslangic ?? bit.Date.AddMonths(-12)).Date;

        var birimAdi = await _context.Birimler
            .AsNoTracking()
            .Where(b => b.Id == birimId)
            .Select(b => b.Ad)
            .FirstOrDefaultAsync() ?? string.Empty;

        // IgnoreQueryFilters: `Randevu` üzerinde `!Arsivlendi` global filtresi
        // var. Arşivlenmiş talepler istatistiğe GİRMELİ — "kaç talep geldi"
        // sorusunun cevabı arşive kaldırılınca değişmemeli. Aktif/arşiv ayrımı
        // ayrı bir dağılım olarak veriliyor.
        var satirlar = await _context.Randevular
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => r.BirimId == birimId
                        && r.OlusturmaTarih >= bas
                        && r.OlusturmaTarih <= bit)
            .Select(r => new Satir(
                r.Id,
                r.OlusturmaTarih,
                r.BaslangicTarih,
                r.Arsivlendi,
                r.AjandaDurum,
                r.OzgecmisDurum,
                r.Dosyalar != null ? r.Dosyalar.Count : 0,
                r.Meslek,
                r.Mahalle != null ? r.Mahalle.Ad : null,
                r.RandevuTip != null ? r.RandevuTip.Ad : null,
                r.RandevuTip != null ? r.RandevuTip.Renk : null,
                r.RandevuDurum != null ? r.RandevuDurum.DurumAd : null,
                r.RandevuDurum != null ? r.RandevuDurum.Renk : null,
                r.Birim != null ? r.Birim.Ad : null,
                r.Olusturan))
            .ToListAsync();

        return Hesapla(satirlar, birimAdi, bas, bit);
    }

    // ------------------------------------------------------------------ //
    //  Hesaplama — saf fonksiyon, veritabanına dokunmaz.                  //
    // ------------------------------------------------------------------ //
    private static TalepIstatistikDto Hesapla(
        List<Satir> hepsi, string birimAdi, DateTime bas, DateTime bit)
    {
        var simdi = DateTime.Now;
        var bugun = simdi.Date;
        var haftaBasi = bugun.AddDays(-(((int)bugun.DayOfWeek + 6) % 7)); // Pazartesi
        var ayBasi = new DateTime(bugun.Year, bugun.Month, 1);
        var toplam = hepsi.Count;

        var dto = new TalepIstatistikDto
        {
            BirimAdi = birimAdi,
            BaslangicTarihi = bas.ToString("yyyy-MM-dd"),
            BitisTarihi = bit.ToString("yyyy-MM-dd"),
            UretilmeZamani = simdi.ToString("yyyy-MM-dd HH:mm:ss"),
        };

        // ---- Özet -------------------------------------------------------
        var eklenen = hepsi.Where(x => x.AjandaDurum).ToList();

        // "Onaylandı" durum ADINDAN okunuyor: durum tablosu yönetim ekranından
        // düzenlenebiliyor ve sabit bir kimliğe bağlanmak, durum yeniden
        // oluşturulduğunda sayacı sessizce sıfırlardı.
        var onayli = hepsi.Count(x =>
            x.DurumAd is not null &&
            x.DurumAd.Contains("onay", StringComparison.OrdinalIgnoreCase) &&
            !x.DurumAd.Contains("onaylanma", StringComparison.OrdinalIgnoreCase));

        var onayliEklenmemis = hepsi.Count(x =>
            !x.AjandaDurum &&
            x.DurumAd is not null &&
            x.DurumAd.Contains("onay", StringComparison.OrdinalIgnoreCase) &&
            !x.DurumAd.Contains("onaylanma", StringComparison.OrdinalIgnoreCase));
        _ = onayli;

        // Talep girilişinden randevu gününe kadar geçen süre.
        var gunler = hepsi
            .Where(x => x.AjandaDurum && x.Olusturma.HasValue && x.BaslangicTarih.HasValue
                        && x.BaslangicTarih.Value.Year > 1)
            .Select(x => (x.BaslangicTarih!.Value - x.Olusturma!.Value).TotalDays)
            .Where(g => g >= 0)
            .ToList();

        dto.Ozet = new TalepIstatistikOzetDto
        {
            ToplamTalep = toplam,
            AktifTalep = hepsi.Count(x => !x.Arsivlendi),
            ArsivlenmisTalep = hepsi.Count(x => x.Arsivlendi),
            AjandayaEklenen = eklenen.Count,
            OnayliAmaEklenmemis = onayliEklenmemis,
            OzgecmisYuklu = hepsi.Count(x => x.OzgecmisDurum),
            DosyaEkliTalep = hepsi.Count(x => x.DosyaSayisi > 0),
            BugunGelen = hepsi.Count(x => x.Olusturma?.Date == bugun),
            BuHaftaGelen = hepsi.Count(x =>
                x.Olusturma.HasValue && x.Olusturma.Value.Date >= haftaBasi
                && x.Olusturma.Value.Date < haftaBasi.AddDays(7)),
            BuAyGelen = hepsi.Count(x => x.Olusturma.HasValue && x.Olusturma.Value.Date >= ayBasi),
            OrtalamaAjandaGunu = gunler.Count == 0 ? 0 : Yuvarla(gunler.Average()),
        };

        // ---- Zaman serileri ---------------------------------------------
        dto.AylaraGore = hepsi
            .Where(x => x.Olusturma.HasValue)
            .GroupBy(x => new DateTime(x.Olusturma!.Value.Year, x.Olusturma.Value.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new IstatistikSeriNoktasiDto
            {
                Etiket = g.Key.ToString("MMM yy", Tr),
                Tarih = g.Key.ToString("yyyy-MM-dd"),
                Deger = g.Count(),
            })
            .ToList();

        dto.GunlukYogunluk = hepsi
            .Where(x => x.Olusturma.HasValue)
            .GroupBy(x => x.Olusturma!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g => new IstatistikSeriNoktasiDto
            {
                Etiket = g.Key.ToString("dd.MM", Tr),
                Tarih = g.Key.ToString("yyyy-MM-dd"),
                Deger = g.Count(),
            })
            .ToList();

        // ---- Mahalle ve meslek — bu panonun asıl sebebi ------------------
        dto.MahalleyeGore = EnCok(
            hepsi.GroupBy(x => Temizle(x.MahalleAd)), toplam, 15);

        // Meslek serbest metin bir sütun: "Çiftçi", "çiftçi ", "ÇİFTÇİ" aynı
        // şey ama üç ayrı dilim üretirdi. Anahtar normalleştirilir, ETİKET ise
        // grubun ilk yazımından alınır — kullanıcı kendi yazdığını görsün.
        dto.MeslegeGore = EnCok(
            hepsi.GroupBy(x => Temizle(x.Meslek).ToLower(Tr)),
            toplam, 15,
            g => Baslikla(g.Select(x => Temizle(x.Meslek)).First()));

        // ---- Kategorik dağılımlar ---------------------------------------
        dto.TipeGore = EnCok(hepsi.GroupBy(x => Temizle(x.TipAd)), toplam, 20,
            renkSecici: g => g.Select(x => x.TipRenk).FirstOrDefault(r => r != null));

        dto.DurumaGore = EnCok(hepsi.GroupBy(x => Temizle(x.DurumAd)), toplam, 20,
            renkSecici: g => g.Select(x => x.DurumRenk).FirstOrDefault(r => r != null));

        dto.BirimeGore = EnCok(hepsi.GroupBy(x => Temizle(x.BirimAd)), toplam, 15);
        dto.OlusturanaGore = EnCok(hepsi.GroupBy(x => Temizle(x.Kullanici)), toplam, 15);

        // Haftanın günleri: kayıt olmayan gün de GÖSTERİLİR, yoksa "salı hiç
        // talep gelmiyor" bilgisi listeden düşerdi.
        var gunSirasi = new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
        };
        dto.HaftaGunineGore = gunSirasi
            .Select(g => Dilim(
                Tr.DateTimeFormat.GetDayName(g),
                hepsi.Count(x => x.Olusturma?.DayOfWeek == g),
                toplam))
            .ToList();

        // ---- İkili durumlar ---------------------------------------------
        dto.AjandaDurumu = Ikili(hepsi, x => x.AjandaDurum, "Ajandaya eklendi", "Eklenmedi", "#2E7D5B");
        dto.OzgecmisDurumu = Ikili(hepsi, x => x.OzgecmisDurum, "Özgeçmiş var", "Yok", "#1E5FBF");
        dto.ArsivDurumu = Ikili(hepsi, x => !x.Arsivlendi, "Aktif", "Arşivlenmiş", "#A78952");

        return dto;
    }

    // ------------------------------------------------------------------ //
    //  Yardımcılar                                                        //
    // ------------------------------------------------------------------ //

    private static double Yuvarla(double d) => Math.Round(d, 1, MidpointRounding.AwayFromZero);

    private static string Temizle(string? s) =>
        string.IsNullOrWhiteSpace(s) ? "Belirtilmemiş" : s.Trim();

    private static string Baslikla(string s) =>
        s.Length == 0 ? s : Tr.TextInfo.ToTitleCase(s.ToLower(Tr));

    private static IstatistikDilimDto Dilim(string etiket, int deger, int toplam, string? renk = null) => new()
    {
        Etiket = etiket,
        Deger = deger,
        Yuzde = toplam == 0 ? 0 : Yuvarla(deger * 100.0 / toplam),
        Renk = renk,
    };

    /// <summary>
    /// En çok görülen <paramref name="sinir"/> grubu; kalanı "Diğer"de toplar.
    /// </summary>
    /// <remarks>
    /// Mahalle listesi yüzlerce satır: hepsini göndermek grafiği okunmaz
    /// yapıyor, istemcide kesmek ise yüzdeleri yanlış gösteriyordu (kesilen
    /// kısım toplamdan düşmüyor). Kesme SUNUCUDA yapılır ve artık "Diğer"
    /// dilimi olarak GÖRÜNÜR kalır — sessizce kaybolmaz.
    /// </remarks>
    private static List<IstatistikDilimDto> EnCok<TKey>(
        IEnumerable<IGrouping<TKey, Satir>> gruplar,
        int toplam,
        int sinir,
        Func<IGrouping<TKey, Satir>, string>? etiketSecici = null,
        Func<IGrouping<TKey, Satir>, string?>? renkSecici = null)
    {
        var sirali = gruplar
            .Select(g => (
                Etiket: etiketSecici?.Invoke(g) ?? g.Key?.ToString() ?? "Belirtilmemiş",
                Adet: g.Count(),
                Renk: renkSecici?.Invoke(g)))
            .OrderByDescending(x => x.Adet)
            .ThenBy(x => x.Etiket, StringComparer.Create(Tr, ignoreCase: true))
            .ToList();

        if (sirali.Count <= sinir)
        {
            return sirali.Select(x => Dilim(x.Etiket, x.Adet, toplam, x.Renk)).ToList();
        }

        var ilkler = sirali.Take(sinir).Select(x => Dilim(x.Etiket, x.Adet, toplam, x.Renk)).ToList();
        var kalan = sirali.Skip(sinir).Sum(x => x.Adet);
        ilkler.Add(Dilim($"Diğer ({sirali.Count - sinir})", kalan, toplam, "#CBD5E1"));
        return ilkler;
    }

    private static List<IstatistikDilimDto> Ikili(
        List<Satir> kayitlar, Func<Satir, bool> kosul,
        string evetEtiket, string hayirEtiket, string renk)
    {
        var evet = kayitlar.Count(kosul);
        return
        [
            Dilim(evetEtiket, evet, kayitlar.Count, renk),
            Dilim(hayirEtiket, kayitlar.Count - evet, kayitlar.Count, "#CBD5E1"),
        ];
    }
}
