using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>
/// ETKİN BİRİM — kullanıcının o an adına iş yaptığı birim.
/// </summary>
/// <remarks>
/// <para>
/// Başkan yardımcısı kendine bağlı müdürlüğü seçip o müdürlüğün işlerini
/// görebilmeli. Bugün böyle bir şey yok: <c>GetCurrentBirimId()</c> JWT
/// talebinden okunuyor ve oturum boyunca sabit.
/// </para>
///
/// <para>
/// <b>Eski servisler DEĞİŞMİYOR.</b> <c>GetCurrentBirimId()</c> yedi serviste
/// senkron çağrılıyor ve canlı mobil uygulama o davranışa bağlı; vekâleti
/// oraya sokmak iki yıldır çalışan görünürlük kapılarını tek seferde
/// değiştirmek olurdu. Bu arayüz YALNIZCA iş takip modülünde kullanılır.
/// </para>
///
/// <para>
/// <b>Başlığa asla güvenilmez.</b> İstemci <c>X-Etkin-Birim</c> gönderiyor;
/// sunucu her istekte istenen birimin kullanıcının biriminin ALT AĞACINDA
/// olduğunu doğruluyor. Doğrulamayı atlayan tek bir sorgu, bir müdürün
/// başka bir müdürlüğün bütün işini okumasına yeterdi.
/// </para>
/// </remarks>
public interface IEtkinBirim
{
    /// <summary>Başlık adı — istemci ve sunucu tek yerden okusun.</summary>
    const string BaslikAdi = "X-Etkin-Birim";

    /// <summary>
    /// İstek için geçerli birim kimliği.
    /// </summary>
    /// <remarks>
    /// Başlık yoksa kullanıcının kendi birimi. Başlık varsa ve geçerliyse o.
    /// Geçersizse <see cref="BusinessRuleException"/> — sessizce kendi
    /// birimine düşmek, kullanıcıya yanlış birimin verisini gösterip doğru
    /// birimi seçtiğini sandırırdı.
    /// </remarks>
    Task<long> IdAsync(CancellationToken iptal = default);

    /// <summary>Kullanıcı kendi birimi dışında bir birim adına mı çalışıyor?</summary>
    Task<bool> VekaletVarMiAsync(CancellationToken iptal = default);

    /// <summary>
    /// Etkin birim ve ALTINDAKİ bütün birimler — "alt birimler dahil"
    /// süzgeci için.
    /// </summary>
    Task<IReadOnlySet<long>> KapsamAsync(bool altBirimlerDahil, CancellationToken iptal = default);
}

public class EtkinBirim(
    IHttpContextAccessor _baglam,
    ICurrentUserService _kullanici,
    IBirimAgaci _agac,
    IIzinServisi _izinler) : IEtkinBirim
{
    /// <summary>İstek başına bir kez çözülür — aynı istekte defalarca sorulacak.</summary>
    private long? _cozulmus;

    public async Task<long> IdAsync(CancellationToken iptal = default)
    {
        if (_cozulmus is { } hazir) return hazir;

        var kendi = _kullanici.GetCurrentBirimId();
        var istenen = BaslikOku();

        if (istenen is null || istenen == kendi)
        {
            _cozulmus = kendi;
            return kendi;
        }

        // İZİN KAPISI — vekâlet ayrı bir yetkidir.
        //
        // İzni olmayanın başlığı YOK SAYILMAZ, reddedilir: yok saymak
        // kullanıcıya seçtiği birimin verisini gösterdiğini sandırırdı.
        var kullaniciId = await _kullanici.GetUserIdAsync();
        if (kullaniciId is null or 0)
        {
            throw new BusinessRuleException("Oturum çözülemedi.");
        }

        if (!await _izinler.VarMiAsync(kullaniciId.Value, Izinler.GorevBirimKapsam))
        {
            throw new BusinessRuleException(
                "Başka bir birim adına işlem yapma yetkiniz yok.");
        }

        // ALT AĞAÇ KAPISI — yalnızca KENDİ altındaki birimler.
        if (!await _agac.AltAgactaMiAsync(kendi, istenen.Value, iptal))
        {
            throw new BusinessRuleException(
                "Seçilen birim sizin biriminize bağlı değil.");
        }

        _cozulmus = istenen.Value;
        return istenen.Value;
    }

    public async Task<bool> VekaletVarMiAsync(CancellationToken iptal = default) =>
        await IdAsync(iptal) != _kullanici.GetCurrentBirimId();

    public async Task<IReadOnlySet<long>> KapsamAsync(
        bool altBirimlerDahil, CancellationToken iptal = default)
    {
        var etkin = await IdAsync(iptal);
        return altBirimlerDahil
            ? await _agac.AltAgacAsync(etkin, iptal)
            : new HashSet<long> { etkin };
    }

    /// <summary>
    /// Başlığı okur; yoksa ya da sayı değilse <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Bozuk bir başlık (ör. boş dize, harf) <b>hata değil</b>: istemcinin
    /// eski bir sürümü ya da bir vekil sunucu araya girmiş olabilir. Böyle
    /// bir durumda kullanıcının kendi birimi doğru davranıştır. Hata yalnızca
    /// GEÇERLİ ama YETKİSİZ bir birim istendiğinde verilir.
    /// </remarks>
    private long? BaslikOku()
    {
        var deger = _baglam.HttpContext?.Request.Headers[IEtkinBirim.BaslikAdi].ToString();
        if (string.IsNullOrWhiteSpace(deger)) return null;
        return long.TryParse(deger, out var id) && id > 0 ? id : null;
    }
}
