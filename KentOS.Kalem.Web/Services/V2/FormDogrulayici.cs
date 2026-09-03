using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using KentOS.Kalem.Application.Dto.V2.Form;
using KentOS.Kalem.Application.Enums;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>
/// SUNUCU TARAFI FORM DOĞRULAMASI — tanımdan yürütülür.
/// </summary>
/// <remarks>
/// <para>
/// <b>İstemci doğrulaması bir kolaylıktır, kapı burasıdır.</b> Vatandaş
/// yüzeyi anonim; gönderim tarayıcıdan da <c>curl</c>'den de gelebiliyor.
/// Aynı kural nesnesi (<see cref="FormDogrulamaDto"/>) iki yerde okunuyor
/// ama karar yalnızca burada veriliyor.
/// </para>
/// <para>
/// <b>Tanımda olmayan alan REDDEDİLİR</b>, sessizce atılmaz. Atılsaydı
/// gönderim başarılı görünür ve kimse verinin düştüğünü fark etmezdi;
/// üstelik bilinmeyen anahtarları kabul etmek, JSONB'yi saldırganın
/// istediği kadar şişirebileceği bir çöp alanına çevirirdi.
/// </para>
/// </remarks>
public static class FormDogrulayici
{
    /// <summary>Tek bir metin cevabın üst sınırı.</summary>
    /// <remarks>
    /// Alan kendi <c>enCokUzunluk</c>'unu vermese bile bir tavan olmalı:
    /// anonim bir uca 50 MB'lık tek bir metin göndermek, JSONB satırını ve
    /// onunla birlikte her okumayı ağırlaştırırdı.
    /// </remarks>
    public const int MutlakMetinSiniri = 20_000;

    /// <summary>Gövdedeki en çok cevap sayısı.</summary>
    public const int MutlakCevapSayisi = 2_000;

