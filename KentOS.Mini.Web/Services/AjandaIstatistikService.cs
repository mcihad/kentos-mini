using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.Analiz;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using System.Globalization;

namespace KentOS.Mini.Web.Services
{
    /// <summary>
    /// Birim bazlı etkinlik istatistikleri.
    ///
    /// TASARIM KARARI — mevcut sistemi etkilememe garantisi:
    ///  • Yalnızca SELECT yapar; hiçbir yazma, bildirim veya yan etki yoktur.
    ///  • <c>AsNoTracking()</c> — EF change-tracker'a hiçbir varlık girmez, bu
    ///    yüzden başka bir serviste yapılan SaveChanges'i etkileyemez.
    ///  • TEK sorgu ile küçük bir projeksiyon çeker, tüm 21 analiz bellekte
    ///    hesaplanır. Böylece veritabanına 21 ayrı sorgu gitmez ve karmaşık
    ///    GROUP BY'ların PostgreSQL'e çevrilememe riski ortadan kalkar.
    ///  • Mevcut hiçbir servis/DTO/uç değiştirilmemiştir.
    /// </summary>
    public class AjandaIstatistikService(
        AppDbContext _context,
        ICurrentUserService _currentUserService) : IAjandaIstatistikService
    {
        private static readonly CultureInfo Tr = new("tr-TR");

        /// Bellekte hesap için gereken minimum alan kümesi.
        private sealed record Satir(
            long Id,
            string Baslik,
            DateTime Baslangic,
            DateTime? Bitis,
            bool TumGun,
            bool TekrarEden,
            bool BasinKatilsin,
            bool BilgiNotuDurum,
            bool KonusmaMetniDurum,
            bool IsDeleted,
            AjandaStatus Status,
            string? Konum,
            string? Kullanici,
            string? TipAd,
            string? TipRenk,
            string? DurumAd,
            string? DurumRenk,
            bool CicekVar,
            int FotografSayisi,
            int NotSayisi);

        public async Task<AjandaIstatistikDto> GetIstatistiklerAsync(DateTime? baslangic = null, DateTime? bitis = null)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var bit = (bitis ?? DateTime.Now).Date.AddDays(1).AddTicks(-1); // gün sonu
            var bas = (baslangic ?? bit.Date.AddMonths(-12)).Date;

            var birimAdi = await _context.Birimler
                .AsNoTracking()
                .Where(b => b.Id == birimId)
                .Select(b => b.Ad)
                .FirstOrDefaultAsync() ?? string.Empty;

            // IgnoreQueryFilters: silinmiş etkinlikler de istatistiğe girsin
            // (arşiv ekranı silinmişleri de gösteriyor). AsNoTracking zorunlu.
            // GİZLİLİK: başkasının gizli etkinliği istatistiğe DE girmez — aksi
            // halde sayılar üzerinden varlığı anlaşılırdı. Filtre açık bir Where
            // olduğu için IgnoreQueryFilters onu atlamaz.
            var kullaniciId = await _currentUserService.GetUserIdAsync();
            var kullaniciAdi = _currentUserService.GetUsername();

            // Basın kullanıcısı ajandanın yalnızca basına açık kısmını görür.
            // Kapı `ICurrentUserService`te: bu sorguların hepsinde zaten var.
            var yalnizcaBasin = await _currentUserService.YalnizcaBasinMiAsync();

            var satirlar = await _context.Ajandalar
                .AsNoTracking()
                .IgnoreQueryFilters()
                .GorunurOlanlar(kullaniciId, kullaniciAdi, yalnizcaBasin)
                .Where(a => a.BirimId == birimId
                            && a.BaslangicTarihi >= bas
                            && a.BaslangicTarihi <= bit)
                .Select(a => new Satir(
                    a.Id,
                    a.Baslik,
                    a.BaslangicTarihi,
                    a.BitisTarihi,
                    a.TumGun,
                    a.TekrarEden,
                    a.BasinKatilsin,
                    a.BilgiNotuDurum,
                    a.KonusmaMetniDurum,
                    a.IsDeleted,
                    a.Status,
                    a.Konum,
                    a.KullaniciId,
                    a.RandevuTip != null ? a.RandevuTip.Ad : null,
                    a.RandevuTip != null ? a.RandevuTip.Renk : null,
                    a.Durum != null ? a.Durum.Ad : null,
                    a.Durum != null ? a.Durum.Renk : null,
                    a.CicekId != null,
                    a.Photos.Count,
                    a.AjandaNotlar.Count))
                .ToListAsync();

            return Hesapla(satirlar, birimId, birimAdi, bas, bit);
        }

