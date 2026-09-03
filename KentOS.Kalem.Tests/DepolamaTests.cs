using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using KentOS.Kalem.Web.Options;
using KentOS.Kalem.Web.Storage;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// DOSYA DEPOSU sözleşmesi.
///
/// <para>
/// Yerel disk ve nesne deposu aynı arayüzün arkasında. Buradaki testler
/// <b>yerel</b> uygulamayı ve ortak anahtar kurallarını denetler; S3 tarafı
/// canlı bir MinIO gerektirdiği için ayrı doğrulanıyor (bkz. CLAUDE.md).
/// </para>
///
/// <para>
/// En kritik kural <see cref="StorageKey"/>: anahtar veritabanından geliyor
/// ve dizin dışına çıkma denemesi diskteki her dosyayı okunabilir kılardı.
/// </para>
/// </summary>
public class DepolamaTests
{
    private static (LocalFileStorage depo, string kok) Kur()
    {
        var kok = Path.Combine(Path.GetTempPath(), "wc-depo-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(kok);

        var ortam = new TestServisFabrikasi.SahteOrtam { WebRootPath = kok, ContentRootPath = kok };
        var ayar = Options.Create(new StorageOptions { UploadPath = "uploads" });

        return (new LocalFileStorage(ortam, ayar, NullLogger<LocalFileStorage>.Instance), kok);
    }

    // ─────────────────────────────────────────────── anahtar temizliği

    [Theory]
    [InlineData("/uploads/ajanda/a.jpg", "uploads/ajanda/a.jpg")]
    [InlineData("uploads/ajanda/a.jpg", "uploads/ajanda/a.jpg")]
    [InlineData("uploads\\ajanda\\a.jpg", "uploads/ajanda/a.jpg")]
    public void Anahtar_bastaki_egik_cizgi_ve_ters_boluden_arindirilir(string girdi, string beklenen)
    {
        Assert.Equal(beklenen, StorageKey.Normalize(girdi));
    }

    /// <summary>
    /// DİZİN DIŞINA ÇIKMA REDDEDİLİR.
    /// </summary>
    /// <remarks>
    /// Talep dosyasının yolu veritabanından okunuyor ve o tabloya yıllar içinde
    /// elle yazılmış kayıtlar da düşmüş olabilir. <c>..</c> içeren bir yol,
    /// indirme ucunu sunucudaki herhangi bir dosyayı okumaya çevirirdi.
    /// </remarks>
    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("uploads/../../gizli.txt")]
    [InlineData("/uploads/ajanda/../../../appsettings.json")]
    public void Dizin_disina_cikan_anahtar_reddedilir(string anahtar)
    {
        Assert.Throws<ArgumentException>(() => StorageKey.Normalize(anahtar));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_anahtar_reddedilir(string anahtar)
    {
        Assert.Throws<ArgumentException>(() => StorageKey.Normalize(anahtar));
    }

    // ──────────────────────────────────────────────── yerel depo turu

    [Fact]
    public async Task Yaz_oku_sil_turu_calisir()
    {
        var (depo, kok) = Kur();
        try
        {
            var icerik = "merhaba"u8.ToArray();

            await depo.SaveAsync(StorageArea.Public, "uploads/ajanda/a.txt", icerik, "text/plain");

            Assert.True(await depo.ExistsAsync(StorageArea.Public, "uploads/ajanda/a.txt"));
            Assert.Equal(icerik, await depo.ReadAllBytesAsync(StorageArea.Public, "uploads/ajanda/a.txt"));

            // Diskteki yer DEĞİŞMEDİ: veritabanındaki yollar geçerliliğini korumalı.
            Assert.True(File.Exists(Path.Combine(kok, "uploads", "ajanda", "a.txt")));

            await depo.DeleteAsync(StorageArea.Public, "uploads/ajanda/a.txt");
            Assert.False(await depo.ExistsAsync(StorageArea.Public, "uploads/ajanda/a.txt"));
        }
        finally
        {
            Directory.Delete(kok, recursive: true);
        }
    }

    /// <summary>
    /// Olmayan dosya <c>null</c> döner, FIRLATMAZ.
    /// </summary>
    /// <remarks>
    /// "Dosya yok" durumunu çağıran taraf kendi diline (404, iş kuralı hatası)
    /// çeviriyor; depo katmanının istisna atması o çeviriyi imkânsız kılardı.
    /// </remarks>
    [Fact]
    public async Task Olmayan_dosya_null_doner()
    {
        var (depo, kok) = Kur();
        try
        {
            Assert.Null(await depo.OpenReadAsync(StorageArea.Public, "uploads/yok.txt"));
            Assert.Null(await depo.ReadAllBytesAsync(StorageArea.Public, "uploads/yok.txt"));
            Assert.False(await depo.ExistsAsync(StorageArea.Public, "uploads/yok.txt"));
        }
        finally
        {
            Directory.Delete(kok, recursive: true);
        }
    }

    /// <summary>Olmayan dosyayı silmek sessiz geçer — kayıt silinebilmeli.</summary>
    [Fact]
    public async Task Olmayan_dosyayi_silmek_hata_vermez()
    {
        var (depo, kok) = Kur();
        try
        {
            await depo.DeleteAsync(StorageArea.Public, "uploads/yok.txt");
        }
        finally
        {
            Directory.Delete(kok, recursive: true);
        }
    }

    /// <summary>
    /// GİZLİ ALAN AYRI: gönderilen belgeler genel alanla aynı yerde durmamalı.
    /// </summary>
    [Fact]
    public async Task Ozel_alan_genel_alandan_ayridir()
    {
        var (depo, kok) = Kur();
        try
        {
            await depo.SaveAsync(StorageArea.Private, "belge.txt", "gizli"u8.ToArray(), "text/plain");

            // Aynı ADLA genel alanda aranınca bulunmamalı.
            Assert.False(await depo.ExistsAsync(StorageArea.Public, "belge.txt"));
            Assert.True(await depo.ExistsAsync(StorageArea.Private, "belge.txt"));
        }
        finally
        {
            Directory.Delete(kok, recursive: true);
        }
    }

    // ──────────────────────────────────────────── gönderim dizini çözümü

    /// <summary>
    /// Ayar verilmemişse gönderim klasörü <c>wwwroot/uploads/gonderim</c>.
    /// Bu bir KURULUM kararı: o klasöre yazma izni yayında zaten verilmiş.
    /// </summary>
    [Fact]
    public void Gonderim_dizini_varsayilani_wwwroot_altindadir()
    {
        var ortam = new TestServisFabrikasi.SahteOrtam { WebRootPath = "/tmp/kok" };
        var yol = LocalFileStorage.ResolvePrivateRoot(ortam, new StorageOptions());

        Assert.Equal(Path.Combine("/tmp/kok", "uploads", "gonderim"), yol);
    }

    /// <summary>Ayar verilmişse o yol kullanılır — belgeler yayın klasörü dışında durabilsin.</summary>
    [Fact]
    public void Gonderim_dizini_ayardan_okunur()
    {
        var ortam = new TestServisFabrikasi.SahteOrtam { WebRootPath = "/tmp/kok" };
        var yol = LocalFileStorage.ResolvePrivateRoot(
            ortam, new StorageOptions { SendDirectory = "/veri/gonderim" });

        Assert.Equal("/veri/gonderim", yol);
    }

    // ───────────────────────────────────────────────── S3 ayar denetimi

    /// <summary>
    /// EKSİK S3 AYARIYLA UYGULAMA AÇILMAZ.
    /// </summary>
    /// <remarks>
    /// Sessizce yerele düşmek çok daha kötü olurdu: yükleme çalışmaya devam
    /// eder, dosyalar beklenen yere gitmez ve kimse fark etmez.
    /// </remarks>
    [Fact]
    public void Eksik_S3_ayariyla_kayit_hata_verir()
    {
        var yapilandirma = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "S3",
                ["Storage:S3:Endpoint"] = "127.0.0.1:9000",
                // Anahtarlar bilerek EKSİK.
            })
            .Build();

        var hizmetler = new ServiceCollection();
        var hata = Assert.Throws<InvalidOperationException>(
            () => StorageRegistration.AddFileStorage(hizmetler, yapilandirma));

        Assert.Contains("STORAGE__S3__ACCESSKEY", hata.Message);
    }

    /// <summary>Uç noktadaki şema temizlenir — MinIO istemcisi şema kabul etmiyor.</summary>
    [Theory]
    [InlineData("https://s3.ornek.test", "s3.ornek.test")]
    [InlineData("http://127.0.0.1:9000/", "127.0.0.1:9000")]
    [InlineData("127.0.0.1:9000", "127.0.0.1:9000")]
    public void S3_uc_noktasi_semadan_arindirilir(string girdi, string beklenen)
    {
        Assert.Equal(beklenen, new S3StorageOptions { Endpoint = girdi }.NormalizedEndpoint);
    }
}
