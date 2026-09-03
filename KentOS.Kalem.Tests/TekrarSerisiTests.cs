using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Exceptions;
using KentOS.Kalem.Web.Services;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// Tekrarlanan etkinlik (RRULE serisi) DAVRANIŞ testleri — gerçek Postgres ve
/// gerçek <see cref="AppDbContext"/> üzerinde.
///
/// Burada kilitlenen sözleşme: kapsamlar (yalnızca bu / bundan sonrakiler / tümü),
/// notların yalnızca eklendiği tekrara ait kalması, ufuk uzatmanın tekrar
/// üretmemesi (idempotent) ve üretim tavanı.
/// </summary>
[Collection("SeriPostgres")]
public class TekrarSerisiTests : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam;

    public TekrarSerisiTests(SunucuTestOrtami ortam) => _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
        {
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres kullanılamıyor");
        }
    }

    private const long Birim = 1;
    private const long KullaniciNo = 1;
    private const string Kullanici = "seri_test";

    /// <summary>Her test kendi verisiyle başlasın: ajanda/seri/katılımcı tablolarını boşalt.</summary>
    private async Task TemizleAsync()
    {
        using var b = _ortam.Baglam();
        await b.Database.ExecuteSqlRawAsync(
            "TRUNCATE ajanda_katilimcilar, ajanda_notlar, ajanda_olaylar, ajandalar, ajanda_seriler RESTART IDENTITY CASCADE;");
        await _ortam.TemelVerileriKurAsync();
    }

    private static AjandaDto Sablon(string rrule, DateTime baslangic, int sureDakika = 60, string baslik = "Meclis Toplantısı")
        => new()
        {
            Baslik = baslik,
            Aciklama = "Test",
            BaslangicTarihi = baslangic,
            BitisTarihi = baslangic.AddMinutes(sureDakika),
            RandevuTipId = 1,   // ajandalar.randevu_tip_id NOT NULL
            DurumId = 1,        // ajandalar.durum_id NOT NULL
            Tekrar = new AjandaSeriOlusturDto { Rrule = rrule, SureDakika = sureDakika }
        };

    private (AjandaService ajanda, AjandaSeriService seri, SahteMesajServisi mesaj, AppDbContext baglam) Kur(
        long kullaniciId = KullaniciNo, string kullaniciAdi = Kullanici)
    {
        var baglam = _ortam.Baglam();
        var kullanici = new SahteKullaniciServisi(kullaniciId, kullaniciAdi, Birim);
        var (ajanda, seri, mesaj) = TestServisFabrikasi.Kur(baglam, kullanici, _ortam.Mapper);
        return (ajanda, seri, mesaj, baglam);
    }

    // ===================================================================
    //  OLUŞTURMA
    // ===================================================================
    [Fact]
    public async Task Seri_Olusturma_Tekrarlari_Gercek_Satir_Olarak_Uretir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, mesaj, baglam) = Kur();
        using (baglam)
        {
            var sonuc = await ajandaServisi.CreateAsync(
                Sablon("FREQ=WEEKLY;BYDAY=TU;COUNT=5", new DateTime(2026, 9, 1, 14, 0, 0)));

            Assert.NotNull(sonuc.SeriId);
            Assert.Equal("FREQ=WEEKLY;BYDAY=TU;COUNT=5", sonuc.TekrarKurali);
            Assert.Equal("Her hafta Salı, 5 tekrar", sonuc.TekrarOzeti);

            var tekrarlar = await baglam.Ajandalar
                .Where(a => a.SeriId == sonuc.SeriId)
                .OrderBy(a => a.BaslangicTarihi)
                .ToListAsync();

            Assert.Equal(5, tekrarlar.Count);
            Assert.All(tekrarlar, t => Assert.True(t.TekrarEden));
            Assert.All(tekrarlar, t => Assert.Equal(new TimeSpan(14, 0, 0), t.BaslangicTarihi.TimeOfDay));
            Assert.All(tekrarlar, t => Assert.Equal(60, (int)(t.BitisTarihi!.Value - t.BaslangicTarihi).TotalMinutes));
            Assert.Equal(new DateTime(2026, 9, 1, 14, 0, 0), tekrarlar[0].BaslangicTarihi);
            Assert.Equal(new DateTime(2026, 9, 29, 14, 0, 0), tekrarlar[4].BaslangicTarihi);

            // Her tekrarın kendi orijinal başlangıcı (RECURRENCE-ID) kaydedilmiş olmalı.
            Assert.All(tekrarlar, t => Assert.Equal(t.BaslangicTarihi, t.SeriOrijinalBaslangic));

            // Seri için TEK bildirim gider — 5 tekrar için 5 bildirim değil.
            Assert.Single(mesaj.BirimeGidenler);
            Assert.Contains("Tekrarlanan", mesaj.BirimeGidenler[0].Baslik);
        }
    }

    [Fact]
    public async Task Seri_Olusturma_Gecersiz_Kurali_Reddeder()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, _, baglam) = Kur();
        using (baglam)
        {
            var hata = await Assert.ThrowsAsync<BusinessRuleException>(() =>
                ajandaServisi.CreateAsync(Sablon("FREQ=HOURLY", new DateTime(2026, 9, 1, 14, 0, 0))));

            Assert.Contains("geçersiz", hata.Message);
            Assert.Empty(await baglam.Ajandalar.ToListAsync());
        }
    }

    [Fact]
    public async Task Seri_Olusturma_Ufuk_Ve_Tavan_Uygular()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, seriServisi, _, baglam) = Kur();
        using (baglam)
        {
            // Sınırsız günlük kural: 200 tavanı uygulanmalı.
            var sonuc = await ajandaServisi.CreateAsync(
                Sablon("FREQ=DAILY", DateTime.Now.Date.AddDays(1).AddHours(9)));

            var adet = await baglam.Ajandalar.CountAsync(a => a.SeriId == sonuc.SeriId);
            Assert.Equal(AjandaSeriService.EnFazlaTekrar, adet);

            var seri = await seriServisi.GetirAsync(sonuc.Id);
            Assert.NotNull(seri);
            Assert.Equal(AjandaSeriService.EnFazlaTekrar, seri!.UretilenAdet);
        }
    }

    // ===================================================================
    //  KAPSAM: YALNIZCA BU
    // ===================================================================
    [Fact]
    public async Task Yalnizca_Bu_Kapsami_Digerlerine_Dokunmaz_Ve_Tekrari_Ayirir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, _, baglam) = Kur();
        using (baglam)
        {
            var ilk = await ajandaServisi.CreateAsync(
                Sablon("FREQ=WEEKLY;BYDAY=TU;COUNT=4", new DateTime(2026, 9, 1, 14, 0, 0)));

            var ikinci = await baglam.Ajandalar
                .Where(a => a.SeriId == ilk.SeriId)
                .OrderBy(a => a.BaslangicTarihi)
                .Skip(1).FirstAsync();

            // İkinci tekrarın saati 16:00'a çekilir — yalnızca bu tekrar.
            var dto = _ortam.Mapper.Map<AjandaDto>(ikinci);
            dto.BaslangicTarihi = ikinci.BaslangicTarihi.Date.AddHours(16);
            dto.BitisTarihi = dto.BaslangicTarihi.AddHours(1);
            dto.Kapsam = TekrarKapsam.Yalnizca;

            await ajandaServisi.UpdateAsync(dto);

            using var kontrol = _ortam.Baglam();
            var hepsi = await kontrol.Ajandalar
                .Where(a => a.SeriId == ilk.SeriId)
                .OrderBy(a => a.BaslangicTarihi)
                .ToListAsync();

            Assert.Equal(4, hepsi.Count);
            var degisen = hepsi.Single(a => a.Id == ikinci.Id);
            Assert.Equal(new TimeSpan(16, 0, 0), degisen.BaslangicTarihi.TimeOfDay);
            Assert.True(degisen.SeriAyrik);
            // Orijinal başlangıç DEĞİŞMEZ — seri eşleştirmesi bozulmasın.
            Assert.Equal(new TimeSpan(14, 0, 0), degisen.SeriOrijinalBaslangic!.Value.TimeOfDay);

            Assert.All(hepsi.Where(a => a.Id != ikinci.Id),
                a => Assert.Equal(new TimeSpan(14, 0, 0), a.BaslangicTarihi.TimeOfDay));
            Assert.All(hepsi.Where(a => a.Id != ikinci.Id), a => Assert.False(a.SeriAyrik));
        }
    }

    // ===================================================================
    //  KAPSAM: TÜMÜ
    // ===================================================================
    [Fact]
    public async Task Tumu_Kapsami_Saati_Tum_Tekrarlara_Uygular_Ayrik_Olani_Atlar()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, _, baglam) = Kur();
        using (baglam)
        {
            var ilk = await ajandaServisi.CreateAsync(
                Sablon("FREQ=WEEKLY;BYDAY=TU;COUNT=4", new DateTime(2026, 9, 1, 14, 0, 0)));

            var tekrarlar = await baglam.Ajandalar
                .Where(a => a.SeriId == ilk.SeriId).OrderBy(a => a.BaslangicTarihi).ToListAsync();

            // 3. tekrar bireysel düzenlenip ayrılır (istisna).
            var ucuncu = tekrarlar[2];
            var ayrikDto = _ortam.Mapper.Map<AjandaDto>(ucuncu);
            ayrikDto.BaslangicTarihi = ucuncu.BaslangicTarihi.Date.AddHours(18);
            ayrikDto.BitisTarihi = ayrikDto.BaslangicTarihi.AddHours(1);
            await ajandaServisi.UpdateAsync(ayrikDto);

            // Şimdi TÜMÜ kapsamıyla başlık ve saat değişir.
            var tumuDto = _ortam.Mapper.Map<AjandaDto>(tekrarlar[0]);
            tumuDto.Baslik = "Meclis Toplantısı (Yeni)";
            tumuDto.BaslangicTarihi = tekrarlar[0].BaslangicTarihi.Date.AddHours(10);
            tumuDto.BitisTarihi = tumuDto.BaslangicTarihi.AddMinutes(90);
            tumuDto.Kapsam = TekrarKapsam.Tumu;
            await ajandaServisi.UpdateAsync(tumuDto);

            using var kontrol = _ortam.Baglam();
            var sonrasi = await kontrol.Ajandalar
                .Where(a => a.SeriId == ilk.SeriId).OrderBy(a => a.BaslangicTarihi).ToListAsync();

            var ayrik = sonrasi.Single(a => a.Id == ucuncu.Id);
            var digerleri = sonrasi.Where(a => a.Id != ucuncu.Id).ToList();

            Assert.Equal(3, digerleri.Count);
            Assert.All(digerleri, a => Assert.Equal("Meclis Toplantısı (Yeni)", a.Baslik));
            Assert.All(digerleri, a => Assert.Equal(new TimeSpan(10, 0, 0), a.BaslangicTarihi.TimeOfDay));
            Assert.All(digerleri, a => Assert.Equal(90, (int)(a.BitisTarihi!.Value - a.BaslangicTarihi).TotalMinutes));

            // Ayrık tekrar korunur: ne saati ne başlığı değişir.
            Assert.Equal(new TimeSpan(18, 0, 0), ayrik.BaslangicTarihi.TimeOfDay);
            Assert.Equal("Meclis Toplantısı", ayrik.Baslik);
        }
    }

    [Fact]
    public async Task Tumu_Kapsaminda_Gun_Kaydirilirsa_Kural_Yeniden_Capalanir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, seriServisi, _, baglam) = Kur();
        using (baglam)
        {
            // Salı → Perşembe'ye kaydırma (2 gün).
            var ilk = await ajandaServisi.CreateAsync(
                Sablon("FREQ=WEEKLY;BYDAY=TU;COUNT=3", new DateTime(2026, 9, 1, 14, 0, 0)));

            var dto = _ortam.Mapper.Map<AjandaDto>(
                await baglam.Ajandalar.FirstAsync(a => a.Id == ilk.Id));
            dto.BaslangicTarihi = new DateTime(2026, 9, 3, 14, 0, 0);
            dto.BitisTarihi = dto.BaslangicTarihi.AddHours(1);
            dto.Kapsam = TekrarKapsam.Tumu;
            await ajandaServisi.UpdateAsync(dto);

            using var kontrol = _ortam.Baglam();
            var sonrasi = await kontrol.Ajandalar
                .Where(a => a.SeriId == ilk.SeriId).OrderBy(a => a.BaslangicTarihi).ToListAsync();

            Assert.All(sonrasi, a => Assert.Equal(DayOfWeek.Thursday, a.BaslangicTarihi.DayOfWeek));

            var seri = await seriServisi.GetirAsync(ilk.Id);
            Assert.Equal("FREQ=WEEKLY;BYDAY=TH;COUNT=3", seri!.Rrule);
        }
    }

    // ===================================================================
    //  KAPSAM: BUNDAN SONRAKİLER
    // ===================================================================
    [Fact]
    public async Task Bundan_Sonrakiler_Gecmisi_Korur_Yeni_Seri_Acar()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, seriServisi, _, baglam) = Kur();
        using (baglam)
        {
            var ilk = await ajandaServisi.CreateAsync(
                Sablon("FREQ=WEEKLY;BYDAY=TU;COUNT=6", new DateTime(2026, 9, 1, 14, 0, 0)));
            var eskiSeriId = ilk.SeriId!.Value;

            var tekrarlar = await baglam.Ajandalar
                .Where(a => a.SeriId == eskiSeriId).OrderBy(a => a.BaslangicTarihi).ToListAsync();
            var ucuncu = tekrarlar[2];   // 15 Eylül
            // DİKKAT: `ucuncu` izlenen (tracked) varlıktır; güncelleme onu da
            // değiştirir. Karşılaştırma için özgün başlangıç önce kopyalanır.
            var ucuncununOzgunBaslangici = ucuncu.BaslangicTarihi;

            var dto = _ortam.Mapper.Map<AjandaDto>(ucuncu);
            dto.BaslangicTarihi = ucuncu.BaslangicTarihi.Date.AddHours(11);
            dto.BitisTarihi = dto.BaslangicTarihi.AddHours(1);
            dto.Baslik = "Meclis (Saat Değişti)";
            dto.Kapsam = TekrarKapsam.BundanSonrakiler;

            var sonuc = await ajandaServisi.UpdateAsync(dto);

            using var kontrol = _ortam.Baglam();

            // Geçmiş iki tekrar eski seride ve eski saatte kalır.
            var eskiler = await kontrol.Ajandalar
                .Where(a => a.SeriId == eskiSeriId).OrderBy(a => a.BaslangicTarihi).ToListAsync();
            Assert.Equal(2, eskiler.Count);
            Assert.All(eskiler, a => Assert.Equal(new TimeSpan(14, 0, 0), a.BaslangicTarihi.TimeOfDay));
            Assert.All(eskiler, a => Assert.Equal("Meclis Toplantısı", a.Baslik));

            // Eski serinin kuralı kesme noktasından önce biter.
            var eskiSeri = await kontrol.AjandaSeriler.FirstAsync(s => s.Id == eskiSeriId);
            Assert.Contains("UNTIL=", eskiSeri.Rrule);
            Assert.True(eskiSeri.BitisTarihi < ucuncununOzgunBaslangici);

            // Yeni seri: 4 tekrar (6 - 2), yeni saat ve yeni başlık.
            Assert.NotEqual(eskiSeriId, sonuc.SeriId);
            var yeniler = await kontrol.Ajandalar
                .Where(a => a.SeriId == sonuc.SeriId).OrderBy(a => a.BaslangicTarihi).ToListAsync();
            Assert.Equal(4, yeniler.Count);
            Assert.All(yeniler, a => Assert.Equal(new TimeSpan(11, 0, 0), a.BaslangicTarihi.TimeOfDay));
            Assert.All(yeniler, a => Assert.Equal("Meclis (Saat Değişti)", a.Baslik));

            // Düzenlenen kaydın KİMLİĞİ korunur (notları/fotoğrafları yerinde kalsın).
            Assert.Contains(yeniler, a => a.Id == ucuncu.Id);
        }
    }

    [Fact]
    public async Task Bundan_Sonrakiler_Notu_Olan_Tekrari_Silmez_Arsivler()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, _, baglam) = Kur();
        using (baglam)
        {
            var ilk = await ajandaServisi.CreateAsync(
                Sablon("FREQ=WEEKLY;BYDAY=TU;COUNT=5", new DateTime(2026, 9, 1, 14, 0, 0)));

            var tekrarlar = await baglam.Ajandalar
                .Where(a => a.SeriId == ilk.SeriId).OrderBy(a => a.BaslangicTarihi).ToListAsync();

            // 4. tekrara not ekle (ileride kalan bir tekrar).
            await ajandaServisi.CreateNoteAsync(new AjandaNotDto
            {
                AjandaId = tekrarlar[3].Id,
                Not = "Bu tekrara özel not"
            });

            // 2. tekrardan itibaren yeni kurala geç.
            var dto = _ortam.Mapper.Map<AjandaDto>(tekrarlar[1]);
            dto.BaslangicTarihi = tekrarlar[1].BaslangicTarihi.Date.AddHours(9);
            dto.BitisTarihi = dto.BaslangicTarihi.AddHours(1);
            dto.Kapsam = TekrarKapsam.BundanSonrakiler;
            await ajandaServisi.UpdateAsync(dto);

            using var kontrol = _ortam.Baglam();

            // Notlu tekrar fiziksel olarak durmalı (silinmiş işaretli).
            var notlu = await kontrol.Ajandalar.IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id == tekrarlar[3].Id);
            Assert.NotNull(notlu);
            Assert.True(notlu!.IsDeleted);

            // Not da kaybolmamalı.
            Assert.True(await kontrol.AjandaNotlar.AnyAsync(n => n.AjandaId == tekrarlar[3].Id));
        }
    }

    // ===================================================================
    //  NOTLAR TEKRARA ÖZELDİR
    // ===================================================================
    [Fact]
    public async Task Not_Yalnizca_Eklendigi_Tekrara_Ait_Olur()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, _, baglam) = Kur();
        using (baglam)
        {
            var ilk = await ajandaServisi.CreateAsync(
                Sablon("FREQ=DAILY;COUNT=3", new DateTime(2026, 9, 1, 9, 0, 0)));

            var tekrarlar = await baglam.Ajandalar
                .Where(a => a.SeriId == ilk.SeriId).OrderBy(a => a.BaslangicTarihi).ToListAsync();

            await ajandaServisi.CreateNoteAsync(new AjandaNotDto
            {
                AjandaId = tekrarlar[1].Id,
                Not = "Yalnızca 2 Eylül tekrarına ait not"
            });

            var ikinciNotlar = await ajandaServisi.GetNotesAsync(tekrarlar[1].Id);
            var birinciNotlar = await ajandaServisi.GetNotesAsync(tekrarlar[0].Id);
            var ucuncuNotlar = await ajandaServisi.GetNotesAsync(tekrarlar[2].Id);

            Assert.Single(ikinciNotlar);
            Assert.Empty(birinciNotlar);
            Assert.Empty(ucuncuNotlar);
        }
    }

    // ===================================================================
    //  SİLME KAPSAMLARI
    // ===================================================================
    [Fact]
    public async Task Silme_Yalnizca_Bu_Tek_Tekrari_Isaretler()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, _, baglam) = Kur();
        using (baglam)
        {
            var ilk = await ajandaServisi.CreateAsync(
                Sablon("FREQ=DAILY;COUNT=4", new DateTime(2026, 9, 1, 9, 0, 0)));
            var tekrarlar = await baglam.Ajandalar
                .Where(a => a.SeriId == ilk.SeriId).OrderBy(a => a.BaslangicTarihi).ToListAsync();

            await ajandaServisi.DeleteAsync(tekrarlar[1].Id, TekrarKapsam.Yalnizca);

            using var kontrol = _ortam.Baglam();
            var kalan = await kontrol.Ajandalar.Where(a => a.SeriId == ilk.SeriId).CountAsync();
            Assert.Equal(3, kalan);

            var silinen = await kontrol.Ajandalar.IgnoreQueryFilters().FirstAsync(a => a.Id == tekrarlar[1].Id);
            Assert.True(silinen.IsDeleted);
            Assert.True(silinen.SeriAyrik);   // ufuk uzatma onu yeniden üretmesin
        }
    }

    [Fact]
    public async Task Silme_Bundan_Sonrakiler_Ve_Tumu_Kapsamlari()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, _, baglam) = Kur();
        using (baglam)
        {
            var ilk = await ajandaServisi.CreateAsync(
                Sablon("FREQ=DAILY;COUNT=5", new DateTime(2026, 9, 1, 9, 0, 0)));
            var tekrarlar = await baglam.Ajandalar
                .Where(a => a.SeriId == ilk.SeriId).OrderBy(a => a.BaslangicTarihi).ToListAsync();

            await ajandaServisi.DeleteAsync(tekrarlar[3].Id, TekrarKapsam.BundanSonrakiler);

            using (var kontrol = _ortam.Baglam())
            {
                Assert.Equal(3, await kontrol.Ajandalar.CountAsync(a => a.SeriId == ilk.SeriId));
                var seri = await kontrol.AjandaSeriler.FirstAsync(s => s.Id == ilk.SeriId);
                Assert.Contains("UNTIL=", seri.Rrule);
                Assert.False(seri.Iptal);
            }

            await ajandaServisi.DeleteAsync(tekrarlar[0].Id, TekrarKapsam.Tumu);

            using (var kontrol = _ortam.Baglam())
            {
                Assert.Equal(0, await kontrol.Ajandalar.CountAsync(a => a.SeriId == ilk.SeriId));
                var seri = await kontrol.AjandaSeriler.FirstAsync(s => s.Id == ilk.SeriId);
                Assert.True(seri.Iptal);
                // Kayıtlar fiziksel olarak durur (arşiv/geri alma için).
                Assert.Equal(5, await kontrol.Ajandalar.IgnoreQueryFilters().CountAsync(a => a.SeriId == ilk.SeriId));
            }
        }
    }

    // ===================================================================
    //  UFUK UZATMA
    // ===================================================================
    [Fact]
    public async Task Ufuk_Uzatma_Idempotent_Ikinci_Cagride_Yeni_Tekrar_Uretmez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, seriServisi, _, baglam) = Kur();
        using (baglam)
        {
            var ilk = await ajandaServisi.CreateAsync(
                Sablon("FREQ=WEEKLY;BYDAY=MO", DateTime.Now.Date.AddDays(7).AddHours(10)));

            var baslangicAdet = await baglam.Ajandalar.CountAsync(a => a.SeriId == ilk.SeriId);

            // İlk çağrı: ufuk zaten üretildiği için yeni tekrar olmamalı.
            var uretilen1 = await seriServisi.UfkuGenisletAsync();
            var uretilen2 = await seriServisi.UfkuGenisletAsync();

            using var kontrol = _ortam.Baglam();
            var sonAdet = await kontrol.Ajandalar.CountAsync(a => a.SeriId == ilk.SeriId);

            Assert.Equal(0, uretilen1);
            Assert.Equal(0, uretilen2);
            Assert.Equal(baslangicAdet, sonAdet);

            // Aynı orijinal başlangıçtan iki kayıt OLUŞMAMALI.
            var yinelenen = await kontrol.Ajandalar
                .Where(a => a.SeriId == ilk.SeriId)
                .GroupBy(a => a.SeriOrijinalBaslangic)
                .Where(g => g.Count() > 1)
                .CountAsync();
            Assert.Equal(0, yinelenen);
        }
    }

    [Fact]
    public async Task Ufuk_Uzatma_Imi_Geride_Kalmis_Seriyi_Tamamlar()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, seriServisi, _, baglam) = Kur();
        using (baglam)
        {
            var ilk = await ajandaServisi.CreateAsync(
                Sablon("FREQ=WEEKLY;BYDAY=MO", DateTime.Now.Date.AddDays(7).AddHours(10)));
            var seriId = ilk.SeriId!.Value;

            // Ufku yapay olarak geriye çek ve ileriki tekrarları sil:
            // "yalnızca 3 ay üretilmiş" durumunu taklit eder.
            var kesme = DateTime.Now.AddMonths(3);
            var silinecekler = await baglam.Ajandalar
                .Where(a => a.SeriId == seriId && a.BaslangicTarihi > kesme)
                .ToListAsync();
            baglam.Ajandalar.RemoveRange(silinecekler);
            var seri = await baglam.AjandaSeriler.FirstAsync(s => s.Id == seriId);
            seri.UretilenSonTarih = kesme;
            await baglam.SaveChangesAsync();

            var kalanAdet = await baglam.Ajandalar.CountAsync(a => a.SeriId == seriId);
            var uretilen = await seriServisi.UfkuGenisletAsync();

            using var kontrol = _ortam.Baglam();
            var yeniAdet = await kontrol.Ajandalar.CountAsync(a => a.SeriId == seriId);

            Assert.True(uretilen > 0, "Ufuk uzatma yeni tekrar üretmeliydi.");
            Assert.Equal(kalanAdet + uretilen, yeniAdet);
            Assert.True(await kontrol.Ajandalar.AnyAsync(a => a.SeriId == seriId && a.BaslangicTarihi > kesme));
        }
    }

    // ===================================================================
    //  TEK SEFERLİK ↔ TEKRARLANAN DÖNÜŞÜMÜ
    // ===================================================================
    [Fact]
    public async Task Tek_Seferlik_Etkinlik_Tekrarlanan_Yapilabilir()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, seriServisi, _, baglam) = Kur();
        using (baglam)
        {
            // Önce TEK SEFERLİK bir etkinlik.
            var dto = Sablon("FREQ=DAILY;COUNT=2", new DateTime(2026, 9, 1, 14, 0, 0));
            dto.Tekrar = null;
            var tekil = await ajandaServisi.CreateAsync(dto);
            Assert.Null(tekil.SeriId);

            // Kullanıcının eklediği not, dönüşümden sonra da yerinde kalmalı.
            await ajandaServisi.CreateNoteAsync(new AjandaNotDto
            {
                AjandaId = tekil.Id,
                Not = "Dönüşümden önce eklenen not"
            });

            // Şimdi düzenleme ekranından tekrarlı yapılıyor.
            var guncel = _ortam.Mapper.Map<AjandaDto>(
                await baglam.Ajandalar.FirstAsync(a => a.Id == tekil.Id));
            guncel.Tekrar = new AjandaSeriOlusturDto { Rrule = "FREQ=WEEKLY;BYDAY=TU;COUNT=3" };

            var sonuc = await ajandaServisi.UpdateAsync(guncel);

            Assert.NotNull(sonuc.SeriId);
            Assert.Equal(tekil.Id, sonuc.Id);   // KİMLİK KORUNUR
            Assert.Equal("FREQ=WEEKLY;BYDAY=TU;COUNT=3", sonuc.TekrarKurali);

            using var kontrol = _ortam.Baglam();
            var tekrarlar = await kontrol.Ajandalar
                .Where(a => a.SeriId == sonuc.SeriId).ToListAsync();
            Assert.Equal(3, tekrarlar.Count);
            Assert.All(tekrarlar, t => Assert.True(t.TekrarEden));

            // Not, ilk tekrarda (yani özgün kayıtta) duruyor olmalı.
            Assert.True(await kontrol.AjandaNotlar.AnyAsync(n => n.AjandaId == tekil.Id));
        }
    }

    [Fact]
    public async Task Kapsam_Secilince_Kural_Degisikligi_Seriyi_Boler()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, _, baglam) = Kur();
        using (baglam)
        {
            var ilk = await ajandaServisi.CreateAsync(
                Sablon("FREQ=WEEKLY;BYDAY=TU;COUNT=4", new DateTime(2026, 9, 1, 14, 0, 0)));
            var eskiSeriId = ilk.SeriId!.Value;

            var tekrarlar = await baglam.Ajandalar
                .Where(a => a.SeriId == eskiSeriId).OrderBy(a => a.BaslangicTarihi).ToListAsync();
            var ucuncu = tekrarlar[2];

            // Kullanıcı kuralı değiştiriyor ve "bundan sonrakiler" kapsamını
            // SEÇİYOR. (Kapsam "yalnızca bu" olsaydı kural YOKSAYILIRDI —
            // bkz. Tek_Tekrarin_Gunu_Degistirilince_Seri_Bolunmez...)
            var dto = _ortam.Mapper.Map<AjandaDto>(ucuncu);
            dto.Tekrar = new AjandaSeriOlusturDto { Rrule = "FREQ=WEEKLY;BYDAY=TH;COUNT=2" };
            dto.Kapsam = TekrarKapsam.BundanSonrakiler;

            var sonuc = await ajandaServisi.UpdateAsync(dto);

            Assert.NotEqual(eskiSeriId, sonuc.SeriId);

            using var kontrol = _ortam.Baglam();
            var eskiler = await kontrol.Ajandalar.Where(a => a.SeriId == eskiSeriId).ToListAsync();
            Assert.Equal(2, eskiler.Count);   // geçmiş korunur

            var yeniler = await kontrol.Ajandalar.Where(a => a.SeriId == sonuc.SeriId).ToListAsync();
            Assert.Equal(2, yeniler.Count);
            Assert.All(yeniler, a => Assert.Equal(DayOfWeek.Thursday, a.BaslangicTarihi.DayOfWeek));
        }
    }

    [Fact]
    public async Task Tekrar_Kaldirilabilir_Yalnizca_Bu()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, _, baglam) = Kur();
        using (baglam)
        {
            var ilk = await ajandaServisi.CreateAsync(
                Sablon("FREQ=DAILY;COUNT=4", new DateTime(2026, 9, 1, 9, 0, 0)));
            var seriId = ilk.SeriId!.Value;
            var tekrarlar = await baglam.Ajandalar
                .Where(a => a.SeriId == seriId).OrderBy(a => a.BaslangicTarihi).ToListAsync();

            var dto = _ortam.Mapper.Map<AjandaDto>(tekrarlar[1]);
            dto.TekrarKaldir = true;
            dto.Kapsam = TekrarKapsam.Yalnizca;

            var sonuc = await ajandaServisi.UpdateAsync(dto);

            Assert.Null(sonuc.SeriId);
            Assert.False(sonuc.TekrarEden);

            using var kontrol = _ortam.Baglam();
            // Seri diğer üç tekrarla devam eder.
            Assert.Equal(3, await kontrol.Ajandalar.CountAsync(a => a.SeriId == seriId));
            var kopan = await kontrol.Ajandalar.FirstAsync(a => a.Id == tekrarlar[1].Id);
            Assert.Null(kopan.SeriId);
            Assert.False(kopan.IsDeleted);
        }
    }

    [Fact]
    public async Task Tekrar_Kaldirilabilir_Tumu()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, _, baglam) = Kur();
        using (baglam)
        {
            var ilk = await ajandaServisi.CreateAsync(
                Sablon("FREQ=DAILY;COUNT=5", new DateTime(2026, 9, 1, 9, 0, 0)));
            var seriId = ilk.SeriId!.Value;

            var dto = _ortam.Mapper.Map<AjandaDto>(
                await baglam.Ajandalar.FirstAsync(a => a.Id == ilk.Id));
            dto.TekrarKaldir = true;
            dto.Kapsam = TekrarKapsam.Tumu;

            var sonuc = await ajandaServisi.UpdateAsync(dto);

            Assert.Null(sonuc.SeriId);

            using var kontrol = _ortam.Baglam();
            // Yalnızca düzenlenen kayıt kalır; diğerleri kaldırılır.
            Assert.Equal(0, await kontrol.Ajandalar.CountAsync(a => a.SeriId == seriId));
            Assert.True(await kontrol.Ajandalar.AnyAsync(a => a.Id == ilk.Id && !a.IsDeleted));
            Assert.True((await kontrol.AjandaSeriler.FirstAsync(x => x.Id == seriId)).Iptal);
        }
    }

    [Fact]
    public async Task Tek_Tekrarin_Gunu_Degistirilince_Seri_Bolunmez_Ve_Kayit_Kaybolmaz()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        // GERÇEK HATA: Kullanıcı ileriki bir tekrarı "yalnızca bu" kapsamıyla
        // Çarşamba'dan Perşembe'ye aldığında etkinlik kayboluyordu. Sebep:
        // istemci kuralı formdaki tarihten yeniden üretip gönderiyor, sunucu da
        // bunu "kural değişti" sayıp seriyi bölüyordu.
        var (ajandaServisi, _, _, baglam) = Kur();
        using (baglam)
        {
            var ilk = await ajandaServisi.CreateAsync(
                Sablon("FREQ=WEEKLY;BYDAY=WE;COUNT=4", new DateTime(2026, 9, 2, 10, 0, 0)));
            var seriId = ilk.SeriId!.Value;

            var tekrarlar = await baglam.Ajandalar
                .Where(a => a.SeriId == seriId).OrderBy(a => a.BaslangicTarihi).ToListAsync();
            Assert.Equal(4, tekrarlar.Count);

            var ucuncu = tekrarlar[2];                       // 16 Eylül Çarşamba
            var yeniGun = ucuncu.BaslangicTarihi.AddDays(1);  // 17 Eylül Perşembe

            var dto = _ortam.Mapper.Map<AjandaDto>(ucuncu);
            dto.BaslangicTarihi = yeniGun;
            dto.BitisTarihi = yeniGun.AddHours(1);
            dto.Kapsam = TekrarKapsam.Yalnizca;
            // İstemcinin yeniden ürettiği kural: gün değiştiği için BYDAY=TH.
            dto.Tekrar = new AjandaSeriOlusturDto { Rrule = "FREQ=WEEKLY;BYDAY=TH;COUNT=4" };

            var sonuc = await ajandaServisi.UpdateAsync(dto);

            using var kontrol = _ortam.Baglam();

            // 1) Kayıt DURUYOR ve yeni günde.
            var duzenlenen = await kontrol.Ajandalar.FirstOrDefaultAsync(a => a.Id == ucuncu.Id);
            Assert.NotNull(duzenlenen);
            Assert.Equal(yeniGun, duzenlenen!.BaslangicTarihi);
            Assert.Equal(DayOfWeek.Thursday, duzenlenen.BaslangicTarihi.DayOfWeek);
            Assert.True(duzenlenen.SeriAyrik);
            Assert.Equal(seriId, duzenlenen.SeriId);          // seride kalır

            // 2) Seri BÖLÜNMEDİ: hâlâ 4 tekrar, hepsi aynı seride.
            var hepsi = await kontrol.Ajandalar
                .Where(a => a.SeriId == seriId).OrderBy(a => a.BaslangicTarihi).ToListAsync();
            Assert.Equal(4, hepsi.Count);
            Assert.Equal(sonuc.SeriId, seriId);

            // 3) Diğer tekrarlar Çarşamba olarak KALDI.
            Assert.All(hepsi.Where(a => a.Id != ucuncu.Id),
                a => Assert.Equal(DayOfWeek.Wednesday, a.BaslangicTarihi.DayOfWeek));

            // 4) Serinin kuralı DEĞİŞMEDİ.
            var seri = await kontrol.AjandaSeriler.FirstAsync(x => x.Id == seriId);
            Assert.Equal("FREQ=WEEKLY;BYDAY=WE;COUNT=4", seri.Rrule);
        }
    }

    // ===================================================================
    //  SİLİNMİŞ ETKİNLİĞİN DETAYI
    // ===================================================================
    [Fact]
    public async Task Silinmis_Etkinligin_Detayi_Okunabilir_Ama_Duzenlenemez()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, _, _, baglam) = Kur();
        using (baglam)
        {
            var dto = Sablon("FREQ=DAILY;COUNT=2", new DateTime(2026, 9, 1, 9, 0, 0));
            dto.Tekrar = null;
            var etkinlik = await ajandaServisi.CreateAsync(dto);

            await ajandaServisi.DeleteAsync(etkinlik.Id);

            // Detay OKUNABİLİR (arşiv/geçmiş aramasından açılabilmeli).
            var okunan = await ajandaServisi.GetAsync(etkinlik.Id);
            Assert.Equal(etkinlik.Id, okunan.Id);
            Assert.True(okunan.IsDeleted);

            // Yazma yolu silinmiş kaydı GÖRMEZ → düzenlenemez.
            await Assert.ThrowsAsync<EntityNotFoundException>(
                () => ajandaServisi.GetByIdAsync(etkinlik.Id));
        }
    }

    // ===================================================================
    //  SERİ OKUMA
    // ===================================================================
    [Fact]
    public async Task Tek_Seferlik_Etkinlikte_Seri_Null_Doner()
    {
        PostgresYoksaAtla();
        await TemizleAsync();

        var (ajandaServisi, seriServisi, _, baglam) = Kur();
        using (baglam)
        {
            var dto = Sablon("FREQ=DAILY;COUNT=2", new DateTime(2026, 9, 1, 9, 0, 0));
            dto.Tekrar = null;   // tek seferlik
            var sonuc = await ajandaServisi.CreateAsync(dto);

            Assert.Null(sonuc.SeriId);
            Assert.Null(await seriServisi.GetirAsync(sonuc.Id));
        }
    }
}
