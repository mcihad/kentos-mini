using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Web.Services.V2;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// GÖREV DURUM AKIŞI — akışın kendisi.
/// </summary>
/// <remarks>
/// <para>
/// Veritabanı gerektirmez: akış saf bir karar tablosu. Bu yüzden testler de
/// hızlı ve koşulsuz çalışıyor — Postgres olmayan bir makinede de bu
/// bekçiler ateş eder.
/// </para>
/// <para>
/// Burada kilitlenen şey bir uygulama ayrıntısı değil, <b>kurumun iş
/// kuralı</b>: onaysız tamamlanma yok, tamamlanmış iş yeniden açılmaz,
/// bekleyen işin SLA'sı işlemez.
/// </para>
/// </remarks>
public class GorevDurumAkisiTests
{
    // ── onay kapısı ────────────────────────────────────────────────────

    /// <summary>
    /// ONAYSIZ TAMAMLANMA YOK. Modülün en önemli tek kuralı.
    /// </summary>
    /// <remarks>
    /// Personelin "bitirdim" beyanı ile kurumun kabulü aynı şey değil. Tek
    /// adımda tamamlanabilseydi, yapılmamış bir iş kimse bakmadan kapanırdı.
    /// </remarks>
    [Theory]
    [InlineData(GorevDurumu.Yeni)]
    [InlineData(GorevDurumu.Atandi)]
    [InlineData(GorevDurumu.Basladi)]
    [InlineData(GorevDurumu.DevamEdiyor)]
    [InlineData(GorevDurumu.Beklemede)]
    [InlineData(GorevDurumu.IadeEdildi)]
    [InlineData(GorevDurumu.Reddedildi)]
    public void Onay_bekleme_ASILMADAN_tamamlanamaz(GorevDurumu mevcut)
    {
        Assert.False(GorevDurumAkisi.Gecerli(mevcut, GorevDurumu.Tamamlandi));
    }

    [Fact]
    public void Tamamlanmaya_yalnizca_onaydan_gecilir()
    {
        Assert.True(GorevDurumAkisi.Gecerli(
            GorevDurumu.TamamlanmaBekliyor, GorevDurumu.Tamamlandi));
        Assert.True(GorevDurumAkisi.Gecerli(
            GorevDurumu.TamamlanmaBekliyor, GorevDurumu.IadeEdildi));
    }

    // ── son duraklar ───────────────────────────────────────────────────

    /// <summary>
    /// TAMAMLANMIŞ İŞ YENİDEN AÇILMAZ.
    /// </summary>
    /// <remarks>
    /// Yeniden açmak SLA ve hizmet standardı ölçümünü geçmişe dönük
    /// değiştirmek demek. Yeniden yapılması gerekiyorsa YENİ görev açılır ve
    /// ikisi ayrı ayrı ölçülür.
    /// </remarks>
    [Fact]
    public void Tamamlanan_ve_iptal_SON_DURAK()
    {
        Assert.Empty(GorevDurumAkisi.Sonraki(GorevDurumu.Tamamlandi));
        Assert.Empty(GorevDurumAkisi.Sonraki(GorevDurumu.Iptal));

        Assert.True(GorevDurumAkisi.Kapali(GorevDurumu.Tamamlandi));
        Assert.True(GorevDurumAkisi.Kapali(GorevDurumu.Iptal));
    }

    [Theory]
    [InlineData(GorevDurumu.Yeni)]
    [InlineData(GorevDurumu.Basladi)]
    [InlineData(GorevDurumu.TamamlanmaBekliyor)]
    public void Acik_gorev_kapali_sayilmaz(GorevDurumu durum)
    {
        Assert.False(GorevDurumAkisi.Kapali(durum));
    }

    // ── akışın gövdesi ─────────────────────────────────────────────────

    [Fact]
    public void Normal_akis_bastan_sona_yurur()
    {
        var yol = new[]
        {
            GorevDurumu.Yeni, GorevDurumu.Atandi, GorevDurumu.Basladi,
            GorevDurumu.DevamEdiyor, GorevDurumu.TamamlanmaBekliyor,
            GorevDurumu.Tamamlandi,
        };

        for (var i = 0; i < yol.Length - 1; i++)
        {
            Assert.True(GorevDurumAkisi.Gecerli(yol[i], yol[i + 1]),
                $"{yol[i]} → {yol[i + 1]} geçişi kapalı.");
        }
    }

    [Fact]
    public void Iade_edilen_gorev_personele_geri_doner()
    {
        Assert.True(GorevDurumAkisi.Gecerli(GorevDurumu.IadeEdildi, GorevDurumu.DevamEdiyor));
        Assert.True(GorevDurumAkisi.Gecerli(GorevDurumu.IadeEdildi, GorevDurumu.Basladi));
    }

    [Fact]
    public void Reddedilen_gorev_baskasina_atanabilir()
    {
        Assert.True(GorevDurumAkisi.Gecerli(GorevDurumu.Reddedildi, GorevDurumu.Atandi));
    }

    /// <summary>Atanmamış görev doğrudan başlatılamaz — önce sorumlusu olmalı.</summary>
    [Fact]
    public void Atanmamis_gorev_baslatilamaz()
    {
        Assert.False(GorevDurumAkisi.Gecerli(GorevDurumu.Yeni, GorevDurumu.Basladi));
        Assert.False(GorevDurumAkisi.Gecerli(GorevDurumu.Yeni, GorevDurumu.DevamEdiyor));
    }

