using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Services
{
    public class OneriService(
        AppDbContext _context,
        IMapper _mapper,
        UserManager<AppUser> _userManager,
        IMessageService _messageService,
        ICurrentUserService _currentUserService) : IOneriService
    {
        public async Task<OneriDto> AnswerAsync(long id, OneriCevapDto cevap)
        {
            var oneri = await _context.Oneriler.FindAsync(id);
            if (oneri == null)
            {
                throw new EntityNotFoundException("Öneri bulunamadı.");
            }

            oneri.Cevap = cevap.Cevap;
            oneri.CevapTarih = DateTime.Now;
            _context.Oneriler.Update(oneri);
            await _context.SaveChangesAsync();
            var currentUser = await _currentUserService.GetCurrentAsync();
            var message = $"{oneri.Baslik} başlıklı Talep/Öneriniz cevaplandı: {cevap.Cevap}";
            var answerData = new TokenDataDto(NotificationEntity.Oneri, (int)oneri.Id, NotificationAction.OpenDetails);
            await _messageService.CreateAsync(currentUser.Id, currentUser.FcmToken, "Öneriniz cevaplandı", message, SendMessageType.PushNotification, NotifikasyonTip.Always, answerData.ToJson());

            return _mapper.Map<OneriDto>(oneri);
        }

        public async Task<OneriDto> CreateAsync(OneriDto oneriDto)
        {
            var currentUser = await _currentUserService.GetCurrentAsync();
            var oneri = _mapper.Map<Oneri>(oneriDto);
            oneri.KullaniciId = currentUser.Id;
            oneri.KullaniciAdi = await _currentUserService.GetFullNameAsync();
            oneri.Tarih = DateTime.Now;

            await _context.Oneriler.AddAsync(oneri);
            await _context.SaveChangesAsync();
            var message = $"{oneri.Baslik} başlıklı Talep/Öneriniz oluşturuldu.";
            var dataDto = new TokenDataDto(NotificationEntity.Oneri, (int)oneri.Id, NotificationAction.OpenDetails);
            await _messageService.CreateAsync(currentUser.Id, currentUser.FcmToken, "Öneriniz alındı", message, SendMessageType.PushNotification, NotifikasyonTip.Always, dataDto.ToJson());

            //get admin user
            var adminUser = await _userManager.FindByNameAsync("admin");
            if (adminUser != null)
            {
                var adminMessage = $"{oneri.Baslik} başlıklı Talep/Öneri oluşturuldu. {oneri.KullaniciAdi} tarafından.";
                var adminData = new TokenDataDto(NotificationEntity.Oneri, (int)oneri.Id, NotificationAction.OpenDetails);
                await _messageService.CreateAsync(adminUser.Id, adminUser.FcmToken, "Yeni Talep/Öneri", adminMessage, SendMessageType.PushNotification, NotifikasyonTip.Always, adminData.ToJson());
            }
            return _mapper.Map<OneriDto>(oneri);
        }

        public async Task DeleteAsync(long id)
        {
            var oneri = await _context.Oneriler.FindAsync(id);
            if (oneri == null)
            {
                throw new EntityNotFoundException("Öneri bulunamadı.");
            }
            _context.Oneriler.Remove(oneri);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<OneriDto>> GetAllAsync()
        {
            //if user has Sistem role, return all oneriler else return only user's oneriler
            var currentUser = await _currentUserService.GetCurrentAsync();
            if (await _userManager.IsInRoleAsync(currentUser, UserRoles.Sistem))
            {
                var oneriler = await _context.Oneriler.ToListAsync();
                return _mapper.Map<IEnumerable<OneriDto>>(oneriler);
            }
            else
            {
                var oneriler = await _context.Oneriler.Where(x => x.KullaniciId == currentUser.Id).ToListAsync();
                return _mapper.Map<IEnumerable<OneriDto>>(oneriler);
            }
        }

        public async Task<OneriDto> GetAsync(long id)
        {
            var oneri = await _context.Oneriler.FindAsync(id);
            if (oneri == null)
            {
                throw new EntityNotFoundException("Öneri bulunamadı.");
            }

            return _mapper.Map<OneriDto>(oneri);
        }

        public async Task<IEnumerable<OneriDto>> GetOnerilerByUserIdAsync(long userId)
        {
            var oneriler = await _context.Oneriler.FirstOrDefaultAsync(x => x.KullaniciId == userId);
            if (oneriler == null)
            {
                throw new EntityNotFoundException("Öneri bulunamadı.");
            }
            return _mapper.Map<IEnumerable<OneriDto>>(oneriler);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OneriDto>> KullaniciOnerileriAsync(long kullaniciId)
        {
            var oneriler = await _context.Oneriler
                .Where(x => x.KullaniciId == kullaniciId)
                .OrderByDescending(x => x.Tarih)
                .ToListAsync();

            return _mapper.Map<IEnumerable<OneriDto>>(oneriler);
        }

        public async Task<IEnumerable<OneriDto>> GetOnerilerByUserIdAsync(long userId, OneriTip tip)
        {
            var oneriler = await _context.Oneriler.Where(x => x.KullaniciId == userId && x.Tip == tip).ToListAsync();
            return _mapper.Map<IEnumerable<OneriDto>>(oneriler);

        }

        public async Task<IEnumerable<OneriDto>> GetOnerilerByUserIdAsync(long userId, DateTime startDate, DateTime endDate)
        {
            var oneriler = await _context.Oneriler.Where(x => x.KullaniciId == userId && x.Tarih >= startDate && x.Tarih <= endDate).ToListAsync();
            return _mapper.Map<IEnumerable<OneriDto>>(oneriler);
        }

        public async Task<IEnumerable<OneriDto>> GetWaitingOnerilerAsync()
        {
            var oneriler = await _context.Oneriler.Where(x => x.Cevap == null).ToListAsync();
            return _mapper.Map<IEnumerable<OneriDto>>(oneriler);
        }

        public async Task<OneriDto> UpdateAsync(OneriDto oneriDto)
        {
            var oneri = await _context.Oneriler.FindAsync(oneriDto.Id);
            if (oneri == null)
            {
                throw new EntityNotFoundException("Öneri bulunamadı.");
            }

            //map
            _mapper.Map(oneriDto, oneri);
            _context.Oneriler.Update(oneri);
            await _context.SaveChangesAsync();

            return _mapper.Map<OneriDto>(oneri);
        }
    }
}
