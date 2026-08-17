using KentOS.Mini.Application.Identity;

namespace KentOS.Mini.Web.AuthPolicies;

/// <summary>
/// Bir kullanıcının rollerinden hangi politikaları karşıladığını hesaplar.
///
/// <para>
/// NEDEN GEREKLİ: Arayüzün hangi menüyü göstereceğine karar verebilmesi için
/// politikaları istemciye bildirmek gerekiyor. Örneğin <see cref="AuthPolicyNames.Ajanda"/>
/// yalnızca Admin/Sekreter/Yonetici/Baskan'a açık; bunu bilmeyen bir arayüz
/// <c>Kullanici</c> veya <c>Basin</c> rolüne takvimi gösterir ve kullanıcı 403
/// duvarına çarpar.
/// </para>
///
/// <para>
/// <b>Bu yetkilendirme DEĞİLDİR.</b> Yetkinin tek kaynağı
/// <see cref="PolicyRegistrar"/> ile kurulan sunucu politikalarıdır; buradaki
/// liste yalnızca arayüzü şekillendirir. Kurallar iki yerde tanımlı olmasın
/// diye eşleme burada, politika tanımının yanında durur.
/// </para>
/// </summary>
public static class YetkiCozucu
{
    public static string[] Coz(IEnumerable<string> roller)
    {
        var r = roller as IReadOnlyCollection<string> ?? roller.ToList();
        var yetkiler = new List<string>();

        bool VarMi(params string[] adaylar) => adaylar.Any(a => r.Contains(a));

        if (VarMi(UserRoles.Admin, UserRoles.Sekreter, UserRoles.Yonetici,
                  UserRoles.Baskan, UserRoles.Cicek, UserRoles.Medya))
        {
            yetkiler.Add(AuthPolicyNames.OzelKalem);
        }

        if (VarMi(UserRoles.Admin, UserRoles.Sekreter, UserRoles.Yonetici, UserRoles.Baskan))
        {
            yetkiler.Add(AuthPolicyNames.Ajanda);
        }

        if (VarMi(UserRoles.Admin, UserRoles.Baskan, UserRoles.Sibeski))
        {
            yetkiler.Add(AuthPolicyNames.Sibeski);
        }

        if (VarMi(UserRoles.Admin, UserRoles.Baskan, UserRoles.Medya))
        {
            yetkiler.Add(AuthPolicyNames.Medya);
        }

        if (VarMi(UserRoles.Admin, UserRoles.Baskan, UserRoles.Cicek))
        {
            yetkiler.Add(AuthPolicyNames.Cicek);
        }

        return [.. yetkiler];
    }
}
