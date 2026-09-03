using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Application.Dto.V2.IsTakip;
using KentOS.Kalem.Application.Identity;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.AuthPolicies;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Services.V2;

namespace KentOS.Kalem.Web.Controllers.V2;

/// <summary>
/// PERSONEL SEÇİCİSİ — görev ataması, ekip üyeliği ve proje ekibi için.
/// </summary>
/// <remarks>
/// <para>
/// Üç ekran da (<c>TaskAssignments</c>, <c>Teams</c>, <c>ProjectTeam</c>)
/// <c>/api/v2/ayar/birim-kullanicilari</c> ucuna bağlıydı. O uç gizli etkinlik
/// davetlisi seçmek için yazılmış: <b>oturum sahibini listeden çıkarıyor</b> ve
/// yalnızca <b>tam olarak kendi</b> birimini tarıyor. İş takibinde bu iki kural
/// modülü çalışmaz hâle getiriyordu — gerekçesi
/// <see cref="PersonelSecimDto"/> içinde ölçümüyle birlikte yazılı.
/// </para>
///
/// <para>
/// <b>Kapsam ETKİN BİRİMDEN geliyor.</b> Vekâletle başka bir müdürlük adına
/// çalışan kişi o müdürlüğün personelini görür; kapsamın kendisi
/// <see cref="IEtkinBirim"/> içinde yeniden doğrulanıyor, bu uç ayrı bir kapı
/// açmıyor.
/// </para>
///
/// <para>
/// <b>Kapı, listeyi KULLANAN izinlerin birleşimi.</b> Bu listeyi isteyen üç
/// ekran var: göreve atama, ekip üyeliği, proje ekibi. Hiçbirine yetkisi
/// olmayan kullanıcının kurum personelini ad ve unvanıyla dökebilmesi için
/// bir sebep yok. Tek bir izne bağlamak ise yanlış olurdu: ekibi kuran kişi
/// (<c>ekip.yonet</c>) ile projeye üye ekleyen kişi (<c>proje.uyeYonet</c>)
/// aynı role sahip olmak zorunda değil.
/// </para>
/// </remarks>
[Route("api/v2/personel")]
[Izin(
    Izinler.GorevGoruntule, Izinler.GorevEkle, Izinler.GorevAtama,
    Izinler.EkipYonet, Izinler.ProjeGoruntule, Izinler.ProjeYonet,
    Izinler.ProjeUyeYonet)]
public class PersonelController(
    AppDbContext _context,
    ICurrentUserService _kullanici,
    IEtkinBirim _etkinBirim) : V2ControllerBase
{
    /// <summary>
    /// Etkin birimin ve altındaki birimlerin personeli.
    /// </summary>
    /// <param name="ara">Ad, soyad ya da unvanda geçen metin.</param>
    /// <param name="altBirimlerDahil">
    /// Alt birimler de taransın mı? Varsayılan <c>true</c>: bir müdürlük
    /// şefliklerine iş verebilmeli ve o şefliklerin personelini
    /// görebilmelidir.
    /// </param>
    /// <remarks>
    /// Sayfalama YOK ve bilinçli — bu liste bir seçim kutusu dolduruyor.
    /// Sayfalanmış bir açılır liste, kullanıcıyı aradığı kişiyi sayfa sayfa
    /// aramaya zorlardı; bunun yerine <paramref name="ara"/> var. Üst sınır
    /// yine de konuyor: bir kurumda binlerce personel olabilir ve tamamını
    /// tek yanıtta göndermek tarayıcıyı kilitlerdi.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<List<PersonelSecimDto>>(StatusCodes.Status200OK)]
    public async Task<List<PersonelSecimDto>> ListeAsync(
        [FromQuery] string? ara,
        [FromQuery] bool altBirimlerDahil = true,
        CancellationToken iptal = default)
    {
        var etkin = await _etkinBirim.IdAsync(iptal);
        var kapsam = await _etkinBirim.KapsamAsync(altBirimlerDahil, iptal);

        if (kapsam.Count == 0) return [];

        var benim = await _kullanici.GetUserIdAsync();

        var sorgu = _context.Users
            .AsNoTracking()
            .Where(u => u.BirimId != null && kapsam.Contains(u.BirimId.Value));

        var temiz = ara?.Trim();
        if (!string.IsNullOrEmpty(temiz))
        {
            sorgu = sorgu.Where(u =>
                (u.Ad != null && EF.Functions.ILike(u.Ad, $"%{temiz}%"))
                || (u.Soyad != null && EF.Functions.ILike(u.Soyad, $"%{temiz}%"))
                || (u.Unvan != null && EF.Functions.ILike(u.Unvan, $"%{temiz}%")));
        }

        var ham = await sorgu
            .OrderBy(u => u.Ad).ThenBy(u => u.Soyad)
            .Take(300)
            .Select(u => new
            {
                u.Id, u.Ad, u.Soyad, u.Unvan, u.BirimId,
                BirimAd = u.Birim != null ? u.Birim.Ad : null,
            })
            .ToListAsync(iptal);

        return [.. ham
            .Select(u => new PersonelSecimDto
            {
                Id = u.Id,
                Ad = $"{u.Ad} {u.Soyad}".Trim(),
                Unvan = u.Unvan,
                BirimId = u.BirimId,
                BirimAd = u.BirimAd,
                Kendisi = u.Id == benim,
                AltBirimden = u.BirimId != etkin,
            })
            // KENDİSİ EN ÜSTTE. En sık seçilen kişi kullanıcının kendisi:
            // sahada işi üstlenen, ekibi kuran ve projeyi yöneten aynı kişi.
            // Alfabetik sırada "Zeynep" olup listenin dibinde kalması,
            // "kendimi ekleyemiyorum" hissini sürdürürdü.
            .OrderByDescending(u => u.Kendisi)
            .ThenBy(u => u.AltBirimden)
            .ThenBy(u => u.Ad, StringComparer.CurrentCulture)];
    }
}
