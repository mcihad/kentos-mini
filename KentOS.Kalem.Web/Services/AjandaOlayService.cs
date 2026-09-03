using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Data;
using System.Text.Json;

namespace KentOS.Kalem.Web.Services
{
    /// <summary>
    /// Etkinlik zaman çizelgesi kayıtları.
    ///
    /// MEVCUT AKIŞLARI BOZMAMA GARANTİSİ:
    ///  • <see cref="KaydetAsync"/> asıl işlem KAYDEDİLDİKTEN SONRA çağrılır ve
    ///    tüm gövdesi try/catch içindedir. Günlükleme başarısız olsa bile
    ///    kullanıcının işlemi (kaydetme, erteleme, havale…) etkilenmez.
    ///  • Yalnızca yeni tabloya INSERT yapar; hiçbir mevcut tabloya dokunmaz.
    ///  • Okuma tarafı AsNoTracking'dir.
    /// </summary>
    public class AjandaOlayService(
        AppDbContext _context,
        ICurrentUserService _currentUserService,
        ILogger<AjandaOlayService> _logger) : IAjandaOlayService
    {
        private static readonly JsonSerializerOptions _json = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public async Task KaydetAsync(
            long ajandaId,
            AjandaOlayTip tip,
            string aciklama,
            IEnumerable<AjandaAlanDegisikligiDto>? degisiklikler = null)
        {
            try
            {
                var liste = degisiklikler?.ToList();
                var kullanici = await KullaniciAdiAsync();

                _context.AjandaOlaylar.Add(new AjandaOlay
                {
                    AjandaId = ajandaId,
                    Tip = tip,
                    Kullanici = kullanici,
                    Tarih = DateTime.Now,
                    Aciklama = Kisalt(aciklama, 500),
                    DegisikliklerJson = (liste is { Count: > 0 })
                        ? JsonSerializer.Serialize(liste, _json)
                        : null
                });

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Bilinçli olarak yutuluyor: zaman çizelgesi yardımcı bir kayıttır,
                // asıl iş akışını düşürmesine izin verilmez.
                _logger.LogError(ex,
                    "Ajanda olayı kaydedilemedi (AjandaId={AjandaId}, Tip={Tip})",
                    ajandaId, tip);
            }
        }

        public async Task<IEnumerable<AjandaOlayDto>> GetirAsync(long ajandaId)
        {
            // GİZLİLİK KAPISI: gizli etkinliğin zaman çizelgesi de yalnızca
            // ekleyen ve katılımcılara açıktır. Zaman çizelgesi başlık, konum ve
            // değişen alan değerlerini taşıdığı için burası da bir sızıntı noktası.
            var kullaniciId = await _currentUserService.GetUserIdAsync();
            var kullaniciAdi = _currentUserService.GetUsername();
            // Basın kullanıcısı ajandanın yalnızca basına açık kısmını görür.
            // Kapı `ICurrentUserService`te: bu sorguların hepsinde zaten var.
            var yalnizcaBasin = await _currentUserService.YalnizcaBasinMiAsync();

            // BİRİM KAPISI: zaman çizelgesi KAYDIN SAHİBİ birime aittir.
            //
            // Davet edilen birim etkinliği kendi ajandasında görüyor ve not
            // ekleyebiliyor ama "kim neyi değiştirdi, kim erteledi, kime havale
            // etti" dökümü sahibin iç kaydı. `BirimKapsami` kullanılsaydı davet
            // edilen birim bu dökümü de okurdu.
            var birimId = _currentUserService.GetCurrentBirimId();

            var gorunur = await _context.Ajandalar
                .IgnoreQueryFilters()
                .Where(a => a.Id == ajandaId && a.BirimId == birimId)
                .GorunurOlanlar(kullaniciId, kullaniciAdi, yalnizcaBasin)
                .AnyAsync();

            if (!gorunur)
            {
                return [];
            }

            var olaylar = await _context.AjandaOlaylar
                .AsNoTracking()
                .Where(o => o.AjandaId == ajandaId)
                .OrderByDescending(o => o.Tarih)
                .ThenByDescending(o => o.Id)
                .ToListAsync();

            return olaylar.Select(o => new AjandaOlayDto
            {
                Id = o.Id,
                AjandaId = o.AjandaId,
                Tip = o.Tip,
                Kullanici = o.Kullanici,
                Tarih = o.Tarih,
                Aciklama = o.Aciklama,
                Degisiklikler = Coz(o.DegisikliklerJson)
            });
        }

        private static List<AjandaAlanDegisikligiDto> Coz(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try
            {
                return JsonSerializer.Deserialize<List<AjandaAlanDegisikligiDto>>(json)
                       ?? new();
            }
            catch
            {
                // Bozuk/eski biçimdeki kayıt tüm listeyi düşürmesin.
                return new();
            }
        }

        private async Task<string> KullaniciAdiAsync()
        {
            try
            {
                var ad = await _currentUserService.GetFullNameAsync();
                return string.IsNullOrWhiteSpace(ad) ? "Bilinmiyor" : ad;
            }
            catch
            {
                return "Sistem";
            }
        }

        private static string Kisalt(string? s, int max)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Trim();
            return s.Length <= max ? s : s[..max];
        }
    }
}
