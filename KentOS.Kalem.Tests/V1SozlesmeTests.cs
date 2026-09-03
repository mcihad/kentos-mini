using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using KentOS.Kalem.Application.Dto;

namespace KentOS.Kalem.Tests;

/// <summary>
/// v1 API'nin JSON sözleşmesini DONDURUR.
///
/// <para>
/// NEDEN: <c>Controllers/Api/*</c> iki yıldır sahadaki Flutter uygulamasının
/// konuştuğu arayüz. Bir alanın adı değişir, kaldırılır ya da
/// <c>[JsonPropertyName]</c> düşerse mobil uygulama <b>sessizce</b> bozulur —
/// derleme başarılı olur, testler yeşil kalır, hata yalnızca kullanıcının
/// telefonunda görülür.
/// </para>
///
/// <para>
/// Bu test sunucu ayağa kaldırmaz ve veritabanı istemez: yalnızca DTO'ların
/// serileştirilmiş alan adlarını yansımayla okur ve beklenen kümeyle
/// karşılaştırır. Yeni alan EKLEMEK serbesttir (geriye dönük uyumlu); alan
/// çıkarmak veya yeniden adlandırmak testi düşürür.
/// </para>
///
/// <para>
/// Bir alanı bilerek değiştiriyorsanız: önce mobil tarafta karşılığını
/// güncelleyin, sürümü yayınlayın, SONRA buradaki listeyi güncelleyin.
/// </para>
/// </summary>
public class V1SozlesmeTests
{
    /// <summary>
    /// Bir tipin JSON'a çıkacak alan adlarını üretir —
    /// <c>[JsonPropertyName]</c> varsa onu, yoksa camelCase karşılığını alır.
    /// <c>[JsonIgnore]</c> işaretliler dışarıda kalır.
    /// </summary>
    private static HashSet<string> JsonAlanlari(Type tip)
    {
        var adlar = new HashSet<string>(StringComparer.Ordinal);

        foreach (var p in tip.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetCustomAttribute<JsonIgnoreAttribute>() is not null) continue;
            if (p.GetMethod is null || !p.GetMethod.IsPublic) continue;

            var ad = p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                     ?? JsonNamingPolicy.CamelCase.ConvertName(p.Name);
            adlar.Add(ad);
        }

