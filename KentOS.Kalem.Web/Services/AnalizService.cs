using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using KentOS.Kalem.Application.Dto.Analiz;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Extensions;
using KentOS.Kalem.Web.Models;

namespace KentOS.Kalem.Web.Services
{
    public class AnalizService(
        IMemoryCache _memoryCache,
        ICurrentUserService _currentUserService,
        AppDbContext _context) : IAnalizService
    {

        List<String> _monthNames = ["Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];
        public async Task<IEnumerable<RandevuDurumCountDto>> GetRandevuDurumCountAsync()
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendentBirimIds = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            var talepDurumCounts = await _context.Randevular
                .IgnoreQueryFilters()
                .Include(t => t.RandevuDurum)
                .Where(t => descendentBirimIds.Contains(t.BirimId ?? 0))
                .GroupBy(t => t.RandevuDurum)
                .Select(g => new RandevuDurumCountDto
                {
                    Durum = g.Key.DurumAd,
                    Count = g.Count(),
                    Renk = g.Key.Renk
                }).ToListAsync();

            return talepDurumCounts ?? Enumerable.Empty<RandevuDurumCountDto>();
        }
        public async Task<IEnumerable<RandevuBirimCountDto>> GetRandevuBirimCountAsync()
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendentBirimIds = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            var result = await _context.Randevular
                .IgnoreQueryFilters()
                .Include(t => t.Birim)
                .Where(t => descendentBirimIds.Contains(t.BirimId ?? 0))
                .GroupBy(t => t.Birim)
                .Select(g => new RandevuBirimCountDto
                {
                    Birim = g.Key.Ad + " " + g.Key.Yetkili,
                    Count = g.Count()
                }).ToListAsync();
            return result ?? Enumerable.Empty<RandevuBirimCountDto>();
        }

