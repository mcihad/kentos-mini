using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto.V2.Form;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Exceptions;
using KentOS.Kalem.Web.Options;
using KentOS.Kalem.Web.Services.V2;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// VATANDAŞ AYNI FORMU İKİ KEZ GÖNDEREMEZ.
/// </summary>
/// <remarks>
/// <para>
/// "Tek yanıt" ayarı bir dönem <c>form.TekYanit &amp;&amp; telefonSade is not
/// null</c> diyordu: <b>telefon sormayan bir formda ayar açık görünüyor ama
/// hiçbir şey yapmıyordu.</b> Vatandaş gönderiyor, sayfayı yeniliyor,
/// yeniden dolduruyordu — şikâyet edilen davranış buydu.
/// </para>
/// <para>
/// İki ayrı kapı var ve karıştırılmamalı: <b>idempotans</b> aynı
/// gönderimin iki kez ulaşmasını (ağ yeniden denemesi, "geri" tuşu),
/// <b>tek yanıt</b> ise kişinin bilerek ikinci kez doldurmasını karşılar.
/// </para>
/// </remarks>
[Collection(SunucuKoleksiyonu.Ad)]
public class FormTekYanitTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    private static readonly FormTanimiDto Tanim = new()
    {
        Adimlar =
        [
            new()
            {
                Kimlik = "s1",
                Gruplar =
                [
                    new()
                    {
                        Kimlik = "g1",
                        Alanlar =
                        [
                            new()
                            {
                                Kimlik = "a_ad",
                                Tip = FormAlanTipi.KisaMetin,
                                Etiket = "Adınız",
                            },
                        ],
                    },
                ],
            },
        ],
    };

    private async Task<(FormYanitServisi servis, Form form)> KurAsync(bool tekYanit = true)
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var b = _ortam.Baglam();

        var form = new Form
        {
            Baslik = "Ölçüm",
            Durum = FormDurumu.Yayinda,
            ErisimAnahtari = Guid.NewGuid().ToString("N"),
            AnonimTuzu = Convert.ToHexString(Guid.NewGuid().ToByteArray()),
            TekYanit = tekYanit,
        };

        b.Formlar.Add(form);
        await b.SaveChangesAsync();

        var surum = new FormVersion
        {
            FormId = form.Id,
            SurumNo = 1,
            Tanim = JsonSerializer.Serialize(Tanim, FormServisi.JsonAyari),
        };

        b.FormSurumleri.Add(surum);
        await b.SaveChangesAsync();

        form.YayinSurumId = surum.Id;
        await b.SaveChangesAsync();

        /*
          BAĞIMLILIKLAR BİLEREK BOŞ.

          `GonderAsync` yalnızca veritabanına ve JWT ayarına dokunuyor;
          `IFormServisi`/`IInstitutionService`/`IFileStorage` yönetim ve
          dosya yollarında kullanılıyor. Boş geçmek, gönderim yolunun
          onlara SESSİZCE bağlanmasını da yakalar: bağlanırsa test
          `NullReferenceException` ile düşer.
        */
        var servis = new FormYanitServisi(
            b, null!, null!, null!,
            new JwtOptions { Secret = new string('x', 48) });

        return (servis, form);
    }

    private static FormYanitIstegiDto Istek(string? cihaz = null, string? telefon = null,
        string? anahtar = null) => new()
    {
        Cevaplar = new Dictionary<string, object?>
        {
            ["a_ad"] = new Dictionary<string, object?> { ["deger"] = "Ayşe" },
        },
        CihazAnahtari = cihaz,
        Telefon = telefon,
        SurdurmeAnahtari = anahtar,
    };

    /// <summary>
    /// TELEFON SORULMAYAN formda da ikinci gönderim reddedilir.
    /// </summary>
    /// <remarks>
    /// Bozulduğunda belirti sessiz: gönderim 200 döner, yalnızca aynı
    /// kişiden birden çok yanıt birikir.
    /// </remarks>
    [Fact]
    public async Task Ayni_cihazdan_ikinci_gonderim_reddedilir()
    {
        var (servis, form) = await KurAsync();

        await servis.GonderAsync(form.ErisimAnahtari, Istek(cihaz: "cihaz-A"), null, null);

        var hata = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.GonderAsync(form.ErisimAnahtari, Istek(cihaz: "cihaz-A"), null, null));

        Assert.Contains("zaten yanıt", hata.Message);
    }

    [Fact]
    public async Task Baska_cihazdan_gonderim_engellenmez()
    {
        var (servis, form) = await KurAsync();

        await servis.GonderAsync(form.ErisimAnahtari, Istek(cihaz: "cihaz-A"), null, null);
        var ikinci = await servis.GonderAsync(
            form.ErisimAnahtari, Istek(cihaz: "cihaz-B"), null, null);

        Assert.False(string.IsNullOrWhiteSpace(ikinci.TakipNo));
    }

    /// <summary>
    /// TELEFON, cihazı EZER — aynı numara başka tarayıcıdan geçemez.
    /// </summary>
    [Fact]
    public async Task Ayni_telefon_baska_cihazdan_gecemez()
    {
        var (servis, form) = await KurAsync();

        await servis.GonderAsync(form.ErisimAnahtari,
            Istek(cihaz: "cihaz-A", telefon: "0541 298 34 50"), null, null);

        // Aynı numaranın BAŞKA YAZIMI: `Telefon.Duzelt` tek biçime indiriyor.
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.GonderAsync(form.ErisimAnahtari,
                Istek(cihaz: "cihaz-B", telefon: "+90 541 298 34 50"), null, null));
    }

    /// <summary>
    /// AYNI GÖNDERİM İKİ KEZ ULAŞIRSA aynı kayıt döner — hata değil.
    /// </summary>
    /// <remarks>
    /// Kapı sırası burada ölçülüyor: idempotans denetimi "tek yanıt"
    /// denetiminden ÖNCE olmak zorunda. Ters sırada bu test
    /// <c>BusinessRuleException</c> ile düşer — yani kaydedilmiş bir
    /// gönderimin ağ yeniden denemesi vatandaşa hata gösterirdi.
    /// </remarks>
    [Fact]
    public async Task Ayni_anahtarla_ikinci_istek_ayni_kaydi_doner()
    {
        var (servis, form) = await KurAsync();

        var ilk = await servis.GonderAsync(
            form.ErisimAnahtari, Istek(cihaz: "cihaz-C", anahtar: "gonderim-1"), null, null);

        var tekrar = await servis.GonderAsync(
            form.ErisimAnahtari, Istek(cihaz: "cihaz-C", anahtar: "gonderim-1"), null, null);

        Assert.Equal(ilk.TakipNo, tekrar.TakipNo);

        using var b = _ortam.Baglam();
        var sayi = await b.FormYanitlari.CountAsync(y => y.FormId == form.Id);
        Assert.Equal(1, sayi);

        // Sayaç da bir kez artmalı: iki kez artsaydı yanıt sınırı olan bir
        // form, ağ yeniden denemeleriyle vaktinden önce kapanırdı.
        var form2 = await b.Formlar.AsNoTracking().FirstAsync(f => f.Id == form.Id);
        Assert.Equal(1, form2.YanitSayisi);
    }

    /// <summary>Ayar kapalıyken kimse engellenmez — kapı opt-in.</summary>
    [Fact]
    public async Task Tek_yanit_kapaliyken_tekrar_gonderilebilir()
    {
        var (servis, form) = await KurAsync(tekYanit: false);

        await servis.GonderAsync(form.ErisimAnahtari, Istek(cihaz: "cihaz-A"), null, null);
        var ikinci = await servis.GonderAsync(
            form.ErisimAnahtari, Istek(cihaz: "cihaz-A"), null, null);

        Assert.False(string.IsNullOrWhiteSpace(ikinci.TakipNo));
    }
}
