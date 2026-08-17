using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;

namespace KentOS.Mini.Web.Services.V2;

/// <summary>Günlük programın HTML çıktısı için tek satır.</summary>
public record ProgramSatirVerisi(
    [property: JsonPropertyName("baslangic")] DateTime Baslangic,
    [property: JsonPropertyName("bitis")] DateTime? Bitis,
    [property: JsonPropertyName("tumGun")] bool TumGun,
    [property: JsonPropertyName("baslik")] string Baslik,
    [property: JsonPropertyName("konum")] string? Konum,
    [property: JsonPropertyName("tip")] string? Tip,
    [property: JsonPropertyName("tipRenk")] string? TipRenk,
    [property: JsonPropertyName("durum")] string? Durum,
    [property: JsonPropertyName("durumRenk")] string? DurumRenk,
    [property: JsonPropertyName("irtibatKisi")] string? IrtibatKisi,
    [property: JsonPropertyName("irtibatTelefon")] string? IrtibatTelefon,
    [property: JsonPropertyName("basinKatilsin")] bool BasinKatilsin,
    [property: JsonPropertyName("konusmaMetni")] bool KonusmaMetni,
    [property: JsonPropertyName("bilgiNotu")] bool BilgiNotu);

/// <summary>
/// Günlük programın <b>yazdırılabilir HTML</b> çıktısı.
///
/// <para>
/// PDF'in yanında HTML de var çünkü ikisinin işi farklı: PDF e-postayla
/// gönderilen sabit bir belge, HTML ise tarayıcıda görülüp <b>yazıcı
/// ayarlarıyla</b> basılan bir sayfa. Eski arayüzde yalnızca HTML vardı ve
/// paylaşmak için ekran görüntüsü alınıyordu.
/// </para>
///
/// <para>
/// Çıktı tek dosya: dış CSS/JS yok, yazı tipi sistem yazı tipi. Belediye
/// ağındaki bir yazıcı bilgisayarında internet olmayabilir; dış kaynağa
/// bağımlı bir sayfa orada bozuk basılırdı.
/// </para>
/// </summary>
public static class GunlukProgramHtml
{
    private static readonly CultureInfo Tr = new("tr-TR");

    /// <summary>HTML'e gömülecek her metin buradan geçer.</summary>
    private static string G(string? metin) => WebUtility.HtmlEncode(metin ?? string.Empty);

    private static string Saat(ProgramSatirVerisi s) =>
        s.TumGun
            ? "Tüm gün"
            : s.Bitis is null
                ? s.Baslangic.ToString("HH:mm")
                : $"{s.Baslangic:HH:mm} – {s.Bitis:HH:mm}";

    /// <summary>Renk yoksa kurumsal lacivert; geçersiz değerler elenir.</summary>
    private static string Renk(string? ham, string yedek = "#002E6D")
    {
        if (string.IsNullOrWhiteSpace(ham)) return yedek;
        var t = ham.Trim();
        return System.Text.RegularExpressions.Regex.IsMatch(t, "^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")
            ? t : yedek;
    }

    /// <param name="birim">
    /// Başlıktaki birim — ÇIKTIYI ALANIN birimi. Sabit "BAŞKANLIK MAKAMI"
    /// yazıyordu; program kâğıdı elden ele dolaşıyor ve hangi birimin günü
    /// olduğunu doğru söylemeli.
    /// </param>
    /// <param name="kurumAdi">
    /// Başlıktaki kurum. KODA YAZILMAZ — kurum kaydından gelir; uygulama
    /// başka belediyelerde de çalışacak.
    /// </param>
    /// <param name="amblem">
    /// Amblemin web yolu. Boşsa logo alanı hiç çizilmez: var olmayan bir
    /// dosyaya işaret eden kırık resim simgesi, logosuz çıktıdan kötü.
    /// </param>
    public static string Uret(
        DateTime gun,
        ProgramTasarimi tasarim,
        List<ProgramSatirVerisi> satirlar,
        string birim,
        string kurumAdi,
        string? amblem)
    {
        var govde = tasarim switch
        {
            ProgramTasarimi.Kompakt => Kompakt(satirlar),
            ProgramTasarimi.Detayli => Detayli(satirlar),
            ProgramTasarimi.BosNot => BosNot(satirlar),
            ProgramTasarimi.SaatSeridi => SaatSeridi(satirlar),
            ProgramTasarimi.Pano => Pano(satirlar),
            _ => Standart(satirlar),
        };

        var baslik = tasarim switch
        {
            ProgramTasarimi.Detayli => "GÜNLÜK PROGRAM — DETAYLI",
            ProgramTasarimi.BosNot => "GÜNLÜK PROGRAM — NOT SAYFASI",
            ProgramTasarimi.Pano => "GÜNLÜK PROGRAM",
            _ => "GÜNLÜK PROGRAM",
        };

        // Pano tasarımı duvara asılıyor: yatay sayfa ve büyük punto.
        var yatay = tasarim == ProgramTasarimi.Pano;

        return Iskelet(gun, baslik, govde, satirlar.Count, yatay, tasarim, birim, kurumAdi, amblem);
    }

