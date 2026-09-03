using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Dto.V2.IsTakip;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Data;

namespace KentOS.Kalem.Web.Services.V2;

/// <summary>
/// İŞ ZAMAN ÇİZELGESİ — "bu görevde ne oldu, kim yaptı?"
/// </summary>
/// <remarks>
/// <para>
/// <c>AjandaOlayService</c> kalıbı. <c>randevu_hareketler</c> gibi Postgres
/// tetikleyicisiyle DEĞİL uygulama katmanında yazılıyor: tetikleyici kimin
/// hangi birim adına çalıştığını bilmiyor ve o bilgi olmadan vekâletle
/// yapılan işlemin izi kayboluyor.
/// </para>
/// <para>
/// <b>Yazma istisna yutar.</b> Çizelge yardımcı bir kayıt; asıl iş akışını
/// düşürmesine izin verilmez. Bir görevin atanması, çizelgeye satır
/// yazılamadığı için başarısız olmamalı.
/// </para>
/// </remarks>
public interface IIsOlayServisi
{
    /// <summary>Olay yazar. Hata durumunda sessizce vazgeçer.</summary>
    Task YazAsync(IsVarligi tur, long varlikId, GorevOlayTipi tip,
        string? aciklama = null, IReadOnlyList<AjandaAlanDegisikligiDto>? degisiklikler = null,
        CancellationToken iptal = default);

    /// <summary>Bir varlığın çizelgesi — en yeni önce.</summary>
    Task<List<IsOlayDto>> ListeAsync(IsVarligi tur, long varlikId, CancellationToken iptal = default);

    /// <summary>Bu eşik için daha önce olay yazılmış mı?</summary>
    /// <remarks>
    /// SLA işçisinin "aynı görevi iki kez bildirme" bekçisi. Ayrı bir
    /// "bildirildi mi" kolonu açmak yerine çizelgenin kendisi hafıza olarak
    /// kullanılıyor: bildirim zaten oraya yazılıyor ve iki kayıt arasında
    /// tutarsızlık ihtimali kalmıyor.
    /// </remarks>
    Task<bool> VarMiAsync(IsVarligi tur, long varlikId, GorevOlayTipi tip,
        string? aciklama = null, CancellationToken iptal = default);

    /// <summary>Varlık silinince çizelgesini de siler.</summary>
    Task VarligaAitleriSilAsync(IsVarligi tur, long varlikId, CancellationToken iptal = default);
}

