using System.Text.Json;
using KentOS.Mini.Application.Dto.V2.Form;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Web.Services.V2;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// FORM DOĞRULAMASI — anonim yüzeyin tek kapısı.
/// </summary>
/// <remarks>
/// Vatandaş yüzeyi anonim; gönderim tarayıcıdan da <c>curl</c>'den de
/// gelebiliyor. İstemcideki doğrulama bir kolaylık, karar burada veriliyor —
/// bu yüzden testler "istemci hiç yokmuş gibi" yazıldı.
/// </remarks>
public class FormDogrulamaTests
{
    private static FormTanimiDto Tanim(params FormAlaniDto[] alanlar) => new()
    {
        Adimlar =
        [
            new FormAdimiDto
            {
                Kimlik = "a1",
                Gruplar = [new FormGrubuDto { Kimlik = "g1", Alanlar = [.. alanlar] }],
            },
        ],
    };

    /// <summary>
    /// Gövde SARMALAYICI ile kurulur: <c>{ "alan": { "deger": … } }</c>.
    /// </summary>
    /// <remarks>
    /// Düz değerle test etmek, gerçek istekte kullanılan şekli hiç
    /// denememek demekti — bu modülün en olası sessiz hatası tam olarak
    /// budur.
    /// </remarks>
    private static Dictionary<string, object?> Gelen(params (string, object?)[] c) =>
        c.ToDictionary(
            x => x.Item1,
            x => (object?)new Dictionary<string, object?> { ["deger"] = x.Item2 });

    // ─────────────────────────────────────────────── bilinmeyen alan

    /// <summary>
    /// Tanımda olmayan alan REDDEDİLİR, sessizce atılmaz.
    /// </summary>
    /// <remarks>
    /// Atılsaydı gönderim başarılı görünür ve kimse verinin düştüğünü fark
    /// etmezdi; üstelik bilinmeyen anahtarları kabul etmek JSONB'yi
    /// saldırganın istediği kadar şişirebileceği bir çöp alanına çevirirdi.
    /// </remarks>
    [Fact]
    public void Tanimda_olmayan_alan_reddedilir()
    {
        var t = Tanim(new FormAlaniDto { Kimlik = "ad", Tip = FormAlanTipi.KisaMetin, Etiket = "Ad" });
        var s = FormDogrulayici.Dogrula(t, Gelen(("ad", "Ali"), ("uydurma", "x")));

        Assert.False(s.Gecerli);
        Assert.Contains("uydurma", s.Hatalar.Keys);
    }

    // ─────────────────────────────────────────────── seçim güvenliği

    /// <summary>
    /// Seçenek listesinde OLMAYAN değer kabul edilmez.
    /// </summary>
    /// <remarks>
    /// İstemcinin gönderdiğine güvenilseydi, açılır listeye elle yazılan
    /// herhangi bir metin veritabanına girerdi.
    /// </remarks>
    [Fact]
    public void Listede_olmayan_secim_reddedilir()
    {
        var t = Tanim(new FormAlaniDto
        {
            Kimlik = "renk", Tip = FormAlanTipi.TekSecim, Etiket = "Renk",
            Secenekler = [new() { Kimlik = "kirmizi", Etiket = "Kırmızı" }],
        });

        Assert.True(FormDogrulayici.Dogrula(t, Gelen(("renk", "kirmizi"))).Gecerli);
        Assert.False(FormDogrulayici.Dogrula(t, Gelen(("renk", "mor"))).Gecerli);
    }

    [Fact]
    public void Cok_secimde_secim_sayisi_sinirlanir()
    {
        var t = Tanim(new FormAlaniDto
        {
            Kimlik = "ilgi", Tip = FormAlanTipi.CokSecim, Etiket = "İlgi",
            Secenekler = [new() { Kimlik = "a" }, new() { Kimlik = "b" }, new() { Kimlik = "c" }],
            Dogrulama = new FormDogrulamaDto { EnAzSecim = 2, EnCokSecim = 2 },
        });

        Assert.False(FormDogrulayici.Dogrula(t, Gelen(("ilgi", new List<string> { "a" }))).Gecerli);
        Assert.True(FormDogrulayici.Dogrula(t, Gelen(("ilgi", new List<string> { "a", "b" }))).Gecerli);
        Assert.False(FormDogrulayici.Dogrula(t, Gelen(("ilgi", new List<string> { "a", "b", "c" }))).Gecerli);
    }

