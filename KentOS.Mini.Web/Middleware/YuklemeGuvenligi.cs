namespace KentOS.Mini.Web.Middleware;

/// <summary>
/// Yüklenen dosyaların TARAYICIDA ÇALIŞMASINI engeller.
/// </summary>
/// <remarks>
/// <para>
/// <b>Kapatılan açık — depolanmış XSS, uygulamanın KENDİ kaynağında.</b>
/// Yükleme uçları dosya adının uzantısını kullanıcıdan alıp diske olduğu
/// gibi yazıyordu (<c>Guid + Path.GetExtension(gelenAd)</c>) ve
/// <c>wwwroot/uploads</c> altındaki her şey <b>kimlik doğrulanmadan</b>
/// servis ediliyor.
/// </para>
/// <para>
/// Ölçülen sömürü: talebe <c>zararli.html</c> eklendi, dosya
/// <c>/uploads/randevu/{guid}.html</c> adresinden <b>jetonsuz</b>,
/// <c>200</c> ve <c>Content-Type: text/html</c> ile indi ve script çalıştı.
/// Sayfa uygulamayla AYNI kaynakta olduğu için
/// <c>localStorage['sv-jetonu']</c>'na erişebiliyor; jeton 15 saat geçerli
/// ve iptal listesi yok — yani tam hesap devri.
/// </para>
/// <para>
/// <b>Neden servis anında, yükleme anında değil:</b> yükleme yolu tek değil
/// (etkinlik fotoğrafı, talep dosyası, özgeçmiş, görev eki, portal
/// fotoğrafı…). Her birine ayrı bir denetim koymak, birini unutmayı davet
/// eder ve <b>diskte zaten duran</b> dosyaları da kurtarmaz. Buradaki tek
/// kural hepsini birden kapatıyor. Yükleme tarafındaki doğrulama yine
/// eklendi ama o, kullanıcıya anlaşılır bir hata vermek için — güvenlik
/// sınırı burası.
/// </para>
/// <para>
/// <b>Sıra önemlidir:</b> <c>UseStaticFiles</c>'tan ÖNCE eklenmelidir;
/// sonra eklenirse statik dosya ara katmanı yanıtı çoktan yazmış olur.
/// <c>YuklemeGuvenligiTests</c> hem kuralı hem sıralamayı bekçiler.
/// </para>
/// </remarks>
public static class YuklemeGuvenligi
{
    /// <summary>Kuralın uygulandığı kök.</summary>
    public const string Yol = "/uploads";

    /// <summary>
    /// Tarayıcıda BELGE olarak çalışabilen uzantılar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Liste "zararlı dosyalar" değil, <b>tarayıcının script çalıştırdığı
    /// bağlamlar</b>. <c>.svg</c> de burada: <c>&lt;img&gt;</c> içinde
    /// zararsız ama adrese doğrudan gidildiğinde içindeki <c>&lt;script&gt;</c>
    /// çalışır. <c>.xml</c> aynı sebeple (XSLT).
    /// </para>
    /// <para>
    /// Sunucuda çalışabilecek uzantılar da (<c>.cshtml</c>, <c>.php</c>,
    /// <c>.aspx</c>) burada: bugünkü kurulumda işlenmiyorlar ama uygulama
    /// başka kurumlarda başka bir yapılandırmayla yayınlanacak.
    /// </para>
    /// </remarks>
    private static readonly HashSet<string> CalisabilirUzantilar =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".html", ".htm", ".xhtml", ".shtml", ".xht",
            ".svg", ".svgz", ".xml", ".xsl", ".xslt",
            ".js", ".mjs", ".cjs", ".css",
            ".swf", ".jar", ".class",
            ".php", ".php3", ".php4", ".php5", ".phtml",
            ".asp", ".aspx", ".ashx", ".asmx", ".cshtml", ".razor",
            ".jsp", ".jspx", ".cgi", ".pl", ".py", ".rb",
            ".exe", ".dll", ".bat", ".cmd", ".com", ".scr", ".msi",
            ".ps1", ".sh", ".vbs", ".hta", ".wsf",
        };

    /// <summary>Bu uzantı tarayıcıda ya da sunucuda çalışabilir mi?</summary>
    public static bool Calisabilir(string? uzanti) =>
        !string.IsNullOrEmpty(uzanti) && CalisabilirUzantilar.Contains(uzanti);

    /// <summary>
    /// İstek yüklenen bir dosyaya gidiyor ve dosya çalışabilir tipte mi?
    /// </summary>
    public static bool Etkisizlestirilmeli(PathString istekYolu) =>
        istekYolu.StartsWithSegments(Yol, StringComparison.OrdinalIgnoreCase)
        && Calisabilir(Path.GetExtension(istekYolu.Value));

    public static IApplicationBuilder UseYuklemeGuvenligi(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (Etkisizlestirilmeli(context.Request.Path))
            {
                /*
                  DOSYA SİLİNMİYOR, ETKİSİZLEŞTİRİLİYOR.

                  404 vermek de bir seçenekti ama diskte duran meşru
                  dosyaları da kaybettirirdi (birinin gerçekten .xml eki
                  yüklemiş olması olağan). İndirilebilir kalıyor, yalnızca
                  tarayıcı onu BELGE olarak açmıyor:

                  - `application/octet-stream`: içerik tipi artık text/html
                    değil, yani render edilmiyor.
                  - `Content-Disposition: attachment`: adrese doğrudan
                    gidildiğinde sayfa açılmıyor, dosya iniyor.
                  - `nosniff` (global başlık) tipin tahmin edilmesini de
                    kapatıyor; üçü birlikte gerekli.
                */
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["Content-Type"] = "application/octet-stream";
                    context.Response.Headers["Content-Disposition"] = "attachment";
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    return Task.CompletedTask;
                });
            }

            await next();
        });
}
