using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Web.Services.V2;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// SLA TARAMASI — süre aşımı bildirimleri.
/// </summary>
/// <remarks>
/// <para>
/// Kilitlenen kural tek cümle: <b>aynı görev için aynı uyarı iki kez
/// gönderilmez</b>. Bekçi ayrı bir "bildirildi mi" kolonu değil, zaman
/// çizelgesinin kendisi.
/// </para>
/// <para>
/// Tarama işçiden ayrı bir servis olduğu için doğrudan çağrılabiliyor;
/// işçinin içinde kalsaydı bu davranışı doğrulamanın tek yolu bir saat
/// beklemek olurdu.
/// </para>
/// </remarks>
[Collection(SunucuKoleksiyonu.Ad)]
public class SlaTests(SunucuTestOrtami ortam) : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam = ortam;
    private readonly SahteMesajServisi _mesajlar = new();

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres yok");
    }

    private (ISlaTarayici Tarayici, AppDbContextKisayolu Baglam) Kur()
    {
        var baglam = _ortam.Baglam();
        return (new SlaTarayici(baglam, _mesajlar, NullLogger<SlaTarayici>.Instance),
                new AppDbContextKisayolu(baglam));
    }

    /// <summary>Süresi geçmiş, açık bir görev yazar.</summary>
    private async Task<long> GecikmisGorevAsync(int gecikmeSaati = 5, long birimId = 1)
    {
        using var b = _ortam.Baglam();
        await _ortam.TemelVerileriKurAsync();

        var g = new WorkTask
        {
            TakipNo = "SLA-" + Guid.NewGuid().ToString("N")[..10],
            Baslik = "Gecikmiş iş",
            BirimId = birimId,
            Durum = GorevDurumu.DevamEdiyor,
            BaslamaTarihi = DateTime.Now.AddHours(-gecikmeSaati - 1),
            SlaBitis = DateTime.Now.AddHours(-gecikmeSaati),
        };

        b.Gorevler.Add(g);
        await b.SaveChangesAsync();
        return g.Id;
    }

    private async Task<int> UyariSayisiAsync(long gorevId)
    {
        using var b = _ortam.Baglam();
        return await b.IsOlaylari.CountAsync(o =>
            o.VarlikTuru == IsVarligi.Gorev &&
            o.VarlikId == gorevId &&
            o.Tip == GorevOlayTipi.SlaUyarisi);
    }

    /// <summary>
    /// AYNI GÖREV İÇİN İKİ KEZ BİLDİRİLMEZ.
    /// </summary>
    /// <remarks>
    /// İşçi saatte bir çalışıyor. Bekçi olmasaydı süresi aşmış bir görev
    /// kapanana kadar HER SAAT yeniden bildirilir ve yönetici bildirimleri
    /// tamamen susturmayı öğrenirdi.
    /// </remarks>
    [Fact]
    public async Task Ayni_gorev_IKI_KEZ_bildirilmez()
    {
        PostgresYoksaAtla();

        var gorevId = await GecikmisGorevAsync();
        var (tarayici, _) = Kur();

        var ilk = await tarayici.TaraAsync();
        Assert.True(ilk >= 1);
        Assert.Equal(1, await UyariSayisiAsync(gorevId));

        // İkinci tur: aynı görev artık atlanıyor.
        var (tarayici2, _) = Kur();
        await tarayici2.TaraAsync();

        Assert.Equal(1, await UyariSayisiAsync(gorevId));
    }

    /// <summary>Süresi AŞMAMIŞ görev bildirilmez.</summary>
    [Fact]
    public async Task Suresi_asmamis_gorev_bildirilmez()
    {
        PostgresYoksaAtla();
        await _ortam.TemelVerileriKurAsync();

        long gorevId;
        using (var b = _ortam.Baglam())
        {
            var g = new WorkTask
            {
                TakipNo = "SLA-" + Guid.NewGuid().ToString("N")[..10],
                Baslik = "Süresi var",
                BirimId = 1,
                Durum = GorevDurumu.DevamEdiyor,
                SlaBitis = DateTime.Now.AddHours(5),
            };
            b.Gorevler.Add(g);
            await b.SaveChangesAsync();
            gorevId = g.Id;
        }

        var (tarayici, _) = Kur();
        await tarayici.TaraAsync();

        Assert.Equal(0, await UyariSayisiAsync(gorevId));
    }

    /// <summary>
    /// KAPANMIŞ görev bildirilmez — ölçüm bitti.
    /// </summary>
    [Fact]
    public async Task Kapanmis_gorev_bildirilmez()
    {
        PostgresYoksaAtla();

        var gorevId = await GecikmisGorevAsync();

        using (var b = _ortam.Baglam())
        {
            await b.Gorevler.Where(g => g.Id == gorevId)
                .ExecuteUpdateAsync(s => s.SetProperty(g => g.Durum, GorevDurumu.Tamamlandi));
        }

        var (tarayici, _) = Kur();
        await tarayici.TaraAsync();

        Assert.Equal(0, await UyariSayisiAsync(gorevId));
    }

    /// <summary>
    /// HEDEF BULUNAMASA DA OLAY YAZILIR.
    /// </summary>
    /// <remarks>
    /// Aksi hâlde yöneticisi olmayan bir birimin görevi her turda yeniden
    /// denenir ve sonsuza kadar sorgu üretirdi. Kayıt "bu aşım görüldü"
    /// demek; bildirimin gitmesi ayrı bir şey.
    /// </remarks>
    [Fact]
    public async Task Hedef_yoksa_bile_olay_yazilir()
    {
        PostgresYoksaAtla();

        // 2 numaralı birimde `gorev.onayla` izni olan kullanıcı yok.
        var gorevId = await GecikmisGorevAsync(birimId: 2);
        var (tarayici, _) = Kur();

        await tarayici.TaraAsync();

        Assert.Equal(1, await UyariSayisiAsync(gorevId));
    }

    /// <summary>Bağlamı `using` ile kapatmak için ince sarmalayıcı.</summary>
    private sealed class AppDbContextKisayolu(KentOS.Kalem.Web.Data.AppDbContext b) : IDisposable
    {
        public KentOS.Kalem.Web.Data.AppDbContext Baglam { get; } = b;
        public void Dispose() => Baglam.Dispose();
    }
}
