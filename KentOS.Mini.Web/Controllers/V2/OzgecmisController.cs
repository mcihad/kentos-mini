using Microsoft.AspNetCore.Mvc;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Web.AuthPolicies;
using KentOS.Mini.Web.Services.V2;
using System.Text.Json;

namespace KentOS.Mini.Web.Controllers.V2;

/// <summary>
/// ÖZGEÇMİŞ HAVUZU — kurum genelinde aranabilir CV listesi.
/// </summary>
/// <remarks>
/// <para>
/// Havuz iki kaynaktan beslenir: doğrudan buraya yüklenen özgeçmişler ve
/// <b>iş taleplerine</b> eklenenler. İkisi aynı listede döner, hangisinin
/// nereden geldiği <c>talepId</c> ile bellidir.
/// </para>
/// <para>
/// <b>Birim süzgeci yok.</b> Modülün varlık sebebi kaydın birimler arasında
/// dolaşabilmesi: bir müdürlüğün elindeki özgeçmiş, işe alacak olan başka
/// müdürlüğe de görünmeli. Kapı tek — <c>ozgecmis.goruntule</c>.
/// </para>
/// </remarks>
[Route("api/v2/ozgecmis")]
[Izin(Izinler.OzgecmisGoruntule)]
public class OzgecmisController(IOzgecmisServisi _ozgecmis) : V2ControllerBase
{
    /// <summary>Havuz — arama ve süzgeçlerle.</summary>
    [HttpGet]
    [ProducesResponseType<SayfaliSonuc<OzgecmisOzetDto>>(StatusCodes.Status200OK)]
    public Task<SayfaliSonuc<OzgecmisOzetDto>> ListeAsync([FromQuery] OzgecmisSuzgeci suzgec)
        => _ozgecmis.ListeAsync(suzgec);

    /// <summary>Tek kayıt: bilgiler ve paylaşım geçmişi.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<OzgecmisDetayDto>(StatusCodes.Status200OK)]
    public Task<OzgecmisDetayDto> DetayAsync(long id) => _ozgecmis.DetayAsync(id);

    /// <summary>
    /// Havuza yeni özgeçmiş — <c>multipart/form-data</c>.
    /// </summary>
    /// <remarks>
    /// Dosya ve alanlar TEK istekte gider: önce kaydı açıp sonra dosya
    /// yüklemek, ikinci adım başarısız olduğunda havuzda dosyasız kayıtlar
    /// bırakıyordu.
    /// </remarks>
    [HttpPost]
    [Izin(Izinler.OzgecmisEkle)]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [ProducesResponseType<OzgecmisDetayDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> OlusturAsync()
    {
        var (istek, dosya) = await GovdeyiCozAsync(dosyaZorunlu: true);
        return Ok(await _ozgecmis.OlusturAsync(istek, dosya!));
    }

    /// <summary>Bilgileri (ve istenirse dosyayı) günceller.</summary>
    [HttpPut("{id:long}")]
    [Izin(Izinler.OzgecmisDuzenle)]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [ProducesResponseType<OzgecmisDetayDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GuncelleAsync(long id)
    {
        var (istek, dosya) = await GovdeyiCozAsync(dosyaZorunlu: false);
        return Ok(await _ozgecmis.GuncelleAsync(id, istek, dosya));
    }

    [HttpDelete("{id:long}")]
    [Izin(Izinler.OzgecmisSil)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SilAsync(long id)
    {
        await _ozgecmis.SilAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Dosyayı indirir — <b>kimlik denetimli</b>.
    /// </summary>
    /// <remarks>
    /// <c>wwwroot/uploads</c> altındaki statik yol v1 ve eski MVC için açık
    /// duruyor ama özgeçmiş kişisel bir belge: yeni istemciler bu ucu kullanır
    /// ve istek jetonla gelir.
    /// </remarks>
    [HttpGet("{id:long}/dosya")]
    public async Task<IActionResult> DosyaAsync(long id)
    {
        var (icerik, ad, tur) = await _ozgecmis.DosyaAsync(id);
        return File(icerik, tur, ad);
    }

    /// <summary>Özgeçmişi başka kullanıcılara yönlendirir.</summary>
    [HttpPost("{id:long}/paylas")]
    [Izin(Izinler.OzgecmisPaylas)]
    [ProducesResponseType<PaylasimSonucu>(StatusCodes.Status200OK)]
    public async Task<PaylasimSonucu> PaylasAsync(long id, [FromBody] PaylasimIstegi istek)
        => new() { Adet = await _ozgecmis.PaylasAsync(id, istek) };

    // ── yardımcı ───────────────────────────────────────────────────────

    /// <summary>
    /// Çok parçalı gövdeden alanları ve dosyayı çıkarır.
    /// </summary>
    /// <remarks>
    /// Alanlar hem düz form alanı (<c>adSoyad=…</c>) hem de tek bir
    /// <c>veri</c> JSON parçası olarak gönderilebilir: web <c>FormData</c> ile
    /// düz alan yolluyor, mobil tarafta tek JSON daha kolay.
    /// </remarks>
    private async Task<(OzgecmisIstegi Istek, YuklenenDosya? Dosya)> GovdeyiCozAsync(
        bool dosyaZorunlu)
    {
        if (!Request.HasFormContentType)
        {
            throw new Exceptions.BusinessRuleException(
                "İstek çok parçalı (multipart/form-data) gönderilmelidir.");
        }

        var form = await Request.ReadFormAsync();

        OzgecmisIstegi istek;
        if (form.TryGetValue("veri", out var ham) && !string.IsNullOrWhiteSpace(ham))
        {
            istek = JsonSerializer.Deserialize<OzgecmisIstegi>(ham!,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new OzgecmisIstegi();
        }
        else
        {
            istek = new OzgecmisIstegi
            {
                AdSoyad = form["adSoyad"].ToString(),
                Telefon = Bos(form["telefon"]),
                Eposta = Bos(form["eposta"]),
                MeslekId = Sayi(form["meslekId"]),
                MeslekAd = Bos(form["meslekAd"]),
                MahalleId = Sayi(form["mahalleId"]),
                Adres = Bos(form["adres"]),
                Aciklama = Bos(form["aciklama"]),
                TalepId = Sayi(form["talepId"]),
            };
        }

        var dosya = form.Files.FirstOrDefault();
        if (dosya is null)
        {
            if (dosyaZorunlu)
                throw new Exceptions.BusinessRuleException("Özgeçmiş dosyası zorunludur.");
            return (istek, null);
        }

        using var bellek = new MemoryStream();
        await dosya.CopyToAsync(bellek);

        return (istek, new YuklenenDosya(
            Path.GetFileName(dosya.FileName),
            dosya.ContentType,
            bellek.ToArray()));

        static string? Bos(Microsoft.Extensions.Primitives.StringValues d)
            => string.IsNullOrWhiteSpace(d) ? null : d.ToString();

        static long? Sayi(Microsoft.Extensions.Primitives.StringValues d)
            => long.TryParse(d.ToString(), out var s) ? s : null;
    }
}

/// <summary>Kaç kişiye yönlendirildi.</summary>
public class PaylasimSonucu
{
    [System.Text.Json.Serialization.JsonPropertyName("adet")]
    public int Adet { get; set; }
}
