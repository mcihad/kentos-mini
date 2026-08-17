using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Services.V2;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>Kullanıcının adına çalışabileceği bir birim.</summary>
public class KapsamBirimiDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("yetkili")] public string? Yetkili { get; set; }

    /// <summary>Ağaçtaki derinlik — arayüz girintiyi buradan çizer.</summary>
    [JsonPropertyName("derinlik")] public int Derinlik { get; set; }

    /// <summary>Kullanıcının KENDİ birimi mi?</summary>
    [JsonPropertyName("kendiBirimi")] public bool KendiBirimi { get; set; }
}

/// <summary>
/// BİRİM KAPSAMI — "hangi birim adına çalışabilirim?"
/// </summary>
/// <remarks>
/// <para>
/// Başkan yardımcısı kendine bağlı müdürlüğü seçip o müdürlüğün işlerini
/// görebilmeli. Bu uç seçilebilecek birimleri listeler; seçim istemcide
/// tutulur ve sonraki isteklerde <c>X-Etkin-Birim</c> başlığıyla gönderilir.
/// </para>
/// <para>
/// <b>Liste bir yetki belgesi değil.</b> Asıl kapı her istekte
/// <see cref="IEtkinBirim"/> içinde: başlıkta gelen birim gerçekten
/// kullanıcının alt ağacında mı diye yeniden denetlenir. Buradaki liste
/// yalnızca arayüzün ne göstereceğini söyler — istemciden gelen hiçbir şey
/// yetki yerine geçmez.
/// </para>
/// </remarks>
[Route("api/v2/birim-kapsam")]
[Izin(Izinler.GorevBirimKapsam)]
public class BirimKapsamController(
    AppDbContext _context,
    ICurrentUserService _kullanici,
    IBirimAgaci _agac) : V2ControllerBase
{
    /// <summary>
    /// Kullanıcının kendi birimi ve ALTINDAKİ birimler, ağaç sırasıyla.
    /// </summary>
    /// <remarks>
    /// Sayfalama YOK ve bilinçli: bir kullanıcının alt ağacı en fazla birkaç
    /// düzine birim. Bu liste bir seçim kutusunu dolduruyor; sayfalanmış bir
    /// açılır liste, kullanıcıyı aradığı müdürlüğü sayfa sayfa aramaya
    /// zorlardı.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<List<KapsamBirimiDto>>(StatusCodes.Status200OK)]
    public async Task<List<KapsamBirimiDto>> ListeAsync(CancellationToken iptal)
    {
        var kendi = _kullanici.GetCurrentBirimId();
        if (kendi <= 0) return [];

        var idler = await _agac.AltAgacAsync(kendi, iptal);

        var birimler = await _context.Birimler
            .AsNoTracking()
            .Where(b => idler.Contains(b.Id))
            .Select(b => new { b.Id, b.Ad, b.Yetkili, b.UstBirimId })
            .ToListAsync(iptal);

        // AĞAÇ SIRASI (ön sıralı gezinme) — düz "derinlik sonra ad" DEĞİL.
        //
        // Ölçüldü ve yanlış çıktı: derinliğe göre sıralayınca bütün 2. seviye
        // birimler, 1. seviyenin SONUNCUSUNUN altındaymış gibi görünüyordu.
        // Girinti ile sıra birbirini yalanlıyordu — kullanıcı "Park Müdürlüğü
        // Zabıta'ya mı bağlı?" diye sorardı.
        //
        // Çözüm: her birime kökten kendisine kadar olan ADLARDAN bir yol
        // üretiliyor ve sıralama o yola göre yapılıyor. Kardeşler ada göre
        // sıralanırken çocuklar ebeveyninin hemen altında kalıyor.
        var dugumler = birimler.ToDictionary(b => b.Id);

        (int Derinlik, string Yol) Konum(long id)
        {
            var parcalar = new List<string>();
            var yurur = id;

            // Kökten aşağı değil, yapraktan yukarı yürünüp ters çevriliyor.
            while (dugumler.TryGetValue(yurur, out var d))
            {
                parcalar.Add(d.Ad);
                if (yurur == kendi) break;
                if (d.UstBirimId is not { } ust) break;
                yurur = ust;

                // Veri hatasıyla oluşmuş bir çevrim sonsuz döngü yapardı.
                if (parcalar.Count > 20) break;
            }

            parcalar.Reverse();

            // Ayırıcı BİRİM AYIRICI (U+001F): birim adında geçemeyecek bir
            // karakter olmalı. Eğik çizgi ya da boşluk seçilseydi adında o
            // karakteri taşıyan bir birim sıralamayı bozardı. Kaçış dizisiyle
            // yazılıyor — kaynakta görünmez bir kontrol karakteri bırakmak
            // sonraki okuyucu için tuzak.
            return (parcalar.Count - 1, string.Join('\u001f', parcalar));
        }

        return [.. birimler
            .Select(b =>
            {
                var (derinlik, yol) = Konum(b.Id);
                return (Yol: yol, Dto: new KapsamBirimiDto
                {
                    Id = b.Id,
                    Ad = b.Ad,
                    Yetkili = b.Yetkili,
                    Derinlik = derinlik,
                    KendiBirimi = b.Id == kendi,
                });
            })
            .OrderBy(x => x.Yol, StringComparer.CurrentCulture)
            .Select(x => x.Dto)];
    }
}
