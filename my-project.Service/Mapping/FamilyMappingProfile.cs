using AutoMapper;
using my_project.Core.DTOs.Admin;
using my_project.Core.DTOs.Families;
using my_project.Core.Entities;
using my_project.Core.Models;

namespace my_project.Service.Mapping;

public class FamilyMappingProfile : Profile
{
    public FamilyMappingProfile()
    {
        CreateMap<FamilyListItem, FamilyDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Family.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Family.Name));

        // JoinCode is filled by the service, for managers only
        CreateMap<FamilyListItem, FamilyDetailsDto>()
            .IncludeBase<FamilyListItem, FamilyDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.Family.CreatedAt))
            .ForMember(dest => dest.JoinCode, opt => opt.Ignore());

        CreateMap<FamilyMember, FamilyMemberDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email));

        CreateMap<AdminFamilyListItem, AdminFamilyDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Family.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Family.Name))
            .ForMember(dest => dest.JoinCode, opt => opt.MapFrom(src => src.Family.JoinCode))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.Family.CreatedAt));

        // Members are filled by the service
        CreateMap<AdminFamilyListItem, AdminFamilyDetailsDto>()
            .IncludeBase<AdminFamilyListItem, AdminFamilyDto>()
            .ForMember(dest => dest.Members, opt => opt.Ignore());
    }
}
