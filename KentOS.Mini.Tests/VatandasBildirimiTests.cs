using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Mini.Application.Dto.V2.IsTakip;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Services.V2;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// VATANDAŞ BİLDİRİMİ — uygulamanın tek anonim yazma yolu.
/// </summary>
/// <remarks>
/// <para>
/// Buradaki testler ötekilerden daha çok "güvenlik testi": kimliği
/// doğrulanmamış bir kaynak veritabanına satır yazıyor. Kilitlenen şeyler
/// doğrulama kodunun açık saklanmaması, biletin başka numaraya
/// geçmemesi, mükerrer yönlendirmenin engellenmesi ve numara başına sınır.
/// </para>
/// </remarks>
[Collection(SunucuKoleksiyonu.Ad)]
public class VatandasBildirimiTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;
    private readonly SahteMesajServisi _mesajlar = new();

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    static VatandasBildirimiTests()
    {
        // Bilet ve yükleme anahtarı JWT anahtarıyla imzalanıyor; testte de
        // bir kez kurulması gerekiyor.
        VatandasBildirimServisi.ImzaAnahtariniKur(
            "test-imza-anahtari-en-az-otuziki-karakter-uzunlugunda");
    }

    private (IVatandasBildirimServisi Servis, AppDbContext Baglam) Kur(long birimId = 1)
    {
        var baglam = _ortam.Baglam();
        var kullanici = new SahteKullaniciServisi(1, "test", birimId);

        var etkin = new EtkinBirim(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            kullanici,
            new BirimAgaci(_ortam.Baglam(), new MemoryCache(new MemoryCacheOptions())),
            new HerSeyeIzinli());

        var olaylar = new IsOlayServisi(baglam, kullanici, etkin, NullLogger<IsOlayServisi>.Instance);
        var ekler = new IsEkServisi(baglam, new TestServisFabrikasi.SahteDepo(), kullanici,
            NullLogger<IsEkServisi>.Instance);
        var yorumlar = new IsYorumServisi(baglam, kullanici);

        var gorevler = new GorevServisi(
            baglam, kullanici, etkin, olaylar, ekler, yorumlar,
            new EkipServisi(baglam, etkin), _mesajlar, NullLogger<GorevServisi>.Instance);

        var servis = new VatandasBildirimServisi(
            baglam, kullanici, ekler, olaylar, gorevler, _mesajlar,
            NullLogger<VatandasBildirimServisi>.Instance);

        return (servis, baglam);
    }

    private static string YeniTelefon() => "532" + Random.Shared.Next(1_000_000, 9_999_999);

    /// <summary>Kodu kuyruktan okur — SMS gerçekten gönderilmiyor.</summary>
    private string KuyruktakiKod(string telefon)
    {
        var mesaj = _mesajlar.TekKisiyeGidenMesajlar
            .Last(m => m.Jeton == VatandasBildirimServisi.TelefonSadelestir(telefon));

        return new string(mesaj.Icerik.Where(char.IsDigit).Take(6).ToArray());
    }

    private async Task<string> BiletAlAsync(IVatandasBildirimServisi servis, string telefon)
    {
        await servis.KodGonderAsync(telefon, "127.0.0.1");
        var sonuc = await servis.KodDogrulaAsync(telefon, KuyruktakiKod(telefon), "127.0.0.1");
        return sonuc.Bilet;
    }

    // ── telefon normalleştirme ─────────────────────────────────────────

    /// <summary>
    /// AYNI NUMARA HER BİÇİMDE AYNI.
    /// </summary>
    /// <remarks>
    /// Ham metinle eşleştirme yapılsaydı aynı kişi her yazım biçiminde
    /// yeniden hız sınırı hakkı kazanırdı.
    /// </remarks>
    [Theory]
    [InlineData("0532 123 45 67")]
    [InlineData("+90 532 123 45 67")]
    [InlineData("532-123-4567")]
    [InlineData("905321234567")]
    [InlineData("5321234567")]
    public void Telefon_her_bicimde_ayni_sadelesir(string girdi)
    {
        Assert.Equal("5321234567", VatandasBildirimServisi.TelefonSadelestir(girdi));
    }

    // ── doğrulama ──────────────────────────────────────────────────────

    /// <summary>
    /// KOD AÇIK SAKLANMAZ.
    /// </summary>
    /// <remarks>
    /// Veritabanına erişen biri bekleyen bütün kodları okuyabilseydi, telefon
    /// doğrulaması bir güvenlik önlemi değil bir formalite olurdu.
    /// </remarks>
    [Fact]
    public async Task Kod_veritabaninda_ACIK_saklanmaz()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (servis, baglam) = Kur();
        var telefon = YeniTelefon();

        await servis.KodGonderAsync(telefon, "127.0.0.1");

        var kod = KuyruktakiKod(telefon);
        var kayit = await baglam.TelefonDogrulamalari
            .OrderByDescending(d => d.Id)
            .FirstAsync();

        Assert.DoesNotContain(kod, kayit.KodKarmasi);
        Assert.Equal(64, kayit.KodKarmasi.Length);
    }

    /// <summary>Bir kod BİR KEZ kullanılır.</summary>
    [Fact]
    public async Task Kod_ikinci_kez_kullanilamaz()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (servis, _) = Kur();
        var telefon = YeniTelefon();

        await servis.KodGonderAsync(telefon, "127.0.0.1");
        var kod = KuyruktakiKod(telefon);

        await servis.KodDogrulaAsync(telefon, kod, "127.0.0.1");

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.KodDogrulaAsync(telefon, kod, "127.0.0.1"));
    }

    /// <summary>
    /// DENEME SINIRI. Altı haneli bir kod sınırsız denemede bulunur.
    /// </summary>
    [Fact]
    public async Task Cok_fazla_hatali_deneme_kodu_kapatir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (servis, _) = Kur();
        var telefon = YeniTelefon();

        await servis.KodGonderAsync(telefon, "127.0.0.1");
        var dogruKod = KuyruktakiKod(telefon);

        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                servis.KodDogrulaAsync(telefon, "000000", "127.0.0.1"));
        }

        // DOĞRU kod bile artık kabul edilmiyor: sayaç dolduktan sonra
        // kodun kendisi geçersiz.
        var hata = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.KodDogrulaAsync(telefon, dogruKod, "127.0.0.1"));

        Assert.Contains("hatalı deneme", hata.Message);
    }

    /// <summary>Bir dakika içinde ikinci kod istenemez — SMS bombardımanı.</summary>
    [Fact]
    public async Task Ust_uste_kod_istenemez()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (servis, _) = Kur();
        var telefon = YeniTelefon();

        await servis.KodGonderAsync(telefon, "127.0.0.1");

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.KodGonderAsync(telefon, "127.0.0.1"));
    }

    // ── bilet ──────────────────────────────────────────────────────────

    /// <summary>
    /// BAŞKA NUMARANIN BİLETİ KULLANILAMAZ.
    /// </summary>
    /// <remarks>
    /// Telefon biletin İÇİNDE imzalı; olmasaydı bir kez doğrulama yapan kişi
    /// aynı biletle istediği numara adına bildirim açardı.
    /// </remarks>
    [Fact]
    public async Task Baska_numaranin_bileti_kullanilamaz()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (servis, _) = Kur();
        var bilet = await BiletAlAsync(servis, YeniTelefon());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.BildirAsync(new VatandasBildirimiIstegiDto
            {
                AdSoyad = "Başkası",
                Telefon = YeniTelefon(),
                Bilet = bilet,
                Konu = "Deneme",
                Aciklama = "Deneme",
            }, "127.0.0.1"));
    }

    /// <summary>Uydurulmuş bilet reddedilir — imza denetimi.</summary>
    [Fact]
    public async Task Sahte_bilet_REDDEDILIR()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (servis, _) = Kur();
        var telefon = YeniTelefon();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.BildirAsync(new VatandasBildirimiIstegiDto
            {
                AdSoyad = "Sahteci",
                Telefon = telefon,
                // İmzasız ama biçimi doğru bir bilet.
                Bilet = $"{VatandasBildirimServisi.TelefonSadelestir(telefon)}|{DateTime.Now.AddHours(1).Ticks}.ABC",
                Konu = "Deneme",
                Aciklama = "Deneme",
            }, "127.0.0.1"));
    }

    // ── bildirim ───────────────────────────────────────────────────────

    [Fact]
    public async Task Bildirim_kaydedilir_ve_takip_numarasi_doner()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (servis, baglam) = Kur();
        var telefon = YeniTelefon();
        var bilet = await BiletAlAsync(servis, telefon);

        var sonuc = await servis.BildirAsync(new VatandasBildirimiIstegiDto
        {
            AdSoyad = "Ayşe Vatandaş",
            Telefon = telefon,
            Bilet = bilet,
            Konu = "Sokak lambası yanmıyor",
            Aciklama = "İki haftadır karanlık.",
            Enlem = 39.7480,
            Boylam = 37.0185,
        }, "127.0.0.1");

        Assert.StartsWith($"VB-{DateTime.Now.Year}-", sonuc.TakipNo);
        Assert.False(string.IsNullOrWhiteSpace(sonuc.YuklemeAnahtari));

        var kayit = await baglam.VatandasBildirimleri.FirstAsync(b => b.TakipNo == sonuc.TakipNo);
        Assert.Equal(VatandasBildirimDurumu.Yeni, kayit.Durum);
        Assert.Null(kayit.BirimId);
        Assert.Null(kayit.GorevId);
    }

    /// <summary>
    /// NUMARA BAŞINA SINIR — doğrulanmış numara bile sınırsız yazamaz.
    /// </summary>
    /// <remarks>
    /// Tek bir doğrulamayla yüzlerce bildirim açmak, karşılama ekranını
    /// kullanılamaz hâle getirirdi.
    /// </remarks>
    [Fact]
    public async Task Saatlik_bildirim_siniri_uygulanir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (servis, _) = Kur();
        var telefon = YeniTelefon();
        var bilet = await BiletAlAsync(servis, telefon);

        VatandasBildirimiIstegiDto Istek(int i) => new()
        {
            AdSoyad = "Ayşe Vatandaş",
            Telefon = telefon,
            Bilet = bilet,
            Konu = $"Bildirim {i}",
            Aciklama = "Deneme",
        };

        for (var i = 0; i < 3; i++) await servis.BildirAsync(Istek(i), "127.0.0.1");

        var hata = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.BildirAsync(Istek(4), "127.0.0.1"));

        Assert.Contains("çok sayıda bildirim", hata.Message);
    }

    // ── karşılama ──────────────────────────────────────────────────────

    /// <summary>
    /// YÖNLENDİRME GÖREV AÇAR ve bilgileri taşır.
    /// </summary>
    [Fact]
    public async Task Yonlendirme_gorev_acar_ve_bilgileri_tasir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (servis, baglam) = Kur();
        var telefon = YeniTelefon();
        var bilet = await BiletAlAsync(servis, telefon);

        var sonuc = await servis.BildirAsync(new VatandasBildirimiIstegiDto
        {
            AdSoyad = "Mehmet Vatandaş",
            Telefon = telefon,
            Bilet = bilet,
            Konu = "Çukur var",
            Aciklama = "Okul çıkışında.",
            Enlem = 39.7477,
            Boylam = 37.0179,
            Adres = "Atatürk Caddesi",
        }, "127.0.0.1");

        var kayit = await baglam.VatandasBildirimleri.AsNoTracking()
            .FirstAsync(b => b.TakipNo == sonuc.TakipNo);

        var sonrasi = await servis.YonlendirAsync(kayit.Id, new BildirimYonlendirmeDto
        {
            BirimId = 2,
            Oncelik = GorevOnceligi.Yuksek,
            Not = "Fen işlerine.",
        });

        Assert.Equal(VatandasBildirimDurumu.Yonlendirildi, sonrasi.Durum);
        Assert.NotNull(sonrasi.GorevId);

        var gorev = await baglam.Gorevler.AsNoTracking().FirstAsync(g => g.Id == sonrasi.GorevId);

        // Görev SEÇİLEN birimde — karşılama personelinin biriminde değil.
        Assert.Equal(2, gorev.BirimId);
        Assert.Equal(GorevKaynagi.Vatandas, gorev.Kaynak);
        Assert.Equal(kayit.Id, gorev.KaynakId);
        Assert.Equal(39.7477, gorev.Enlem);
        Assert.Equal(GorevOnceligi.Yuksek, gorev.Oncelik);

        // Vatandaşın iletişimi göreve taşınıyor: sahadaki personelin arayıp
        // yeri sorabilmesi gerekiyor.
        Assert.Contains("Mehmet Vatandaş", gorev.Aciklama!);
        Assert.Contains(kayit.TakipNo, gorev.Aciklama!);
    }

    /// <summary>
    /// BİR BİLDİRİM BİR KEZ YÖNLENDİRİLİR.
    /// </summary>
    /// <remarks>
    /// Aksi hâlde aynı şikayet için birden çok görev açılırdı.
    /// </remarks>
    [Fact]
    public async Task Ikinci_yonlendirme_REDDEDILIR()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (servis, baglam) = Kur();
        var telefon = YeniTelefon();
        var bilet = await BiletAlAsync(servis, telefon);

        var sonuc = await servis.BildirAsync(new VatandasBildirimiIstegiDto
        {
            AdSoyad = "Test", Telefon = telefon, Bilet = bilet,
            Konu = "Konu", Aciklama = "Açıklama",
        }, "127.0.0.1");

        var kayit = await baglam.VatandasBildirimleri.AsNoTracking()
            .FirstAsync(b => b.TakipNo == sonuc.TakipNo);

        await servis.YonlendirAsync(kayit.Id, new BildirimYonlendirmeDto { BirimId = 1 });

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.YonlendirAsync(kayit.Id, new BildirimYonlendirmeDto { BirimId = 2 }));

        // Reddetmek de artık mümkün değil — kayıt işlenmiş durumda.
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.ReddetAsync(kayit.Id, "Vazgeçtim"));
    }

    /// <summary>RET GEREKÇESİZ yapılamaz.</summary>
    [Fact]
    public async Task Ret_gerekcesiz_yapilamaz()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (servis, baglam) = Kur();
        var telefon = YeniTelefon();
        var bilet = await BiletAlAsync(servis, telefon);

        var sonuc = await servis.BildirAsync(new VatandasBildirimiIstegiDto
        {
            AdSoyad = "Test", Telefon = telefon, Bilet = bilet,
            Konu = "Konu", Aciklama = "Açıklama",
        }, "127.0.0.1");

        var kayit = await baglam.VatandasBildirimleri.AsNoTracking()
            .FirstAsync(b => b.TakipNo == sonuc.TakipNo);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servis.ReddetAsync(kayit.Id, "   "));

        var reddedildi = await servis.ReddetAsync(kayit.Id, "Mükerrer kayıt.");
        Assert.Equal(VatandasBildirimDurumu.Reddedildi, reddedildi.Durum);
        Assert.Null(reddedildi.GorevId);
    }

    /// <summary>
    /// MÜKERRER SAYACI — aynı numaradan gelen önceki kayıtlar.
    /// </summary>
    /// <remarks>
    /// Karşılama ekranının en sık işi mükerrer ayıklamak; sayı görünmeseydi
    /// personel aynı çukur için beş ayrı görev açardı.
    /// </remarks>
    [Fact]
    public async Task Mukerrer_sayaci_onceki_kayitlari_gosterir()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        var (servis, _) = Kur();
        var telefon = YeniTelefon();
        var bilet = await BiletAlAsync(servis, telefon);

        for (var i = 0; i < 3; i++)
        {
            await servis.BildirAsync(new VatandasBildirimiIstegiDto
            {
                AdSoyad = "Test", Telefon = telefon, Bilet = bilet,
                Konu = $"Konu {i}", Aciklama = "Açıklama",
            }, "127.0.0.1");
        }

        var liste = await servis.ListeAsync(
            new SayfaIstegi { Boyut = 50, Ara = telefon }, VatandasBildirimDurumu.Yeni);

        Assert.All(liste.Veriler, b => Assert.Equal(2, b.AyniNumaradanOnceki));
    }

    /// <summary>Bu testlerin konusu yetki değil AKIŞ; izin kapısı açık.</summary>
    private sealed class HerSeyeIzinli : IIzinServisi
    {
        public Task<IReadOnlySet<string>> IzinleriAsync(long kullaniciId) =>
            Task.FromResult<IReadOnlySet<string>>(Izinler.Adlar.ToHashSet());

        public Task<bool> VarMiAsync(long kullaniciId, string izin) => Task.FromResult(true);
        public void Dusur(long kullaniciId) { }
        public Task RolDegistiAsync(long rolId) => Task.CompletedTask;
    }
}