        return adlar;
    }

    private static void SozlesmeyiDogrula(Type tip, params string[] beklenen)
    {
        var gercek = JsonAlanlari(tip);
        var eksik = beklenen.Where(a => !gercek.Contains(a)).ToList();

        Assert.True(
            eksik.Count == 0,
            $"{tip.Name}: mobil uygulamanın beklediği alan(lar) KAYIP → {string.Join(", ", eksik)}. " +
            $"Mevcut alanlar: {string.Join(", ", gercek.OrderBy(x => x))}. " +
            "v1 sözleşmesi değiştirilemez; yeni alanları v2'ye ekleyin.");
    }

    // ------------------------------------------------------------------ ajanda

    [Fact]
    public void AjandaDto_mobil_alanlarini_korur()
    {
        SozlesmeyiDogrula(typeof(AjandaDto),
            "id", "baslik", "aciklama", "konum", "koordinat",
            "baslangicTarihi", "bitisTarihi", "tumGun",
            "irtibatKisi", "irtibatTelefon",
            "basinKatilsin", "konusmaMetniDurum", "bilgiNotuDurum", "resimVar",
            "tekrarEden", "isDeleted", "bilgiNotu", "konusmaMetni",
            "olusturmaTarihi", "guncellemeTarihi",
            "kullaniciId", "birimId", "randevuId",
            "randevuTipId", "durumId", "durum", "cicekId",
            "photos", "cicek", "status",
            // gizlilik
            "gizli", "katilimcilar", "katilimciIdler",
            // tekrar
            "seriId", "seriOrijinalBaslangic", "seriAyrik",
            "tekrarKurali", "tekrarOzeti", "tekrarBitisi",
            "tekrar", "tekrarKaldir", "kapsam");
    }

    [Fact]
    public void AjandaNotDto_mobil_alanlarini_korur()
        => SozlesmeyiDogrula(typeof(AjandaNotDto),
            "id", "not", "ajandaId", "olusturan", "olusturulmaTarihi");

    [Fact]
    public void AjandaPhotoDto_mobil_alanlarini_korur()
        => SozlesmeyiDogrula(typeof(AjandaPhotoDto),
            "id", "filename", "contentType", "ajandaId");

    [Fact]
    public void AjandaSeriDto_mobil_alanlarini_korur()
        => SozlesmeyiDogrula(typeof(AjandaSeriDto),
            "id", "rrule", "ozet", "dtstart", "sureDakika",
            "bitisTarihi", "tekrarSayisi", "uretilenSonTarih",
            "iptal", "uretilenAdet", "ilkAjandaId");

    [Fact]
    public void KatilimciDto_mobil_alanlarini_korur()
        => SozlesmeyiDogrula(typeof(KatilimciDto),
            "id", "ad", "soyad", "unvan", "birimAd", "tamAd");

    // ------------------------------------------------------------------ oturum

    [Fact]
    public void LoginDto_mobil_alanlarini_korur()
        => SozlesmeyiDogrula(typeof(LoginDto), "username", "password");

    /// <remarks>
    /// Dikkat: C# özelliği <c>Expiration</c> ama JSON adı <c>validTo</c>
    /// (<c>[JsonPropertyName]</c> ile). Mobil taraf da <c>json['validTo']</c>
    /// okuyor — ikisi tutarlı. C# adını sözleşme sanmak kolay bir hata.
    /// </remarks>
    [Fact]
    public void LoginResponseDto_mobil_alanlarini_korur()
        => SozlesmeyiDogrula(typeof(LoginResponseDto), "token", "validTo");

    /// <remarks>
    /// <c>userName</c> YOK — mobil `UserModel` yalnızca aşağıdaki alanları
    /// okuyor (bkz. workcollab/lib/models/user_model.dart).
    /// </remarks>
    [Fact]
    public void UserDto_mobil_alanlarini_korur()
        => SozlesmeyiDogrula(typeof(UserDto),
            "id", "ad", "soyad", "unvan", "email", "telefon",
            "birimId", "ustBirimId", "birimAd", "fcmToken", "roles",
            // Sonradan EKLENDİ (geriye dönük uyumlu): mobil, gizlilik
            // anahtarını ve dosya gönderme düğmesini bu bayraklara göre
            // gösteriyor.
            "gizliEtkinlikEkleyebilir", "dosyaGonderebilir");

    // ------------------------------------------------------------------- talep

    [Fact]
    public void RandevuDto_mobil_alanlarini_korur()
        => SozlesmeyiDogrula(typeof(RandevuDto),
            "id", "konu", "ad", "soyad", "telefon", "email", "adres",
            "baslangicTarih", "bitisTarih", "aciklama",
            "birimId", "randevuTipId", "randevuDurumId", "mahalleId");

    // --------------------------------------------------------------- bildirim

    [Fact]
    public void TokenDataDto_bildirim_sozlesmesini_korur()
        => SozlesmeyiDogrula(typeof(TokenDataDto), "entity", "id", "action");

    // ------------------------------------------------------- yeni alan eklemek

    [Fact]
    public void Yeni_alan_eklemek_sozlesmeyi_bozmaz()
    {
        // Sözleşme "en az bu alanlar" diye okunur. Test yalnızca EKSİLMEYİ
        // yakalar; eklemek geriye dönük uyumludur ve serbesttir. Bu test o
        // niyeti belgeler — biri testi "tam eşitlik"e çevirmek isterse burada
        // neden öyle olmadığını görsün.
        var alanlar = JsonAlanlari(typeof(AjandaDto));
        Assert.Contains("baslik", alanlar);
        Assert.True(alanlar.Count >= 30, "AjandaDto beklenenden az alan taşıyor.");
    }
}
