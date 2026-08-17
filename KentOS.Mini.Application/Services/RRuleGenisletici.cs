using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KentOS.Mini.Application.Services
{
    /// <summary>
    /// RRULE kuralını somut tekrar TARİHLERİNE açar.
    ///
    /// Saf fonksiyondur: veritabanı, saat dilimi, "şu an" bilgisi kullanmaz —
    /// aynı girdiye her zaman aynı çıktıyı verir, bu yüzden birim testleriyle
    /// tamamen kapatılabilir. Saatler kayan (floating) yerel saattir: DTSTART
    /// saati her tekrarda birebir korunur, yaz saati geçişlerinde kaymaz.
    /// </summary>
    public static class RRuleGenisletici
    {
        /// <summary>Tek bir seride üretilecek en fazla tekrar (güvenlik tavanı).</summary>
        public const int VarsayilanEnFazlaAdet = 200;

        /// <summary>
        /// Sonsuz kurallar için üretim ufku (ay). Bu ufka yaklaşıldıkça
        /// <c>AjandaSeriService.UfkuGenisletAsync</c> ileriye taşır.
        /// </summary>
        public const int VarsayilanUfukAy = 18;

        /// <summary>
        /// Kuralı genişletir. Sonuç ARTAN sıradadır ve ilk öğe kural DTSTART'ı
        /// kapsıyorsa DTSTART'ın kendisidir.
        /// </summary>
        /// <param name="rrule">RRULE metni (önekli veya öneksiz).</param>
        /// <param name="dtstart">Serinin başlangıcı — saat bilgisi tüm tekrarlara taşınır.</param>
        /// <param name="ufuk">Bu tarihten SONRAKİ tekrarlar üretilmez (dahil).</param>
        /// <param name="enFazlaAdet">Güvenlik tavanı; COUNT'tan bağımsız üst sınır.</param>
        /// <param name="baslangicDahil">
        /// null değilse yalnızca bu tarihten itibaren olan tekrarlar döndürülür
        /// (ufuk uzatmada "zaten üretilenleri atla" için kullanılır). COUNT sayımı
        /// yine serinin başından yapılır, yani atlanan tekrarlar da sayılır.
        /// </param>
        public static List<DateTime> Genislet(
            string rrule,
            DateTime dtstart,
            DateTime ufuk,
            int enFazlaAdet = VarsayilanEnFazlaAdet,
            DateTime? baslangicDahil = null)
        {
            var kural = RRuleKural.Ayristir(rrule);
            return Genislet(kural, dtstart, ufuk, enFazlaAdet, baslangicDahil);
        }

        public static List<DateTime> Genislet(
            RRuleKural kural,
            DateTime dtstart,
            DateTime ufuk,
            int enFazlaAdet = VarsayilanEnFazlaAdet,
            DateTime? baslangicDahil = null)
        {
            if (enFazlaAdet < 1)
            {
                return [];
            }

            var sonuc = new List<DateTime>();
            var uretilenToplam = 0;              // COUNT sayacı (atlananlar dahil)
            var sinirUntil = kural.BitisTarihi;
            var sinirAdet = kural.Adet;

            // Sonsuz döngü emniyeti: dönem üretimi hiç tekrar vermezse bile
            // sınırsız ilerlemeyi engelleyen sert bir tur tavanı.
            var enFazlaDonem = 10_000;

            foreach (var donemBasi in Donemler(kural, dtstart, ufuk, enFazlaDonem))
            {
                foreach (var aday in DonemTekrarlari(kural, dtstart, donemBasi))
                {
                    if (aday < dtstart)
                    {
                        continue;   // DTSTART'tan önceki adaylar kurala dahil değildir
                    }
                    if (sinirUntil != null && aday > sinirUntil.Value)
                    {
                        return sonuc;
                    }
                    if (sinirAdet != null && uretilenToplam >= sinirAdet.Value)
                    {
                        return sonuc;
                    }

                    uretilenToplam++;

                    if (aday > ufuk)
                    {
                        return sonuc;
                    }
                    if (baslangicDahil != null && aday < baslangicDahil.Value)
                    {
                        continue;   // zaten üretilmiş tekrar — say ama döndürme
                    }

                    sonuc.Add(aday);
                    if (sonuc.Count >= enFazlaAdet)
                    {
                        return sonuc;
                    }
                }
            }

            return sonuc;
        }

        /// <summary>
        /// Kuralın dönem başlangıçlarını üretir (gün/hafta/ay/yıl), INTERVAL kadar
        /// atlayarak. Haftalık kuralda dönem başı, WKST'e göre hizalanmış hafta başıdır.
        /// </summary>
        private static IEnumerable<DateTime> Donemler(RRuleKural kural, DateTime dtstart, DateTime ufuk, int enFazlaDonem)
        {
            var ilk = kural.Siklik switch
            {
                RRuleSiklik.Gunluk => dtstart.Date,
                RRuleSiklik.Haftalik => HaftaBasina(dtstart.Date, kural.HaftaBasi),
                RRuleSiklik.Aylik => new DateTime(dtstart.Year, dtstart.Month, 1),
                _ => new DateTime(dtstart.Year, 1, 1)
            };

            var donem = ilk;
            for (var i = 0; i < enFazlaDonem; i++)
            {
                if (donem > ufuk)
                {
                    yield break;
                }

                yield return donem;

                donem = kural.Siklik switch
                {
                    RRuleSiklik.Gunluk => donem.AddDays(kural.Aralik),
                    RRuleSiklik.Haftalik => donem.AddDays(7 * kural.Aralik),
                    RRuleSiklik.Aylik => donem.AddMonths(kural.Aralik),
                    _ => donem.AddYears(kural.Aralik)
                };
            }
        }

        /// <summary>Bir dönemin içindeki tekrar tarihleri (artan sırada).</summary>
        private static IEnumerable<DateTime> DonemTekrarlari(RRuleKural kural, DateTime dtstart, DateTime donemBasi)
        {
            var saat = dtstart.TimeOfDay;

            switch (kural.Siklik)
            {
                case RRuleSiklik.Gunluk:
                    if (AylardaVar(kural, donemBasi.Month))
                    {
                        yield return donemBasi.Date + saat;
                    }
                    break;

                case RRuleSiklik.Haftalik:
                {
                    // BYDAY yoksa DTSTART'ın günü kullanılır (RFC 5545 varsayılanı).
                    var gunler = kural.Gunler.Count > 0
                        ? kural.Gunler.Select(g => g.Gun).Distinct().ToList()
                        : [dtstart.DayOfWeek];

                    // Hafta içi sırayı WKST'e göre ver, tarih sırası bozulmasın.
                    var sirali = gunler
                        .Select(g => new { Gun = g, Ofset = OfsetHaftaBasindan(g, kural.HaftaBasi) })
                        .OrderBy(x => x.Ofset)
                        .ToList();

                    foreach (var x in sirali)
                    {
                        var tarih = donemBasi.Date.AddDays(x.Ofset);
                        if (AylardaVar(kural, tarih.Month))
                        {
                            yield return tarih + saat;
                        }
                    }
                    break;
                }

                case RRuleSiklik.Aylik:
                    foreach (var t in AylikTekrarlar(kural, dtstart, donemBasi.Year, donemBasi.Month, saat))
                    {
                        yield return t;
                    }
                    break;

                case RRuleSiklik.Yillik:
                {
                    // BYMONTH verilmezse DTSTART'ın ayı kullanılır.
                    var aylar = kural.Aylar.Count > 0 ? kural.Aylar.OrderBy(x => x).ToList() : [dtstart.Month];
                    foreach (var ay in aylar)
                    {
                        foreach (var t in AylikTekrarlar(kural, dtstart, donemBasi.Year, ay, saat, yillik: true))
                        {
                            yield return t;
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Belirli bir (yıl, ay) içindeki tekrarlar: BYMONTHDAY, sıralı BYDAY
        /// (ör. 2TU / -1FR) veya hiçbiri verilmemişse DTSTART'ın ayın günü.
        /// </summary>
        private static IEnumerable<DateTime> AylikTekrarlar(
            RRuleKural kural, DateTime dtstart, int yil, int ay, TimeSpan saat, bool yillik = false)
        {
            if (!yillik && !AylardaVar(kural, ay))
            {
                yield break;
            }

            var ayinGunSayisi = DateTime.DaysInMonth(yil, ay);

            if (kural.AyinGunleri.Count > 0)
            {
                var gunler = kural.AyinGunleri
                    .Select(g => g > 0 ? g : ayinGunSayisi + g + 1)   // -1 → son gün
                    .Where(g => g >= 1 && g <= ayinGunSayisi)
                    .Distinct()
                    .OrderBy(g => g);

                foreach (var g in gunler)
                {
                    yield return new DateTime(yil, ay, g) + saat;
                }
                yield break;
            }

            if (kural.Gunler.Count > 0)
            {
                var tarihler = new List<DateTime>();
                foreach (var kg in kural.Gunler)
                {
                    if (kg.Sira == null)
                    {
                        // Sıra verilmediyse aydaki TÜM o günler (ör. her Pazartesi).
                        for (var g = 1; g <= ayinGunSayisi; g++)
                        {
                            var t = new DateTime(yil, ay, g);
                            if (t.DayOfWeek == kg.Gun)
                            {
                                tarihler.Add(t + saat);
                            }
                        }
                        continue;
                    }

                    var hedef = SiraliGun(yil, ay, kg.Gun, kg.Sira.Value);
                    if (hedef != null)
                    {
                        tarihler.Add(hedef.Value + saat);
                    }
                }

                foreach (var t in tarihler.Distinct().OrderBy(t => t))
                {
                    yield return t;
                }
                yield break;
            }

            // Ne BYMONTHDAY ne BYDAY: DTSTART'ın ayın günü. Ayda o gün yoksa
            // (31 Ocak → Şubat) o dönem ATLANIR — RFC 5545 davranışı.
            var istenen = dtstart.Day;
            if (istenen <= ayinGunSayisi)
            {
                yield return new DateTime(yil, ay, istenen) + saat;
            }
        }

        /// <summary>Ayın n'inci (veya sondan n'inci) belirli günü.</summary>
        private static DateTime? SiraliGun(int yil, int ay, DayOfWeek gun, int sira)
        {
            var gunSayisi = DateTime.DaysInMonth(yil, ay);
            var tumu = new List<DateTime>();
            for (var g = 1; g <= gunSayisi; g++)
            {
                var t = new DateTime(yil, ay, g);
                if (t.DayOfWeek == gun)
                {
                    tumu.Add(t);
                }
            }

            if (sira > 0)
            {
                return sira <= tumu.Count ? tumu[sira - 1] : null;
            }

            var indeks = tumu.Count + sira;   // -1 → son
            return indeks >= 0 && indeks < tumu.Count ? tumu[indeks] : null;
        }

        /// <summary>
        /// Serinin SON tekrarının tarihi. Kural sonsuzsa (COUNT ve UNTIL yoksa)
        /// <c>null</c> döner.
        ///
        /// Arayüzler bunu "… tarihinde bitecek" satırında gösterir: kullanıcı
        /// "10 tekrar" yazısından serinin ne zaman biteceğini kafadan hesaplamak
        /// zorunda kalmasın. UNTIL'li kuralda UNTIL'in kendisi değil, o tarihe
        /// kadar GERÇEKTEN düşen son tekrar döner (UNTIL çoğu zaman gün sonudur).
        /// </summary>
        public static DateTime? SonTekrar(string? rrule, DateTime dtstart)
        {
            if (string.IsNullOrWhiteSpace(rrule))
            {
                return null;
            }

            try
            {
                return SonTekrar(RRuleKural.Ayristir(rrule), dtstart);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public static DateTime? SonTekrar(RRuleKural kural, DateTime dtstart)
        {
            if (kural.Adet == null && kural.BitisTarihi == null)
            {
                return null;    // sonsuz seri — bitiş tarihi yok
            }

            var enFazla = Math.Min(kural.Adet ?? VarsayilanEnFazlaAdet, VarsayilanEnFazlaAdet);
            var ufuk = kural.BitisTarihi ?? AdedeGoreUfuk(kural, dtstart, enFazla);
            var tekrarlar = Genislet(kural, dtstart, ufuk, enFazla);
            return tekrarlar.Count > 0 ? tekrarlar[^1] : null;
        }

        /// <summary>
        /// COUNT'lu (UNTIL'siz) bir kuralın son tekrarını kesin kapsayan üst sınır.
        /// Genişletici COUNT'ta zaten duracağı için bu yalnızca dönem üretimini
        /// sonlandıran bir emniyet tavanıdır; taşma olursa <see cref="DateTime.MaxValue"/>.
        /// </summary>
        private static DateTime AdedeGoreUfuk(RRuleKural kural, DateTime dtstart, int adet)
        {
            try
            {
                return kural.Siklik switch
                {
                    RRuleSiklik.Gunluk => dtstart.AddDays((double)kural.Aralik * adet + 7),
                    RRuleSiklik.Haftalik => dtstart.AddDays(7.0 * kural.Aralik * adet + 7),
                    RRuleSiklik.Aylik => dtstart.AddMonths(kural.Aralik * adet + 1),
                    _ => dtstart.AddYears(kural.Aralik * adet + 1)
                };
            }
            catch (ArgumentOutOfRangeException)
            {
                return DateTime.MaxValue;
            }
        }

        private static bool AylardaVar(RRuleKural kural, int ay)
            => kural.Aylar.Count == 0 || kural.Aylar.Contains(ay);

        private static DateTime HaftaBasina(DateTime tarih, DayOfWeek haftaBasi)
            => tarih.AddDays(-OfsetHaftaBasindan(tarih.DayOfWeek, haftaBasi));

        private static int OfsetHaftaBasindan(DayOfWeek gun, DayOfWeek haftaBasi)
            => ((int)gun - (int)haftaBasi + 7) % 7;

        // -------------------------------------------------------------- Türkçe özet
        private static readonly string[] GunAdlari =
            ["Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi"];

        private static readonly string[] AyAdlari =
            ["", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
             "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];

        private static readonly string[] SiraAdlari = ["", "birinci", "ikinci", "üçüncü", "dördüncü", "beşinci"];

        /// <summary>
        /// Kuralın insan-okur Türkçe özeti — arayüzlerde "Her hafta Pazartesi,
        /// 10 tekrar" gibi gösterilir. Ayrıştırılamayan kuralda kuralın kendisi
        /// döner (kullanıcıya hata göstermek yerine ham kuralı göstermek yeterli).
        /// </summary>
        public static string Ozet(string? rrule)
        {
            if (string.IsNullOrWhiteSpace(rrule))
            {
                return string.Empty;
            }

            try
            {
                return Ozet(RRuleKural.Ayristir(rrule));
            }
            catch (FormatException)
            {
                return rrule;
            }
        }

        public static string Ozet(RRuleKural kural)
        {
            var parcalar = new List<string>();

            var aralik = kural.Aralik;
            switch (kural.Siklik)
            {
                case RRuleSiklik.Gunluk:
                    parcalar.Add(aralik == 1 ? "Her gün" : $"{aralik} günde bir");
                    break;

                case RRuleSiklik.Haftalik:
                {
                    var bas = aralik == 1 ? "Her hafta" : $"{aralik} haftada bir";
                    if (kural.Gunler.Count > 0)
                    {
                        bas += " " + string.Join(", ", kural.Gunler
                            .OrderBy(g => OfsetHaftaBasindan(g.Gun, kural.HaftaBasi))
                            .Select(g => GunAdlari[(int)g.Gun]));
                    }
                    parcalar.Add(bas);
                    break;
                }

                case RRuleSiklik.Aylik:
                {
                    var bas = aralik == 1 ? "Her ay" : $"{aralik} ayda bir";
                    bas += AyIciAnlatim(kural);
                    parcalar.Add(bas);
                    break;
                }

                case RRuleSiklik.Yillik:
                {
                    var bas = aralik == 1 ? "Her yıl" : $"{aralik} yılda bir";
                    if (kural.Aylar.Count > 0)
                    {
                        bas += " " + string.Join(", ", kural.Aylar.OrderBy(a => a).Select(a => AyAdlari[a]));
                    }
                    bas += AyIciAnlatim(kural);
                    parcalar.Add(bas);
                    break;
                }
            }

            if (kural.Adet != null)
            {
                parcalar.Add($"{kural.Adet.Value} tekrar");
            }
            else if (kural.BitisTarihi != null)
            {
                parcalar.Add($"{kural.BitisTarihi.Value:dd.MM.yyyy} tarihine kadar");
            }

            return string.Join(", ", parcalar);
        }

        private static string AyIciAnlatim(RRuleKural kural)
        {
            if (kural.AyinGunleri.Count > 0)
            {
                var gunler = kural.AyinGunleri.Select(g => g < 0
                    ? (g == -1 ? "son gün" : $"sondan {Math.Abs(g)}. gün")
                    : $"{g}.");
                return " " + string.Join(", ", gunler) + (kural.AyinGunleri.All(g => g > 0) ? " günü" : "");
            }

            if (kural.Gunler.Count > 0)
            {
                var ifadeler = kural.Gunler.Select(g =>
                {
                    var gunAd = GunAdlari[(int)g.Gun];
                    if (g.Sira == null)
                    {
                        return gunAd;
                    }
                    return g.Sira.Value > 0
                        ? $"{SiraAdlari[Math.Min(g.Sira.Value, 5)]} {gunAd}"
                        : (g.Sira.Value == -1 ? $"son {gunAd}" : $"sondan {Math.Abs(g.Sira.Value)}. {gunAd}");
                });
                return " " + string.Join(", ", ifadeler);
            }

            return string.Empty;
        }
    }
}
