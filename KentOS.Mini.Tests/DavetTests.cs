using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Services.V2;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// DAVET LİSTELERİ ve PROTOKOL KATEGORİLERİ.
///
/// <para>
/// Davet, protokolden seçilen kişilerin takibi. Kilitlenen kurallar:
/// birim izolasyonu, aynı kişinin iki kez eklenememesi, arama/mesaj
/// eylemlerinin cevaptan ayrı tutulması ve kategori adlarının tekilliği.
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class DavetTests : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam;

    private const long Birim1Kullanici = 1;
    private const string Birim1KullaniciAdi = "ekleyen";
    private const long Birim2Kullanici = 4;
    private const string Birim2KullaniciAdi = "digerbirim";

    public DavetTests(SunucuTestOrtami ortam) => _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
        {
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres kullanılamıyor");
        }
    }

    private async Task TemizleAsync()
    {
        using var b = _ortam.Baglam();
        await b.Database.ExecuteSqlRawAsync(
            "TRUNCATE davet_kisileri, davetler, protokoller, protokol_kategorileri RESTART IDENTITY CASCADE;");
        await _ortam.TemelVerileriKurAsync();
    }

    private DavetServisi Servis(Web.Data.AppDbContext b, long kullaniciId, string kadi, long birimId)
        => new(b, new SahteKullaniciServisi(kullaniciId, kadi, birimId));

    private ProtokolServisi ProtokolServisi(
        Web.Data.AppDbContext b, long kullaniciId = 1, string kadi = "ekleyen", long birimId = 1)
        => new(b, new SahteKullaniciServisi(kullaniciId, kadi, birimId));

    /// <summary>Bir kategori ve içinde iki protokol kaydı kurar.</summary>
    private async Task<(long kategoriId, long p1, long p2)> ProtokolKurAsync()
    {
        using var b = _ortam.Baglam();

        var k = new ProtokolKategori { Ad = "Mülki İdare", SiraNo = 1 };
        b.ProtokolKategorileri.Add(k);
        await b.SaveChangesAsync();

        var p1 = new Protokol { KategoriId = k.Id, AdSoyad = "Vali", SiraNo = 1, Aktif = true };
        var p2 = new Protokol { KategoriId = k.Id, AdSoyad = "Vali Yardımcısı", SiraNo = 2, Aktif = true };
        b.Protokoller.AddRange(p1, p2);
        await b.SaveChangesAsync();

        return (k.Id, p1.Id, p2.Id);
    }

    // ───────────────────────────────────────────── kategori

    [Fact]
    public async Task Ayni_kategori_iki_kez_acilamaz()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        using var b = _ortam.Baglam();
        var servis = ProtokolServisi(b);

        await servis.KategoriOlusturAsync(new ProtokolKategoriIstegi { Ad = "Mülki İdare" });

        // Büyük/küçük harf farkı da engellenmeli: kategoriyi tabloya almanın
        // sebebi tam olarak "Mülki İdare" / "Mülki idare" ikilemiydi.
        var hata = await Assert.ThrowsAsync<BusinessRuleException>(
            () => servis.KategoriOlusturAsync(new ProtokolKategoriIstegi { Ad = "mülki idare" }));

        Assert.Contains("zaten var", hata.Message);
    }

    [Fact]
    public async Task Kullanimdaki_kategori_silinemez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        var (kategoriId, _, _) = await ProtokolKurAsync();

        using var b = _ortam.Baglam();

        var hata = await Assert.ThrowsAsync<BusinessRuleException>(
            () => ProtokolServisi(b).KategoriSilAsync(kategoriId));

        Assert.Contains("kayıt var", hata.Message);
    }

    [Fact]
    public async Task Kategorisiz_protokol_kaydi_olusturulamaz()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        using var b = _ortam.Baglam();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => ProtokolServisi(b).OlusturAsync(new ProtokolIstegi
            {
                KategoriId = 9999,
                AdSoyad = "Hayalet",
            }));
    }

    // ───────────────────────────────────────────── davet

    [Fact]
    public async Task Kategorinin_tamami_tek_seferde_eklenir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        var (kategoriId, _, _) = await ProtokolKurAsync();

        using var b = _ortam.Baglam();
        var servis = Servis(b, Birim1Kullanici, Birim1KullaniciAdi, 1);

        var davet = await servis.OlusturAsync(new DavetIstegi { Baslik = "Resepsiyon" });
        var dolu = await servis.KisiEkleAsync(davet.Id, new DavetKisiEkleIstegi { KategoriId = kategoriId });

        Assert.Equal(2, dolu.KisiSayisi);
        Assert.Equal(2, dolu.Beklemede);
    }

    /// <summary>
    /// "Tümünü ekle" ikinci kez basıldığında düşmemeli.
    /// </summary>
    /// <remarks>
    /// Benzersizlik kısıtı var; zaten ekli olanlar atlanmazsa işlem hata
    /// fırlatır ve kullanıcı listeyi bozulmuş sanır.
    /// </remarks>
    [Fact]
    public async Task Ayni_kisi_iki_kez_eklenmez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        var (kategoriId, p1, _) = await ProtokolKurAsync();

        using var b = _ortam.Baglam();
        var servis = Servis(b, Birim1Kullanici, Birim1KullaniciAdi, 1);

        var davet = await servis.OlusturAsync(new DavetIstegi { Baslik = "Resepsiyon" });
        await servis.KisiEkleAsync(davet.Id, new DavetKisiEkleIstegi { KategoriId = kategoriId });

        // Aynı kategori + zaten ekli tek kişi: adet DEĞİŞMEMELİ.
        var ikinci = await servis.KisiEkleAsync(davet.Id, new DavetKisiEkleIstegi
        {
            KategoriId = kategoriId,
            ProtokolIdler = [p1],
        });

        Assert.Equal(2, ikinci.KisiSayisi);
    }

    /// <summary>
    /// Arama/mesaj EYLEMİ ile katılım CEVABI ayrı alanlar.
    /// </summary>
    /// <remarks>
    /// Tek enum'a sıkıştırılsaydı "arandı ama cevap yok" ile "hiç aranmadı"
    /// ayırt edilemezdi — listeyi takip edenin ilk sorusu tam olarak bu.
    /// </remarks>
    [Fact]
    public async Task Arandi_isareti_cevaptan_bagimsiz()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        var (kategoriId, _, _) = await ProtokolKurAsync();

        using var b = _ortam.Baglam();
        var servis = Servis(b, Birim1Kullanici, Birim1KullaniciAdi, 1);

        var davet = await servis.OlusturAsync(new DavetIstegi { Baslik = "Resepsiyon" });
        var dolu = await servis.KisiEkleAsync(davet.Id, new DavetKisiEkleIstegi { KategoriId = kategoriId });
        var kisiId = dolu.Kisiler[0].Id;

        // Yalnızca "arandı" — cevap hâlâ beklemede.
        var arandi = await servis.KisiGuncelleAsync(davet.Id, kisiId,
            new DavetKisiGuncelleIstegi { Arandi = true });

        Assert.True(arandi.Arandi);
        Assert.Equal(DavetDurumu.Beklemede, arandi.Durum);

        // Sonra cevap gelir.
        var cevapli = await servis.KisiGuncelleAsync(davet.Id, kisiId,
            new DavetKisiGuncelleIstegi { Durum = DavetDurumu.Katilacak, Not = "Eşiyle gelecek" });

        Assert.True(cevapli.Arandi);
        Assert.Equal(DavetDurumu.Katilacak, cevapli.Durum);
        Assert.Equal("Eşiyle gelecek", cevapli.Not);
    }

    [Fact]
    public async Task Arama_tarihi_ilk_isarette_damgalanir_ve_korunur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        var (kategoriId, _, _) = await ProtokolKurAsync();

        using var b = _ortam.Baglam();
        var servis = Servis(b, Birim1Kullanici, Birim1KullaniciAdi, 1);

        var davet = await servis.OlusturAsync(new DavetIstegi { Baslik = "Resepsiyon" });
        var dolu = await servis.KisiEkleAsync(davet.Id, new DavetKisiEkleIstegi { KategoriId = kategoriId });
        var kisiId = dolu.Kisiler[0].Id;

        await servis.KisiGuncelleAsync(davet.Id, kisiId, new DavetKisiGuncelleIstegi { Arandi = true });

        using var kontrol = _ortam.Baglam();
        var ilk = await kontrol.DavetKisileri.AsNoTracking().FirstAsync(k => k.Id == kisiId);
        Assert.NotNull(ilk.ArandiTarihi);

        // Başka bir alan güncellenince arama tarihi YENİLENMEMELİ.
        await servis.KisiGuncelleAsync(davet.Id, kisiId,
            new DavetKisiGuncelleIstegi { Arandi = true, Not = "ikinci kez" });

        using var kontrol2 = _ortam.Baglam();
        var sonra = await kontrol2.DavetKisileri.AsNoTracking().FirstAsync(k => k.Id == kisiId);
        Assert.Equal(ilk.ArandiTarihi, sonra.ArandiTarihi);
    }

    /// <summary>Davetler birime aittir; başka birim göremez.</summary>
    [Fact]
    public async Task Baska_birim_daveti_goremez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        using var b = _ortam.Baglam();

        var birim1 = Servis(b, Birim1Kullanici, Birim1KullaniciAdi, 1);
        var davet = await birim1.OlusturAsync(new DavetIstegi { Baslik = "Birim 1 daveti" });

        var birim2 = Servis(b, Birim2Kullanici, Birim2KullaniciAdi, 2);

        var liste = await birim2.ListeAsync(new DavetSuzgeci());
        Assert.Empty(liste.Veriler);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => birim2.DetayAsync(davet.Id));
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => birim2.KisiEkleAsync(davet.Id, new DavetKisiEkleIstegi { ProtokolIdler = [1] }));
        await Assert.ThrowsAsync<EntityNotFoundException>(() => birim2.SilAsync(davet.Id));
    }

    [Fact]
    public async Task Davet_silinince_kisileri_de_gider_protokol_kalir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        var (kategoriId, _, _) = await ProtokolKurAsync();

        using var b = _ortam.Baglam();
        var servis = Servis(b, Birim1Kullanici, Birim1KullaniciAdi, 1);

        var davet = await servis.OlusturAsync(new DavetIstegi { Baslik = "Resepsiyon" });
        await servis.KisiEkleAsync(davet.Id, new DavetKisiEkleIstegi { KategoriId = kategoriId });

        await servis.SilAsync(davet.Id);

        using var kontrol = _ortam.Baglam();
        Assert.Empty(await kontrol.DavetKisileri.Where(k => k.DavetId == davet.Id).ToListAsync());

        // Protokol listesi ETKİLENMEZ: davet geçici, protokol kalıcı kayıt.
        Assert.Equal(2, await kontrol.Protokoller.CountAsync());
    }

    /// <summary>PDF çıktısı dört türde de üretilebilmeli.</summary>
    [Theory]
    [InlineData(DavetCiktiTuru.Durumlu)]
    [InlineData(DavetCiktiTuru.Telefonlu)]
    [InlineData(DavetCiktiTuru.BosKatilim)]
    [InlineData(DavetCiktiTuru.BosProtokol)]
    public async Task Pdf_ciktisi_uretilir(DavetCiktiTuru tur)
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        var (kategoriId, _, _) = await ProtokolKurAsync();

        using var b = _ortam.Baglam();
        var servis = Servis(b, Birim1Kullanici, Birim1KullaniciAdi, 1);

        var davet = await servis.OlusturAsync(new DavetIstegi
        {
            Baslik = "Cumhuriyet Bayramı",
            Tarih = new DateTime(2026, 10, 29, 19, 0, 0),
            Yer = "Kültür Merkezi",
        });
        await servis.KisiEkleAsync(davet.Id, new DavetKisiEkleIstegi { KategoriId = kategoriId });

        var kurum = new SahteKurumServisi("Örnek Belediyesi");
        var (icerik, ad) = await new DavetCiktiServisi(
            servis, new SahteKullaniciServisi(1, "ekleyen", 1), kurum).PdfAsync(davet.Id, tur);

        // ÇIKTI KURUM KAYDINI OKUDU MU? Ad koda yazılıyken de PDF üretiliyor
        // ve test yeşil geçiyordu; arıza tam olarak orada saklanmıştı.
        Assert.True(kurum.OkumaSayisi > 0, "PDF kurum adını kurum kaydından okumadı.");

        Assert.NotEmpty(icerik);
        Assert.EndsWith(".pdf", ad);
        // PDF imzası: dosyanın gerçekten PDF olduğunu doğrular.
        Assert.Equal("%PDF"u8.ToArray(), icerik.Take(4).ToArray());
    }

    // ═══════════════════════════════════ protokol kişisinin davet geçmişi

    /// <summary>
    /// Kişinin geçmişi: hangi törene çağrıldı, ne cevap verdi, ne not düşüldü.
    /// </summary>
    /// <remarks>
    /// Telefonu elinde tutan kişi aramadan önce geçen sefer ne olduğunu bilmek
    /// istiyor; "geçen tören için de aramıştık, gelemedi" bilgisi konuşmanın
    /// tonunu belirliyor.
    /// </remarks>
    [Fact]
    public async Task Protokol_kisisinin_DAVET_GECMISI_okunur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        var (kategoriId, p1, _) = await ProtokolKurAsync();

        using var b = _ortam.Baglam();
        var davetler = Servis(b, 1, "ekleyen", 1);

        var eski = await davetler.OlusturAsync(new DavetIstegi
        {
            Baslik = "Geçen yılki tören",
            Tarih = new DateTime(2025, 5, 19),
        });
        var yeni = await davetler.OlusturAsync(new DavetIstegi
        {
            Baslik = "Açılış resepsiyonu",
            Tarih = new DateTime(2026, 10, 29),
        });

        foreach (var d in new[] { eski, yeni })
        {
            await davetler.KisiEkleAsync(d.Id, new DavetKisiEkleIstegi { ProtokolIdler = [p1] });
        }

        // Eski davette gelmemiş, notu var.
        var eskiDetay = await davetler.DetayAsync(eski.Id);
        await davetler.KisiGuncelleAsync(eski.Id, eskiDetay.Kisiler[0].Id, new DavetKisiGuncelleIstegi
        {
            Durum = DavetDurumu.Katilmayacak,
            Arandi = true,
            Not = "Yurt dışındaydı",
        });

        var gecmis = await ProtokolServisi(b).DavetGecmisiAsync(p1);

        Assert.Equal(2, gecmis.Count);
        // EN YENİ ÖNCE: "geçen sefer ne olmuştu" sorusunun cevabı en üstte.
        Assert.Equal("Açılış resepsiyonu", gecmis[0].Baslik);
        Assert.Equal("Geçen yılki tören", gecmis[1].Baslik);

        // Eylem (arandı) ile cevap (katılmayacak) AYRI alanlar; tek enuma
        // sıkıştırılsaydı "arandı ama cevap yok" ayırt edilemezdi.
        Assert.True(gecmis[1].Arandi);
        Assert.Equal("Katılmayacak", gecmis[1].DurumAd);
        Assert.Equal("Yurt dışındaydı", gecmis[1].Not);

        // Durum etiketi SUNUCUDA üretilir; web ve mobilde iki kez kurmak
        // birinin eksik kalması demekti.
        Assert.Equal("Beklemede", gecmis[0].DurumAd);
    }

    [Fact]
    public async Task Davet_gecmisi_BASKA_birimin_davetini_gostermez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();
        var (_, p1, _) = await ProtokolKurAsync();

        using var b = _ortam.Baglam();

        // 2. birimin daveti — protokol kaydı kurum geneli ama davet listesi
        // birime ait; başka birimin kime ne sorduğu görünmemeli.
        var digerBirim = Servis(b, 4, "digerbirim", 2);
        var d = await digerBirim.OlusturAsync(new DavetIstegi { Baslik = "Başka birimin daveti" });
        await digerBirim.KisiEkleAsync(d.Id, new DavetKisiEkleIstegi { ProtokolIdler = [p1] });

        var gecmis = await ProtokolServisi(b, 1, "ekleyen", 1).DavetGecmisiAsync(p1);

        Assert.Empty(gecmis);
    }
}
