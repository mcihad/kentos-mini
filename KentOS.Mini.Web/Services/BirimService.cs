using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Services
{
    public class BirimService(
        ICurrentUserService userService,
        IMapper mapper,
        AppDbContext context) : IBirimService
    {
        public async Task<Birim> GetCurrentAsync()
        {
            var birimId = await GetCurrentBirimIdAsync();
            if (birimId==0)
            {
                throw new EntityNotFoundException($"Birim bulunamadı");
            }

            var birim = await context.Birimler.FindAsync(birimId);
            if (birim == null)
            {
                throw new EntityNotFoundException($"Birim bulunamadı");
            }

            return birim;
        }

        public async Task<long> GetCurrentBirimIdAsync()
        {
            var user = await userService.GetCurrentAsync();
            return user.BirimId?? 0;
        }

        public async Task<Birim> GetAsync(long id)
        {
            var birim = await context.Birimler.FindAsync(id);
            if (birim == null)
            {
                throw new EntityNotFoundException($"Birim bulunamadı");
            }
            return birim;
        }

        public async Task<IEnumerable<BirimDto>> GetAltBirimlerAsync()
        {
            var birimId = await GetCurrentBirimIdAsync();
            var birimler = await context.Birimler
                .Where(b => b.UstBirimId == birimId)
                .ToListAsync();
            return mapper.Map<IEnumerable<BirimDto>>(birimler);
        }

        public async Task<IEnumerable<BirimDto>> GetBirimlerAsync()
        {
            var birimler = await context.Birimler
                .ToListAsync();
            return mapper.Map<IEnumerable<BirimDto>>(birimler);
        }

        public async Task<BirimDto> GetUstBirimAsync()
        {
            var birimId = await GetCurrentBirimIdAsync();
            var ustBirim = await context.Birimler
                .Where(b => b.UstBirimId == birimId)
                .FirstOrDefaultAsync();

            if (ustBirim == null)
            {
                throw new EntityNotFoundException($"Üst birim bulunamadı");
            }

            return mapper.Map<BirimDto>(ustBirim);
        }
    }
}
