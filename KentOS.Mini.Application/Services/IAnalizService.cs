using KentOS.Mini.Application.Dto.Analiz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Services
{
    public interface IAnalizService
    {
        Task<IEnumerable<RandevuDurumCountDto>> GetRandevuDurumCountAsync();
        Task<IEnumerable<RandevuBirimCountDto>> GetRandevuBirimCountAsync();
        Task<IEnumerable<RandevuMonthCountDto>> GetRandevuMonthCountAsync();
        Task<IEnumerable<RandevuTipCountDto>> GetRandevuTipCountAsync();
        Task<IEnumerable<RandevuDurumCountDto>> GetRandevuDurumCountAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<RandevuBirimCountDto>> GetRandevuBirimCountAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<RandevuMonthCountDto>> GetRandevuMonthCountAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<RandevuTipCountDto>> GetRandevuTipCountAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<RandevuDurumCountDto>> GetRandevuDurumCountAsync(DateTime startDate, DateTime endDate, int birimId);
        Task<IEnumerable<RandevuBirimCountDto>> GetRandevuBirimCountAsync(DateTime startDate, DateTime endDate, int birimId);
        Task<IEnumerable<RandevuMonthCountDto>> GetRandevuMonthCountAsync(DateTime startDate, DateTime endDate, int birimId);
        Task<IEnumerable<RandevuTipCountDto>> GetRandevuTipCountAsync(DateTime startDate, DateTime endDate, int birimId);
        Task<IEnumerable<RandevuArchivedCountDtoByBirim>> GetArchivedCountByBirimAsync();

    }
}
