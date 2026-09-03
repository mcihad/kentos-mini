using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace KentOS.Kalem.Application.Services
{
    /// <summary>Tekrar sıklığı — RFC 5545 FREQ.</summary>
    public enum RRuleSiklik
    {
        Gunluk = 0,
        Haftalik = 1,
        Aylik = 2,
        Yillik = 3
    }

    /// <summary>
    /// BYDAY öğesi: gün + isteğe bağlı sıra numarası.
    /// <c>MO</c> → (Pazartesi, null) · <c>2TU</c> → (Salı, 2) · <c>-1FR</c> → (Cuma, -1).
    /// </summary>
    public readonly record struct RRuleGun(DayOfWeek Gun, int? Sira);

    /// <summary>
    /// Ayrıştırılmış RFC 5545 tekrar kuralı (RRULE).
    ///
    /// DESTEKLENEN ALT KÜME — arayüzlerin (mobil + web) üretebildiği kuralların
    /// tamamı: FREQ (DAILY|WEEKLY|MONTHLY|YEARLY), INTERVAL, COUNT, UNTIL,
    /// BYDAY (sıra ekli hâliyle: 1MO, -1FR), BYMONTHDAY (negatif dahil: -1),
    /// BYMONTH, WKST. Bunların dışındaki bir parça gelirse kural REDDEDİLİR
    /// (sessizce yoksaymak, kullanıcının kurduğu kuraldan farklı tekrarlar
    /// üretilmesi anlamına gelirdi).
    ///
    /// NEDEN KÜTÜPHANE YOK: Uygulamanın tüm zaman verisi yerel ve saat dilimsiz
    /// (<c>timestamp without time zone</c>, Npgsql legacy timestamp davranışı).
    /// iCal kütüphaneleri değerleri saat dilimli tiplere çevirir; yaz saati
    /// geçişlerinde "her Pazartesi 14:00" olan bir toplantı 13:00/15:00'e kayabilir.
    /// Buradaki genişletici doğrudan <see cref="DateTime"/> üzerinde, kayan
    /// (floating) yerel saat mantığıyla çalışır — saat neyse o kalır.
    /// </summary>
    public sealed class RRuleKural
    {
        public RRuleSiklik Siklik { get; init; }

        /// <summary>INTERVAL — kaç sıklıkta bir (varsayılan 1).</summary>
        public int Aralik { get; init; } = 1;

        /// <summary>COUNT — toplam tekrar sayısı (null ise sınırsız).</summary>
        public int? Adet { get; init; }

        /// <summary>UNTIL — son tarih dahil (null ise sınırsız).</summary>
        public DateTime? BitisTarihi { get; init; }

        /// <summary>BYDAY listesi.</summary>
        public IReadOnlyList<RRuleGun> Gunler { get; init; } = Array.Empty<RRuleGun>();

        /// <summary>BYMONTHDAY listesi (1..31 veya -1..-31).</summary>
        public IReadOnlyList<int> AyinGunleri { get; init; } = Array.Empty<int>();

        /// <summary>BYMONTH listesi (1..12).</summary>
        public IReadOnlyList<int> Aylar { get; init; } = Array.Empty<int>();

        /// <summary>WKST — hafta başlangıcı (varsayılan Pazartesi).</summary>
        public DayOfWeek HaftaBasi { get; init; } = DayOfWeek.Monday;

        // ---------------------------------------------------------------- ayrıştırma
        private static readonly Dictionary<string, DayOfWeek> GunKodlari = new(StringComparer.OrdinalIgnoreCase)
        {
            ["MO"] = DayOfWeek.Monday,
            ["TU"] = DayOfWeek.Tuesday,
            ["WE"] = DayOfWeek.Wednesday,
            ["TH"] = DayOfWeek.Thursday,
            ["FR"] = DayOfWeek.Friday,
            ["SA"] = DayOfWeek.Saturday,
            ["SU"] = DayOfWeek.Sunday
        };

        private static readonly Dictionary<DayOfWeek, string> GunKodlariTers =
            GunKodlari.GroupBy(x => x.Value).ToDictionary(g => g.Key, g => g.First().Key.ToUpperInvariant());

        /// <summary>
        /// RRULE metnini ayrıştırır. "RRULE:" öneki varsa atılır.
        /// Hatalı/desteklenmeyen kuralda <see cref="FormatException"/> atar.
        /// </summary>
        public static RRuleKural Ayristir(string rrule)
        {
            if (string.IsNullOrWhiteSpace(rrule))
            {
                throw new FormatException("Tekrar kuralı boş olamaz.");
            }

            var metin = rrule.Trim();
            if (metin.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase))
            {
                metin = metin["RRULE:".Length..];
            }

            RRuleSiklik? siklik = null;
            var aralik = 1;
            int? adet = null;
            DateTime? until = null;
            var gunler = new List<RRuleGun>();
            var ayinGunleri = new List<int>();
            var aylar = new List<int>();
            var haftaBasi = DayOfWeek.Monday;

            foreach (var parca in metin.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var esit = parca.IndexOf('=');
                if (esit <= 0)
                {
                    throw new FormatException($"Tekrar kuralı parçası anlaşılamadı: '{parca}'.");
                }

                var ad = parca[..esit].Trim().ToUpperInvariant();
                var deger = parca[(esit + 1)..].Trim();

                switch (ad)
                {
                    case "FREQ":
                        siklik = deger.ToUpperInvariant() switch
                        {
                            "DAILY" => RRuleSiklik.Gunluk,
                            "WEEKLY" => RRuleSiklik.Haftalik,
                            "MONTHLY" => RRuleSiklik.Aylik,
                            "YEARLY" => RRuleSiklik.Yillik,
                            _ => throw new FormatException($"Desteklenmeyen tekrar sıklığı: '{deger}'. (DAILY, WEEKLY, MONTHLY, YEARLY)")
                        };
                        break;

                    case "INTERVAL":
                        if (!int.TryParse(deger, out aralik) || aralik < 1 || aralik > 366)
                        {
                            throw new FormatException($"INTERVAL 1 ile 366 arasında olmalıdır: '{deger}'.");
                        }
                        break;

                    case "COUNT":
                        if (!int.TryParse(deger, out var a) || a < 1)
                        {
                            throw new FormatException($"COUNT 1'den küçük olamaz: '{deger}'.");
                        }
                        adet = a;
                        break;

                    case "UNTIL":
                        until = UntilAyristir(deger);
                        break;

                    case "BYDAY":
                        foreach (var g in deger.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            gunler.Add(GunAyristir(g.Trim()));
                        }
                        break;

                    case "BYMONTHDAY":
                        foreach (var g in deger.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!int.TryParse(g.Trim(), out var gun) || gun == 0 || gun < -31 || gun > 31)
                            {
                                throw new FormatException($"BYMONTHDAY -31..-1 veya 1..31 olmalıdır: '{g}'.");
                            }
                            ayinGunleri.Add(gun);
                        }
                        break;

                    case "BYMONTH":
                        foreach (var g in deger.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!int.TryParse(g.Trim(), out var ay) || ay < 1 || ay > 12)
                            {
                                throw new FormatException($"BYMONTH 1..12 olmalıdır: '{g}'.");
                            }
                            aylar.Add(ay);
                        }
                        break;

                    case "WKST":
                        if (!GunKodlari.TryGetValue(deger, out haftaBasi))
                        {
                            throw new FormatException($"WKST anlaşılamadı: '{deger}'.");
                        }
                        break;

                    default:
                        throw new FormatException(
                            $"Desteklenmeyen tekrar kuralı parçası: '{ad}'. Desteklenenler: FREQ, INTERVAL, COUNT, UNTIL, BYDAY, BYMONTHDAY, BYMONTH, WKST.");
                }
            }

            if (siklik == null)
            {
                throw new FormatException("Tekrar kuralında FREQ zorunludur.");
            }
            if (adet != null && until != null)
            {
                throw new FormatException("COUNT ve UNTIL aynı kuralda kullanılamaz (RFC 5545).");
            }
            if (siklik == RRuleSiklik.Gunluk && gunler.Count > 0)
            {
                throw new FormatException("Günlük tekrarda BYDAY kullanılamaz.");
            }
            if (siklik == RRuleSiklik.Haftalik && gunler.Any(g => g.Sira != null))
            {
                throw new FormatException("Haftalık tekrarda BYDAY sıra numarası (ör. 2TU) kullanılamaz.");
            }
            if (siklik != RRuleSiklik.Aylik && siklik != RRuleSiklik.Yillik && ayinGunleri.Count > 0)
            {
                throw new FormatException("BYMONTHDAY yalnızca aylık/yıllık tekrarda kullanılabilir.");
            }
            if (gunler.Count > 0 && ayinGunleri.Count > 0)
            {
                throw new FormatException("BYDAY ve BYMONTHDAY aynı kuralda desteklenmiyor.");
            }

            return new RRuleKural
            {
                Siklik = siklik.Value,
                Aralik = aralik,
                Adet = adet,
                BitisTarihi = until,
                Gunler = gunler,
                AyinGunleri = ayinGunleri,
                Aylar = aylar,
                HaftaBasi = haftaBasi
            };
        }

        private static RRuleGun GunAyristir(string kod)
        {
            if (kod.Length < 2)
            {
                throw new FormatException($"BYDAY anlaşılamadı: '{kod}'.");
            }

            var harfler = kod[^2..];
            var onEk = kod[..^2];

            if (!GunKodlari.TryGetValue(harfler, out var gun))
            {
                throw new FormatException($"BYDAY gün kodu anlaşılamadı: '{kod}'. (MO, TU, WE, TH, FR, SA, SU)");
            }

            if (string.IsNullOrEmpty(onEk))
            {
                return new RRuleGun(gun, null);
            }

            if (!int.TryParse(onEk, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var sira)
                || sira == 0 || sira < -5 || sira > 5)
            {
                throw new FormatException($"BYDAY sıra numarası -5..-1 veya 1..5 olmalıdır: '{kod}'.");
            }

            return new RRuleGun(gun, sira);
        }

        /// <summary>
        /// UNTIL değeri: <c>20261231</c>, <c>20261231T235959</c> veya
        /// <c>20261231T235959Z</c>. 'Z' (UTC) işareti KABUL EDİLİR ama saat
        /// dönüştürülmez — sistemin tüm zamanları yerel ve dilimsizdir; dönüştürmek
        /// bitiş gününü kaydırırdı.
        /// </summary>
        private static DateTime UntilAyristir(string deger)
        {
            var d = deger.Trim().TrimEnd('Z', 'z');
            var bicimler = new[] { "yyyyMMdd'T'HHmmss", "yyyyMMdd'T'HHmm", "yyyyMMdd" };

            if (DateTime.TryParseExact(d, bicimler, CultureInfo.InvariantCulture, DateTimeStyles.None, out var sonuc))
            {
                // Yalnızca gün verildiyse günün sonuna kadar geçerli sayılır.
                return d.Length == 8 ? sonuc.Date.AddDays(1).AddSeconds(-1) : sonuc;
            }

            throw new FormatException($"UNTIL biçimi anlaşılamadı: '{deger}'. (yyyyMMdd veya yyyyMMddTHHmmss)");
        }

        /// <summary>
        /// Kuralı YENİ bir başlangıca çapalar.
        ///
        /// Tekrarlanan bir etkinliğin tarihi "tümü" veya "bundan sonrakiler"
        /// kapsamıyla kaydırıldığında kuralın da yeni tarihe uyması gerekir; aksi
        /// hâlde ileride üretilecek tekrarlar ESKİ güne düşer (ör. kural
        /// <c>BYDAY=MO</c> kalır ama etkinlikler Çarşamba'ya taşınmıştır).
        ///
        /// Kaydırma mantığı:
        /// • BYDAY (sırasız): her gün, gün farkı kadar kaydırılır (mod 7).
        /// • BYDAY (sıralı, ör. 1TU): yeni başlangıçtan yeniden hesaplanır.
        /// • BYMONTHDAY: yeni başlangıcın ayın günü ile değiştirilir (negatif
        ///   değerler, "son gün" anlamını koruduğu için olduğu gibi bırakılır).
        /// • BYMONTH: yıllık kuralda yeni başlangıcın ayına taşınır.
        /// COUNT/UNTIL/INTERVAL/FREQ değişmez.
        /// </summary>
        public RRuleKural YenidenCapala(DateTime eskiBaslangic, DateTime yeniBaslangic)
        {
            var gunFarki = (yeniBaslangic.Date - eskiBaslangic.Date).Days;
            if (gunFarki == 0)
            {
                return this;   // yalnızca saat değişmiş; kural aynı kalır
            }

            var yeniGunler = Gunler.Count == 0
                ? Gunler
                : Gunler.Select(g => g.Sira == null
                        ? new RRuleGun((DayOfWeek)((((int)g.Gun + gunFarki) % 7 + 7) % 7), null)
                        : new RRuleGun(yeniBaslangic.DayOfWeek, AyinKacinciGunu(yeniBaslangic)))
                    .Distinct()
                    .ToList();

            var yeniAyinGunleri = AyinGunleri.Count == 0
                ? AyinGunleri
                : AyinGunleri.Select(g => g < 0 ? g : yeniBaslangic.Day).Distinct().ToList();

            var yeniAylar = Aylar.Count == 0 || Siklik != RRuleSiklik.Yillik
                ? Aylar
                : new List<int> { yeniBaslangic.Month };

            return new RRuleKural
            {
                Siklik = Siklik,
                Aralik = Aralik,
                Adet = Adet,
                BitisTarihi = BitisTarihi,
                Gunler = yeniGunler,
                AyinGunleri = yeniAyinGunleri,
                Aylar = yeniAylar,
                HaftaBasi = HaftaBasi
            };
        }

        /// <summary>Tarihin, ay içinde kaçıncı (aynı isimli) gün olduğunu verir: 1..5.</summary>
        private static int AyinKacinciGunu(DateTime tarih) => (tarih.Day - 1) / 7 + 1;

        /// <summary>
        /// Kuralın bitişini <paramref name="bitis"/> olarak değiştirir; COUNT varsa
        /// kaldırılır (RFC 5545 ikisini birlikte yasaklar). "Bundan sonrakiler"
        /// kapsamında eski seriyi kapatmak için kullanılır.
        /// </summary>
        public RRuleKural BitisleKapat(DateTime bitis) => new()
        {
            Siklik = Siklik,
            Aralik = Aralik,
            Adet = null,
            BitisTarihi = bitis,
            Gunler = Gunler,
            AyinGunleri = AyinGunleri,
            Aylar = Aylar,
            HaftaBasi = HaftaBasi
        };

        /// <summary>
        /// COUNT'lu bir kural bölündüğünde (bundan sonrakiler) yeni serinin tekrar
        /// sayısını "kalan" kadar yapar. COUNT içermeyen kuralda hiçbir şey değişmez.
        ///
        /// Olmazsa toplam tekrar sayısı kendiliğinden artar: 6 tekrarlık bir seri
        /// 3. tekrardan bölündüğünde 2 + 6 = 8 tekrar olurdu.
        /// </summary>
        public RRuleKural KalanAdetle(int oncekiAdet)
        {
            if (Adet == null)
            {
                return this;
            }

            var kalan = Math.Max(1, Adet.Value - oncekiAdet);
            return new RRuleKural
            {
                Siklik = Siklik,
                Aralik = Aralik,
                Adet = kalan,
                BitisTarihi = BitisTarihi,
                Gunler = Gunler,
                AyinGunleri = AyinGunleri,
                Aylar = Aylar,
                HaftaBasi = HaftaBasi
            };
        }

        // ------------------------------------------------------------------- yazma
        /// <summary>Kuralı standart RRULE metnine çevirir (önek olmadan).</summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("FREQ=").Append(Siklik switch
            {
                RRuleSiklik.Gunluk => "DAILY",
                RRuleSiklik.Haftalik => "WEEKLY",
                RRuleSiklik.Aylik => "MONTHLY",
                _ => "YEARLY"
            });

            if (Aralik > 1)
            {
                sb.Append(";INTERVAL=").Append(Aralik);
            }
            if (Gunler.Count > 0)
            {
                sb.Append(";BYDAY=").Append(string.Join(",", Gunler.Select(g =>
                    (g.Sira?.ToString(CultureInfo.InvariantCulture) ?? string.Empty) + GunKodlariTers[g.Gun])));
            }
            if (AyinGunleri.Count > 0)
            {
                sb.Append(";BYMONTHDAY=").Append(string.Join(",", AyinGunleri));
            }
            if (Aylar.Count > 0)
            {
                sb.Append(";BYMONTH=").Append(string.Join(",", Aylar));
            }
            if (Adet != null)
            {
                sb.Append(";COUNT=").Append(Adet.Value);
            }
            if (BitisTarihi != null)
            {
                sb.Append(";UNTIL=").Append(BitisTarihi.Value.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }
    }
}
