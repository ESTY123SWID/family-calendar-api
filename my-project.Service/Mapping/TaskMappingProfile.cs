using AutoMapper;
using my_project.Core.DTOs.Tasks;
using my_project.Core.Entities;

namespace my_project.Service.Mapping;

public class TaskMappingProfile : Profile
{
    public TaskMappingProfile()
    {
        // IsOverdue depends on the current time (IClock) – filled by the service
        CreateMap<FamilyTask, TaskDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event.Title))
            .ForMember(dest => dest.AssigneeName, opt => opt.MapFrom(src => src.Assignee!.FullName))
            .ForMember(dest => dest.IsOverdue, opt => opt.Ignore());
    }
}
