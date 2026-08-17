using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>Giriş denemesinin sonucu.</summary>
public enum GirisSonucTuru
{
    Basarili,
    KimlikHatali,
    Kilitli,
}

/// <summary>Giriş sonucu ve başarılıysa üretilen jeton.</summary>
public record GirisSonucu(
    [property: JsonPropertyName("tur")] GirisSonucTuru Tur,
    [property: JsonPropertyName("jeton")] string? Jeton = null,
    [property: JsonPropertyName("gecerlilikSonu")] DateTime? GecerlilikSonu = null)
{
    /// <summary>
    /// Kullanıcıya gösterilecek mesaj.
    ///
    /// <para>
    /// "Kullanıcı yok" ve "şifre yanlış" AYNI mesajı döndürür — farklı mesaj
    /// vermek, geçerli kullanıcı adlarının tespitine (enumeration) yol açar.
    /// Bu, v1'den taşınan bilinçli bir karardır.
    /// </para>
    /// </summary>
    [JsonPropertyName("mesaj")]
    public string Mesaj => Tur switch
    {
        GirisSonucTuru.Kilitli =>
            "Çok fazla hatalı deneme nedeniyle hesabınız geçici olarak kilitlendi. " +
            "Lütfen birkaç dakika sonra tekrar deneyin.",
        _ => "Kullanıcı adı veya şifre hatalı",
    };
}

public interface IOturumServisi
{
    Task<GirisSonucu> GirisYapAsync(string kullaniciAdi, string parola);
}

/// <summary>
/// Giriş akışının TEK uygulaması — hem v1 (<c>/api/AccountApi/Login</c>) hem
/// v2 (<c>/api/v2/oturum/giris</c>) bunu çağırır.
///
/// <para>
/// NEDEN ORTAK: Kimlik doğrulama akışının iki kopyası olması, birinde yapılan
/// bir güvenlik düzeltmesinin diğerinde unutulması demektir. Akış aynen v1'den
/// taşındı; davranış eşitliği <c>OturumServisiTests</c> ile kanıtlanıyor.
/// </para>
///
/// <para>
/// Controller'lar sonucu KENDİ biçimlerinde döndürür: v1
/// <c>ErrorResponseDto</c>/<c>LoginResponseDto</c>, v2 <c>HataYaniti</c>. Bu
/// sayede v1'in JSON sözleşmesi bit düzeyinde korunur.
/// </para>
/// </summary>
public class OturumServisi(
    UserManager<AppUser> _userManager,
    IJwtService _jwtService,
    IOturumKaydiServisi _oturumKaydi) : IOturumServisi
{
    public async Task<GirisSonucu> GirisYapAsync(string kullaniciAdi, string parola)
    {
        var kullanici = await _userManager.FindByNameAsync(kullaniciAdi);

        // Kullanıcı yok VEYA şifre yanlış → AYNI cevap (enumeration önlemi).
        if (kullanici is null)
        {
            // Denetim kaydı gerçeği yazar; KULLANICIYA verilen cevap yine aynı.
            await _oturumKaydi.KaydetAsync(
                null, kullaniciAdi, OturumOlayi.Giris, false, "Kullanıcı bulunamadı");
            return new GirisSonucu(GirisSonucTuru.KimlikHatali);
        }

        // Brute-force: hesap kilitliyse hemen reddet. `CheckPasswordAsync`
        // kilitlenmeyi kendisi işletmez, akış manuel kurulmak zorunda.
        if (await _userManager.IsLockedOutAsync(kullanici))
        {
            await _oturumKaydi.KaydetAsync(
                kullanici.Id, kullaniciAdi, OturumOlayi.Giris, false, "Hesap kilitli");
            return new GirisSonucu(GirisSonucTuru.Kilitli);
        }

        if (!await _userManager.CheckPasswordAsync(kullanici, parola))
        {
            await _userManager.AccessFailedAsync(kullanici);
            await _oturumKaydi.KaydetAsync(
                kullanici.Id, kullaniciAdi, OturumOlayi.Giris, false, "Parola hatalı");
            return new GirisSonucu(GirisSonucTuru.KimlikHatali);
        }

        await _userManager.ResetAccessFailedCountAsync(kullanici);

        var roller = await _userManager.GetRolesAsync(kullanici);

        var talepler = new List<Claim>
        {
            new(ClaimTypes.Name, kullanici.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var rol in roller)
        {
            talepler.Add(new Claim(ClaimTypes.Role, rol));
        }

        // Birim KAYDI aranmaz, yalnızca kullanıcıdaki kimlik yazılır.
        //
        // Önceden burada `IBirimService.GetAsync` çağrılıyordu ve birimi
        // olmayan (ya da birimi silinmiş) kullanıcı, parolası doğru olsa bile
        // "Kayıt bulunamadı" ile giriş YAPAMIYORDU — yönetim formunda birim
        // "Seçilmedi" bırakılabildiği için erişilebilir bir kilitlenme.
        // Talep, `ICurrentUserService.GetCurrentBirimIdAsync`'in birimsiz
        // kullanıcı için zaten döndürdüğü değerle aynı: 0.
        talepler.Add(new Claim(
            WorkClaimTypes.BirimIdIdentifier,
            (kullanici.BirimId ?? 0).ToString()));
        talepler.Add(new Claim(WorkClaimTypes.UserIdIdentifier, kullanici.Id.ToString()));

        var jeton = _jwtService.GenerateToken(talepler);

        await _oturumKaydi.KaydetAsync(kullanici.Id, kullaniciAdi, OturumOlayi.Giris, true);

        return new GirisSonucu(
            GirisSonucTuru.Basarili,
            new JwtSecurityTokenHandler().WriteToken(jeton),
            jeton.ValidTo);
    }
}
