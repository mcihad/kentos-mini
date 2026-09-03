using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Services
{
    public interface IBirimService
    {
        Task<long> GetCurrentBirimIdAsync();
        Task<Birim> GetCurrentAsync();
        Task<Birim> GetAsync(long id);

        Task<IEnumerable<BirimDto>> GetAltBirimlerAsync();
        Task<IEnumerable<BirimDto>> GetBirimlerAsync();
        Task<BirimDto> GetUstBirimAsync();
    }
}
