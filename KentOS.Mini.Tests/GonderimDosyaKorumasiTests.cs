using Microsoft.AspNetCore.Http;
using KentOS.Mini.Web.Middleware;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// Gönderilen belgeler HTTP'den doğrudan indirilememeli.
///
/// <para>
/// Dosyalar <c>wwwroot/uploads/gonderim</c> altında duruyor — orası uygulama
/// havuzunun zaten yazabildiği yer, ayrı klasör her yayında elle izin
/// gerektirirdi. Karşılığında <c>wwwroot</c> altı kimlik doğrulanmadan servis
/// edildiği için bu ara katman zorunlu: kırılırsa <b>gizli belgeler adresi
/// bilen herkese açılır</b>.
/// </para>
/// </summary>
public class GonderimDosyaKorumasiTests
{
    [Theory]
    // Klasörün kendisi ve altındaki her şey kapalı.
    [InlineData("/uploads/gonderim")]
    [InlineData("/uploads/gonderim/")]
    [InlineData("/uploads/gonderim/9f2c1a44-1e2b.pdf")]
    [InlineData("/uploads/gonderim/alt/klasor/belge.docx")]
    // Windows dosya sistemi büyük/küçük harf duyarsız; adres de öyle olmalı.
    [InlineData("/UPLOADS/GONDERIM/belge.pdf")]
    [InlineData("/Uploads/Gonderim/belge.pdf")]
    public void Gonderim_klasoru_kapali(string yol)
    {
        Assert.True(GonderimDosyaKorumasi.Kapali(new PathString(yol)));
    }

    [Theory]
    // Diğer yüklemeler açık kalmalı — etkinlik fotoğrafları ve talep dosyaları.
    [InlineData("/uploads/ajanda/foto.jpg")]
    [InlineData("/uploads/talep/dilekce.pdf")]
    [InlineData("/uploads")]
    // Ön ek benzerliği yetmez: `StartsWith` kullanılsaydı bunlar da kapanırdı.
    [InlineData("/uploads/gonderimler-arsivi/rapor.pdf")]
    [InlineData("/uploads/gonderim-eski/rapor.pdf")]
    // API ucu ASLA kapanmamalı: indirmenin tek meşru yolu orası.
    [InlineData("/api/v2/gonderim/12/dosya")]
    [InlineData("/yeni/gonderim/12")]
    public void Diger_yollar_acik(string yol)
    {
        Assert.False(GonderimDosyaKorumasi.Kapali(new PathString(yol)));
    }

    /// <summary>
    /// Ara katman <c>UseStaticFiles</c>'tan ÖNCE eklenmiş olmalı.
    /// </summary>
    /// <remarks>
    /// Sıra bozulursa kural sessizce etkisiz kalır: statik dosya ara katmanı
    /// isteği önce yakalar, dosyayı verir ve koruma hiç çalışmaz. Derleme
    /// geçer, testlerin geri kalanı yeşil kalır. Bu yüzden sıra
    /// <c>Program.cs</c> metninden doğrulanıyor.
    /// </remarks>
    [Fact]
    public void Koruma_statik_dosyalardan_once_eklenmis()
    {
        var programYolu = Path.Combine(
            KokDizin(), "KentOS.Mini.Web", "Program.cs");

        Assert.True(File.Exists(programYolu), $"Program.cs bulunamadı: {programYolu}");

        var metin = File.ReadAllText(programYolu);
        var koruma = metin.IndexOf("UseGonderimDosyaKorumasi()", StringComparison.Ordinal);
        var statik = metin.IndexOf("app.UseStaticFiles()", StringComparison.Ordinal);

        Assert.True(koruma >= 0, "UseGonderimDosyaKorumasi() ardışık düzenden kaldırılmış.");
        Assert.True(statik >= 0, "app.UseStaticFiles() bulunamadı.");
        Assert.True(koruma < statik,
            "UseGonderimDosyaKorumasi() çağrısı UseStaticFiles()'tan SONRA kalmış — " +
            "gönderilen belgeler kimlik doğrulanmadan indirilebilir hâle gelir.");
    }

    /// <summary>Çözüm kök dizinini test derlemesinin konumundan yukarı çıkarak bulur.</summary>
    private static string KokDizin()
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);

        while (dizin is not null && !File.Exists(Path.Combine(dizin.FullName, "KentOS.Mini.sln")))
        {
            dizin = dizin.Parent;
        }

        return dizin?.FullName ?? throw new InvalidOperationException("Çözüm kökü bulunamadı.");
    }
}