public class IsOlayServisi(
    AppDbContext _context,
    ICurrentUserService _kullanici,
    IEtkinBirim _etkinBirim,
    ILogger<IsOlayServisi> _kayit) : IIsOlayServisi
{
    public async Task YazAsync(IsVarligi tur, long varlikId, GorevOlayTipi tip,
        string? aciklama = null, IReadOnlyList<AjandaAlanDegisikligiDto>? degisiklikler = null,
        CancellationToken iptal = default)
    {
        try
        {
            // Vekâlet izi: işlem hangi birim ADINA yapıldı. Kullanıcının kendi
            // birimi değil — başkan yardımcısı bir müdürlük adına iş
            // yaptığında "bunu bize kim yazdı?" sorusunun cevabı burada kalır.
            long? birim = null;
            try { birim = await _etkinBirim.IdAsync(iptal); }
            catch { /* birim çözülemezse olay yine de yazılır */ }

            _context.IsOlaylari.Add(new WorkEvent
            {
                VarlikTuru = tur,
                VarlikId = varlikId,
                Tip = tip,
                Aciklama = aciklama,
                DegisikliklerJson = degisiklikler is { Count: > 0 }
                    ? JsonSerializer.Serialize(degisiklikler)
                    : null,
                Kullanici = await _kullanici.GetFullNameAsync(),
                BirimId = birim is > 0 ? birim : null,
                Tarih = DateTime.Now,
            });

            await _context.SaveChangesAsync(iptal);
        }
        catch (Exception hata)
        {
            // YUTULUYOR ve bu bilinçli: çizelge yardımcı kayıt. Yine de
            // loglanıyor, yoksa çizelgenin sessizce boş kalması fark edilmez.
            _kayit.LogWarning(hata,
                "İş olayı yazılamadı: {Tur}/{VarlikId} {Tip}", tur, varlikId, tip);
        }
    }

    public async Task<List<IsOlayDto>> ListeAsync(
        IsVarligi tur, long varlikId, CancellationToken iptal = default)
    {
        var olaylar = await _context.IsOlaylari
            .AsNoTracking()
            .Where(o => o.VarlikTuru == tur && o.VarlikId == varlikId)
            .OrderByDescending(o => o.Tarih)
            .ThenByDescending(o => o.Id)
            .ToListAsync(iptal);

        return [.. olaylar.Select(o => new IsOlayDto
        {
            Id = o.Id,
            Tip = o.Tip,
            TipAd = OlayAdi(o.Tip),
            Aciklama = o.Aciklama,
            Kullanici = o.Kullanici,
            Tarih = o.Tarih,
            Degisiklikler = Coz(o.DegisikliklerJson),
        })];
    }

    public Task<bool> VarMiAsync(IsVarligi tur, long varlikId, GorevOlayTipi tip,
        string? aciklama = null, CancellationToken iptal = default)
    {
        var sorgu = _context.IsOlaylari
            .AsNoTracking()
            .Where(o => o.VarlikTuru == tur && o.VarlikId == varlikId && o.Tip == tip);

        if (aciklama is not null)
            sorgu = sorgu.Where(o => o.Aciklama == aciklama);

        return sorgu.AnyAsync(iptal);
    }

    public async Task VarligaAitleriSilAsync(
        IsVarligi tur, long varlikId, CancellationToken iptal = default)
    {
        await _context.IsOlaylari
            .Where(o => o.VarlikTuru == tur && o.VarlikId == varlikId)
            .ExecuteDeleteAsync(iptal);
    }

    /// <summary>Bozuk JSON çizelgeyi düşürmesin — o satır değişiklikleri boş gösterir.</summary>
    private static List<IsOlayDegisiklikDto> Coz(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            var ham = JsonSerializer.Deserialize<List<AjandaAlanDegisikligiDto>>(json);
            return ham is null
                ? []
                : [.. ham.Select(d => new IsOlayDegisiklikDto
                {
                    Alan = d.Alan,
                    Eski = d.Eski,
                    Yeni = d.Yeni,
                })];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>Olay tipinin okunabilir adı — SUNUCUDA üretilir.</summary>
    public static string OlayAdi(GorevOlayTipi tip) => tip switch
    {
        GorevOlayTipi.Olusturuldu => "Oluşturuldu",
        GorevOlayTipi.Guncellendi => "Güncellendi",
        GorevOlayTipi.DurumDegisti => "Durum değişti",
        GorevOlayTipi.Atandi => "Atandı",
        GorevOlayTipi.AtamaKaldirildi => "Atama kaldırıldı",
        GorevOlayTipi.AsamaTamamlandi => "Aşama tamamlandı",
        GorevOlayTipi.AsamaGeriAlindi => "Aşama geri alındı",
        GorevOlayTipi.TamamlanmayaGonderildi => "Onaya gönderildi",
        GorevOlayTipi.Onaylandi => "Onaylandı",
        GorevOlayTipi.IadeEdildi => "İade edildi",
        GorevOlayTipi.Reddedildi => "Reddedildi",
        GorevOlayTipi.IptalEdildi => "İptal edildi",
        GorevOlayTipi.YorumEklendi => "Yorum eklendi",
        GorevOlayTipi.EkEklendi => "Dosya eklendi",
        GorevOlayTipi.AltGorevAcildi => "Alt görev açıldı",
        GorevOlayTipi.BirimAdinaIslem => "Başka birim adına işlem",
        GorevOlayTipi.SlaUyarisi => "Süre aşımı",
        _ => tip.ToString(),
    };
}
