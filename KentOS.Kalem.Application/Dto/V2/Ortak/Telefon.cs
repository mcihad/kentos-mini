using System.Linq;

namespace KentOS.Kalem.Application.Dto.V2.Ortak;

/// <summary>
/// Telefon numarasının tek normalleştirme noktası.
/// </summary>
/// <remarks>
/// <para>
/// GERÇEK HATA: doğrulayıcı bitişik <c>0XXXXXXXXXX</c> istiyordu ama
/// veritabanındaki numaralar boşlukluydu (<c>0541 298 34 50</c>) — eski MVC
/// formu öyle kaydediyordu. Bir kullanıcıyı açıp hiçbir şeye dokunmadan
/// kaydetmek <b>400</b> veriyordu ve hata mesajı da telefonu işaret ettiği
/// için kimse alanı değiştirmemişken suçlanan alan oydu.
/// </para>
/// <para>
/// Çözüm biçimi ZORLAMAK değil, temizlemek: kullanıcı nasıl yazarsa yazsın
/// (boşluk, tire, parantez, <c>+90</c>, başında <c>0</c> olmadan) tek biçime
/// indiriyoruz. Reddetmek yalnızca gerçekten numara olmayan girdiler için.
/// </para>
/// </remarks>
public static class Telefon
{
    /// <summary>
    /// Numarayı <c>0XXXXXXXXXX</c> biçimine indirir; çözemezse girdiyi
    /// kırpılmış hâlde geri verir (doğrulayıcı reddetsin diye).
    /// </summary>
    public static string? Duzelt(string? ham)
    {
        if (string.IsNullOrWhiteSpace(ham)) return null;

        var rakamlar = new string(ham.Where(char.IsDigit).ToArray());

        // +90 / 90 önekini at.
        if (rakamlar.Length == 12 && rakamlar.StartsWith("90"))
        {
            rakamlar = rakamlar[2..];
        }

        // Başında 0 olmadan yazılan 10 hane (5551112233).
        if (rakamlar.Length == 10 && rakamlar.StartsWith('5'))
        {
            rakamlar = "0" + rakamlar;
        }

        return rakamlar.Length == 0 ? ham.Trim() : rakamlar;
    }

    /// <summary>
    /// GÖSTERİLECEK biçim: <c>0532 111 22 33</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Aynı numara veritabanında dört ayrı yazımla duruyor
    /// (<c>05412983451</c>, <c>0541 298 34 50</c>, <c>+90 541 298 34 52</c>,
    /// <c>541 298 34 50</c>) — eski MVC formu serbest metin alıyordu. Basılı
    /// ya da süzülen bir listede alt alta dört biçim, telefonu çeviren kişiyi
    /// her satırda yeniden okumaya zorluyor ve iki kaydın aynı kişi olup
    /// olmadığı bakışta anlaşılmıyor.
    /// </para>
    /// <para>
    /// <b>11 hane ve <c>0</c> ile başlamıyorsa DOKUNULMAZ</b> — yabancı
    /// numarayı ya da kısa hattı bozmaktansa olduğu gibi göstermek doğru.
    /// </para>
    /// <para>
    /// Kural bir dönem <c>HalkGunuCiktiServisi</c> içinde özel bir metottu ve
    /// yalnızca halk günü çıktılarında uygulanıyordu; liste çıktıları
    /// eklenirken kopyalanmak yerine buraya çıkarıldı. İstemcideki karşılığı
    /// <c>data/format.ts → phone()</c>.
    /// </para>
    /// </remarks>
    public static string Bicimle(string? ham)
    {
        var sade = Duzelt(ham);
        if (sade is null) return string.Empty;

        var rakam = new string(sade.Where(char.IsDigit).ToArray());

        return rakam.Length == 11 && rakam.StartsWith('0')
            ? $"{rakam[..4]} {rakam[4..7]} {rakam[7..9]} {rakam[9..]}"
            : sade;
    }
}
