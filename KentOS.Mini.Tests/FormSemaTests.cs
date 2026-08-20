using KentOS.Mini.Application.Dto.V2.Form;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Web.Services.V2;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// FORM ŞEMASININ DEĞİŞMEZLERİ.
/// </summary>
public class FormSemaTests
{
    /// <summary>
    /// ALAN TİPİ SAYILARI DONDURULDU.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tip, yayınlanmış formların JSONB tanımında <b>sayı</b> olarak
    /// saklanıyor. Bir üyeyi taşımak ya da araya değer sokmak, canlıdaki
    /// bütün formların sorularını başka tiplere çevirir — hiçbir istisna
    /// atmadan: "Kısa metin" sorusu bir gün "Matris" olarak açılır.
    /// </para>
    /// <para>
    /// Enum'ı metne çevirmek (<c>JsonStringEnumConverter</c>) da düşünüldü
    /// ve ELENDİ: bu deponun bütün v2 DTO'larında enum sayı gidiyor ve
    /// SPA'nın üretilen tipleri her yerde <c>number</c>. Tek bir istisna,
    /// <c>Ajanda.KullaniciId</c> ↔ <c>AjandaKatilimci.KullaniciId</c>
    /// sınıfı bir asimetri üretirdi. Korkuyu ortadan kaldıran şey
    /// dönüştürücü değil, bu test.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(FormAlanTipi.KisaMetin, 0)]
    [InlineData(FormAlanTipi.UzunMetin, 1)]
    [InlineData(FormAlanTipi.Eposta, 2)]
    [InlineData(FormAlanTipi.Telefon, 3)]
    [InlineData(FormAlanTipi.TcKimlik, 4)]
    [InlineData(FormAlanTipi.Url, 5)]
    [InlineData(FormAlanTipi.Sayi, 10)]
    [InlineData(FormAlanTipi.Tarih, 11)]
    [InlineData(FormAlanTipi.Saat, 12)]
    [InlineData(FormAlanTipi.TarihSaat, 13)]
    [InlineData(FormAlanTipi.TarihAraligi, 14)]
    [InlineData(FormAlanTipi.TekSecim, 20)]
    [InlineData(FormAlanTipi.CokSecim, 21)]
    [InlineData(FormAlanTipi.AcilirListe, 22)]
    [InlineData(FormAlanTipi.CokluAcilirListe, 23)]
    [InlineData(FormAlanTipi.EvetHayir, 24)]
    [InlineData(FormAlanTipi.Olcek, 30)]
    [InlineData(FormAlanTipi.Nps, 31)]
    [InlineData(FormAlanTipi.Yildiz, 32)]
    [InlineData(FormAlanTipi.MatrisTekSecim, 40)]
    [InlineData(FormAlanTipi.MatrisCokSecim, 41)]
    [InlineData(FormAlanTipi.Siralama, 42)]
    [InlineData(FormAlanTipi.Dosya, 50)]
    [InlineData(FormAlanTipi.Konum, 51)]
    [InlineData(FormAlanTipi.Imza, 52)]
    [InlineData(FormAlanTipi.Baslik, 60)]
    [InlineData(FormAlanTipi.Aciklama, 61)]
    [InlineData(FormAlanTipi.Ayirici, 62)]
    [InlineData(FormAlanTipi.Gorsel, 63)]
    public void Alan_tipi_sayilari_donduruldu(FormAlanTipi tip, int beklenen)
        => Assert.Equal(beklenen, (int)tip);

    [Theory]
    [InlineData(FormDurumu.Taslak, 0)]
    [InlineData(FormDurumu.Yayinda, 1)]
    [InlineData(FormDurumu.Kapali, 2)]
    [InlineData(FormDurumu.Arsiv, 3)]
    public void Form_durum_sayilari_donduruldu(FormDurumu d, int beklenen)
        => Assert.Equal(beklenen, (int)d);

    [Theory]
    [InlineData(FormErisimi.Anonim, 0)]
    [InlineData(FormErisimi.TelefonDogrulamali, 1)]
    [InlineData(FormErisimi.Personel, 2)]
    public void Erisim_sayilari_donduruldu(FormErisimi e, int beklenen)
        => Assert.Equal(beklenen, (int)e);

    /// <summary>
    /// Veri taşıyan HER tipin doğrulayıcısı olmalı.
    /// </summary>
    /// <remarks>
    /// Doğrulayıcısı olmayan bir tip <b>sessizce her şeyi kabul eder</b>:
    /// alan çalışır, veri girer, kimse denetlenmediğini fark etmez.
    /// Paletten çıkarılmış tipler bile denetlenir — kapsam dışı bırakma
    /// TASARIMCININ kararı, doğrulayıcının değil; API'den yine gelebilir.
    /// </remarks>
    [Fact]
    public void Veri_tasiyan_her_tip_dogrulaniyor()
    {
        var denetimsiz = new List<string>();

        foreach (FormAlanTipi tip in Enum.GetValues<FormAlanTipi>())
        {
            if (FormDogrulayici.BlokMu(tip)) continue;

            var tanim = new FormTanimiDto
            {
                Adimlar =
                [
                    new FormAdimiDto
                    {
                        Kimlik = "a",
                        Gruplar =
                        [
                            new FormGrubuDto
                            {
                                Kimlik = "g",
                                Alanlar =
                                [
                                    new FormAlaniDto
                                    {
                                        Kimlik = "x", Tip = tip, Etiket = "X",
                                        // Seçim tipleri için BOŞ seçenek listesi:
                                        // hiçbir değer geçerli olmamalı.
                                        Secenekler = [],
                                        Satirlar = [],
                                        Sutunlar = [],
                                    },
                                ],
                            },
                        ],
                    },
                ],
            };

            var govde = new Dictionary<string, object?>
            {
                ["x"] = new Dictionary<string, object?> { ["deger"] = "!!uydurma-deger!!" },
            };

            var s = FormDogrulayici.Dogrula(tanim, govde);

            // Metin tipleri bu değeri KABUL EDER; onlar için denetim
            // uzunluk/desen üzerinden. Yalnızca "her şeyi kabul eden ve
            // hiçbir kuralı olmayan" tipleri arıyoruz.
            var metinselMi = tip is FormAlanTipi.KisaMetin or FormAlanTipi.UzunMetin
                or FormAlanTipi.Konum or FormAlanTipi.Imza or FormAlanTipi.TarihAraligi
                or FormAlanTipi.Dosya or FormAlanTipi.EvetHayir;

            if (!metinselMi && s.Gecerli) denetimsiz.Add(tip.ToString());
        }

        Assert.True(denetimsiz.Count == 0,
            "Şu tipler uydurma bir değeri sessizce kabul etti: " + string.Join(", ", denetimsiz));
    }
}
