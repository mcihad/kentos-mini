using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Services
{
    public interface IUserService
    {
        Task<UserDto> Get();
        Task<UserSettingDto> GetSetting();
        Task<UserSettingDto> UpdateSetting(UserSettingDto setting);
        Task<bool> HasReceiveNotification(long userId, NotifikasyonTip tip);
        Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
        Task<PasswordChangeResponseDto> PasswordChange(PasswordChangeDto changePasswordDto);
        void LogoutAsync();
    }
}
