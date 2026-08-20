using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;

namespace KentOS.Mini.Web.Services
{
    public class UserService(
        AppDbContext _context, 
        ICurrentUserService currentUserService,
        IMapper _mapper,
        UserManager<AppUser> _userManager) : IUserService
    {
        public async Task<UserDto> Get()
        {
            //get current user , user birim and roles and return as UserDto
            var user = await currentUserService.GetCurrentAsync();
            var roles = await _userManager.GetRolesAsync(user);
            var birim = await _context.Birimler.FindAsync(currentUserService.GetCurrentBirimId());
            var ustBirimId = birim?.UstBirimId;
            return new UserDto
            {
                Id = user.Id,
                Ad = user.Ad,
                Soyad = user.Soyad,
                Unvan = user.Unvan,
                Email = user.Email,
                Telefon = user.PhoneNumber,
                // `birim.Id` idi: birimi olmayan (ya da birimi silinmiş)
                // kullanıcıda burada NullReferenceException atıp `Me` ucu 500
                // dönüyordu — mobil uygulama açılışta oturumu kuramıyordu.
                BirimId = birim?.Id ?? user.BirimId ?? 0,
                UstBirimId = ustBirimId,
                BirimAd = birim?.Ad,
                FcmToken = user.FcmToken,
                Roles = [.. roles],
                GizliEtkinlikEkleyebilir = user.GizliEtkinlikEkleyebilir,
                DosyaGonderebilir = user.DosyaGonderebilir
            };
        }

        public async Task<bool> HasReceiveNotification(long userId, NotifikasyonTip tip)
        {
            if (tip == NotifikasyonTip.Always)
            {
                return true;
            }

            var setting = await _context.UserSettings.FirstOrDefaultAsync(x => x.UserId == userId);
            if (setting == null)
            {
                /*
                  AYAR SATIRI YOKSA VARSAYILAN AÇIK.

                  Burası `false` dönüyordu: ayar satırı ancak `GetSetting()`
                  ilk kez çağrıldığında oluşuyor, dolayısıyla ayar ekranını hiç
                  açmamış bir kullanıcı ajanda ve talep bildirimlerinin
                  TAMAMINI sessizce kaçırıyordu.

                  Entity'deki bütün bayrakların varsayılanı `true`; satırın
                  yokluğu "tercih belirtilmemiş" demektir, "hiçbirini
                  istemiyorum" değil.
                */
                return true;
            }

            return tip switch
            {
                NotifikasyonTip.HideOldAgendas => setting.HideOldAgendas,
                NotifikasyonTip.AgendaOnCreated => setting.AgendaOnCreated,
                NotifikasyonTip.AgendaOnOrganized => setting.AgendaOnOrganized,
                NotifikasyonTip.AgendaOnDeleted => setting.AgendaOnDeleted,
                NotifikasyonTip.AgendaOnUpdated => setting.AgendaOnUpdated,
                NotifikasyonTip.AgendaOnStatusChange => setting.AgendaOnStatusChange,
                NotifikasyonTip.AgendaOnImageUpload => setting.AgendaOnImageUpload,
                NotifikasyonTip.AgendaOnNoteAdded => setting.AgendaOnNoteAdded,
                NotifikasyonTip.AgendaOnPostponed => setting.AgendaOnPostponed,
                NotifikasyonTip.AgendaOnFlowerSent => setting.AgendaOnFlowerSent,
                NotifikasyonTip.AgendaOnFlowerDeleted => setting.AgendaOnFlowerDeleted,
                NotifikasyonTip.RequestOnCreated => setting.RequestOnCreated,
                NotifikasyonTip.RequestOnOrganized => setting.RequestOnOrganized,
                NotifikasyonTip.RequestOnDeleted => setting.RequestOnDeleted,
                NotifikasyonTip.RequestOnUpdated => setting.RequestOnUpdated,
                NotifikasyonTip.RequestOnFileAttached => setting.RequestOnFileAttached,
                NotifikasyonTip.RequestOnStatusChange => setting.RequestOnStatusChange,
                NotifikasyonTip.RequestOnNoteAdded => setting.RequestOnNoteAdded,
                NotifikasyonTip.RequestOnRemittance => setting.RequestOnRemittance,
                NotifikasyonTip.RequestOnAddedToAgenda => setting.RequestOnAddedToAgenda,

                NotifikasyonTip.TaskOnAssigned => setting.TaskOnAssigned,
                NotifikasyonTip.TaskOnStatusChange => setting.TaskOnStatusChange,
                NotifikasyonTip.TaskOnApprovalNeeded => setting.TaskOnApprovalNeeded,
                NotifikasyonTip.TaskOnOverdue => setting.TaskOnOverdue,
                NotifikasyonTip.ProjectOnTeamChange => setting.ProjectOnTeamChange,
                NotifikasyonTip.PublicDayOnAssigned => setting.PublicDayOnAssigned,
                NotifikasyonTip.PublicDayOnResult => setting.PublicDayOnResult,
                NotifikasyonTip.InvitationOnAssigned => setting.InvitationOnAssigned,
                NotifikasyonTip.InvitationOnResponse => setting.InvitationOnResponse,
                NotifikasyonTip.FileOnReceived => setting.FileOnReceived,
                NotifikasyonTip.ResumeOnShared => setting.ResumeOnShared,
                NotifikasyonTip.InboxOnReceived => setting.InboxOnReceived,
                NotifikasyonTip.CitizenReportOnUpdate => setting.CitizenReportOnUpdate,

                _ => false
            };
        }


        public async Task<UserSettingDto> GetSetting()
        {
            var user = await currentUserService.GetCurrentAsync();
            var setting = await _context.UserSettings.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (setting == null)
            {
                setting = new UserSetting
                {
                    UserId = user.Id
                };
                _context.UserSettings.Add(setting);
                await _context.SaveChangesAsync();
            }

            return _mapper.Map<UserSettingDto>(setting);

        }

        public Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            //TODO not implemented
            throw new NotImplementedException();
        }

        public void LogoutAsync()
        {
            //TODO not implemented
            throw new NotImplementedException();
        }

        public async Task<UserSettingDto> UpdateSetting(UserSettingDto setting)
        {
            var user = await currentUserService.GetCurrentAsync();
            var userSetting = await _context.UserSettings.FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (userSetting == null)
            {
                userSetting = new UserSetting
                {
                    UserId = user.Id
                };
                _context.UserSettings.Add(userSetting);
                await _context.SaveChangesAsync();
            }

            userSetting = _mapper.Map(setting, userSetting);
            _context.UserSettings.Update(userSetting);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserSettingDto>(userSetting);
        }

        public async Task<PasswordChangeResponseDto> PasswordChange(PasswordChangeDto changePasswordDto)
        {
            var user = await currentUserService.GetCurrentAsync();
            if (user == null)
            {
                return new PasswordChangeResponseDto
                {
                    Success = false,
                    Message = "Kullanıcı bulunamadı."
                };
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                changePasswordDto.Password,
                changePasswordDto.NewPassword
            );

            if (result.Succeeded)
            {
                return new PasswordChangeResponseDto
                {
                    Success = true,
                    Message = "Şifreniz başarıyla değiştirildi."
                };
            }
            else
            {
                return new PasswordChangeResponseDto
                {
                    Success = false,
                    Message = string.Join(" ", result.Errors.Select(e => e.Description))
                };
            }
        }

    }
}
