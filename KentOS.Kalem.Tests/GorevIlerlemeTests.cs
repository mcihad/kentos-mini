using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Web.Services.V2;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// İLERLEME KURALI — "aşamaları tamamlasak bile ilerlemiyor" arızasının bekçisi.
/// </summary>
/// <remarks>
/// <para>
/// Projenin yüzdesi <c>tamamlanan görev / toplam görev</c> ile hesaplanıyordu:
/// bir görev YÖNETİCİ ONAYINDAN geçene kadar sıfır sayılıyordu. Beş aşamalı
/// bir işin dördünü bitiren ekip, proje kartında, kilometre taşında ve gantt
/// çubuğunda hiçbir hareket görmüyordu. Kullanıcının bildirdiği arıza buydu.
/// </para>
/// <para>
/// Kural artık tek yerde (<see cref="GorevDurumAkisi.Ilerleme"/>) ve beş yer
/// oradan besleniyor: görev listesi, görev detayı, proje özeti, kilometre
/// taşı, gantt. Buradaki testler kuralın <b>davranışını</b> kilitliyor —
/// yeniden "kapandı mı?" ikilisine dönerse düşer.
/// </para>
/// </remarks>
public class GorevIlerlemeTests
{
    [Fact]
    public void Asama_kapandikca_ilerleme_ARTAR()
    {
        // Asıl arıza buydu: dördü de 0 dönüyordu.
        var oranlar = Enumerable.Range(0, 5)
            .Select(biten => GorevDurumAkisi.Ilerleme(GorevDurumu.DevamEdiyor, 4, biten))
            .ToList();

        Assert.Equal(0, oranlar[0]);
        Assert.True(oranlar[1] > oranlar[0], "1/4 aşama %0'dan büyük olmalı.");
        Assert.True(oranlar[2] > oranlar[1], "2/4 aşama 1/4'ten büyük olmalı.");
        Assert.True(oranlar[3] > oranlar[2], "3/4 aşama 2/4'ten büyük olmalı.");
    }

    [Fact]
    public void Butun_asamalar_bitse_bile_ONAYSIZ_gorev_yuzde_yuz_olmaz()
    {
        // Modülün en önemli kuralı beyan ile kabulün ayrı olması; çubuğun
        // dolması, kabul edilmemiş bir işi bitmiş göstermek olurdu.
        var beyan = GorevDurumAkisi.Ilerleme(GorevDurumu.TamamlanmaBekliyor, 4, 4);

        Assert.Equal(GorevDurumAkisi.OnaySiniri, beyan);
        Assert.True(beyan < 100);
    }

    [Fact]
    public void Onaylanan_gorev_asamasi_olmasa_bile_yuzde_yuz()
    {
        Assert.Equal(100, GorevDurumAkisi.Ilerleme(GorevDurumu.Tamamlandi, 0, 0));
        Assert.Equal(100, GorevDurumAkisi.Ilerleme(GorevDurumu.Tamamlandi, 4, 4));

        // Aşamaların yarısı kapalıyken onaylanmış bir görev de %100: onay son
        // sözdür, aşama sayacı değil.
        Assert.Equal(100, GorevDurumAkisi.Ilerleme(GorevDurumu.Tamamlandi, 4, 2));
    }

    [Fact]
    public void Asamasiz_gorev_de_ilerleme_gosterir()
    {
        // Modüldeki görevlerin çoğunun aşaması yok. Yalnızca aşamaya bakan bir
        // kural onları sonsuza kadar %0'da bırakırdı.
        Assert.True(GorevDurumAkisi.Ilerleme(GorevDurumu.DevamEdiyor, 0, 0) > 0);
        Assert.True(
            GorevDurumAkisi.Ilerleme(GorevDurumu.DevamEdiyor, 0, 0)
            > GorevDurumAkisi.Ilerleme(GorevDurumu.Atandi, 0, 0));
    }

    [Fact]
    public void Beklemedeki_gorevin_ilerlemesi_GERI_GITMEZ()
    {
        // Beklemede duraklamadır, geri dönüş değil: personelin yaptığı işi
        // ekranda geri almak olurdu.
        Assert.Equal(
            GorevDurumAkisi.Ilerleme(GorevDurumu.DevamEdiyor, 0, 0),
            GorevDurumAkisi.Ilerleme(GorevDurumu.Beklemede, 0, 0));

        // Aşamalı görevde de aynı: kapanan aşamalar beklemeye alınınca
        // silinmiyor.
        Assert.Equal(50, GorevDurumAkisi.Ilerleme(GorevDurumu.Beklemede, 4, 2));
    }

    [Fact]
    public void Iptal_ve_reddedilen_gorev_ORTALAMAYA_girmez()
    {
        // On görevin dokuzu bitmiş, biri iptal edilmişse proje %90 değil
        // %100 olmalı: iptal edilen iş yapılmayacak, eksik değil.
        Assert.False(GorevDurumAkisi.IlerlemeyeGirer(GorevDurumu.Iptal));
        Assert.False(GorevDurumAkisi.IlerlemeyeGirer(GorevDurumu.Reddedildi));

        Assert.True(GorevDurumAkisi.IlerlemeyeGirer(GorevDurumu.Yeni));
        Assert.True(GorevDurumAkisi.IlerlemeyeGirer(GorevDurumu.Beklemede));
        Assert.True(GorevDurumAkisi.IlerlemeyeGirer(GorevDurumu.Tamamlandi));
    }

    [Theory]
    [InlineData(GorevDurumu.Yeni)]
    [InlineData(GorevDurumu.Atandi)]
    [InlineData(GorevDurumu.Basladi)]
    [InlineData(GorevDurumu.DevamEdiyor)]
    [InlineData(GorevDurumu.Beklemede)]
    [InlineData(GorevDurumu.TamamlanmaBekliyor)]
    [InlineData(GorevDurumu.IadeEdildi)]
    [InlineData(GorevDurumu.Reddedildi)]
    [InlineData(GorevDurumu.Iptal)]
    [InlineData(GorevDurumu.Tamamlandi)]
    public void Her_durumda_oran_0_ile_100_arasinda(GorevDurumu durum)
    {
        foreach (var (toplam, biten) in new[] { (0, 0), (1, 0), (1, 1), (7, 3), (7, 7) })
        {
            var oran = GorevDurumAkisi.Ilerleme(durum, toplam, biten);
            Assert.InRange(oran, 0, 100);
        }
    }
}