        // ---------------------------------------------------------------- //
        //  Hesaplama — saf fonksiyon, DB'ye dokunmaz.                       //
        // ---------------------------------------------------------------- //
        private static AjandaIstatistikDto Hesapla(
            List<Satir> hepsi, long birimId, string birimAdi, DateTime bas, DateTime bit)
        {
            var simdi = DateTime.Now;
            var bugun = simdi.Date;
            var haftaBasi = bugun.AddDays(-(((int)bugun.DayOfWeek + 6) % 7)); // Pazartesi
            var ayBasi = new DateTime(bugun.Year, bugun.Month, 1);

            // Silinmemiş kayıtlar "aktif" analizlerin tabanıdır; toplam sayımlar
            // silinmişleri de içerir (arşiv bakış açısı).
            var aktif = hepsi.Where(x => !x.IsDeleted).ToList();

            var dto = new AjandaIstatistikDto
            {
                BirimId = birimId,
                BirimAdi = birimAdi,
                BaslangicTarihi = bas.ToString("yyyy-MM-dd"),
                BitisTarihi = bit.ToString("yyyy-MM-dd"),
                UretilmeZamani = simdi.ToString("yyyy-MM-dd HH:mm:ss"),
            };

            // ---- 1. Özet -------------------------------------------------
            var sureliler = aktif.Where(x => x.Bitis.HasValue && x.Bitis > x.Baslangic).ToList();
            var tamamlanan = aktif.Count(x => x.Status == AjandaStatus.Completed);
            var iptal = aktif.Count(x => x.Status == AjandaStatus.Canceled);
            var bekleyen = aktif.Count(x => x.Status == AjandaStatus.Pending);
            var kararVerilen = tamamlanan + iptal + bekleyen;

            dto.Ozet = new IstatistikOzetDto
            {
                ToplamEtkinlik = hepsi.Count,
                AktifEtkinlik = aktif.Count,
                SilinmisEtkinlik = hepsi.Count(x => x.IsDeleted),
                TamamlananEtkinlik = tamamlanan,
                IptalEdilenEtkinlik = iptal,
                BekleyenEtkinlik = bekleyen,
                GecmisEtkinlik = aktif.Count(x => x.Baslangic < simdi),
                GelecekEtkinlik = aktif.Count(x => x.Baslangic >= simdi),
                BugunkuEtkinlik = aktif.Count(x => x.Baslangic.Date == bugun),
                BuHaftaEtkinlik = aktif.Count(x => x.Baslangic.Date >= haftaBasi && x.Baslangic.Date < haftaBasi.AddDays(7)),
                BuAyEtkinlik = aktif.Count(x => x.Baslangic >= ayBasi && x.Baslangic < ayBasi.AddMonths(1)),
                OrtalamaSureDakika = sureliler.Count == 0 ? 0
                    : Yuvarla(sureliler.Average(x => (x.Bitis!.Value - x.Baslangic).TotalMinutes)),
                OrtalamaNotSayisi = aktif.Count == 0 ? 0 : Yuvarla(aktif.Average(x => x.NotSayisi)),
                OrtalamaFotografSayisi = aktif.Count == 0 ? 0 : Yuvarla(aktif.Average(x => x.FotografSayisi)),
                ToplamNot = hepsi.Sum(x => x.NotSayisi),
                ToplamFotograf = hepsi.Sum(x => x.FotografSayisi),
                TamamlanmaOrani = kararVerilen == 0 ? 0 : Yuvarla(tamamlanan * 100.0 / kararVerilen),
            };

            // ---- 2. Aylara göre (boş aylar 0 ile doldurulur) --------------
            var aylik = aktif.GroupBy(x => new DateTime(x.Baslangic.Year, x.Baslangic.Month, 1))
                             .ToDictionary(g => g.Key, g => g.Count());
            for (var ay = new DateTime(bas.Year, bas.Month, 1); ay <= bit; ay = ay.AddMonths(1))
            {
                dto.AylaraGore.Add(new IstatistikSeriNoktasiDto
                {
                    Etiket = ay.ToString("MMM yy", Tr),
                    Tarih = ay.ToString("yyyy-MM-dd"),
                    Deger = aylik.TryGetValue(ay, out var s) ? s : 0
                });
            }

            // ---- 3. Yıllara göre -----------------------------------------
            dto.YillaraGore = Dilimle(aktif.GroupBy(x => x.Baslangic.Year.ToString())
                                           .OrderBy(g => g.Key), aktif.Count);

            // ---- 4. Tipe göre --------------------------------------------
            dto.TipeGore = Dilimle(
                aktif.GroupBy(x => x.TipAd ?? "Belirtilmemiş").OrderByDescending(g => g.Count()),
                aktif.Count,
                g => g.Select(x => x.TipRenk).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r)));

            // ---- 5. Duruma göre ------------------------------------------
            dto.DurumaGore = Dilimle(
                aktif.GroupBy(x => x.DurumAd ?? "Belirtilmemiş").OrderByDescending(g => g.Count()),
                aktif.Count,
                g => g.Select(x => x.DurumRenk).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r)));

            // ---- 6. Statüye göre -----------------------------------------
            dto.StatuyeGore = new List<IstatistikDilimDto>
            {
                Dilim("Beklemede", bekleyen, aktif.Count, "#F59E0B"),
                Dilim("Tamamlandı", tamamlanan, aktif.Count, "#10B981"),
                Dilim("İptal Edildi", iptal, aktif.Count, "#EF4444"),
            };

            // ---- 7. Haftanın günü ----------------------------------------
            string[] gunAdlari = { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi", "Pazar" };
            dto.HaftaGunineGore = gunAdlari.Select((ad, i) =>
            {
                // DayOfWeek: Pazar=0 … Cumartesi=6 → Pazartesi=0 olacak şekilde kaydır.
                var adet = aktif.Count(x => ((int)x.Baslangic.DayOfWeek + 6) % 7 == i);
                return Dilim(ad, adet, aktif.Count);
            }).ToList();

            // ---- 8. Saat aralığı (2'şer saat) ----------------------------
            dto.SaatAraliginaGore = Enumerable.Range(0, 12).Select(i =>
            {
                var basSaat = i * 2;
                var adet = aktif.Count(x => x.Baslangic.Hour >= basSaat && x.Baslangic.Hour < basSaat + 2);
                return Dilim($"{basSaat:00}-{basSaat + 2:00}", adet, aktif.Count);
            }).ToList();

            // ---- 9. Günün bölümü -----------------------------------------
            dto.GunBolumuneGore = new List<IstatistikDilimDto>
            {
                Dilim("Sabah (06-12)", aktif.Count(x => x.Baslangic.Hour is >= 6 and < 12), aktif.Count, "#FBBF24"),
                Dilim("Öğleden Sonra (12-18)", aktif.Count(x => x.Baslangic.Hour is >= 12 and < 18), aktif.Count, "#F97316"),
                Dilim("Akşam (18-24)", aktif.Count(x => x.Baslangic.Hour >= 18), aktif.Count, "#6366F1"),
                Dilim("Gece (00-06)", aktif.Count(x => x.Baslangic.Hour < 6), aktif.Count, "#1E293B"),
            };

            // ---- 10. Konum (ilk 10) --------------------------------------
            dto.KonumaGore = Dilimle(
                aktif.Where(x => !string.IsNullOrWhiteSpace(x.Konum))
                     .GroupBy(x => x.Konum!.Trim())
                     .OrderByDescending(g => g.Count()).Take(10),
                aktif.Count);

            // ---- 11. Oluşturan (ilk 10) ----------------------------------
            dto.OlusturanaGore = Dilimle(
                aktif.Where(x => !string.IsNullOrWhiteSpace(x.Kullanici))
                     .GroupBy(x => x.Kullanici!.Trim())
                     .OrderByDescending(g => g.Count()).Take(10),
                aktif.Count);

            // ---- 12-17. İkili dağılımlar ---------------------------------
            dto.BasinKatilimi = Ikili(aktif, x => x.BasinKatilsin, "Basın Katılıyor", "Basın Katılmıyor", "#0EA5E9");
            dto.FotografDurumu = Ikili(aktif, x => x.FotografSayisi > 0, "Fotoğraflı", "Fotoğrafsız", "#8B5CF6");
            dto.CicekDurumu = Ikili(aktif, x => x.CicekVar, "Çiçek Gönderildi", "Çiçek Yok", "#EC4899");
            dto.TumGunDurumu = Ikili(aktif, x => x.TumGun, "Tüm Gün", "Saatli", "#14B8A6");
            dto.TekrarDurumu = Ikili(aktif, x => x.TekrarEden, "Tekrar Eden", "Tek Seferlik", "#F43F5E");
            dto.HazirlikDurumu = new List<IstatistikDilimDto>
            {
                Dilim("Bilgi Notu Hazır", aktif.Count(x => x.BilgiNotuDurum), aktif.Count, "#22C55E"),
                Dilim("Konuşma Metni Hazır", aktif.Count(x => x.KonusmaMetniDurum), aktif.Count, "#3B82F6"),
                Dilim("Her İkisi de Hazır", aktif.Count(x => x.BilgiNotuDurum && x.KonusmaMetniDurum), aktif.Count, "#A855F7"),
                Dilim("Hazırlık Yok", aktif.Count(x => !x.BilgiNotuDurum && !x.KonusmaMetniDurum), aktif.Count, "#94A3B8"),
            };

            // ---- 18. Günlük yoğunluk (son 90 gün) ------------------------
            var gunluk = aktif.GroupBy(x => x.Baslangic.Date).ToDictionary(g => g.Key, g => g.Count());
            var ilkGun = bugun.AddDays(-89);
            for (var g = ilkGun; g <= bugun; g = g.AddDays(1))
            {
                dto.GunlukYogunluk.Add(new IstatistikSeriNoktasiDto
                {
                    Etiket = g.ToString("dd.MM"),
                    Tarih = g.ToString("yyyy-MM-dd"),
                    Deger = gunluk.TryGetValue(g, out var s) ? s : 0
                });
            }

            // ---- 19. Süre dağılımı ---------------------------------------
            int SureAdet(Func<double, bool> kosul) =>
                sureliler.Count(x => kosul((x.Bitis!.Value - x.Baslangic).TotalMinutes));
            dto.SureDagilimi = new List<IstatistikDilimDto>
            {
                Dilim("0-30 dk", SureAdet(m => m <= 30), sureliler.Count),
                Dilim("30-60 dk", SureAdet(m => m > 30 && m <= 60), sureliler.Count),
                Dilim("1-2 saat", SureAdet(m => m > 60 && m <= 120), sureliler.Count),
                Dilim("2-4 saat", SureAdet(m => m > 120 && m <= 240), sureliler.Count),
                Dilim("4+ saat", SureAdet(m => m > 240), sureliler.Count),
            };

            // ---- 20. En çok not alan etkinlikler -------------------------
            dto.EnCokNotAlanEtkinlikler = hepsi
                .Where(x => x.NotSayisi > 0)
                .OrderByDescending(x => x.NotSayisi)
                .Take(10)
                .Select(x => new IstatistikDilimDto
                {
                    Etiket = Kisalt(x.Baslik),
                    Deger = x.NotSayisi,
                    Yuzde = 0
                })
                .ToList();

            // ---- 21. Aylık tamamlanma oranı ------------------------------
            foreach (var nokta in dto.AylaraGore)
            {
                var ay = DateTime.ParseExact(nokta.Tarih, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                var ayKayitlari = aktif.Where(x => x.Baslangic.Year == ay.Year && x.Baslangic.Month == ay.Month).ToList();
                var ayKarar = ayKayitlari.Count(x => x.Status != AjandaStatus.Pending);
                var ayTamam = ayKayitlari.Count(x => x.Status == AjandaStatus.Completed);
                dto.AylikTamamlanmaOrani.Add(new IstatistikSeriNoktasiDto
                {
                    Etiket = nokta.Etiket,
                    Tarih = nokta.Tarih,
                    Deger = ayKarar == 0 ? 0 : (int)Math.Round(ayTamam * 100.0 / ayKarar)
                });
            }

            return dto;
        }

        // ---------------------------------------------------------------- //
        //  Küçük yardımcılar                                                //
        // ---------------------------------------------------------------- //
        private static double Yuvarla(double d) => Math.Round(d, 1);

        private static IstatistikDilimDto Dilim(string etiket, int deger, int toplam, string? renk = null) => new()
        {
            Etiket = etiket,
            Deger = deger,
            Yuzde = toplam == 0 ? 0 : Yuvarla(deger * 100.0 / toplam),
            Renk = renk
        };

        private static List<IstatistikDilimDto> Dilimle<TKey>(
            IEnumerable<IGrouping<TKey, Satir>> gruplar,
            int toplam,
            Func<IGrouping<TKey, Satir>, string?>? renkSecici = null) =>
            gruplar.Select(g => Dilim(g.Key?.ToString() ?? "Belirtilmemiş", g.Count(), toplam, renkSecici?.Invoke(g)))
                   .ToList();

        private static List<IstatistikDilimDto> Ikili(
            List<Satir> kayitlar, Func<Satir, bool> kosul, string evetEtiket, string hayirEtiket, string renk)
        {
            var evet = kayitlar.Count(kosul);
            return new List<IstatistikDilimDto>
            {
                Dilim(evetEtiket, evet, kayitlar.Count, renk),
                Dilim(hayirEtiket, kayitlar.Count - evet, kayitlar.Count, "#CBD5E1"),
            };
        }

        private static string Kisalt(string? s, int max = 40)
        {
            if (string.IsNullOrWhiteSpace(s)) return "(başlıksız)";
            s = s.Trim();
            return s.Length <= max ? s : s[..max] + "…";
        }
    }
}
