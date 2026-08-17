using KentOS.Mini.Application.Services;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// Tekrar kuralı (RRULE) ayrıştırma + genişletme testleri.
///
/// Genişletici saf fonksiyon olduğu için burada veritabanı/oturum yoktur —
/// takvim mantığının tamamı bu dosyada kilitlenir. Saatlerin KAYMAMASI özellikle
/// önemlidir: "meclis toplantısı her ayın ilk Salı 14:00" yaz saati geçişinde de
/// 14:00 kalmalıdır.
/// </summary>
public class RRuleTests
{
    private static readonly DateTime Ufuk2030 = new(2030, 1, 1);

    // ------------------------------------------------------------------ ayrıştırma
    [Fact]
    public void Ayristir_Onek_Ve_Bosluk_Toleransli()
    {
        var kural = RRuleKural.Ayristir("  RRULE:FREQ=WEEKLY;BYDAY=MO,WE;INTERVAL=2  ");

        Assert.Equal(RRuleSiklik.Haftalik, kural.Siklik);
        Assert.Equal(2, kural.Aralik);
        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Wednesday], kural.Gunler.Select(g => g.Gun));
    }

    [Fact]
    public void Ayristir_Siralı_Byday_Okur()
    {
        var kural = RRuleKural.Ayristir("FREQ=MONTHLY;BYDAY=1TU");

        Assert.Single(kural.Gunler);
        Assert.Equal(DayOfWeek.Tuesday, kural.Gunler[0].Gun);
        Assert.Equal(1, kural.Gunler[0].Sira);
    }

    [Theory]
    [InlineData("")]                                   // boş
    [InlineData("INTERVAL=2")]                         // FREQ yok
    [InlineData("FREQ=HOURLY")]                        // desteklenmeyen sıklık
    [InlineData("FREQ=DAILY;COUNT=3;UNTIL=20301231")]  // ikisi birlikte olamaz
    [InlineData("FREQ=DAILY;BYDAY=MO")]                // günlükte BYDAY
    [InlineData("FREQ=WEEKLY;BYDAY=2TU")]              // haftalıkta sıra
    [InlineData("FREQ=WEEKLY;BYMONTHDAY=15")]          // haftalıkta BYMONTHDAY
    [InlineData("FREQ=DAILY;BYSETPOS=1")]              // desteklenmeyen parça
    [InlineData("FREQ=DAILY;INTERVAL=0")]              // geçersiz aralık
    [InlineData("FREQ=MONTHLY;BYMONTHDAY=0")]          // geçersiz gün
    public void Ayristir_Gecersiz_Kurallari_Reddeder(string rrule)
    {
        Assert.Throws<FormatException>(() => RRuleKural.Ayristir(rrule));
    }

    [Fact]
    public void ToString_Kurali_Geri_Uretir()
    {
        const string metin = "FREQ=MONTHLY;INTERVAL=2;BYDAY=-1FR;COUNT=5";
        Assert.Equal(metin, RRuleKural.Ayristir(metin).ToString());
    }

    // ------------------------------------------------------------------- günlük
    [Fact]
    public void Gunluk_Count_Kadar_Uretir_Ve_Saati_Korur()
    {
        var dtstart = new DateTime(2026, 3, 2, 14, 30, 0);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=DAILY;COUNT=4", dtstart, Ufuk2030);

        Assert.Equal(4, tekrarlar.Count);
        Assert.Equal(dtstart, tekrarlar[0]);
        Assert.Equal(new DateTime(2026, 3, 5, 14, 30, 0), tekrarlar[3]);
        Assert.All(tekrarlar, t => Assert.Equal(new TimeSpan(14, 30, 0), t.TimeOfDay));
    }

    [Fact]
    public void Gunluk_Interval_Atlar()
    {
        var dtstart = new DateTime(2026, 3, 2, 9, 0, 0);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=DAILY;INTERVAL=3;COUNT=3", dtstart, Ufuk2030);

        Assert.Equal(
            [new DateTime(2026, 3, 2, 9, 0, 0), new DateTime(2026, 3, 5, 9, 0, 0), new DateTime(2026, 3, 8, 9, 0, 0)],
            tekrarlar);
    }

    [Fact]
    public void Yaz_Saati_Gecisinde_Saat_Kaymaz()
    {
        // Türkiye 2016'dan beri kalıcı UTC+3; yine de kayan saat mantığını
        // Avrupa'nın geçiş tarihlerini kapsayan bir aralıkla doğrula.
        var dtstart = new DateTime(2026, 3, 25, 14, 0, 0);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=WEEKLY;BYDAY=WE;COUNT=4", dtstart, Ufuk2030);

        Assert.All(tekrarlar, t => Assert.Equal(new TimeSpan(14, 0, 0), t.TimeOfDay));
        Assert.All(tekrarlar, t => Assert.Equal(DayOfWeek.Wednesday, t.DayOfWeek));
    }

    // ------------------------------------------------------------------ haftalık
    [Fact]
    public void Haftalik_Byday_Yoksa_Dtstart_Gununu_Kullanir()
    {
        var dtstart = new DateTime(2026, 3, 3, 10, 0, 0);   // Salı
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=WEEKLY;COUNT=3", dtstart, Ufuk2030);

        Assert.All(tekrarlar, t => Assert.Equal(DayOfWeek.Tuesday, t.DayOfWeek));
        Assert.Equal(new DateTime(2026, 3, 17, 10, 0, 0), tekrarlar[2]);
    }

    [Fact]
    public void Haftalik_Coklu_Gun_Tarih_Sirasinda_Gelir()
    {
        var dtstart = new DateTime(2026, 3, 2, 8, 0, 0);    // Pazartesi
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=WEEKLY;BYDAY=MO,WE,FR;COUNT=6", dtstart, Ufuk2030);

        Assert.Equal(
            [
                new DateTime(2026, 3, 2, 8, 0, 0),   // Pzt
                new DateTime(2026, 3, 4, 8, 0, 0),   // Çar
                new DateTime(2026, 3, 6, 8, 0, 0),   // Cum
                new DateTime(2026, 3, 9, 8, 0, 0),
                new DateTime(2026, 3, 11, 8, 0, 0),
                new DateTime(2026, 3, 13, 8, 0, 0)
            ],
            tekrarlar);
    }

    [Fact]
    public void Haftalik_Interval2_Haftayi_Atlar_Ve_Dtstart_Oncesini_Uretmez()
    {
        // DTSTART Çarşamba; kural Pazartesi+Cuma. Aynı haftanın Pazartesi'si
        // DTSTART'tan ÖNCE olduğu için üretilmemeli.
        var dtstart = new DateTime(2026, 3, 4, 15, 0, 0);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=WEEKLY;INTERVAL=2;BYDAY=MO,FR;COUNT=4", dtstart, Ufuk2030);

        Assert.Equal(
            [
                new DateTime(2026, 3, 6, 15, 0, 0),    // aynı hafta Cuma
                new DateTime(2026, 3, 16, 15, 0, 0),   // iki hafta sonra Pzt
                new DateTime(2026, 3, 20, 15, 0, 0),   // ve Cuma
                new DateTime(2026, 3, 30, 15, 0, 0)
            ],
            tekrarlar);
    }

    // --------------------------------------------------------------------- aylık
    [Fact]
    public void Aylik_Bymonthday_Coklu_Gun()
    {
        var dtstart = new DateTime(2026, 3, 1, 9, 0, 0);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=MONTHLY;BYMONTHDAY=1,15;COUNT=4", dtstart, Ufuk2030);

        Assert.Equal(
            [
                new DateTime(2026, 3, 1, 9, 0, 0),
                new DateTime(2026, 3, 15, 9, 0, 0),
                new DateTime(2026, 4, 1, 9, 0, 0),
                new DateTime(2026, 4, 15, 9, 0, 0)
            ],
            tekrarlar);
    }

    [Fact]
    public void Aylik_Son_Gun_Negatif_Deger()
    {
        var dtstart = new DateTime(2026, 1, 31, 17, 0, 0);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=MONTHLY;BYMONTHDAY=-1;COUNT=3", dtstart, Ufuk2030);

        Assert.Equal(
            [
                new DateTime(2026, 1, 31, 17, 0, 0),
                new DateTime(2026, 2, 28, 17, 0, 0),   // artık yıl değil
                new DateTime(2026, 3, 31, 17, 0, 0)
            ],
            tekrarlar);
    }

    [Fact]
    public void Aylik_Byday_Yoksa_31i_Olmayan_Ay_Atlanir()
    {
        // RFC 5545: ayda o gün yoksa o dönem üretilmez (31 Ocak → Şubat/Nisan atlanır).
        var dtstart = new DateTime(2026, 1, 31, 12, 0, 0);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=MONTHLY;COUNT=3", dtstart, Ufuk2030);

        Assert.Equal(
            [
                new DateTime(2026, 1, 31, 12, 0, 0),
                new DateTime(2026, 3, 31, 12, 0, 0),
                new DateTime(2026, 5, 31, 12, 0, 0)
            ],
            tekrarlar);
    }

    [Fact]
    public void Aylik_Ilk_Sali_Meclis_Senaryosu()
    {
        // "Meclis toplantısı her ayın ilk Salı günü saat 14:00"
        var dtstart = new DateTime(2026, 3, 3, 14, 0, 0);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=MONTHLY;BYDAY=1TU;COUNT=4", dtstart, Ufuk2030);

        Assert.Equal(
            [
                new DateTime(2026, 3, 3, 14, 0, 0),
                new DateTime(2026, 4, 7, 14, 0, 0),
                new DateTime(2026, 5, 5, 14, 0, 0),
                new DateTime(2026, 6, 2, 14, 0, 0)
            ],
            tekrarlar);
    }

    [Fact]
    public void Aylik_Son_Cuma_Encumen_Senaryosu()
    {
        var dtstart = new DateTime(2026, 3, 27, 11, 0, 0);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=MONTHLY;BYDAY=-1FR;COUNT=3", dtstart, Ufuk2030);

        Assert.Equal(
            [
                new DateTime(2026, 3, 27, 11, 0, 0),
                new DateTime(2026, 4, 24, 11, 0, 0),
                new DateTime(2026, 5, 29, 11, 0, 0)
            ],
            tekrarlar);
    }

    [Fact]
    public void Aylik_Sirasiz_Byday_Aydaki_Tum_Gunleri_Uretir()
    {
        var dtstart = new DateTime(2026, 3, 2, 10, 0, 0);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=MONTHLY;BYDAY=MO;COUNT=5", dtstart, Ufuk2030);

        Assert.Equal(
            [
                new DateTime(2026, 3, 2, 10, 0, 0),
                new DateTime(2026, 3, 9, 10, 0, 0),
                new DateTime(2026, 3, 16, 10, 0, 0),
                new DateTime(2026, 3, 23, 10, 0, 0),
                new DateTime(2026, 3, 30, 10, 0, 0)
            ],
            tekrarlar);
    }

    // --------------------------------------------------------------------- yıllık
    [Fact]
    public void Yillik_Dtstart_Gununu_Tekrarlar()
    {
        var dtstart = new DateTime(2026, 10, 29, 10, 0, 0);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=YEARLY;COUNT=3", dtstart, new DateTime(2035, 1, 1));

        Assert.Equal(
            [
                new DateTime(2026, 10, 29, 10, 0, 0),
                new DateTime(2027, 10, 29, 10, 0, 0),
                new DateTime(2028, 10, 29, 10, 0, 0)
            ],
            tekrarlar);
    }

    [Fact]
    public void Yillik_Bymonth_Ve_Byday_Birlikte()
    {
        var dtstart = new DateTime(2026, 9, 7, 9, 0, 0);   // Eylül'ün ilk Pazartesi
        var tekrarlar = RRuleGenisletici.Genislet(
            "FREQ=YEARLY;BYMONTH=9;BYDAY=1MO;COUNT=3", dtstart, new DateTime(2035, 1, 1));

        Assert.Equal(
            [
                new DateTime(2026, 9, 7, 9, 0, 0),
                new DateTime(2027, 9, 6, 9, 0, 0),
                new DateTime(2028, 9, 4, 9, 0, 0)
            ],
            tekrarlar);
    }

    // ---------------------------------------------------------------- sınırlar
    [Fact]
    public void Until_Tarihinden_Sonrasini_Uretmez()
    {
        var dtstart = new DateTime(2026, 3, 2, 9, 0, 0);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=DAILY;UNTIL=20260305T090000", dtstart, Ufuk2030);

        Assert.Equal(4, tekrarlar.Count);
        Assert.Equal(new DateTime(2026, 3, 5, 9, 0, 0), tekrarlar[^1]);
    }

    [Fact]
    public void Until_Sadece_Gun_Verilirse_Gun_Sonuna_Kadar()
    {
        var dtstart = new DateTime(2026, 3, 2, 23, 30, 0);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=DAILY;UNTIL=20260304", dtstart, Ufuk2030);

        Assert.Equal(3, tekrarlar.Count);   // 2, 3 ve 4 Mart 23:30 dahil
    }

    [Fact]
    public void Ufuk_Sinirsiz_Kurali_Kirpar()
    {
        var dtstart = new DateTime(2026, 3, 2, 9, 0, 0);
        var ufuk = new DateTime(2026, 3, 10, 23, 59, 59);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=DAILY", dtstart, ufuk);

        Assert.Equal(9, tekrarlar.Count);
        Assert.Equal(new DateTime(2026, 3, 10, 9, 0, 0), tekrarlar[^1]);
    }

    [Fact]
    public void EnFazlaAdet_Tavani_Uygulanir()
    {
        var dtstart = new DateTime(2026, 3, 2, 9, 0, 0);
        var tekrarlar = RRuleGenisletici.Genislet("FREQ=DAILY", dtstart, Ufuk2030, enFazlaAdet: 200);

        Assert.Equal(200, tekrarlar.Count);
    }

    [Fact]
    public void BaslangicDahil_Zaten_Uretilenleri_Atlar_Ama_Count_Sayimini_Bozmaz()
    {
        // Ufuk uzatma senaryosu: ilk 3 tekrar zaten üretilmiş; COUNT=5 kuralında
        // yalnızca kalan 2 tekrar dönmeli.
        var dtstart = new DateTime(2026, 3, 2, 9, 0, 0);
        var kalan = RRuleGenisletici.Genislet(
            "FREQ=DAILY;COUNT=5", dtstart, Ufuk2030, baslangicDahil: new DateTime(2026, 3, 5));

        Assert.Equal(
            [new DateTime(2026, 3, 5, 9, 0, 0), new DateTime(2026, 3, 6, 9, 0, 0)],
            kalan);
    }

    [Fact]
    public void Genisletme_Idempotent_Ayni_Sonucu_Verir()
    {
        var dtstart = new DateTime(2026, 3, 2, 9, 0, 0);
        var bir = RRuleGenisletici.Genislet("FREQ=WEEKLY;BYDAY=MO,TH;COUNT=10", dtstart, Ufuk2030);
        var iki = RRuleGenisletici.Genislet("FREQ=WEEKLY;BYDAY=MO,TH;COUNT=10", dtstart, Ufuk2030);

        Assert.Equal(bir, iki);
        Assert.Equal(bir.Distinct().Count(), bir.Count);       // tekrar eden tarih yok
        Assert.Equal(bir.OrderBy(x => x), bir);                 // artan sırada
    }

    // ----------------------------------------------------------------- Türkçe özet
    [Theory]
    [InlineData("FREQ=DAILY", "Her gün")]
    [InlineData("FREQ=DAILY;INTERVAL=3", "3 günde bir")]
    [InlineData("FREQ=WEEKLY;BYDAY=MO,WE", "Her hafta Pazartesi, Çarşamba")]
    [InlineData("FREQ=WEEKLY;INTERVAL=2;BYDAY=FR;COUNT=8", "2 haftada bir Cuma, 8 tekrar")]
    [InlineData("FREQ=MONTHLY;BYDAY=1TU", "Her ay birinci Salı")]
    [InlineData("FREQ=MONTHLY;BYDAY=-1FR", "Her ay son Cuma")]
    [InlineData("FREQ=MONTHLY;BYMONTHDAY=15", "Her ay 15. günü")]
    [InlineData("FREQ=MONTHLY;BYMONTHDAY=-1", "Her ay son gün")]
    [InlineData("FREQ=YEARLY;BYMONTH=9;BYDAY=1MO", "Her yıl Eylül birinci Pazartesi")]
    [InlineData("FREQ=DAILY;UNTIL=20261231T235959", "Her gün, 31.12.2026 tarihine kadar")]
    public void Ozet_Turkce_Metin_Uretir(string rrule, string beklenen)
    {
        Assert.Equal(beklenen, RRuleGenisletici.Ozet(rrule));
    }

    [Fact]
    public void Ozet_Bozuk_Kuralda_Kuralin_Kendisini_Dondurur()
    {
        Assert.Equal("FREQ=HOURLY", RRuleGenisletici.Ozet("FREQ=HOURLY"));
        Assert.Equal(string.Empty, RRuleGenisletici.Ozet((string?)null));
    }

    // ------------------------------------------------------- Serinin bitiş tarihi
    // Arayüzde "10 tekrar" yazısı serinin NE ZAMAN biteceğini anlatmıyordu;
    // COUNT'lu kuralda da somut son tekrar tarihi üretilmeli.

    [Fact]
    public void SonTekrar_Adetli_Kuralda_Somut_Tarih_Verir()
    {
        var dtstart = new DateTime(2026, 8, 19, 14, 0, 0);   // Çarşamba

        var son = RRuleGenisletici.SonTekrar("FREQ=WEEKLY;BYDAY=WE;COUNT=6", dtstart);

        // 19.08 + 5 hafta = 23.09.2026, saat korunur.
        Assert.Equal(new DateTime(2026, 9, 23, 14, 0, 0), son);
    }

    [Fact]
    public void SonTekrar_Untilli_Kuralda_Untile_Kadarki_Son_Tekrari_Verir()
    {
        var dtstart = new DateTime(2026, 8, 19, 14, 0, 0);

        // UNTIL gün sonudur; son tekrar UNTIL'in kendisi değil, 30.09 Çarşamba.
        var son = RRuleGenisletici.SonTekrar(
            "FREQ=WEEKLY;BYDAY=WE;UNTIL=20261002T235959", dtstart);

        Assert.Equal(new DateTime(2026, 9, 30, 14, 0, 0), son);
    }

    [Fact]
    public void SonTekrar_Sonsuz_Kuralda_Null_Doner()
    {
        var dtstart = new DateTime(2026, 8, 19, 14, 0, 0);

        Assert.Null(RRuleGenisletici.SonTekrar("FREQ=WEEKLY;BYDAY=WE", dtstart));
    }

    [Fact]
    public void SonTekrar_Aylik_Ve_Yillik_Kurallarda_Da_Calisir()
    {
        var dtstart = new DateTime(2026, 1, 15, 9, 30, 0);

        Assert.Equal(new DateTime(2026, 12, 15, 9, 30, 0),
            RRuleGenisletici.SonTekrar("FREQ=MONTHLY;BYMONTHDAY=15;COUNT=12", dtstart));

        Assert.Equal(new DateTime(2028, 1, 15, 9, 30, 0),
            RRuleGenisletici.SonTekrar("FREQ=YEARLY;COUNT=3", dtstart));
    }

    [Fact]
    public void SonTekrar_Bozuk_Veya_Bos_Kuralda_Null_Doner()
    {
        var dtstart = new DateTime(2026, 8, 19, 14, 0, 0);

        Assert.Null(RRuleGenisletici.SonTekrar((string?)null, dtstart));
        Assert.Null(RRuleGenisletici.SonTekrar("   ", dtstart));
        Assert.Null(RRuleGenisletici.SonTekrar("FREQ=HOURLY;COUNT=5", dtstart));
    }

    [Fact]
    public void SonTekrar_Guvenlik_Tavanini_Asmaz()
    {
        var dtstart = new DateTime(2026, 1, 1, 8, 0, 0);

        // COUNT tavanın üstünde olsa bile üretim 200 tekrarda durur; dönen tarih
        // 200. günün kendisidir (sonsuz döngüye girmez).
        var son = RRuleGenisletici.SonTekrar("FREQ=DAILY;COUNT=5000", dtstart);

        Assert.Equal(dtstart.AddDays(RRuleGenisletici.VarsayilanEnFazlaAdet - 1), son);
    }
}
