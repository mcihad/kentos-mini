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

        /// <summary>
        /// Çiçekçinin SMS bağlantısından gördüğü kart — giriş gerektirmez.
        /// </summary>
        /// <remarks>
        /// <see cref="GetCicekKartAsync"/> tam <c>CicekDto</c> döndürüyor ve
        /// içinde <b>doğrulama kodu</b> var; anonim bir uçta o yanıt, kodu
        /// bağlantıyı açan herkese verirdi. Bu metot yalnızca işi yapmaya
        /// yetecek alanları döner.
        /// </remarks>
        Task<Dto.V2.Cicek.CicekTeslimKartiDto> TeslimKartiAsync(string guid);

        Task<bool> CicekKartGonderildiAsync(string guid,int dogrulamaKodu);
        Task<bool> AddCicekAsync(long cicekciId, CicekDto cicekDto);
    }
}
