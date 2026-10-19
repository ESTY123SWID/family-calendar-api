using AutoMapper;
using my_project.Core.DTOs.Admin;
using my_project.Core.DTOs.Notifications;
using my_project.Core.DTOs.Users;
using my_project.Core.Entities;
using my_project.Core.Models;

namespace my_project.Service.Mapping;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<User, UserSummaryDto>();

        CreateMap<User, AdminUserDto>()
            .ForMember(dest => dest.FamiliesCount, opt => opt.Ignore());

        CreateMap<AdminUserListItem, AdminUserDto>()
            .IncludeMembers(src => src.User)
            .ForMember(dest => dest.FamiliesCount, opt => opt.MapFrom(src => src.FamiliesCount));

        CreateMap<NotificationSettings, NotificationSettingsDto>();
    }
}
