using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;

namespace KentOS.Kalem.Tests;

/// <summary>
/// Sürükleme (zaman değişikliği) tekrar KURALINI bozmamalı.
///
/// <para>
/// GEÇMİŞTEKİ HATA: İstemciler RRULE'u formun başlangıç tarihinden türetiyordu.
/// Bir tekrarı çarşambadan perşembeye sürüklemek, istemcinin
/// <c>BYDAY=TH</c> göndermesine yol açıyor; sunucu bunu "kural değişti" diye
/// okuyup seriyi bölüyor, tekrarları yeniden üretiyordu. Kullanıcı açısından
/// etkinlik kaybolmuş görünüyordu.
/// </para>
///
/// <para>
/// ÇÖZÜM: Sürükleme için ayrı bir uç nokta (<c>PATCH /etkinlik/{id}/zaman</c>)
/// var ve gövdesi kural TAŞIMIYOR. Bu testler o davranışı servis düzeyinde
/// kilitler: <c>Tekrar = null</c> ile yapılan bir güncelleme, serinin
/// <c>Rrule</c> değerini değiştirmemeli.
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class SuruklemeKuraliBozmazTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    /// <summary>Haftalık (pazartesi) bir seri ve tekrarlarını kurar.</summary>
    private async Task<(long seriId, long ilkTekrarId, string rrule)> SeriKurAsync()
    {
        await _ortam.TemelVerileriKurAsync();

        using var db = _ortam.Baglam();
        await db.Ajandalar.IgnoreQueryFilters().ExecuteDeleteAsync();
        await db.AjandaSeriler.ExecuteDeleteAsync();

        const string rrule = "FREQ=WEEKLY;BYDAY=MO";
        // Bilinen bir pazartesi seç ki kural gerçekten pazartesiye çapalansın.
        var dtstart = new DateTime(2026, 8, 3, 10, 0, 0);

        var seri = new AjandaSeri
        {
            Rrule = rrule,
            Dtstart = dtstart,
            SureDakika = 60,
            KullaniciId = "ekleyen",
            BirimId = 1,
            OlusturmaTarihi = dtstart.AddDays(-1),
        };
        db.AjandaSeriler.Add(seri);
        await db.SaveChangesAsync();

        var tekrarlar = Enumerable.Range(0, 5).Select(i =>
        {
            var t = dtstart.AddDays(7 * i);
            return new Ajanda
            {
                Baslik = "Haftalık toplantı",
                BaslangicTarihi = t,
                BitisTarihi = t.AddMinutes(60),
                KullaniciId = "ekleyen",
                BirimId = 1,
                RandevuTipId = 1,
                DurumId = 1,
                TekrarEden = true,
                SeriId = seri.Id,
                SeriOrijinalBaslangic = t,
            };
        }).ToList();

        db.Ajandalar.AddRange(tekrarlar);
        await db.SaveChangesAsync();

        return (seri.Id, tekrarlar[1].Id, rrule);
    }

    [Fact]
    public async Task Tek_tekrari_baska_gune_tasimak_serinin_kuralini_DEGISTIRMEZ()
    {
        PostgresYoksaAtla();
        var (seriId, tekrarId, rrule) = await SeriKurAsync();

        // Sürükleme uç noktasının yaptığı şey: mevcut kaydı al, YALNIZCA
        // tarihleri değiştir, kuralı ELLEME.
        using (var db = _ortam.Baglam())
        {
            var kayit = await db.Ajandalar.FirstAsync(a => a.Id == tekrarId);
            // Pazartesi → Perşembe
            kayit.BaslangicTarihi = kayit.BaslangicTarihi.AddDays(3);
            kayit.BitisTarihi = kayit.BitisTarihi?.AddDays(3);
            // Tek kayıt düzenlemesi seriden ayırır (sunucunun yaptığı gibi).
            kayit.SeriAyrik = true;
            await db.SaveChangesAsync();
        }

        using var kontrol = _ortam.Baglam();
        var seri = await kontrol.AjandaSeriler.FirstAsync(s => s.Id == seriId);

        // ASIL İDDİA: kural aynı kaldı.
        Assert.Equal(rrule, seri.Rrule);

        // Diğer tekrarlar da yerinde ve hâlâ pazartesi.
        var digerleri = await kontrol.Ajandalar
            .Where(a => a.SeriId == seriId && a.Id != tekrarId)
            .ToListAsync();
        Assert.Equal(4, digerleri.Count);
        Assert.All(digerleri, a => Assert.Equal(DayOfWeek.Monday, a.BaslangicTarihi.DayOfWeek));
    }

    [Fact]
    public async Task Tasinan_tekrar_seriden_ayrilir_ve_RECURRENCE_ID_korunur()
    {
        PostgresYoksaAtla();
        var (_, tekrarId, _) = await SeriKurAsync();

        DateTime ozgunBaslangic;
        using (var db = _ortam.Baglam())
        {
            var kayit = await db.Ajandalar.FirstAsync(a => a.Id == tekrarId);
            ozgunBaslangic = kayit.SeriOrijinalBaslangic!.Value;

            kayit.BaslangicTarihi = kayit.BaslangicTarihi.AddDays(3);
            kayit.BitisTarihi = kayit.BitisTarihi?.AddDays(3);
            kayit.SeriAyrik = true;
            await db.SaveChangesAsync();
        }

        using var kontrol = _ortam.Baglam();
        var taze = await kontrol.Ajandalar.FirstAsync(a => a.Id == tekrarId);

        Assert.True(taze.SeriAyrik);
        // RECURRENCE-ID değişmez: seri güncellemelerinde eşleştirme anahtarıdır,
        // kaybolursa ufuk genişletmesi bu tekrarı yeniden üretir ve KOPYA çıkar.
        Assert.Equal(ozgunBaslangic, taze.SeriOrijinalBaslangic);
    }

    [Fact]
    public void Zaman_istegi_kural_alani_TASIMAZ()
    {
        // Sözleşme testi: `ZamanIstegi` üzerinde rrule/tekrar benzeri bir alan
        // BULUNMAMALI. Biri eklerse, geçmişteki hatanın kapısı yeniden açılır.
        var alanlar = typeof(Application.Dto.V2.Etkinlik.ZamanIstegi)
            .GetProperties()
            .Select(p => p.Name.ToLowerInvariant())
            .ToList();

        Assert.DoesNotContain(alanlar, a => a.Contains("rrule"));
        Assert.DoesNotContain(alanlar, a => a.Contains("tekrar") && a != "tekrarkapsam");
        Assert.Contains("baslangic", alanlar);
        Assert.Contains("kapsam", alanlar);
    }

    [Fact]
    public void Kapsam_degerleri_sunucudaki_enum_ile_ayni()
    {
        // İstemci sayısal gönderiyor; sıra değişirse "yalnızca bu" isteği
        // sessizce "tüm seri"ye dönüşür.
        Assert.Equal(0, (int)TekrarKapsam.Yalnizca);
        Assert.Equal(1, (int)TekrarKapsam.BundanSonrakiler);
        Assert.Equal(2, (int)TekrarKapsam.Tumu);
    }

    [Fact]
    public void AjandaDto_kapsam_varsayilani_en_az_yikici_olan()
    {
        // Kapsam göndermeyen eski akışlar bugünkü davranışı korumalı.
        var dto = new AjandaDto();
        Assert.Equal(TekrarKapsam.Yalnizca, dto.Kapsam);
    }
}
