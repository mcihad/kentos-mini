using System.Text.RegularExpressions;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;

namespace KentOS.Kalem.Tests;

/// <summary>
/// Bildirim tercihi zincirinin KOPUK HALKASINI yakalar.
///
/// <para>
/// Zincir dört parçadan oluşuyor ve dördü ayrı dosyada:
/// <c>NotifikasyonTip</c> (enum) → <c>UserSetting</c> (kolon) →
/// <c>UserSettingDto</c> (istemciye giden alan) →
/// <c>UserService.HasReceiveNotification</c> (switch kolu).
/// </para>
///
/// <para>
/// <b>Eksik halka sessizdir.</b> <c>HasReceiveNotification</c> tanımadığı tipte
/// <c>false</c> döner: yeni bir tip eklenip switch kolu unutulursa o bildirim
/// hiç gönderilmez — istisna atılmaz, günlüğe bir şey düşmez, testler yeşil
/// kalır. Kullanıcı yalnızca "bildirim gelmiyor" der. Aynı şekilde DTO alanı
/// unutulursa ayar ekranında o satır hiç görünmez.
/// </para>
///
/// <para>
/// Bu testler veritabanı istemez: kilitlenen şey verinin kendisi değil,
/// dört dosyanın birbiriyle tutarlılığı.
/// </para>
/// </summary>
public class BildirimTercihleriTests
{
    /// <summary>Kapıya girmeyen, yalnızca görünüm tercihi olan alanlar.</summary>
    private static readonly HashSet<string> KapiDisi = ["Always", "HideOldAgendas"];

    private static IEnumerable<NotifikasyonTip> BildirimTipleri() =>
        Enum.GetValues<NotifikasyonTip>().Where(t => !KapiDisi.Contains(t.ToString()));

    /// <summary>Her bildirim tipinin bir <c>UserSetting</c> kolonu olmalı.</summary>
    [Fact]
    public void Her_tipin_bir_ayar_kolonu_var()
    {
        var kolonlar = typeof(UserSetting).GetProperties()
            .Where(p => p.PropertyType == typeof(bool))
            .Select(p => p.Name)
            .ToHashSet();

        var eksik = BildirimTipleri()
            .Select(t => t.ToString())
            .Where(ad => !kolonlar.Contains(ad))
            .ToList();

        Assert.True(eksik.Count == 0,
            $"UserSetting'te kolonu olmayan tip(ler): {string.Join(", ", eksik)}");
    }

    /// <summary>
    /// Her tip istemciye de gitmeli — yoksa ayar ekranında satır çizilemez.
    /// </summary>
    [Fact]
    public void Her_tipin_bir_dto_alani_var()
    {
        var alanlar = typeof(UserSettingDto).GetProperties()
            .Where(p => p.PropertyType == typeof(bool))
            .Select(p => p.Name)
            .ToHashSet();

        var eksik = BildirimTipleri()
            .Select(t => t.ToString())
            .Where(ad => !alanlar.Contains(ad))
            .ToList();

        Assert.True(eksik.Count == 0,
            $"UserSettingDto'da alanı olmayan tip(ler): {string.Join(", ", eksik)}");
    }

    /// <summary>
    /// VARSAYILAN AÇIK.
    ///
    /// <para>
    /// Kapalı başlayan bir bildirim, ayarı hiç açmayan kullanıcı için hiç var
    /// olmamış demektir. Yeni bir kolon eklerken <c>= true</c> yazmayı unutmak
    /// bildirimi sessizce susturur.
    /// </para>
    /// </summary>
    [Fact]
    public void Ayar_bayraklari_varsayilan_acik()
    {
        var yeni = new UserSetting();

        var kapali = typeof(UserSetting).GetProperties()
            .Where(p => p.PropertyType == typeof(bool))
            .Where(p => !(bool)(p.GetValue(yeni) ?? false))
            .Select(p => p.Name)
            .ToList();

        Assert.True(kapali.Count == 0,
            $"varsayılanı kapalı gelen bayrak(lar): {string.Join(", ", kapali)}");
    }

    /// <summary>
    /// <c>HasReceiveNotification</c> switch'inde her tipin bir kolu olmalı.
    ///
    /// <para>
    /// Kaynak dosya OKUNARAK denetleniyor: kapı bir <c>switch</c> ifadesi ve
    /// eksik kol çalışma anında <c>_ =&gt; false</c> koluna düşüyor, yani
    /// davranış olarak "bildirim yok"tan ayırt edilemiyor. Hatayı yakalamanın
    /// tek ucuz yolu metni taramak — ön yüzde de aynı gerekçeyle kaynak tarayan
    /// testler var (bkz. `frontend/CLAUDE.md`, "Kaynak tarayan testler
    /// bilinçli").
    /// </para>
    /// </summary>
    [Fact]
    public void Kapi_her_tipi_tanir()
    {
        var yol = KaynakYolu("KentOS.Kalem.Web", "Services", "UserService.cs");
        var kaynak = File.ReadAllText(yol);

        // `NotifikasyonTip.X => setting.X,` kollarını topla
        var kollar = Regex.Matches(kaynak, @"NotifikasyonTip\.(\w+)\s*=>")
            .Select(m => m.Groups[1].Value)
            .ToHashSet();

        var eksik = BildirimTipleri()
            .Select(t => t.ToString())
            .Where(ad => !kollar.Contains(ad))
            .ToList();

        Assert.True(eksik.Count == 0,
            $"HasReceiveNotification switch'inde kolu olmayan tip(ler): {string.Join(", ", eksik)}. "
            + "Kol eklenmezse bildirim sessizce hiç gönderilmez.");
    }

    /// <summary>
    /// AYAR SATIRI YOKSA BİLDİRİM GELİR.
    ///
    /// <para>
    /// Kapı satır yokken <c>false</c> dönüyordu: ayar satırı ancak
    /// <c>GetSetting()</c> ilk kez çağrıldığında oluşuyor, dolayısıyla ayar
    /// ekranını hiç açmamış kullanıcı bildirimlerin tamamını kaçırıyordu.
    /// Satırın yokluğu "tercih belirtilmemiş" demektir, "istemiyorum" değil.
    /// </para>
    /// </summary>
    [Fact]
    public void Ayar_satiri_yoksa_bildirim_gelir()
    {
        var yol = KaynakYolu("KentOS.Kalem.Web", "Services", "UserService.cs");
        var kaynak = File.ReadAllText(yol);

        var blok = Regex.Match(kaynak, @"if \(setting == null\)\s*\{(.*?)\}", RegexOptions.Singleline);

        Assert.True(blok.Success, "`setting == null` koruması bulunamadı.");
        Assert.Contains("return true", blok.Groups[1].Value);
    }

    /// <summary>Depo kökünden dosya yolu — test çalışma dizini bin/ altında.</summary>
    private static string KaynakYolu(params string[] parcalar)
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);
        while (dizin is not null && !File.Exists(Path.Combine(dizin.FullName, "KentOS.Kalem.sln")))
        {
            dizin = dizin.Parent;
        }
        Assert.NotNull(dizin);
        return Path.Combine([dizin!.FullName, .. parcalar]);
    }
}