    // ─────────────────────────────────────────────── matris

    /// <summary>
    /// Matriste hem SATIR hem SÜTUN tanımda olmalı.
    /// </summary>
    /// <remarks>
    /// Yalnızca sütun denetlenseydi, olmayan bir satıra cevap yazmak
    /// JSONB'ye uydurma anahtar sokardı.
    /// </remarks>
    [Fact]
    public void Matriste_uydurma_satir_reddedilir()
    {
        var t = Tanim(new FormAlaniDto
        {
            Kimlik = "m", Tip = FormAlanTipi.MatrisTekSecim, Etiket = "Değerlendirme",
            Satirlar = [new() { Kimlik = "hiz" }, new() { Kimlik = "kalite" }],
            Sutunlar = [new() { Kimlik = "iyi" }, new() { Kimlik = "kotu" }],
        });

        var gecerli = new Dictionary<string, object?> { ["hiz"] = "iyi" };
        Assert.True(FormDogrulayici.Dogrula(t, Gelen(("m", gecerli))).Gecerli);

        var uydurmaSatir = new Dictionary<string, object?> { ["yok"] = "iyi" };
        Assert.False(FormDogrulayici.Dogrula(t, Gelen(("m", uydurmaSatir))).Gecerli);

        var uydurmaSutun = new Dictionary<string, object?> { ["hiz"] = "yok" };
        Assert.False(FormDogrulayici.Dogrula(t, Gelen(("m", uydurmaSutun))).Gecerli);
    }

    // ─────────────────────────────────────────────── koşullu görünürlük

    /// <summary>
    /// GÖRÜNMEYEN zorunlu alan hata vermez.
    /// </summary>
    /// <remarks>
    /// İstemci alanı hiç göstermiyor, dolayısıyla değer de göndermiyor.
    /// Doğrulama koşulu hesaplamasaydı form gönderilemez hâle gelirdi.
    /// </remarks>
    [Fact]
    public void Kosulu_saglanmayan_zorunlu_alan_hata_vermez()
    {
        var t = Tanim(
            new FormAlaniDto { Kimlik = "sikayet", Tip = FormAlanTipi.EvetHayir, Etiket = "Şikâyetiniz var mı?" },
            new FormAlaniDto
            {
                Kimlik = "detay", Tip = FormAlanTipi.UzunMetin, Etiket = "Detay", Zorunlu = true,
                Kosul = new FormKosuluDto
                {
                    Kurallar =
                    [
                        new FormKosulKuraliDto
                        {
                            AlanKimligi = "sikayet",
                            Operator = FormKosulOperatoru.Esit,
                            Deger = "true",
                        },
                    ],
                },
            });

        // Şikâyet YOK → detay görünmüyor → zorunluluk aranmıyor.
        Assert.True(FormDogrulayici.Dogrula(t, Gelen(("sikayet", "false"))).Gecerli);

        // Şikâyet VAR → detay görünüyor → zorunluluk işliyor.
        var s = FormDogrulayici.Dogrula(t, Gelen(("sikayet", "true")));
        Assert.False(s.Gecerli);
        Assert.Contains("detay", s.Hatalar.Keys);
    }

    // ─────────────────────────────────────────────── biçim kuralları

    [Theory]
    [InlineData("10000000146", true)]   // geçerli algoritma
    [InlineData("11111111111", false)]
    [InlineData("01234567890", false)]  // sıfırla başlayamaz
    [InlineData("123", false)]
    public void Tc_kimlik_algoritmasi(string tc, bool beklenen)
    {
        var t = Tanim(new FormAlaniDto { Kimlik = "tc", Tip = FormAlanTipi.TcKimlik, Etiket = "TC" });
        Assert.Equal(beklenen, FormDogrulayici.Dogrula(t, Gelen(("tc", tc))).Gecerli);
    }

