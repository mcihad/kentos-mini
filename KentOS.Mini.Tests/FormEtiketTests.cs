using System.Text.Json;
using KentOS.Mini.Application.Dto.V2.Form;
using KentOS.Mini.Application.Enums;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// CEVAP → OKUNUR METİN.
/// </summary>
/// <remarks>
/// <para>
/// JSONB'de seçenek KİMLİĞİ duruyor (<c>o_3</c>, <c>r_1</c>). Yanıt
/// detayı, özet raporu ve Excel çıktısı bir dönem bu kimliği olduğu gibi
/// basıyordu: matris cevabı <c>r_1: c_2</c>, çoklu seçim
/// <c>o_spor, o_kultur</c> diye okunuyordu. Kullanıcının şikâyeti buydu.
/// </para>
/// <para>
/// Hata <b>sessiz</b>: istisna atılmıyor, sayfa açılıyor, yalnızca yazan
/// şey anlamsız. Bu yüzden bekçi davranışı ölçüyor.
/// </para>
/// </remarks>
public class FormEtiketTests
{
    private static FormAlaniDto Secimli() => new()
    {
        Kimlik = "a_kanal",
        Tip = FormAlanTipi.TekSecim,
        Etiket = "Nereden duydunuz?",
        Secenekler =
        [
            new() { Kimlik = "o_afis", Etiket = "Afiş" },
            new() { Kimlik = "o_diger", Etiket = "Diğer", DigerMi = true },
        ],
    };

    private static FormAlaniDto Matris() => new()
    {
        Kimlik = "a_mat",
        Tip = FormAlanTipi.MatrisTekSecim,
        Etiket = "Puanlayın",
        Satirlar =
        [
            new() { Kimlik = "r_temiz", Etiket = "Temizlik" },
            new() { Kimlik = "r_ulasim", Etiket = "Ulaşım" },
        ],
        Sutunlar =
        [
            new() { Kimlik = "c_iyi", Etiket = "İyi" },
            new() { Kimlik = "c_orta", Etiket = "Orta" },
        ],
    };

    /// <summary>Servisin gördüğü şekle çevirir — JSONB'den çözülmüş hâli.</summary>
    private static object? Sarmal(object deger, string? metin = null)
    {
        var govde = metin is null
            ? JsonSerializer.Serialize(new { deger })
            : JsonSerializer.Serialize(new { deger, metin });

        return JsonSerializer.Deserialize<JsonElement>(govde);
    }

    [Fact]
    public void Tek_secim_kimligi_degil_etiketi_yazar()
        => Assert.Equal("Afiş", Cevir(Secimli(), Sarmal("o_afis")));

    [Fact]
    public void Coklu_secim_her_kimligi_ayri_cevirir()
    {
        var alan = Secimli();
        alan.Tip = FormAlanTipi.CokSecim;
        alan.Secenekler!.Add(new FormSecenegiDto { Kimlik = "o_sosyal", Etiket = "Sosyal medya" });

        Assert.Equal("Afiş, Sosyal medya", Cevir(alan, Sarmal(new[] { "o_afis", "o_sosyal" })));
    }

    [Fact]
    public void Matris_satir_ve_sutunu_birlikte_cevirir()
    {
        var cevap = Sarmal(new Dictionary<string, string>
        {
            ["r_temiz"] = "c_iyi",
            ["r_ulasim"] = "c_orta",
        });

        Assert.Equal("Temizlik: İyi · Ulaşım: Orta", Cevir(Matris(), cevap));
    }

    [Fact]
    public void Diger_secildiginde_serbest_metin_parantez_icinde()
        => Assert.Equal("Diğer (Belediye afişi)",
            Cevir(Secimli(), Sarmal("o_diger", "Belediye afişi")));

    [Fact]
    public void Evet_hayir_turkce_yazilir()
    {
        var alan = new FormAlaniDto { Kimlik = "a", Tip = FormAlanTipi.EvetHayir };
        Assert.Equal("Evet", Cevir(alan, Sarmal(true)));
        Assert.Equal("Hayır", Cevir(alan, Sarmal(false)));
    }

    /// <summary>
    /// Tanımdan SİLİNMİŞ seçeneğin kimliği kalır — boş dönmez.
    /// </summary>
    /// <remarks>
    /// Eski sürümle verilmiş bir cevap, seçenek sonradan silinmişse
    /// çözülemiyor. Boş göstermek "bu soruya cevap verilmemiş" demek
    /// olurdu; ham kimlik en azından bir iz.
    /// </remarks>
    [Fact]
    public void Cozulemeyen_kimlik_oldugu_gibi_kalir()
        => Assert.Equal("o_kayip", Cevir(Secimli(), Sarmal("o_kayip")));

    /// <summary>
    /// YİNELENEN KİMLİK 500 VERMEZ.
    /// </summary>
    /// <remarks>
    /// Tasarımcıda alan tipi seçimden matrise çevrildiğinde eski
    /// <c>Secenekler</c> listesi tanımda kalabiliyor ve kimlikleri
    /// <c>Sutunlar</c> ile çakışabiliyor. Çözücü bir dönem
    /// <c>ToDictionary</c> kullanıyordu; orada <c>ArgumentException</c>
    /// atıp yanıt ekranını komple düşürürdü.
    /// </remarks>
    [Fact]
    public void Yinelenen_kimlik_istisna_atmaz()
    {
        var alan = Matris();
        alan.Secenekler = [new() { Kimlik = "c_iyi", Etiket = "Eski etiket" }];

        var cevap = Sarmal(new Dictionary<string, string> { ["r_temiz"] = "c_iyi" });

        Assert.Equal("Temizlik: Eski etiket", Cevir(alan, cevap));
    }

    /// <summary>Servisteki özel çeviriciyi yansımayla çağırır.</summary>
    private static string Cevir(FormAlaniDto alan, object? sarmal)
    {
        var tip = typeof(KentOS.Mini.Web.Services.V2.FormServisi).Assembly
            .GetType("KentOS.Mini.Web.Services.V2.FormDegerMetni")!;

        var metot = tip.GetMethod("Metin")!;
        return (string)metot.Invoke(null, [alan, sarmal, " · "])!;
    }
}
