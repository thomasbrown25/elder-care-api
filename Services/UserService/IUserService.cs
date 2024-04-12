using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using elder_care_api.Dtos.User;
using elder_care_api.Dtos.UserSetting;

namespace elder_care_api.Data
{
    public interface IUserService
    {
        Task<ServiceResponse<LoadUserDto>> Register(User user, string password);
        Task<ServiceResponse<LoadUserDto>> Login(string email, string password);
        Task<bool> UserExists(string email);
        ServiceResponse<LoadUserDto> LoadUser();
        ServiceResponse<string> DeleteUser(int userId);
        Task<ServiceResponse<SettingsDto>> GetSettings();
        Task<ServiceResponse<SettingsDto>> SaveSettings(SettingsDto newSettings);
    }
}