    [Fact]
    public void Metin_mutlak_siniri_asamaz()
    {
        var t = Tanim(new FormAlaniDto { Kimlik = "n", Tip = FormAlanTipi.UzunMetin, Etiket = "Not" });
        var uzun = new string('a', FormDogrulayici.MutlakMetinSiniri + 1);

        Assert.False(FormDogrulayici.Dogrula(t, Gelen(("n", uzun))).Gecerli);
    }

    /// <summary>
    /// KÖTÜ DESEN alanı geçerli SAYMAZ.
    /// </summary>
    /// <remarks>
    /// Desen formu kuran yetkiliden geliyor. Katastrofik geri izleme yapan
    /// bir desen zaman aşımına uğradığında alanı "geçti" saymak, denetimi
    /// hiç koymamaktan kötü olurdu.
    /// </remarks>
    [Fact]
    public void Zaman_asimina_ugrayan_desen_gecerli_saymaz()
    {
        var t = Tanim(new FormAlaniDto
        {
            Kimlik = "x", Tip = FormAlanTipi.KisaMetin, Etiket = "X",
            Dogrulama = new FormDogrulamaDto { Desen = "^(a+)+$" },
        });

        var kurban = new string('a', 40) + "!";
        Assert.False(FormDogrulayici.Dogrula(t, Gelen(("x", kurban))).Gecerli);
    }

    // ─────────────────────────────────────────────── taslak

    /// <summary>Yarım kayıtta zorunluluk aranmaz, biçim yine denetlenir.</summary>
    [Fact]
    public void Taslakta_zorunluluk_aranmaz_ama_bicim_aranir()
    {
        var t = Tanim(
            new FormAlaniDto { Kimlik = "ad", Tip = FormAlanTipi.KisaMetin, Etiket = "Ad", Zorunlu = true },
            new FormAlaniDto { Kimlik = "eposta", Tip = FormAlanTipi.Eposta, Etiket = "E-posta" });

        Assert.True(FormDogrulayici.Dogrula(t, Gelen(), taslakMi: true).Gecerli);
        Assert.False(FormDogrulayici.Dogrula(t, Gelen(("eposta", "bozuk")), taslakMi: true).Gecerli);
    }

    // ─────────────────────────────────────────────── JsonElement yolu

    /// <summary>
    /// Gerçek gövde <c>JsonElement</c> taşır; doğrulayıcı onu çözmeli.
    /// </summary>
    /// <remarks>
    /// Testlerin düz .NET tipleriyle geçip gerçek istekte patlaması bu
    /// modülün en olası sessiz hatasıydı: gövde
    /// <c>Dictionary&lt;string, object?&gt;</c> olarak bağlanınca her değer
    /// <c>JsonElement</c> geliyor.
    /// </remarks>
    [Fact]
    public void JsonElement_govdesi_cozulur()
    {
        var t = Tanim(
            new FormAlaniDto { Kimlik = "ad", Tip = FormAlanTipi.KisaMetin, Etiket = "Ad", Zorunlu = true },
            new FormAlaniDto
            {
                Kimlik = "ilgi", Tip = FormAlanTipi.CokSecim, Etiket = "İlgi",
                Secenekler = [new() { Kimlik = "a" }, new() { Kimlik = "b" }],
            });

        var govde = JsonSerializer.Deserialize<Dictionary<string, object?>>(
            """{"ad":{"deger":"Ali"},"ilgi":{"deger":["a","b"]}}""")!;

        var s = FormDogrulayici.Dogrula(t, govde);

        Assert.True(s.Gecerli);
        Assert.Equal("Ali", FormDogrulayici.Deger(s.TemizCevaplar["ad"]));
        Assert.Equal(new List<string> { "a", "b" }, FormDogrulayici.Deger(s.TemizCevaplar["ilgi"]));
    }

    [Fact]
    public void Cok_fazla_cevap_reddedilir()
    {
        var t = Tanim(new FormAlaniDto { Kimlik = "a", Tip = FormAlanTipi.KisaMetin, Etiket = "A" });
        var govde = Enumerable.Range(0, FormDogrulayici.MutlakCevapSayisi + 1)
            .ToDictionary(i => $"k{i}",
                i => (object?)new Dictionary<string, object?> { ["deger"] = "x" });

        Assert.False(FormDogrulayici.Dogrula(t, govde).Gecerli);
    }
}
