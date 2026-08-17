using KentOS.Mini.Application.Enums;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Tests;

/// <summary>
/// Halk günü çıktılarında <b>başlık sayısı ile sütun sayısı</b> tutmak zorunda.
/// </summary>
/// <remarks>
/// <para>
/// Gerçek arıza: sonuç raporunda PDF tablosunun 7 sütunu vardı ama başlıklar
/// Excel ile ORTAK listeden geliyordu ve orada 8 tane. QuestPDF fazla hücreyi
/// sessizce <b>ikinci başlık satırına</b> sarıyor; "İlgilenilecek" ilk sütuna
/// düşüp "Sıra"nın hemen altında, ona yapışık görünüyordu. Ne bir hata ne bir
/// uyarı çıkıyordu — kâğıt basılana kadar kimse fark etmedi.
/// </para>
/// <para>
/// Aynı bilgi iki çıktıda farklı taşınıyor (Excel'de ayrı sütun, PDF'te
/// durumun yanında ★) ve bu kasıtlı; kural, her çıktının başlık sayısının
/// KENDİ sütun sayısına eşit olması.
/// </para>
/// </remarks>
public class HalkGunuCiktiSutunTests
{
    public static TheoryData<HalkGunuCiktiTuru> Turler =>
    [
        HalkGunuCiktiTuru.Program,
        HalkGunuCiktiTuru.Sonuc,
        HalkGunuCiktiTuru.Imza,
    ];

    [Theory]
    [MemberData(nameof(Turler))]
    public void Pdf_baslik_sayisi_sutun_sayisina_esit(HalkGunuCiktiTuru tur)
    {
        var sutunlar = HalkGunuCiktiServisi.PdfSutunlari(tur);

        Assert.NotEmpty(sutunlar);
        Assert.All(sutunlar, s => Assert.False(string.IsNullOrWhiteSpace(s.Baslik)));
        Assert.All(sutunlar, s => Assert.True(s.Genislik > 0));
    }

    [Theory]
    [MemberData(nameof(Turler))]
    public void Excel_baslik_sayisi_satir_hucre_sayisina_esit(HalkGunuCiktiTuru tur)
    {
        var basliklar = HalkGunuCiktiServisi.Basliklar(tur);
        var satir = HalkGunuCiktiServisi.Satir(OrnekKatilim(), sira: 1, saat: "14:00", tur);

        Assert.Equal(basliklar.Length, satir.Length);
    }

    /// <summary>
    /// PDF'te "İlgilenilecek" AYRI SÜTUN DEĞİL: durumun yanına konan ★.
    /// Sütun olarak geri eklenirse bu test, tabloyu bozan hâli yakalar.
    /// </summary>
    [Fact]
    public void Pdf_sonuc_raporunda_ilgilenilecek_ayri_sutun_degil()
    {
        var basliklar = HalkGunuCiktiServisi.PdfSutunlari(HalkGunuCiktiTuru.Sonuc)
            .Select(s => s.Baslik).ToArray();

        Assert.DoesNotContain("İlgilenilecek", basliklar);
        Assert.Equal(
            ["Sıra", "Saat", "Telefon", "Ad Soyad", "Açıklama", "Durum", "Görüşme Notu"],
            basliklar);
    }

    /// <summary>Excel'de ise ayrı sütun OLMALI — orada tablo süzülüp sayılıyor.</summary>
    [Fact]
    public void Excel_sonuc_raporunda_ilgilenilecek_ayri_sutun()
    {
        Assert.Contains("İlgilenilecek", HalkGunuCiktiServisi.Basliklar(HalkGunuCiktiTuru.Sonuc));
    }

    /// <summary>
    /// Katılım çizelgesi kayıtlı durumu GÖSTERİR.
    /// </summary>
    /// <remarks>
    /// Çizelge herkes için boş kutu basıyordu: salonda "Gelmedi" işaretlenen
    /// vatandaş, kâğıtta gelenlerden ayırt edilemiyordu. Belgenin adı katılım
    /// çizelgesi ama katılımı okumuyordu.
    ///
    /// Boş kalması gereken TEK durum "Bekliyor": gün başlamadan basılan kâğıt
    /// elle işaretlenecek, oraya baştan bir işaret koymak formu bozardı.
    /// </remarks>
    [Theory]
    [InlineData(KatilimDurumu.Bekliyor, "")]
    [InlineData(KatilimDurumu.Geldi, "Geldi")]
    [InlineData(KatilimDurumu.Gorusuldu, "Geldi")]
    [InlineData(KatilimDurumu.Gelmedi, "Gelmedi")]
    [InlineData(KatilimDurumu.Iptal, "İptal")]
    public void Katilim_cizelgesi_kayitli_durumu_yazar(KatilimDurumu durum, string beklenen)
    {
        var satir = HalkGunuCiktiServisi.Satir(
            new HalkGunuKatilimDto { AdSoyad = "Ayşe Demir", Durum = durum },
            sira: 1, saat: "14:00", HalkGunuCiktiTuru.Imza);

        // "Geldi" sütunu 6. hücre (0'dan: 5).
        Assert.Equal(beklenen, satir[5]);
    }

    private static HalkGunuKatilimDto OrnekKatilim() => new()
    {
        Id = 1,
        AdSoyad = "Ayşe Demir",
        Telefon = "0541 298 34 51",
        Konu = "Su borusu",
        DurumAd = "Görüşüldü",
        GorusmeNotu = "İncelenecek.",
        DegerlendirmeyeEsas = true,
    };
}
