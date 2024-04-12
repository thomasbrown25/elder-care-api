using AutoMapper;
using elder_care_api.Dtos.User;
using elder_care_api.Dtos.UserSetting;

namespace elder_care_api
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, LoadUserDto>();
            CreateMap<SettingsDto, UserSettings>();
            CreateMap<UserSettings, SettingsDto>();
        }
    }
}
