using KentOS.Kalem.Application.Services;

namespace KentOS.Kalem.Tests;

/// <summary>
/// SMS YER TUTUCULARI — metin gönderim anında doldurulur.
/// </summary>
/// <remarks>
/// Sunucu bir dönem yalnızca <c>{gonderici}</c> ve <c>{alici}</c>
/// değiştiriyordu ve bunu arayüzde söyleyen hiçbir şey yoktu: özellik vardı
/// ama kimse bilmiyordu. Katalog artık tek yerde ve istemcilere de sunuluyor.
/// </remarks>
public class SmsYerTutucuTests
{
    [Fact]
    public void Katalogda_mukerrer_ad_yok()
    {
        var adlar = SmsYerTutucu.Katalog.Select(k => k.Ad).ToList();
        Assert.Equal(adlar.Count, adlar.Distinct().Count());
    }

    [Fact]
    public void Her_kaydin_basligi_ve_aciklamasi_var()
    {
        // Seçicide adı değil BAŞLIĞI görüyoruz; boş bir başlık, listede boş
        // bir satır demek.
        Assert.All(SmsYerTutucu.Katalog, k =>
        {
            Assert.False(string.IsNullOrWhiteSpace(k.Baslik), k.Ad);
            Assert.False(string.IsNullOrWhiteSpace(k.Aciklama), k.Ad);
        });
    }

    [Fact]
    public void Yer_tutucular_degerleriyle_degisir()
    {
        var d = new Dictionary<string, string?> { ["alici"] = "Ahmet", ["saat"] = "14:30" };

        var sonuc = SmsYerTutucu.Uygula("Sayın {alici}, saat {saat}.", d);

        Assert.Equal("Sayın Ahmet, saat 14:30.", sonuc);
    }

    /// <summary>
    /// Değeri OLMAYAN yer tutucu BOŞA çevrilir.
    /// </summary>
    /// <remarks>
    /// Konumu girilmemiş bir etkinlikte mesajda "Konum: {konum}" yazması, boş
    /// bırakmaktan daha kötü.
    /// </remarks>
    [Fact]
    public void Degeri_olmayan_yer_tutucu_bosa_cevrilir()
    {
        var d = new Dictionary<string, string?> { ["konum"] = null };

        Assert.Equal("Yer: .", SmsYerTutucu.Uygula("Yer: {konum}.", d));
    }

    /// <summary>
    /// BİLİNMEYEN yer tutucu olduğu gibi kalır.
    /// </summary>
    /// <remarks>
    /// Boşa çevirmek, kullanıcının yazım hatasını (<c>{tarıh}</c>) sessizce
    /// yutup mesajı eksik gönderirdi; metinde görünmesi hatayı belli ediyor.
    /// </remarks>
    [Fact]
    public void Bilinmeyen_yer_tutucu_KORUNUR()
    {
        var d = new Dictionary<string, string?> { ["alici"] = "Ahmet" };

        Assert.Equal("Ahmet {tarıh}", SmsYerTutucu.Uygula("{alici} {tarıh}", d));
    }

    [Fact]
    public void Etkinlik_degerleri_tarih_saat_ve_gunu_turkce_uretir()
    {
        var d = SmsYerTutucu.EtkinlikDegerleri(
            "Muhtarlar Toplantısı",
            new DateTime(2026, 8, 20, 14, 30, 0),
            "Başkanlık Makamı",
            "Belediye Başkanlığı");

        Assert.Equal("20.08.2026", d["tarih"]);
        Assert.Equal("14:30", d["saat"]);
        Assert.Equal("Perşembe", d["gun"]);
        Assert.Equal("Başkanlık Makamı", d["konum"]);
    }

    [Fact]
    public void Bos_metin_bos_doner()
    {
        Assert.Equal(string.Empty, SmsYerTutucu.Uygula(null, new Dictionary<string, string?>()));
    }

    /// <summary>
    /// Katalogdaki HER ad, etkinlik değerleri ya da gönderim bağlamı
    /// tarafından üretiliyor mu?
    /// </summary>
    /// <remarks>
    /// Katalogda listelenip hiçbir yerde doldurulmayan bir yer tutucu,
    /// kullanıcıya "bunu kullanabilirsin" deyip mesajda ham metin bırakırdı.
    /// </remarks>
    [Fact]
    public void Katalogdaki_her_ad_gercekten_dolduruluyor()
    {
        var uretilen = SmsYerTutucu.EtkinlikDegerleri("b", DateTime.Now, "k", "br").Keys
            .Concat(["alici", "gonderici"])  // gönderim bağlamından gelenler
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var eksik = SmsYerTutucu.Katalog
            .Where(k => !uretilen.Contains(k.Ad))
            .Select(k => k.Ad)
            .ToList();

        Assert.True(eksik.Count == 0, "Doldurulmayan yer tutucu: " + string.Join(", ", eksik));
    }
}
