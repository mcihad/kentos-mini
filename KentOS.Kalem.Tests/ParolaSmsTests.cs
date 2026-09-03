using System.Text.RegularExpressions;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// SIFIRLANAN PAROLA VERİTABANINDA DÜZ METİN KALMAZ.
/// </summary>
/// <remarks>
/// <para>
/// Yönetici bir parolayı sıfırladığında yeni parola SMS gövdesinde gidiyor ve
/// o gövde <c>messages</c> tablosuna yazılıyor. <c>FirebaseWorker</c>
/// gönderimden sonra yalnızca <c>IsSuccess</c> işaretleyip satırı bırakıyor
/// ve hiçbir yer de silmiyordu: <b>sıfırlanan her parola veritabanında düz
/// metin olarak süresiz duruyordu.</b> <c>sistem_hatalari</c> tablosunda
/// <c>parola</c> alanı maskeleniyor; burada maskesiz kalıyordu.
/// </para>
/// <para>
/// <b>Kaynak taranarak</b> doğrulanıyor: zincir üç dosyada (istek işaretler →
/// worker temizler → alan modelde tanımlı) ve kopan halka SESSİZ — mesaj yine
/// gider, yalnızca gövdesi tabloda kalır. Davranışı sınamak arka plan
/// servisini ve gerçek SMS sağlayıcısını çalıştırmayı gerektirirdi.
/// </para>
/// </remarks>
public class ParolaSmsTests
{
    private static string Oku(string gorecelYol)
    {
        var kok = KaynakKoku();
        return File.ReadAllText(Path.Combine(kok, gorecelYol));
    }

    /// <summary>Depo kökünü test derlemesinin konumundan yukarı çıkarak bulur.</summary>
    private static string KaynakKoku()
    {
        var dizin = AppContext.BaseDirectory;

        while (dizin is not null && !Directory.Exists(Path.Combine(dizin, "KentOS.Kalem.Web")))
        {
            dizin = Directory.GetParent(dizin)?.FullName;
        }

        return dizin ?? throw new DirectoryNotFoundException("Depo kökü bulunamadı.");
    }

    [Fact]
    public void Parola_smsi_hassas_isaretiyle_gonderilir()
    {
        var kaynak = Oku(Path.Combine("KentOS.Kalem.Web", "Services", "V2", "YonetimServisi.cs"));

        // Parola sıfırlama metodundaki SMS çağrısı `hassas: true` taşımalı.
        var metot = Regex.Match(
            kaynak,
            @"public async Task ParolaSifirlaAsync.*?\n    \}",
            RegexOptions.Singleline).Value;

        Assert.False(string.IsNullOrEmpty(metot), "ParolaSifirlaAsync bulunamadı.");
        Assert.Contains("hassas: true", metot);
    }

    [Fact]
    public void Worker_hassas_icerigi_temizler()
    {
        var kaynak = Oku(Path.Combine("KentOS.Kalem.Web", "Services", "FirebaseWorker.cs"));

        Assert.Contains("HassasIcerigiTemizle", kaynak);

        // BAŞARIDA da DENEME TÜKENİNCE de çağrılmalı: yalnızca başarıda
        // yapılsaydı gönderilemeyen bir parola tabloda kalırdı — ve
        // gönderilemeyen mesaj tam da en uzun duran mesajdır.
        var cagriSayisi = Regex.Matches(kaynak, @"HassasIcerigiTemizle\(message\)").Count;
        Assert.True(cagriSayisi >= 2, $"En az iki çağrı bekleniyordu, {cagriSayisi} bulundu.");
    }

    [Fact]
    public void Hassas_alani_modelde_tanimli()
    {
        var kaynak = Oku(Path.Combine("KentOS.Kalem.Application", "Models", "Message.cs"));

        Assert.Contains("[Column(\"hassas\")]", kaynak);
        Assert.Contains("public bool Hassas", kaynak);
    }
}