        public async Task<IEnumerable<RandevuBirimCountDto>> GetRandevuBirimCountAsync(DateTime startDate, DateTime endDate)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendentBirimIds = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            var result = await _context.Randevular.IgnoreQueryFilters()
                .Include(t => t.Birim)
                .Where(t => descendentBirimIds.Contains(t.BirimId ?? 0) && t.BaslangicTarih >= startDate && t.BitisTarih <= endDate)
                .GroupBy(t => t.Birim)
                .Select(g => new RandevuBirimCountDto
                {
                    Birim = g.Key.Ad + " " + g.Key.Yetkili,
                    Count = g.Count()
                }).ToListAsync();
            return result ?? Enumerable.Empty<RandevuBirimCountDto>();
        }

        public async Task<IEnumerable<RandevuBirimCountDto>> GetRandevuBirimCountAsync(DateTime startDate, DateTime endDate, int birimId)
        {
            var descendentBirimIds = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            var result = await _context.Randevular.IgnoreQueryFilters()
                .Include(t => t.Birim)
                .Where(t => descendentBirimIds.Contains(t.BirimId ?? 0) && t.BaslangicTarih >= startDate && t.BitisTarih <= endDate)
                .GroupBy(t => t.Birim)
                .Select(g => new RandevuBirimCountDto
                {
                    Birim = g.Key.Ad + " " + g.Key.Yetkili,
                    Count = g.Count()
                }).ToListAsync();
            return result ?? Enumerable.Empty<RandevuBirimCountDto>();
        }
        public async Task<IEnumerable<RandevuDurumCountDto>> GetRandevuDurumCountAsync(DateTime startDate, DateTime endDate)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendentBirimIds = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            var talepDurumCounts = await _context.Randevular.IgnoreQueryFilters()
                .Include(t => t.RandevuDurum)
                .Where(t => descendentBirimIds.Contains(t.BirimId ?? 0) && t.BaslangicTarih >= startDate && t.BitisTarih <= endDate)
                .GroupBy(t => t.RandevuDurum)
                .Select(g => new RandevuDurumCountDto
                {
                    Durum = g.Key.DurumAd,
                    Count = g.Count(),
                    Renk = g.Key.Renk
                }).ToListAsync();

            return talepDurumCounts ?? Enumerable.Empty<RandevuDurumCountDto>();
        }

        public async Task<IEnumerable<RandevuDurumCountDto>> GetRandevuDurumCountAsync(DateTime startDate, DateTime endDate, int birimId)
        {
            var descendentBirimIds = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            if (!descendentBirimIds.Contains(birimId))
            {
                return Enumerable.Empty<RandevuDurumCountDto>();
            }

            var talepDurumCounts = await _context.Randevular.IgnoreQueryFilters()
                .Include(t => t.RandevuDurum)
                .Where(t => descendentBirimIds.Contains(t.BirimId ?? 0) && t.BaslangicTarih >= startDate && t.BitisTarih <= endDate)
                .GroupBy(t => t.RandevuDurum)
                .Select(g => new RandevuDurumCountDto
                {
                    Durum = g.Key.DurumAd,
                    Count = g.Count(),
                    Renk = g.Key.Renk
                }).ToListAsync();

            return talepDurumCounts ?? Enumerable.Empty<RandevuDurumCountDto>();
        }

        public async Task<IEnumerable<RandevuMonthCountDto>> GetRandevuMonthCountAsync()
        {
            //group by month
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendentBirimIds = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            var result = await _context.Randevular.IgnoreQueryFilters()
                .Where(t => descendentBirimIds.Contains(t.BirimId ?? 0) && t.BaslangicTarih.Value.Year == DateTime.Now.Year)
                .GroupBy(t => t.BaslangicTarih.Value.Month)
                .Select(g => new RandevuMonthCountDto
                {
                    Month = _monthNames[g.Key - 1],
                    Count = g.Count()
                }).ToListAsync();

            return result ?? Enumerable.Empty<RandevuMonthCountDto>();
        }

        public async Task<IEnumerable<RandevuMonthCountDto>> GetRandevuMonthCountAsync(DateTime startDate, DateTime endDate)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendentBirimIds = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            var result = await _context.Randevular.IgnoreQueryFilters()
                .Where(t => descendentBirimIds.Contains(t.BirimId ?? 0) && t.BaslangicTarih >= startDate && t.BitisTarih <= endDate)
                .GroupBy(t => t.BaslangicTarih.Value.Month)
                .Select(g => new RandevuMonthCountDto
                {
                    Month = g.Key.ToString(),
                    Count = g.Count()
                }).ToListAsync();

            return result ?? Enumerable.Empty<RandevuMonthCountDto>();
        }

        public async Task<IEnumerable<RandevuMonthCountDto>> GetRandevuMonthCountAsync(DateTime startDate, DateTime endDate, int birimId)
        {
            var descendentBirimIds = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            if (!descendentBirimIds.Contains(birimId))
            {
                return Enumerable.Empty<RandevuMonthCountDto>();
            }

            var result = await _context.Randevular.IgnoreQueryFilters()
                .Where(t => descendentBirimIds.Contains(t.BirimId ?? 0) && t.BaslangicTarih >= startDate && t.BitisTarih <= endDate)
                .GroupBy(t => t.BaslangicTarih.Value.Month)
                .Select(g => new RandevuMonthCountDto
                {
                    Month = g.Key.ToString(),
                    Count = g.Count()
                }).ToListAsync();

            return result ?? Enumerable.Empty<RandevuMonthCountDto>();
        }

        public async Task<IEnumerable<RandevuTipCountDto>> GetRandevuTipCountAsync()
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendentBirimIds = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            var result = await _context.Randevular.IgnoreQueryFilters()
                .Include(t => t.RandevuTip)
                .Where(t => descendentBirimIds.Contains(t.BirimId ?? 0))
                .GroupBy(t => t.RandevuTip)
                .Select(g => new RandevuTipCountDto
                {
                    Tip = g.Key.Ad,
                    Count = g.Count(),
                    Renk = g.Key.Renk
                }).ToListAsync();
            return result ?? Enumerable.Empty<RandevuTipCountDto>();
        }

        public async  Task<IEnumerable<RandevuTipCountDto>> GetRandevuTipCountAsync(DateTime startDate, DateTime endDate)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendentBirimIds = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            var result = await _context.Randevular.IgnoreQueryFilters()
                .Include(t => t.RandevuTip)
                .Where(t => descendentBirimIds.Contains(t.BirimId ?? 0) && t.BaslangicTarih >= startDate && t.BitisTarih <= endDate)
                .GroupBy(t => t.RandevuTip)
                .Select(g => new RandevuTipCountDto
                {
                    Tip = g.Key.Ad,
                    Count = g.Count(),
                    Renk = g.Key.Renk
                }).ToListAsync();

            return result ?? Enumerable.Empty<RandevuTipCountDto>();
        }

        public async Task<IEnumerable<RandevuTipCountDto>> GetRandevuTipCountAsync(DateTime startDate, DateTime endDate, int birimId)
        {
            var descendentBirimIds = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            if (!descendentBirimIds.Contains(birimId))
            {
                return Enumerable.Empty<RandevuTipCountDto>();
            }
            var result = await _context.Randevular.IgnoreQueryFilters()
                .Include(t => t.RandevuTip)
                .Where(t => descendentBirimIds.Contains(t.BirimId ?? 0) && t.BaslangicTarih >= startDate && t.BitisTarih <= endDate)
                .GroupBy(t => t.RandevuTip)
                .Select(g => new RandevuTipCountDto
                {
                    Tip = g.Key.Ad,
                    Count = g.Count(),
                    Renk = g.Key.Renk
                }).ToListAsync();
            return result ?? Enumerable.Empty<RandevuTipCountDto>();
        }

        public async Task<IEnumerable<RandevuArchivedCountDtoByBirim>> GetArchivedCountByBirimAsync()
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendentBirimIds = _context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            var result = await _context.Randevular.IgnoreQueryFilters()
                .Where(t => descendentBirimIds.Contains(t.BirimId ?? 0) && t.Arsivlendi)
                .GroupBy(t => t.Birim)
                .Select(g => new RandevuArchivedCountDtoByBirim
                {
                    BirimAdi = g.Key.Ad + " " + g.Key.Yetkili,
                    ArchivedCount= g.Count(),
                    TotalCount = g.Count()
                }).ToListAsync();
            return result ?? Enumerable.Empty<RandevuArchivedCountDtoByBirim>();
        }
    }
}