    // ─────────────────────────────────────────────────── tasarımlar

    private static string Standart(List<ProgramSatirVerisi> s)
    {
        if (s.Count == 0) return BosMesaj();

        var sb = new StringBuilder();
        sb.Append("<table class='p'><thead><tr><th style='width:120px'>Saat</th><th>Konu</th><th style='width:30%'>Yer</th></tr></thead><tbody>");

        foreach (var e in s)
        {
            sb.Append("<tr>")
              .Append($"<td class='saat'>{G(Saat(e))}</td>")
              .Append("<td>")
              .Append($"<span class='cubuk' style='background:{Renk(e.DurumRenk, Renk(e.TipRenk))}'></span>")
              .Append($"<span class='konu'>{G(e.Baslik)}</span>")
              .Append(EtiketSatiri(e))
              .Append("</td>")
              .Append($"<td class='yer'>{G(e.Konum ?? "—")}</td>")
              .Append("</tr>");
        }

        return sb.Append("</tbody></table>").ToString();
    }

    private static string Kompakt(List<ProgramSatirVerisi> s)
    {
        if (s.Count == 0) return BosMesaj();

        var sb = new StringBuilder("<div class='kompakt'>");
        foreach (var e in s)
        {
            sb.Append("<div class='k-satir'>")
              .Append($"<div class='k-saat'>{G(Saat(e))}</div>")
              .Append("<div class='k-govde'>")
              .Append($"<div class='k-konu'>{G(e.Baslik)}</div>")
              .Append(string.IsNullOrWhiteSpace(e.Konum) ? "" : $"<div class='k-yer'>{G(e.Konum)}</div>")
              .Append("</div></div>");
        }
        return sb.Append("</div>").ToString();
    }