    /// <summary>
    /// Kullanıcı deseni için zaman aşımı.
    /// </summary>
    /// <remarks>
    /// Desen formu kuran yetkiliden geliyor ve kötü yazılmış bir desen
    /// (iç içe yıldız) katastrofik geri izlemeyle isteği kilitleyebilir.
    /// 50 ms, meşru bir desen için fazlasıyla yeterli.
    /// </remarks>
    private static readonly TimeSpan DesenZamanAsimi = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Cevap sarmalayıcısındaki alan adları.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Yanıt belgesi <c>{ "alanKimligi": { "deger": …, "metin": …,
    /// "dosyalar": [...] } }</c> şeklinde. Düz değer de denenebilirdi ama
    /// "Diğer" seçeneğinin yanındaki serbest metni taşıyamıyor; yan bir
    /// <c>alanKimligi__diger</c> anahtarı ise "tanımda olmayan anahtar
    /// reddedilir" kuralıyla çatışırdı.
    /// </para>
    /// <para>
    /// <b>Kısa anahtar (<c>d</c>, <c>m</c>) kullanılmıyor:</b> bayt kazancı
    /// TOAST sıkıştırmasıyla zaten geri geliyor, okunabilirlik ise yıllar
    /// sonra <c>psql</c>'den tek bir kaydı inceleyen kişinin tek şansı.
    /// </para>
    /// <para>
    /// Containment sorgusu bozulmuyor:
    /// <c>cevaplar @&gt; '{"a_7f3a":{"deger":"evet"}}'</c> — GIN indeksi
    /// bunu karşılıyor.
    /// </para>
    /// </remarks>
    public const string DegerAlani = "deger";
    public const string MetinAlani = "metin";
    public const string DosyalarAlani = "dosyalar";

    /// <summary>Sarmalayıcıdan asıl değeri çıkarır.</summary>
    public static object? Deger(object? sarmal)
    {
        var d = Normalize(sarmal);

        if (d is Dictionary<string, object?> m && m.ContainsKey(DegerAlani))
        {
            return m[DegerAlani];
        }

        return d;
    }

    /// <summary>Sarmalayıcıdaki "Diğer" serbest metni.</summary>
    public static string? SerbestMetin(object? sarmal) =>
        Normalize(sarmal) is Dictionary<string, object?> m
            && m.TryGetValue(MetinAlani, out var t)
                ? Normalize(t) as string
                : null;

    /// <summary>Doğrulama sonucu.</summary>
    public sealed record Sonuc(
        bool Gecerli,
        Dictionary<string, string> Hatalar,
        Dictionary<string, object?> TemizCevaplar);

    /// <summary>
    /// Gönderilen cevapları tanıma göre doğrular ve TEMİZLER.
    /// </summary>
    /// <param name="tanim">Yanıtın verildiği sürümün tanımı.</param>
    /// <param name="gelen">İstemcinin gönderdiği ham sözlük.</param>
    /// <param name="taslakMi">
    /// Yarım kayıtta zorunluluk aranmaz; kullanıcı henüz doldurmayı
    /// bitirmedi. Biçim kuralları yine de işler — bozuk veri taslakta da
    /// saklanmamalı.
    /// </param>
    public static Sonuc Dogrula(
        FormTanimiDto tanim,
        Dictionary<string, object?> gelen,
        bool taslakMi = false)
    {
        var hatalar = new Dictionary<string, string>();
        var temiz = new Dictionary<string, object?>();

        if (gelen.Count > MutlakCevapSayisi)
        {
            hatalar["_"] = "Gönderilen cevap sayısı sınırın üzerinde.";
            return new Sonuc(false, hatalar, temiz);
        }

        var alanlar = TumAlanlar(tanim).ToDictionary(a => a.Kimlik, a => a);

        // TANIMDA OLMAYAN ANAHTAR: sessizce atmak yerine reddediyoruz.
        foreach (var anahtar in gelen.Keys)
        {
            if (!alanlar.ContainsKey(anahtar))
            {
                hatalar[anahtar] = "Bu formda böyle bir alan yok.";
            }
        }

        if (hatalar.Count > 0) return new Sonuc(false, hatalar, temiz);

        foreach (var alan in alanlar.Values)
        {
            // İçerik blokları yanıt üretmez.
            if (BlokMu(alan.Tip)) continue;

            // GÖRÜNMEYEN ALAN DOĞRULANMAZ. Koşulu sağlanmayan bir alanın
            // "zorunlu" olması, istemcinin hiç göstermediği bir alan için
            // hata vermek demekti.
            if (!Gorunur(alan, tanim, gelen)) continue;

            gelen.TryGetValue(alan.Kimlik, out var sarmal);
            var deger = Normalize(Deger(sarmal));
            var serbest = SerbestMetin(sarmal);

            if (Bos(deger))
            {
                if (alan.Zorunlu && !taslakMi)
                {
                    hatalar[alan.Kimlik] = "Bu alan zorunlu.";
                }
                continue;
            }

            var hata = AlaniDogrula(alan, deger, out var temizDeger);
            if (hata is not null)
            {
                hatalar[alan.Kimlik] = hata;
                continue;
            }

            /*
              SERBEST METİN yalnızca "Diğer" işaretliyse saklanır.

              Koşulsuz saklansaydı, kullanıcı "Diğer"i seçip sonra vazgeçse
              bile yazdığı metin kayıtta kalır ve raporda görünürdü.
            */
            var digerSecili = serbest is { Length: > 0 }
                && (alan.Secenekler ?? []).Any(x => x.DigerMi
                    && (Metin(temizDeger) == x.Kimlik
                        || (temizDeger is List<string> l && l.Contains(x.Kimlik))));

            var kutu = new Dictionary<string, object?> { [DegerAlani] = temizDeger };

            if (digerSecili)
            {
                kutu[MetinAlani] = serbest!.Length > MutlakMetinSiniri
                    ? serbest[..MutlakMetinSiniri]
                    : serbest;
            }

            temiz[alan.Kimlik] = kutu;
        }

        return new Sonuc(hatalar.Count == 0, hatalar, temiz);
    }

    /// <summary>Tanımdaki bütün alanlar — adım ve grup ayrımı gözetmeden.</summary>
    public static IEnumerable<FormAlaniDto> TumAlanlar(FormTanimiDto tanim) =>
        (tanim.Adimlar ?? []).SelectMany(a => a.Gruplar ?? [])
            .SelectMany(g => g.Alanlar ?? []);

    /// <summary>Bu tip yanıt üretmeyen bir içerik bloğu mu?</summary>
    public static bool BlokMu(FormAlanTipi tip) =>
        tip is FormAlanTipi.Baslik or FormAlanTipi.Aciklama
            or FormAlanTipi.Ayirici or FormAlanTipi.Gorsel;

    // ────────────────────────────────────────────────── koşullu görünürlük

    /// <summary>
    /// Alan (ve kapsayan grubu) şu anki cevaplarla görünür mü?
    /// </summary>
    private static bool Gorunur(
        FormAlaniDto alan, FormTanimiDto tanim, Dictionary<string, object?> cevaplar)
    {
        var grup = (tanim.Adimlar ?? []).SelectMany(a => a.Gruplar ?? [])
            .FirstOrDefault(g => (g.Alanlar ?? []).Any(x => x.Kimlik == alan.Kimlik));

        if (grup?.Kosul is not null && !KosulSaglandi(grup.Kosul, cevaplar)) return false;
        if (alan.Kosul is not null && !KosulSaglandi(alan.Kosul, cevaplar)) return false;

        return true;
    }

    /// <summary>
    /// Bağlaçlı koşulun değerlendirilmesi.
    /// </summary>
    /// <remarks>
    /// Boş kural listesi <b>koşulsuz</b> demektir (görünür): tasarımcıda
    /// "koşul ekle" deyip hiçbir kural yazmayan kullanıcı, alanı
    /// kaybetmemeli.
    /// </remarks>
    public static bool KosulSaglandi(FormKosuluDto kosul, Dictionary<string, object?> cevaplar)
    {
        var kurallar = kosul.Kurallar ?? [];
        if (kurallar.Count == 0) return true;

        return kosul.Baglac == FormKosulBaglaci.Veya
            ? kurallar.Any(k => KuralSaglandi(k, cevaplar))
            : kurallar.All(k => KuralSaglandi(k, cevaplar));
    }

    private static bool KuralSaglandi(FormKosulKuraliDto kural, Dictionary<string, object?> cevaplar)
    {
        cevaplar.TryGetValue(kural.AlanKimligi, out var ham);
        var deger = Normalize(Deger(ham));

        return kural.Operator switch
        {
            FormKosulOperatoru.Dolu => !Bos(deger),
            FormKosulOperatoru.Bos => Bos(deger),
            FormKosulOperatoru.Esit => Metin(deger) == (kural.Deger ?? string.Empty),
            FormKosulOperatoru.EsitDegil => Metin(deger) != (kural.Deger ?? string.Empty),

            // İÇERİR çok seçimde de çalışır: liste değerinde eleman arıyor,
            // metinde alt dize. İkisini ayırmak tasarımcıda iki ayrı
            // operatör göstermek demekti.
            FormKosulOperatoru.Icerir => Icerir(deger, kural.Deger),
            FormKosulOperatoru.IcermeZ => !Icerir(deger, kural.Deger),

            FormKosulOperatoru.Buyuk => Sayi(deger) is { } s1
                && Sayi(kural.Deger) is { } h1 && s1 > h1,
            FormKosulOperatoru.Kucuk => Sayi(deger) is { } s2
                && Sayi(kural.Deger) is { } h2 && s2 < h2,

            _ => true,
        };
    }

    private static bool Icerir(object? deger, string? aranan)
    {
        if (aranan is null) return false;
        if (deger is List<string> liste) return liste.Contains(aranan);
        return Metin(deger).Contains(aranan, StringComparison.OrdinalIgnoreCase);
    }

    // ────────────────────────────────────────────────── alan doğrulaması

    /// <summary>Hata mesajı döner; <c>null</c> ise geçerli.</summary>
    private static string? AlaniDogrula(FormAlaniDto alan, object? deger, out object? temiz)
    {
        temiz = deger;
        var k = alan.Dogrulama;

        switch (alan.Tip)
        {
            case FormAlanTipi.KisaMetin:
            case FormAlanTipi.UzunMetin:
            case FormAlanTipi.Url:
            {
                var m = Metin(deger);
                if (m.Length > MutlakMetinSiniri) return "Girilen metin çok uzun.";
                if (k?.EnAzUzunluk is { } az && m.Length < az) return $"En az {az} karakter girin.";
                if (k?.EnCokUzunluk is { } cok && m.Length > cok) return $"En çok {cok} karakter girebilirsiniz.";
                if (alan.Tip == FormAlanTipi.Url && !Uri.TryCreate(m, UriKind.Absolute, out _))
                    return "Geçerli bir adres girin.";
                var d = DesenHatasi(k, m);
                if (d is not null) return d;
                temiz = m;
                return null;
            }

            case FormAlanTipi.Eposta:
            {
                var m = Metin(deger);
                // Basit ve kasıtlı gevşek: RFC'ye tam uyan bir desen meşru
                // adresleri de eliyor. Asıl doğrulama gerekiyorsa e-postaya
                // kod göndermek gerekir, desen değil.
                if (!m.Contains('@') || m.StartsWith('@') || m.EndsWith('@') || m.Contains(' '))
                    return "Geçerli bir e-posta adresi girin.";
                temiz = m;
                return null;
            }

            case FormAlanTipi.Telefon:
            {
                var rakam = new string(Metin(deger).Where(char.IsDigit).ToArray());
                if (rakam.Length is < 10 or > 13) return "Geçerli bir telefon numarası girin.";
                temiz = Metin(deger);
                return null;
            }

            case FormAlanTipi.TcKimlik:
            {
                var m = new string(Metin(deger).Where(char.IsDigit).ToArray());
                if (!TcGecerli(m)) return "Geçerli bir T.C. kimlik numarası girin.";
                temiz = m;
                return null;
            }

            case FormAlanTipi.Sayi:
            case FormAlanTipi.Olcek:
            case FormAlanTipi.Nps:
            case FormAlanTipi.Yildiz:
            {
                if (Sayi(deger) is not { } s) return "Sayı girin.";
                if (k?.EnAzDeger is { } az && s < az) return $"En az {az} olmalı.";
                if (k?.EnCokDeger is { } cok && s > cok) return $"En çok {cok} olmalı.";
                if (alan.Ayarlar?.EnAz is { } aaz && s < aaz) return $"En az {aaz} olmalı.";
                if (alan.Ayarlar?.EnCok is { } acok && s > acok) return $"En çok {acok} olmalı.";
                temiz = s;
                return null;
            }

            case FormAlanTipi.Tarih:
            case FormAlanTipi.TarihSaat:
            {
                if (!DateTime.TryParse(Metin(deger), CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var t))
                    return "Geçerli bir tarih girin.";
                if (k?.EnAzTarih is { } az && t < az) return $"{az:dd.MM.yyyy} tarihinden önce olamaz.";
                if (k?.EnCokTarih is { } cok && t > cok) return $"{cok:dd.MM.yyyy} tarihinden sonra olamaz.";
                temiz = t;
                return null;
            }

            case FormAlanTipi.Saat:
                if (!TimeSpan.TryParse(Metin(deger), out _)) return "Geçerli bir saat girin.";
                temiz = Metin(deger);
                return null;

            case FormAlanTipi.EvetHayir:
                temiz = Metin(deger).Equals("true", StringComparison.OrdinalIgnoreCase)
                    || Metin(deger) == "1"
                    || Metin(deger).Equals("evet", StringComparison.OrdinalIgnoreCase);
                return null;

            case FormAlanTipi.TekSecim:
            case FormAlanTipi.AcilirListe:
            {
                var m = Metin(deger);
                // SEÇENEK LİSTESİNDE OLMAYAN DEĞER REDDEDİLİR. İstemcinin
                // gönderdiğine güvenilseydi, açılır listeye elle yazılmış
                // herhangi bir metin veritabanına girerdi.
                if (!(alan.Secenekler ?? []).Any(x => x.Kimlik == m))
                    return "Geçersiz seçim.";
                temiz = m;
                return null;
            }

            case FormAlanTipi.CokSecim:
            case FormAlanTipi.CokluAcilirListe:
            case FormAlanTipi.Siralama:
            {
                var liste = Liste(deger);
                var gecerliler = (alan.Secenekler ?? []).Select(x => x.Kimlik).ToHashSet();
                if (liste.Any(x => !gecerliler.Contains(x))) return "Geçersiz seçim.";
                if (k?.EnAzSecim is { } az && liste.Count < az) return $"En az {az} seçim yapın.";
                if (k?.EnCokSecim is { } cok && liste.Count > cok) return $"En çok {cok} seçim yapabilirsiniz.";
                temiz = liste;
                return null;
            }

            case FormAlanTipi.MatrisTekSecim:
            case FormAlanTipi.MatrisCokSecim:
            {
                /*
                  MATRİS: { satirKimligi: sutunKimligi }  ya da
                          { satirKimligi: [sutunKimligi, ...] }

                  Hem satır hem sütun tanımda olmalı. Yalnızca sütun
                  denetlenseydi, olmayan bir satıra cevap yazmak JSONB'ye
                  uydurma anahtar sokardı.
                */
                var satirlar = (alan.Satirlar ?? []).Select(x => x.Kimlik).ToHashSet();
                var sutunlar = (alan.Sutunlar ?? []).Select(x => x.Kimlik).ToHashSet();

                /*
                  MATRİS DEĞERİ SÖZLÜK OLMAK ZORUNDA.

                  `Sozluk()` sözlük olmayan bir değerde BOŞ sözlük dönüyor
                  ve altındaki döngü hiç çalışmıyordu: matris alanına düz
                  bir metin göndermek doğrulamadan geçip JSONB'ye çöp
                  yazıyordu. Sessiz bir hata — istisna yok, uyarı yok.
                  (Bekçi `FormSemaTests.Veri_tasiyan_her_tip_dogrulaniyor`
                  bunu yayına çıkmadan yakaladı.)
                */
                if (deger is not Dictionary<string, object?> sozluk)
                {
                    return "Geçersiz matris cevabı.";
                }

                foreach (var (satir, secim) in sozluk)
                {
                    if (!satirlar.Contains(satir)) return "Geçersiz satır.";

                    var secimler = alan.Tip == FormAlanTipi.MatrisCokSecim
                        ? Liste(secim)
                        : [Metin(secim)];

                    if (secimler.Any(x => !sutunlar.Contains(x))) return "Geçersiz seçim.";
                }

                if (alan.Zorunlu && sozluk.Count < satirlar.Count)
                    return "Tüm satırları işaretleyin.";

                temiz = sozluk;
                return null;
            }

            case FormAlanTipi.Dosya:
                // Dosyanın kendisi ayrı uçtan yükleniyor; burada yalnızca
                // istemcinin gönderdiği kimlik listesi duruyor.
                temiz = Liste(deger);
                return null;

            case FormAlanTipi.Konum:
            case FormAlanTipi.Imza:
            case FormAlanTipi.TarihAraligi:
                temiz = Metin(deger).Length > MutlakMetinSiniri ? null : deger;
                return temiz is null ? "Girilen değer çok uzun." : null;

            default:
                temiz = Metin(deger);
                return null;
        }
    }

    private static string? DesenHatasi(FormDogrulamaDto? k, string metin)
    {
        if (string.IsNullOrWhiteSpace(k?.Desen)) return null;

        try
        {
            if (!Regex.IsMatch(metin, k.Desen, RegexOptions.None, DesenZamanAsimi))
            {
                return k.DesenMesaji ?? "Girilen değer beklenen biçimde değil.";
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // Desen çalışmadıysa alanı GEÇERLİ saymıyoruz: kötü bir desen
            // yüzünden denetimi atlamak, denetimi hiç koymamaktan kötü.
            return "Bu alan doğrulanamadı. Sistem yöneticinize bildirin.";
        }
        catch (ArgumentException)
        {
            return "Bu alan doğrulanamadı. Sistem yöneticinize bildirin.";
        }

        return null;
    }

    /// <summary>T.C. kimlik numarası algoritması.</summary>
    private static bool TcGecerli(string tc)
    {
        if (tc.Length != 11 || tc[0] == '0') return false;

        var r = tc.Select(c => c - '0').ToArray();
        var tek = r[0] + r[2] + r[4] + r[6] + r[8];
        var cift = r[1] + r[3] + r[5] + r[7];

        var onuncu = ((tek * 7) - cift) % 10;
        if (onuncu < 0) onuncu += 10;

        var onbirinci = (r.Take(10).Sum()) % 10;

        return r[9] == onuncu && r[10] == onbirinci;
    }

    // ────────────────────────────────────────────────── değer yardımcıları

    /// <summary>
    /// <c>JsonElement</c>'i düz .NET değerine indirger.
    /// </summary>
    /// <remarks>
    /// Gövde <c>Dictionary&lt;string, object?&gt;</c> olarak bağlanınca her
    /// değer <c>JsonElement</c> geliyor. Tip başına ayrı ayrı çözmek yerine
    /// tek yerde normalleştirmek, her doğrulama kolunun aynı şeyi tekrar
    /// yapmasını önlüyor.
    /// </remarks>
    public static object? Normalize(object? ham) => ham switch
    {
        null => null,
        JsonElement e => e.ValueKind switch
        {
            JsonValueKind.String => e.GetString(),
            JsonValueKind.Number => e.TryGetDecimal(out var d) ? d : null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => e.EnumerateArray()
                .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.ToString())
                .Where(x => x is not null).Select(x => x!).ToList(),
            JsonValueKind.Object => e.EnumerateObject()
                .ToDictionary(p => p.Name, p => Normalize(p.Value)),
            _ => null,
        },
        _ => ham,
    };

    private static bool Bos(object? d) => d switch
    {
        null => true,
        string s => string.IsNullOrWhiteSpace(s),
        List<string> l => l.Count == 0,
        Dictionary<string, object?> m => m.Count == 0,
        _ => false,
    };

    private static string Metin(object? d) => d switch
    {
        null => string.Empty,
        string s => s.Trim(),
        bool b => b ? "true" : "false",
        decimal m => m.ToString(CultureInfo.InvariantCulture),
        _ => d.ToString() ?? string.Empty,
    };

    private static List<string> Liste(object? d) => d switch
    {
        List<string> l => l,
        string s when !string.IsNullOrWhiteSpace(s) => [s],
        IEnumerable<object?> e => e.Select(Metin).Where(x => x.Length > 0).ToList(),
        _ => [],
    };

    private static Dictionary<string, object?> Sozluk(object? d) =>
        d as Dictionary<string, object?> ?? [];

    private static decimal? Sayi(object? d) => d switch
    {
        decimal m => m,
        int i => i,
        double db => (decimal)db,
        string s when decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) => r,
        _ => null,
    };
}
