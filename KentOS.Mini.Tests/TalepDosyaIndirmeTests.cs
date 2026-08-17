using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Services.V2;
using KentOS.Mini.Web.Storage;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// Talep dosyasının KİMLİK DENETİMLİ indirilmesi.
///
/// <para>
/// GERÇEK DURUM: talep dosyaları <c>wwwroot/uploads</c> altında ve statik
/// dosya ara katmanıyla <b>kimlik doğrulanmadan</b> servis ediliyor. Mobil
/// uygulama da adresi elle kurup (<c>{kök}{dosya.path}</c>) işletim sistemine
/// devrediyordu — istek uygulamanın dışına çıktığı için jeton taşımıyor,
/// yalnızca klasörün açık olması sayesinde çalışıyordu. Özgeçmiş gibi kişisel
/// belgeler de aynı yoldan iniyordu.
/// </para>
///
/// <para>
/// Statik yol v1'in ve eski MVC arayüzünün bağlı olduğu davranış olduğu için
/// KAPATILMIYOR. Bunun yerine yeni istemcilerin kullanacağı, birim
/// süzgecinden geçen bir uç eklendi. Buradaki testler o süzgeci kilitler.
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class TalepDosyaIndirmeTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;

    // SunucuTestOrtami: 1/2/3 → birim 1, 4 → birim 2.
    private const long Birim1Kullanici = 1;
    private const long Birim2Kullanici = 4;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    private async Task TemizleAsync()
    {
        using var b = _ortam.Baglam();
        await b.Database.ExecuteSqlRawAsync(
            "TRUNCATE dosyalar, randevular RESTART IDENTITY CASCADE;");
        await _ortam.TemelVerileriKurAsync();
    }

    /// <summary>Servisi verilen kullanıcı kimliğiyle kurar.</summary>
    private (DosyaServisi servis, TestServisFabrikasi.SahteDepo depo) Servis(long kullaniciId, long birimId)
    {
        var depo = new TestServisFabrikasi.SahteDepo();

        // `IAjandaService` yalnızca FOTOĞRAF yükleme yolunda kullanılıyor;
        // indirme ona hiç dokunmuyor. Koca arayüzü sahtelemek yerine null
        // geçiliyor: yol bir gün oraya uğrarsa test gürültüyle düşsün.
        var servis = new DosyaServisi(
            _ortam.Baglam(),
            depo,
            new SahteKullaniciServisi(kullaniciId, $"k{kullaniciId}", birimId),
            null!,
            new SahteMesajServisi(),
            NullLogger<DosyaServisi>.Instance);

        return (servis, depo);
    }

    /// <summary>Verilen birimde bir talep ve ona bağlı bir dosya yazar.</summary>
    private async Task<long> DosyaYazAsync(
        TestServisFabrikasi.SahteDepo depo, long birimId, string icerik)
    {
        using var db = _ortam.Baglam();

        // `randevular` şemasında mahalle_id, randevu_durum_id, ajanda_durum ve
        // ozgecmis_durum NOT NULL — entity sınıfı isteğe bağlı ilan etse de
        // sütunlar zorunlu. Testin bu uyumsuzluğu bilmesi gerekiyor.
        var mahalleId = await db.Mahalleler.Select(m => m.Id).FirstOrDefaultAsync();
        if (mahalleId == 0)
        {
            var m = new Mahalle { Ad = "Test Mahallesi" };
            db.Mahalleler.Add(m);
            await db.SaveChangesAsync();
            mahalleId = m.Id;
        }

        var durumId = await db.RandevuDurumlar.Select(d => d.Id).FirstOrDefaultAsync();
        if (durumId == 0)
        {
            var d = new RandevuDurum { DurumAd = "Beklemede", Renk = "#ebcc34" };
            db.RandevuDurumlar.Add(d);
            await db.SaveChangesAsync();
            durumId = d.Id;
        }

        var talep = new Randevu
        {
            MahalleId = mahalleId,
            RandevuDurumId = durumId,
            Ad = "Test",
            Soyad = "Başvuran",
            Konu = "Dosyalı talep",
            BirimId = birimId,
            // Şemada NOT NULL: `Randevu` bunları isteğe bağlı ilan ediyor ama
            // sütunlar zorunlu — canlıdan gelen bir uyumsuzluk.
            BaslangicTarih = DateTime.Now,
            BitisTarih = DateTime.Now.AddMinutes(30),
            OlusturmaTarih = DateTime.Now,
        };
        db.Randevular.Add(talep);
        await db.SaveChangesAsync();

        // Dosya gerçekten depoda olmalı: servis var olmayan dosyada da
        // "bulunamadı" atıyor ve iki hatayı ayırt edemezdik.
        var gorecelYol = $"/uploads/randevu/{Guid.NewGuid():N}.pdf";
        await depo.SaveAsync(StorageArea.Public, gorecelYol,
            System.Text.Encoding.UTF8.GetBytes(icerik), "application/pdf");

        var dosya = new RandevuDosya
        {
            Ad = "ozgecmis.pdf",
            Path = gorecelYol,
            ContentType = "application/pdf",
            Size = icerik.Length,
            OlusturmaTarih = DateTime.Now,
            RandevuId = talep.Id,
        };
        db.Dosyalar.Add(dosya);
        await db.SaveChangesAsync();

        return dosya.Id;
    }

    [Fact]
    public async Task Kendi_biriminin_dosyasi_indirilebilir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, depo) = Servis(Birim1Kullanici, birimId: 1);
        var dosyaId = await DosyaYazAsync(depo, birimId: 1, icerik: "gizli ozgecmis");

        var (akis, ad, tur) = await servis.TalepDosyasiAsync(dosyaId);

        using var okuyucu = new StreamReader(akis);
        Assert.Equal("gizli ozgecmis", await okuyucu.ReadToEndAsync());
        Assert.Equal("ozgecmis.pdf", ad);
        Assert.Equal("application/pdf", tur);
    }

    [Fact]
    public async Task BASKA_birimin_dosyasi_INDIRILEMEZ()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        // Dosya 1. birimin talebine ait; indirmeye çalışan 2. birimde.
        var (yazan, depo) = Servis(Birim1Kullanici, birimId: 1);
        var dosyaId = await DosyaYazAsync(depo, birimId: 1, icerik: "baskasinin belgesi");
        Assert.NotNull(yazan);

        var (yabanci, _) = Servis(Birim2Kullanici, birimId: 2);

        // "Yetkin yok" değil "bulunamadı": başkasının dosyasının VAR olduğunu
        // doğrulamak bile bilgi sızdırır.
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => yabanci.TalepDosyasiAsync(dosyaId));
    }

    [Fact]
    public async Task Olmayan_dosya_bulunamadi_atar()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, _) = Servis(Birim1Kullanici, birimId: 1);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => servis.TalepDosyasiAsync(999_999));
    }

    [Fact]
    public async Task Kayit_var_ama_disk_bos_ise_bulunamadi_atar()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, depo) = Servis(Birim1Kullanici, birimId: 1);
        var dosyaId = await DosyaYazAsync(depo, birimId: 1, icerik: "silinecek");

        // Depodaki dosyayı sil, kayıt kalsın — yayın taşımalarında olan bu.
        using (var db = _ortam.Baglam())
        {
            var yol = await db.Dosyalar.Where(d => d.Id == dosyaId)
                .Select(d => d.Path).FirstAsync();
            await depo.DeleteAsync(StorageArea.Public, yol!);
        }

        // Akış açmaya çalışıp IOException fırlatmak yerine anlaşılır bir hata:
        // kullanıcıya "dosya sunucuda yok" demek gerekiyor.
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => servis.TalepDosyasiAsync(dosyaId));
    }

    [Fact]
    public async Task Arsivlenmis_talebin_dosyasi_da_inebilir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (servis, depo) = Servis(Birim1Kullanici, birimId: 1);
        var dosyaId = await DosyaYazAsync(depo, birimId: 1, icerik: "arsiv belgesi");

        using (var db = _ortam.Baglam())
        {
            await db.Randevular.IgnoreQueryFilters()
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Arsivlendi, true));
        }

        // `Randevu` üzerinde `!Arsivlendi` global süzgeci var; hesaba
        // katılmazsa arşive düşen her talebin dosyası ERİŞİLEMEZ olurdu.
        var (akis, _, _) = await servis.TalepDosyasiAsync(dosyaId);
        using var okuyucu = new StreamReader(akis);
        Assert.Equal("arsiv belgesi", await okuyucu.ReadToEndAsync());
    }
}
