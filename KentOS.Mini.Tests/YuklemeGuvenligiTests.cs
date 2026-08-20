using System.Reflection;
using Microsoft.AspNetCore.Http;
using KentOS.Mini.Web.Middleware;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// YÜKLENEN DOSYA TARAYICIDA ÇALIŞMAZ.
/// </summary>
/// <remarks>
/// <para>
/// Kapatılan açık depolanmış XSS'ti ve <b>ölçülerek</b> bulundu: talebe
/// <c>zararli.html</c> eklendi, dosya <c>/uploads/randevu/{guid}.html</c>
/// adresinden <b>jetonsuz</b>, <c>200</c> ve <c>Content-Type: text/html</c>
/// ile indi. Sayfa uygulamayla aynı kaynakta çalıştığı için
/// <c>localStorage</c>'daki jetona erişebiliyordu; jeton 15 saat geçerli ve
/// iptal listesi yok.
/// </para>
/// </remarks>
public class YuklemeGuvenligiTests
{
    [Theory]
    [InlineData("/uploads/randevu/a.html")]
    [InlineData("/uploads/ajanda/a.htm")]
    [InlineData("/uploads/a.svg")]     // <img> içinde zararsız, adrese gidilince script çalışır
    [InlineData("/uploads/a.xml")]     // XSLT
    [InlineData("/uploads/a.js")]
    [InlineData("/UPLOADS/A.HTML")]    // büyük/küçük harfle atlatılamaz
    [InlineData("/uploads/derin/klasor/a.xhtml")]
    public void Calisabilir_dosyalar_etkisizlestirilir(string yol)
        => Assert.True(YuklemeGuvenligi.Etkisizlestirilmeli(new PathString(yol)));

    [Theory]
    [InlineData("/uploads/randevu/a.pdf")]
    [InlineData("/uploads/ajanda/a.jpg")]
    [InlineData("/uploads/a.png")]
    [InlineData("/uploads/a.xlsx")]
    [InlineData("/uploads/a.docx")]
    public void Mesru_belgeler_dokunulmadan_gecer(string yol)
        => Assert.False(YuklemeGuvenligi.Etkisizlestirilmeli(new PathString(yol)));

    /// <summary>
    /// Kural YALNIZCA yükleme klasöründe geçerli.
    /// </summary>
    /// <remarks>
    /// Uygulamanın kendi varlıkları da <c>wwwroot</c> altında ve onlar
    /// gerçekten çalışmak zorunda: <c>/uygulama/index-abc.js</c> ile
    /// <c>/firebase-messaging-sw.js</c> etkisizleştirilseydi SPA hiç
    /// açılmaz, web push sessizce ölürdü.
    /// </remarks>
    [Theory]
    [InlineData("/uygulama/index-abc.js")]
    [InlineData("/firebase-messaging-sw.js")]
    [InlineData("/index.html")]
    [InlineData("/uploadsxyz/a.html")]  // StartsWith değil StartsWithSegments
    public void Yukleme_disindaki_yollar_etkilenmez(string yol)
        => Assert.False(YuklemeGuvenligi.Etkisizlestirilmeli(new PathString(yol)));

    /// <summary>
    /// SIRA: ara katman <c>UseStaticFiles</c>'tan ÖNCE bağlanmalı.
    /// </summary>
    /// <remarks>
    /// Sonra bağlanırsa statik dosya ara katmanı yanıtı çoktan yazmıştır ve
    /// kural <b>sessizce</b> etkisiz kalır — test yeşil, açık açık.
    /// </remarks>
    [Fact]
    public void Ara_katman_statik_dosyalardan_once_baglanir()
    {
        var kaynak = File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
            "KentOS.Mini.Web", "Program.cs"));

        var guvenlik = kaynak.IndexOf("UseYuklemeGuvenligi()", StringComparison.Ordinal);
        var statik = kaynak.IndexOf("UseStaticFiles()", StringComparison.Ordinal);

        Assert.True(guvenlik > 0, "UseYuklemeGuvenligi() Program.cs'te hiç çağrılmıyor.");
        Assert.True(statik > 0, "UseStaticFiles() bulunamadı.");
        Assert.True(guvenlik < statik,
            "UseYuklemeGuvenligi() UseStaticFiles()'tan SONRA çağrılıyor; kural etkisiz.");
    }

    /// <summary>
    /// Yükleme yolları da çalışabilir uzantıyı reddediyor.
    /// </summary>
    /// <remarks>
    /// Güvenlik sınırı ara katman; bu, kullanıcının dosyayı yükleyip
    /// indirilemez bir şey elde etmesini önlüyor. Kaynak taranıyor çünkü
    /// bu yollar <c>MultipartReader</c> akışı istiyor ve testte kurmak,
    /// denetimin varlığını doğrulamaktan pahalı.
    /// </remarks>
    [Theory]
    [InlineData("KentOS.Mini.Web/Services/AjandaService.cs")]
    [InlineData("KentOS.Mini.Web/Services/RandevuService.cs")]
    public void Yukleme_yollari_calisabilir_uzantiyi_reddeder(string dosya)
    {
        var kaynak = File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
            dosya.Replace('/', Path.DirectorySeparatorChar)));

        Assert.Contains("YuklemeGuvenligi.Calisabilir", kaynak);
    }
}
