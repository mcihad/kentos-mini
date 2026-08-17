using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Dto.Randevu;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Services
{
    public class CicekciService(
        AppDbContext _context,
        IAjandaService _ajandaService,
        ICurrentUserService _currentUserService,
        IMapper _mapper
        ) : ICicekciService
    {
        public async Task<CicekciDto> CreateAsync(CicekciDto cicekciDto)
        {
            var cicekci = _mapper.Map<Cicekci>(cicekciDto);
            _context.Cicekciler.Add(cicekci);
            await _context.SaveChangesAsync();
            return _mapper.Map<CicekciDto>(cicekci);
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var cicekci = await _context.Cicekciler.FindAsync(id);
            if (cicekci == null)
            {
                return false;
            }

            _context.Cicekciler.Remove(cicekci);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<CicekciDto>> GetAllAsync()
        {
            var cicekciler = await _context.Cicekciler.ToListAsync();
            return _mapper.Map<IEnumerable<CicekciDto>>(cicekciler);
        }

        public async Task<CicekciDto> GetByIdAsync(long id)
        {
            var cicekci = await _context.Cicekciler.FindAsync(id);
            if (cicekci == null)
            {
                throw new EntityNotFoundException("Çiçekçi bulunamadı.");
            }

            return _mapper.Map<CicekciDto>(cicekci);
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Cicekciler.CountAsync();
        }

        public async Task<IEnumerable<CicekDto>> GetCiceklerAsync(long cicekciId)
        {
            var cicekler = await _context.Cicekler.Where(c => c.CicekciId == cicekciId).ToListAsync();
            return _mapper.Map<IEnumerable<CicekDto>>(cicekler);
        }

        public async Task<CicekciDto> UpdateAsync(CicekciDto cicekciDto)
        {
            var cicekci = await _context.Cicekciler.FindAsync(cicekciDto.Id);
            if (cicekci == null)
            {
                throw new EntityNotFoundException("Çiçekçi bulunamadı.");
            }

            _mapper.Map(cicekciDto, cicekci);

            await _context.SaveChangesAsync();
            return _mapper.Map<CicekciDto>(cicekci);
        }

        public async Task<CicekKartDto> GetCicekKartAsync(string guid)
        {
            var cicek = await _context.Cicekler.FirstOrDefaultAsync(x => x.Guid == guid);
            if (cicek == null)
            {
                throw new EntityNotFoundException("Çiçek kartı bulunamadı");
            }

            var ajanda = await _ajandaService.GetByIdWithoutUserRestrictionAsync(cicek.AjandaId);

            if (ajanda == null)
            {
                throw new EntityNotFoundException("Ajanda bulunamadı");

            }

            var cicekKart = new CicekKartDto
            {
                Cicek = _mapper.Map<CicekDto>(cicek),
                Ajanda = _mapper.Map<AjandaDto>(ajanda)
            };

            return cicekKart;
        }

        public async Task<bool> CicekKartGonderildiAsync(string guid, int dogrulamaKodu)
        {
            var cicek = await _context.Cicekler.FirstOrDefaultAsync(x => x.Guid == guid);
            if (cicek == null)
            {
                throw new EntityNotFoundException("Çiçek kartı bulunamadı");
            }

            if (cicek.DogrulamaKodu != dogrulamaKodu)
            {
                throw new EntityNotFoundException("Doğrulama kodu hatalı");
            }
            cicek.Gonderildi = true;
            cicek.GonderilmeTarihi = DateTime.Now;
            _context.Cicekler.Update(cicek);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddCicekAsync(long cicekciId, CicekDto cicekDto)
        {

            if (cicekDto.AjandaId == null)
            {
                throw new EntityNotFoundException("Ajanda bulunamadı.");
            }

            var cicekGonderDto = new AjandaCicekGonderDto
            {
                CicekciId = cicekciId,
                AjandaId = cicekDto.AjandaId ?? 0,
                Not = cicekDto.Aciklama,
            };

            await _ajandaService.CicekGonderAsync(cicekGonderDto);

            return true;
        }

        public async Task<int> GetDogrulamaKoduAsync(string guid)
        {
            var cicek = await _context.Cicekler.FirstOrDefaultAsync(x => x.Guid == guid);
            if (cicek == null)
            {
                return 0;
            }
            return cicek.DogrulamaKodu;
        }
    }
}
