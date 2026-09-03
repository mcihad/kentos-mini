using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto.V2.Ortak;

/// <summary>
/// Sayfalı liste isteği.
///
/// <para>
/// v2'de <b>hiçbir liste ucu sınırsız veri döndürmez</b>. Sistem iki yıldır
/// canlıda ve bazı tablolar binlerce satır taşıyor; sınırsız bir uç, tek bir
/// istekle sunucuyu da tarayıcıyı da kilitleyebilir. v1'de bu koruma yok ama
/// orası mobil sözleşmesi olduğu için dokunulmuyor.
/// </para>
/// </summary>
public class SayfaIstegi
{
    private int _sayfa = 1;
    private int _boyut = VarsayilanBoyut;

    /// <summary>Varsayılan sayfa boyutu.</summary>
    public const int VarsayilanBoyut = 50;

    /// <summary>
    /// En büyük sayfa boyutu.
    /// </summary>
    /// <remarks>
    /// 200: açılır listelerin (durum, tip, birim) tamamı tek sayfaya sığar,
    /// ama binlerce satırlık bir tablo tek istekte çekilemez. Daha büyük
    /// listeler (mahalle, meslek) <c>ara</c> parametresiyle süzülür.
    /// </remarks>
    public const int EnBuyukBoyut = 200;

    /// <summary>1'den başlar. Küçük değerler 1'e çekilir.</summary>
    [JsonPropertyName("sayfa")]
    public int Sayfa
    {
        get => _sayfa;
        set => _sayfa = value < 1 ? 1 : value;
    }

    /// <summary>Sayfa başına kayıt. <see cref="EnBuyukBoyut"/> ile sınırlanır.</summary>
    [JsonPropertyName("boyut")]
    public int Boyut
    {
        get => _boyut;
        set => _boyut = value switch
        {
            < 1 => VarsayilanBoyut,
            > EnBuyukBoyut => EnBuyukBoyut,
            _ => value,
        };
    }

    /// <summary>Serbest metin arama. Hangi alanlarda arandığı uca özgüdür.</summary>
    [JsonPropertyName("ara")]
    public string? Ara { get; set; }

    /// <summary>Sıralama alanı. Tanınmayan değer yok sayılır, varsayılana düşer.</summary>
    [JsonPropertyName("sirala")]
    public string? Sirala { get; set; }

    /// <summary>Azalan sıralama.</summary>
    [JsonPropertyName("azalan")]
    public bool Azalan { get; set; }

    /// <summary>Atlanacak kayıt sayısı.</summary>
    [JsonIgnore]
    public int Atla => (Sayfa - 1) * Boyut;

    /// <summary>Aramanın normalize hâli (boş metin null sayılır).</summary>
    [JsonIgnore]
    public string? TemizArama =>
        string.IsNullOrWhiteSpace(Ara) ? null : Ara.Trim();
}

/// <summary>
/// Sayfalı yanıt.
///
/// <para>
/// <c>Toplam</c> her zaman doldurulur: istemcinin "kaç kayıt var" sorusuna
/// cevap veremezse ne sayfa numarası gösterebilir ne de "sonuç yok" ile
/// "sayfa dışı" durumunu ayırt edebilir.
/// </para>
/// </summary>
public class SayfaliSonuc<T>
{
    [JsonPropertyName("veriler")]
    public List<T> Veriler { get; set; } = [];

    [JsonPropertyName("sayfa")]
    public int Sayfa { get; set; } = 1;

    [JsonPropertyName("boyut")]
    public int Boyut { get; set; } = SayfaIstegi.VarsayilanBoyut;

    /// <summary>Süzgeçlerden sonraki TOPLAM kayıt sayısı (bu sayfadaki değil).</summary>
    [JsonPropertyName("toplam")]
    public long Toplam { get; set; }

    [JsonPropertyName("toplamSayfa")]
    public int ToplamSayfa => Boyut < 1 ? 0 : (int)Math.Ceiling(Toplam / (double)Boyut);

    [JsonPropertyName("oncekiVar")]
    public bool OncekiVar => Sayfa > 1;

    [JsonPropertyName("sonrakiVar")]
    public bool SonrakiVar => Sayfa < ToplamSayfa;

    public static SayfaliSonuc<T> Olustur(List<T> veriler, long toplam, SayfaIstegi istek) => new()
    {
        Veriler = veriler,
        Sayfa = istek.Sayfa,
        Boyut = istek.Boyut,
        Toplam = toplam,
    };

    /// <summary>Belleğe alınmış bir listeyi sayfalar.</summary>
    /// <remarks>
    /// Yalnızca kaynak sorgu <c>IQueryable</c> DEĞİLKEN kullanılır (mevcut
    /// servisler <c>IEnumerable</c> döndürüyor). Veritabanı düzeyinde
    /// sayfalamak her zaman daha iyidir; yeni kod için
    /// <c>SayfalamaUzantilari.SayfalaAsync</c> tercih edilir.
    /// </remarks>
    public static SayfaliSonuc<T> Bellekten(IEnumerable<T> kaynak, SayfaIstegi istek)
    {
        var liste = kaynak as IReadOnlyList<T> ?? kaynak.ToList();
        return new SayfaliSonuc<T>
        {
            Veriler = liste.Skip(istek.Atla).Take(istek.Boyut).ToList(),
            Sayfa = istek.Sayfa,
            Boyut = istek.Boyut,
            Toplam = liste.Count,
        };
    }
}
