using System.Text.RegularExpressions;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// Çiçek akışının SESSİZ kopma noktalarını kilitler.
/// </summary>
/// <remarks>
/// Akışın iki ucu da sessizce kırılmıştı ve ikisi de istisna atmıyordu:
/// çiçekçiye giden bağlantı 401 dönüyordu (bkz. <see cref="AnonimUcTests"/>),
/// etkinlik detayı ise <c>cicek</c> alanını hep <c>null</c> döndürüyordu.
/// İkincisi özellikle sinsi: yanıt 200, alan var, değeri boş.
/// </remarks>
public class CicekAkisTests
{
    private static string Kaynak(string göreliYol)
    {
        var kok = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine(kok, göreliYol));
    }

    /// <summary>
    /// Etkinlik DETAYI çiçeği de yükler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Liste ucu (<c>GetAllAsync</c>) <c>Cicek</c>'i Include ediyordu, detay
    /// etmiyordu. Yanıtta <c>cicekId</c> dolu, <c>cicek</c> null geliyordu:
    /// istemci "talimat verilmiş mi" biliyor ama "çiçek gitti mi" bilmiyordu
    /// ve etkinlik detayındaki rozet, çiçek teslim edilmiş olsa bile
    /// sonsuza kadar "bekliyor" gösteriyordu.
    /// </para>
    /// <para>
    /// Kaynak taranıyor çünkü hata DAVRANIŞTA sessiz: eksik <c>Include</c>
    /// istisna atmaz, sorgu çalışır, alan boş kalır.
    /// </para>
    /// </remarks>
    [Fact]
    public void Etkinlik_detayi_cicegi_de_yukler()
    {
        var kaynak = Kaynak("KentOS.Kalem.Web/Services/AjandaService.cs");

        var detay = Regex.Match(
            kaynak,
            @"public\s+async\s+Task<AjandaDto>\s+GetAsync\s*\(\s*long\s+id\s*\)(?<govde>[\s\S]*?)\n        \}");

        Assert.True(detay.Success, "AjandaService.GetAsync(long) bulunamadı — imza değişmiş olabilir.");

        var govde = detay.Groups["govde"].Value;

        Assert.Contains("Include(a => a.Cicek)", govde);
        Assert.Contains("Include(a => a.Photos)", govde);
    }

    /// <summary>
    /// Çiçekçiye giden SMS, uygulamanın kendi adresini kullanır.
    /// </summary>
    /// <remarks>
    /// Bağlantı bir dönem kurum alan adına elle yazılmıştı ve karşılığı olan
    /// MVC sayfası kaldırılınca çiçekçinin eline ölü bir adres gidiyordu.
    /// Adres artık <c>App:BaseUrl</c>'den geliyor — beyaz etiket sözleşmesi
    /// de bunu gerektiriyor: kurum adı koda yazılmaz.
    /// </remarks>
    [Fact]
    public void Cicek_smsindeki_adres_koda_yazilmaz()
    {
        var kaynak = Kaynak("KentOS.Kalem.Web/Services/AjandaService.cs");

        var sms = Regex.Match(kaynak, @"cicek-teslim/\{[^}]+\}");
        Assert.True(sms.Success, "SMS bağlantısı /cicek-teslim/{guid} kalıbında değil.");

        Assert.DoesNotMatch(new Regex(@"https?://[a-z0-9.-]*\.(bel\.tr|gov\.tr)"), kaynak);
    }
}
