using KentOS.Kalem.Application.Dto.V2.Form;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>
/// Ham JSONB cevabını kullanıcının gördüğü metne çevirir — TEK YER.
/// </summary>
/// <remarks>
/// <para>
/// <b>Seçim alanlarında JSONB'de seçenek KİMLİĞİ duruyor</b> (<c>sec_3</c>,
/// <c>satir_1</c>). Ekranda, Excel'de ve özet raporunda o kimliği göstermek
/// vatandaşa da personele de hiçbir şey söylemiyor; etiket tanımdan
/// çözülmek zorunda.
/// </para>
/// <para>
/// Çeviri bir dönem <b>üç ayrı yerde</b> duruyordu — yanıt listesi/detayı,
/// Excel çıktısı ve özet raporu — ve üçü ayrışmıştı: özet raporundaki kopya
/// etiketlere hiç bakmıyordu, yani matris ve çoklu seçim özetleri
/// <c>satir_1: sutun_2</c> diye okunuyordu. Depodaki
/// <c>KatilimcilariEsitleAsync</c> dersinin aynısı: kural değişince
/// kopyalardan biri unutuluyor.
/// </para>
/// </remarks>
internal static class FormDegerMetni
{
    /// <summary>Matris satırlarını ayıran işaret; Excel hücresinde farklı.</summary>
    public const string VarsayilanAyrac = " · ";

    /// <summary>
    /// Cevabı okunur metne çevirir; "Diğer" serbest metni parantez içinde.
    /// </summary>
    public static string Metin(FormAlaniDto alan, object? sarmal, string matrisAyraci = VarsayilanAyrac)
    {
        /*
          SARMALAYICI BURADA AÇILIR.

          Cevap `{ "deger": …, "metin": … }` şeklinde saklanıyor. Açılmasaydı
          sonuç sayfası "deger: Ayşe Yılmaz" yazardı — ölçülmüş bir hata.
        */
        var d = FormDogrulayici.Normalize(FormDogrulayici.Deger(sarmal));
        var serbest = FormDogrulayici.SerbestMetin(sarmal);
        if (d is null) return serbest ?? string.Empty;

        var metin = Coz(alan, d, matrisAyraci);

        // "Diğer" seçildiyse serbest metin PARANTEZ İÇİNDE: kullanıcı ne
        // seçtiğini de ne yazdığını da tek satırda görmeli.
        return serbest is { Length: > 0 } ? $"{metin} ({serbest})" : metin;
    }

    /// <summary>
    /// Excel hücresi — sayılar METNE ÇEVRİLMEZ.
    /// </summary>
    /// <remarks>
    /// Metne çevrilseydi sütun üzerinde ortalama/süzgeç alınamazdı; tablo
    /// zaten sayılmak için var.
    /// </remarks>
    public static object Hucre(FormAlaniDto alan, object? sarmal)
    {
        var d = FormDogrulayici.Normalize(FormDogrulayici.Deger(sarmal));
        if (d is decimal m && FormDogrulayici.SerbestMetin(sarmal) is null or "") return m;

        return Metin(alan, sarmal, " | ");
    }

    private static string Coz(FormAlaniDto alan, object? d, string matrisAyraci)
    {
        /*
          GroupBy — ToDictionary DEĞİL.

          Aynı kimlik hem `Secenekler` hem `Sutunlar` içinde bulunabiliyor:
          tasarımcıda alan tipi seçimden matrise çevrildiğinde eski seçenek
          listesi tanımda kalıyor. `ToDictionary` orada `ArgumentException`
          atıp isteği 500'e düşürürdü.
        */
        var etiketler = (alan.Secenekler ?? []).Concat(alan.Sutunlar ?? [])
            .GroupBy(x => x.Kimlik)
            .ToDictionary(g => g.Key, g => g.First().Etiket);

        string Cevir(string k) => etiketler.TryGetValue(k, out var e) ? e : k;

        return d switch
        {
            null => string.Empty,
            bool b => b ? "Evet" : "Hayır",
            List<string> l => string.Join(", ", l.Select(Cevir)),

            // Matris: satır etiketleriyle. İç değerler sarmalı DEĞİL,
            // doğrudan seçenek kimliği — bu yüzden `Cevir` ile çevriliyor.
            Dictionary<string, object?> mat => string.Join(matrisAyraci, mat.Select(x =>
            {
                var satirEtiketi = (alan.Satirlar ?? [])
                    .FirstOrDefault(s => s.Kimlik == x.Key)?.Etiket ?? x.Key;
                var ic = FormDogrulayici.Normalize(x.Value);
                var icMetin = ic is List<string> ll
                    ? string.Join(", ", ll.Select(Cevir))
                    : Cevir(ic?.ToString() ?? string.Empty);
                return $"{satirEtiketi}: {icMetin}";
            })),

            string s => Cevir(s),
            _ => d.ToString() ?? string.Empty,
        };
    }
}