    private static string Detayli(List<ProgramSatirVerisi> s)
    {
        if (s.Count == 0) return BosMesaj();

        var sb = new StringBuilder();
        sb.Append("<table class='p'><thead><tr>")
          .Append("<th style='width:110px'>Saat</th><th>Konu</th><th style='width:20%'>Yer</th>")
          .Append("<th style='width:20%'>İrtibat</th><th style='width:90px'>Hazırlık</th>")
          .Append("</tr></thead><tbody>");

        foreach (var e in s)
        {
            var irtibat = string.Join(" · ", new[] { e.IrtibatKisi, e.IrtibatTelefon }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            var hazirlik = string.Join(" ", new[]
            {
                e.BasinKatilsin ? "BASIN" : null,
                e.KonusmaMetni ? "KM" : null,
                e.BilgiNotu ? "BN" : null,
            }.Where(x => x is not null));

            sb.Append("<tr>")
              .Append($"<td class='saat'>{G(Saat(e))}</td>")
              .Append("<td>")
              .Append($"<span class='cubuk' style='background:{Renk(e.DurumRenk, Renk(e.TipRenk))}'></span>")
              .Append($"<span class='konu'>{G(e.Baslik)}</span>")
              .Append(EtiketSatiri(e))
              .Append("</td>")
              .Append($"<td class='yer'>{G(e.Konum ?? "—")}</td>")
              .Append($"<td class='yer'>{G(irtibat.Length > 0 ? irtibat : "—")}</td>")
              .Append($"<td class='hazirlik'>{G(hazirlik.Length > 0 ? hazirlik : "—")}</td>")
              .Append("</tr>");
        }

        return sb.Append("</tbody></table>").ToString();
    }

    private static string BosNot(List<ProgramSatirVerisi> s)
    {
        var sb = new StringBuilder();
        sb.Append("<table class='p not'><thead><tr><th style='width:110px'>Saat</th><th style='width:35%'>Konu</th><th>Notlar</th></tr></thead><tbody>");

        foreach (var e in s)
        {
            sb.Append("<tr>")
              .Append($"<td class='saat'>{G(Saat(e))}</td>")
              .Append($"<td class='konu'>{G(e.Baslik)}</td>")
              .Append("<td class='cizgili'></td>")
              .Append("</tr>");
        }

        // Program bittikten sonra serbest not alanı.
        for (var i = 0; i < 5; i++)
        {
            sb.Append("<tr><td></td><td></td><td class='cizgili'></td></tr>");
        }

        return sb.Append("</tbody></table>").ToString();
    }

    /// <summary>Dikey saat şeridi — günün akışını görsel olarak gösterir.</summary>
    private static string SaatSeridi(List<ProgramSatirVerisi> s)
    {
        if (s.Count == 0) return BosMesaj();

        var sb = new StringBuilder("<div class='serit'>");
        foreach (var e in s)
        {
            var renk = Renk(e.DurumRenk, Renk(e.TipRenk));
            sb.Append("<div class='s-satir'>")
              .Append($"<div class='s-saat'>{G(e.TumGun ? "—" : e.Baslangic.ToString("HH:mm"))}")
              .Append(e.TumGun || e.Bitis is null ? "" : $"<span>{G(e.Bitis.Value.ToString("HH:mm"))}</span>")
              .Append("</div>")
              .Append($"<div class='s-nokta'><span style='background:{renk}'></span></div>")
              .Append("<div class='s-govde'>")
              .Append($"<div class='s-konu'>{G(e.Baslik)}</div>")
              .Append(string.IsNullOrWhiteSpace(e.Konum) ? "" : $"<div class='s-yer'>{G(e.Konum)}</div>")
              .Append(EtiketSatiri(e))
              .Append("</div></div>");
        }
        return sb.Append("</div>").ToString();
    }

    /// <summary>Pano — duvara asılan, uzaktan okunan büyük punto liste.</summary>
    private static string Pano(List<ProgramSatirVerisi> s)
    {
        if (s.Count == 0) return BosMesaj();

        var sb = new StringBuilder("<div class='pano'>");
        foreach (var e in s)
        {
            var renk = Renk(e.DurumRenk, Renk(e.TipRenk));
            sb.Append($"<div class='p-satir' style='border-left-color:{renk}'>")
              .Append($"<div class='p-saat'>{G(Saat(e))}</div>")
              .Append("<div class='p-govde'>")
              .Append($"<div class='p-konu'>{G(e.Baslik)}</div>")
              .Append(string.IsNullOrWhiteSpace(e.Konum) ? "" : $"<div class='p-yer'>{G(e.Konum)}</div>")
              .Append("</div></div>");
        }
        return sb.Append("</div>").ToString();
    }

    /// <summary>
    /// Hazırlık etiketleri — konu satırının altında.
    /// </summary>
    /// <remarks>
    /// <b>Etkinlik tipi ve durumu basılmaz.</b> İkisi de iç takip alanı:
    /// "Beklemede"/"Onaylandı" programı hazırlayanın iş akışını anlatır,
    /// programı elinde tutan kişinin işine yaramaz. Basılı kâğıt makam
    /// odasında elden ele dolaşıyor ve "Beklemede" damgalı bir satır orada
    /// yanlış bir şey söylüyordu. Renk şeridi duruyor — o, satırları
    /// birbirinden ayırmaya yarıyor, bir durum ilan etmiyor.
    /// </remarks>
    private static string EtiketSatiri(ProgramSatirVerisi e)
    {
        var parcalar = new List<string>();

        if (e.BasinKatilsin) parcalar.Add("<span class='etiket'>Basın</span>");
        if (e.KonusmaMetni) parcalar.Add("<span class='etiket'>Konuşma metni</span>");
        if (e.BilgiNotu) parcalar.Add("<span class='etiket'>Bilgi notu</span>");

        return parcalar.Count == 0 ? "" : $"<div class='etiketler'>{string.Join("", parcalar)}</div>";
    }

    private static string BosMesaj() =>
        "<p class='bos'>Bu güne ait program kaydı bulunmuyor.</p>";

    // ────────────────────────────────────────────────────── iskelet

    private static string Iskelet(
        DateTime gun, string baslik, string govde, int adet, bool yatay,
        ProgramTasarimi tasarim, string birim, string kurumAdi, string? amblem)
    {
        var gunMetni = gun.ToString("d MMMM yyyy dddd", Tr);
        var uretim = DateTime.Now.ToString("dd.MM.yyyy HH:mm", Tr);

        return $$"""
<!doctype html>
<html lang="tr">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Günlük Program — {{G(gun.ToString("dd.MM.yyyy"))}}</title>
<style>
  /*
   * Yazdırma için kurulmuş tek dosyalık sayfa: dış CSS/font YOK.
   * Belediye ağındaki yazıcı bilgisayarında internet olmayabilir; dış
   * kaynağa bağlı bir sayfa orada bozuk basılır.
   */
  @page { size: A4 {{(yatay ? "landscape" : "portrait")}}; margin: 14mm; }

  * { box-sizing: border-box; }
  body {
    margin: 0; padding: 18px;
    font-family: "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
    color: #141C2B; background: #F3F2EE;
    font-variant-numeric: tabular-nums;
    -webkit-print-color-adjust: exact; print-color-adjust: exact;
  }
  .kagit {
    max-width: {{(yatay ? "1180px" : "820px")}};
    margin: 0 auto; background: #fff; padding: 26px 30px;
    box-shadow: 0 2px 14px rgba(16,26,45,.08); border-radius: 10px;
  }

  header { text-align: center; }
  /* Logo SOLDA, kurum adı ortada — eski çıktının yerleşimi. */
  .ustSerit { display: flex; align-items: center; gap: 12px; }
  .ustSerit .logo { width: 62px; height: auto; flex: 0 0 62px; }
  /* Sağda logo kadar boşluk: başlık gerçekten ortalanır. */
  .ustSerit .denge { flex: 0 0 62px; }
  .ustSerit .orta { flex: 1 1 auto; }
  .kurum { font-size: 12px; font-weight: 700; letter-spacing: .1em; color: #002E6D; }
  .makam { font-size: 10px; letter-spacing: .08em; color: #6B675F; margin-top: 2px; }
  h1 { font-size: {{(yatay ? "26px" : "20px")}}; margin: 14px 0 4px; color: #002E6D; letter-spacing: -.01em; }
  .gun { font-size: 13px; color: #5A6478; }
  .kural { height: 2px; background: #A78952; margin: 12px 0 0; border: 0; }

  /* ── ortak tablo ── */
  table.p { width: 100%; border-collapse: collapse; margin-top: 16px; }
  table.p th {
    background: #002E6D; color: #fff; font-size: 11px; font-weight: 700;
    text-align: left; padding: 8px 10px; letter-spacing: .04em;
  }
  table.p td { padding: 9px 10px; border-bottom: 1px solid #E3E1DA; vertical-align: top; font-size: 12.5px; }
  table.p tbody tr:nth-child(even) td { background: #FAF9F6; }
  .saat { font-weight: 700; white-space: nowrap; font-size: 13px; }
  .yer { color: #5A6478; }
  .hazirlik { font-size: 10.5px; color: #5A6478; letter-spacing: .03em; }
  .konu { font-weight: 600; }
  .cubuk {
    display: inline-block; width: 4px; height: 13px; border-radius: 2px;
    margin-right: 7px; vertical-align: -2px;
  }
  .etiketler { margin-top: 5px; display: flex; flex-wrap: wrap; gap: 5px; }
  .etiket {
    display: inline-block; font-size: 9.5px; font-weight: 600; padding: 1px 6px;
    border: 1px solid #CDCAC1; border-radius: 99px; color: #5A6478;
  }

  /* ── boş not sayfası ── */
  table.not td { height: 46px; border: 1px solid #CDCAC1; }
  table.not tbody tr:nth-child(even) td { background: #fff; }
  .cizgili {
    background-image: repeating-linear-gradient(
      to bottom, transparent 0 22px, #EDEBE5 22px 23px);
  }

  /* ── kompakt ── */
  .kompakt { margin-top: 18px; }
  .k-satir { display: flex; gap: 18px; padding: 13px 0; border-bottom: 1px solid #E3E1DA; }
  .k-saat { width: 130px; flex: none; font-size: 17px; font-weight: 700; color: #002E6D; }
  .k-konu { font-size: 16px; font-weight: 600; }
  .k-yer { font-size: 12px; color: #5A6478; margin-top: 3px; }

  /* ── saat şeridi ── */
  .serit { margin-top: 18px; }
  .s-satir { display: flex; gap: 14px; }
  .s-saat {
    width: 62px; flex: none; text-align: right; font-size: 13px; font-weight: 700;
    padding-top: 10px; color: #002E6D;
  }
  .s-saat span { display: block; font-size: 11px; font-weight: 400; color: #8B93A3; }
  .s-nokta { width: 14px; flex: none; position: relative; }
  .s-nokta::before {
    content: ""; position: absolute; left: 6px; top: 0; bottom: 0; width: 1px; background: #E3E1DA;
  }
  .s-nokta span {
    position: absolute; left: 1px; top: 14px; width: 11px; height: 11px;
    border-radius: 99px; border: 2px solid #fff; box-shadow: 0 0 0 1px #E3E1DA;
  }
  .s-satir:first-child .s-nokta::before { top: 14px; }
  .s-satir:last-child .s-nokta::before { bottom: calc(100% - 22px); }
  .s-govde { flex: 1; min-width: 0; padding: 10px 0 16px; }
  .s-konu { font-size: 14px; font-weight: 600; }
  .s-yer { font-size: 12px; color: #5A6478; margin-top: 2px; }

  /* ── pano (duvara asılır) ── */
  .pano { margin-top: 20px; display: grid; gap: 12px; }
  .p-satir {
    display: flex; gap: 22px; align-items: baseline;
    border-left: 6px solid #002E6D; background: #FAF9F6;
    padding: 16px 20px; border-radius: 0 8px 8px 0;
  }
  .p-saat { width: 190px; flex: none; font-size: 24px; font-weight: 700; color: #002E6D; }
  .p-konu { font-size: 22px; font-weight: 600; line-height: 1.25; }
  .p-yer { font-size: 14px; color: #5A6478; margin-top: 4px; }

  .bos { text-align: center; color: #8B93A3; padding: 60px 0; font-size: 13px; }

  footer {
    margin-top: 20px; padding-top: 8px; border-top: 1px solid #CDCAC1;
    display: flex; justify-content: space-between;
    font-size: 10px; color: #8B93A3;
  }

  /* ── yazdırma ── */
  .arac-cubugu {
    max-width: {{(yatay ? "1180px" : "820px")}}; margin: 0 auto 12px;
    display: flex; gap: 8px; justify-content: flex-end;
  }
  .arac-cubugu button {
    font: inherit; font-size: 12.5px; font-weight: 600;
    padding: 8px 14px; border-radius: 8px; cursor: pointer;
    border: 1px solid #CDCAC1; background: #fff; color: #141C2B;
  }
  .arac-cubugu button.birincil { background: #002E6D; color: #fff; border-color: #002E6D; }

  @media print {
    body { background: #fff; padding: 0; }
    .kagit { box-shadow: none; border-radius: 0; padding: 0; max-width: none; }
    .arac-cubugu { display: none; }
    /* Satırlar sayfa sonunda ikiye BÖLÜNMEZ. */
    tr, .k-satir, .s-satir, .p-satir { break-inside: avoid; page-break-inside: avoid; }
    thead { display: table-header-group; }
  }
</style>
</head>
<body>
  <div class="arac-cubugu">
    <button class="birincil" onclick="window.print()">Yazdır</button>
    <button onclick="window.close()">Kapat</button>
  </div>

  <div class="kagit">
    <header>
      <div class="ustSerit">
        {{(string.IsNullOrWhiteSpace(amblem) ? "<div class=\"denge\"></div>" : $"<img class=\"logo\" src=\"{G(amblem)}\" alt=\"\">")}}
        <div class="orta">
          <div class="kurum">{{G(kurumAdi.ToUpper(Tr))}}</div>
          <div class="makam">{{birim.ToUpper(Tr)}}</div>
        </div>
        <div class="denge"></div>
      </div>
      <h1>{{G(baslik)}}</h1>
      <div class="gun">{{G(gunMetni)}}</div>
      <hr class="kural">
    </header>

    {{govde}}

    <footer>
      <span>{{adet}} etkinlik</span>
      <span>{{G(uretim)}}</span>
    </footer>
  </div>

  <script>
    // Yazdırma iletişim kutusu KENDİLİĞİNDEN açılmaz: kullanıcı önce
    // sayfayı görmek, tasarımı beğenmezse başka birini seçmek istiyor.
    document.title = {{System.Text.Json.JsonSerializer.Serialize($"Gunluk-Program-{gun:yyyy-MM-dd}-{tasarim}")}};
  </script>
</body>
</html>
""";
    }
}