    /// <summary>Geriye dönük atlamalar kapalı.</summary>
    [Theory]
    [InlineData(GorevDurumu.DevamEdiyor, GorevDurumu.Yeni)]
    [InlineData(GorevDurumu.TamamlanmaBekliyor, GorevDurumu.Basladi)]
    [InlineData(GorevDurumu.Tamamlandi, GorevDurumu.DevamEdiyor)]
    [InlineData(GorevDurumu.Iptal, GorevDurumu.Yeni)]
    public void Geriye_donuk_gecisler_KAPALI(GorevDurumu mevcut, GorevDurumu hedef)
    {
        Assert.False(GorevDurumAkisi.Gecerli(mevcut, hedef));
    }

    // ── SLA sayacı ─────────────────────────────────────────────────────

    /// <summary>
    /// BEKLEYEN İŞİN SLA'SI İŞLEMEZ.
    /// </summary>
    /// <remarks>
    /// Malzeme bekleyen ya da havaya takılan bir işi "geciktirdi" diye
    /// personele yazmak, ölçümün kendisini anlamsız kılar.
    /// </remarks>
    [Fact]
    public void Beklemede_SLA_durur()
    {
        Assert.False(GorevDurumAkisi.SlaIsliyor(GorevDurumu.Beklemede));
    }

    /// <summary>Kimseye verilmemiş iş geciktirilmiş sayılmaz.</summary>
    [Theory]
    [InlineData(GorevDurumu.Yeni)]
    [InlineData(GorevDurumu.Atandi)]
    public void Baslamamis_gorevde_SLA_islemez(GorevDurumu durum)
    {
        Assert.False(GorevDurumAkisi.SlaIsliyor(durum));
    }

    [Theory]
    [InlineData(GorevDurumu.Basladi)]
    [InlineData(GorevDurumu.DevamEdiyor)]
    [InlineData(GorevDurumu.TamamlanmaBekliyor)]
    [InlineData(GorevDurumu.IadeEdildi)]
    public void Yuruyen_gorevde_SLA_isler(GorevDurumu durum)
    {
        Assert.True(GorevDurumAkisi.SlaIsliyor(durum));
    }

    /// <summary>Kapanmış görevde SLA işlemez — ölçüm bitti.</summary>
    [Theory]
    [InlineData(GorevDurumu.Tamamlandi)]
    [InlineData(GorevDurumu.Iptal)]
    [InlineData(GorevDurumu.Reddedildi)]
    public void Kapali_gorevde_SLA_islemez(GorevDurumu durum)
    {
        Assert.False(GorevDurumAkisi.SlaIsliyor(durum));
    }

    // ── etiketler ──────────────────────────────────────────────────────

    /// <summary>
    /// HER durumun okunabilir Türkçe adı ve geçerli bir rengi olmalı.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Etiket sunucuda üretiliyor ki iki istemci aynı duruma farklı ad
    /// vermesin. Yeni bir durum eklenip <c>switch</c>'e yazılmazsa
    /// <c>_ =&gt;</c> koluna düşer ve arayüzde ham enum adı görünür.
    /// </para>
    /// <para>
    /// Denetim "ad enum adından FARKLI olsun" DEĞİL: bazı adlar Türkçede
    /// zaten aynı ("Yeni", "Normal") ve o kural yanlış yere ateş ediyordu.
    /// Asıl kaçırılan şey <b>bileşik</b> adlar — <c>DevamEdiyor</c>,
    /// <c>TamamlanmaBekliyor</c>. Bu yüzden PascalCase birleşimi aranıyor.
    /// </para>
    /// </remarks>
    [Fact]
    public void Her_durumun_adi_ve_rengi_var()
    {
        foreach (var durum in Enum.GetValues<GorevDurumu>())
        {
            CevrilmisMi(GorevDurumAkisi.Ad(durum), durum.ToString());
            Assert.Matches("^#[0-9A-Fa-f]{6}$", GorevDurumAkisi.Renk(durum));
        }
    }

    [Fact]
    public void Her_oncelik_ve_kaynagin_adi_var()
    {
        foreach (var o in Enum.GetValues<GorevOnceligi>())
            CevrilmisMi(GorevDurumAkisi.OncelikAdi(o), o.ToString());

        foreach (var k in Enum.GetValues<GorevKaynagi>())
            CevrilmisMi(GorevDurumAkisi.KaynakAdi(k), k.ToString());
    }

    /// <summary>Etiket boş olmamalı ve ham PascalCase enum adı olmamalı.</summary>
    private static void CevrilmisMi(string etiket, string enumAdi)
    {
        Assert.False(string.IsNullOrWhiteSpace(etiket), $"{enumAdi} için etiket boş.");

        // `DevamEdiyor` gibi iki büyük harfli birleşim = çevrilmemiş.
        Assert.False(
            System.Text.RegularExpressions.Regex.IsMatch(etiket, "[a-zçğıöşü][A-ZÇĞİÖŞÜ]"),
            $"{enumAdi} için etiket ham enum adı görünüyor: \"{etiket}\"");
    }

    /// <summary>
    /// HER durum akış tablosunda tanımlı olmalı.
    /// </summary>
    /// <remarks>
    /// Tabloda olmayan bir durum <see cref="GorevDurumAkisi.Sonraki"/>'de boş
    /// döner — yani sessizce SON DURAK olur. Yeni bir durum eklenip
    /// bağlanmadığında görev orada kilitlenir ve sebebi hiçbir yerde
    /// görünmez.
    /// </remarks>
    [Fact]
    public void Her_durum_akista_tanimli()
    {
        var sonDuraklar = new[] { GorevDurumu.Tamamlandi, GorevDurumu.Iptal };

        foreach (var durum in Enum.GetValues<GorevDurumu>())
        {
            if (sonDuraklar.Contains(durum)) continue;

            Assert.NotEmpty(GorevDurumAkisi.Sonraki(durum));
        }
    }
}
