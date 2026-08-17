using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Services
{
    public interface ICicekciService
    {
        Task<IEnumerable<CicekciDto>> GetAllAsync();
        Task<CicekciDto> GetByIdAsync(long id);
        Task<CicekciDto> CreateAsync(CicekciDto cicekciDto);
        Task<CicekciDto> UpdateAsync(CicekciDto cicekciDto);
        Task<bool> DeleteAsync(long id);
        Task<int> GetCountAsync();
        Task<IEnumerable<CicekDto>> GetCiceklerAsync(long cicekciId);
        Task<CicekKartDto> GetCicekKartAsync(string guid);
        Task<int> GetDogrulamaKoduAsync(string guid);
        Task<bool> CicekKartGonderildiAsync(string guid,int dogrulamaKodu);
        Task<bool> AddCicekAsync(long cicekciId, CicekDto cicekDto);
    }
}
