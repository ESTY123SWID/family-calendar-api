using AutoMapper;
using my_project.Core.DTOs.Attachments;
using my_project.Core.DTOs.Events;
using my_project.Core.DTOs.Participants;
using my_project.Core.DTOs.Updates;
using my_project.Core.Entities;
using my_project.Core.Models;
using my_project.Service.Common;

namespace my_project.Service.Mapping;

public class EventMappingProfile : Profile
{
    private const string MapsSearchUrl = "https://www.google.com/maps/search/?api=1&query=";

    public EventMappingProfile()
    {
        // The counts come from EventListItem (computed in the database)
        CreateMap<Event, EventSummaryDto>()
            .ForMember(dest => dest.OrganizerName, opt => opt.Ignore())
            .ForMember(dest => dest.ComingCount, opt => opt.Ignore())
            .ForMember(dest => dest.OpenTasksCount, opt => opt.Ignore());

        CreateMap<EventListItem, EventSummaryDto>()
            .IncludeMembers(src => src.Event)
            .ForMember(dest => dest.OrganizerName, opt => opt.MapFrom(src => src.OrganizerName))
            .ForMember(dest => dest.ComingCount, opt => opt.MapFrom(src => src.ComingCount))
            .ForMember(dest => dest.OpenTasksCount, opt => opt.MapFrom(src => src.OpenTasksCount));

        // RsvpCounts, MyRsvp, CanManage and InvitationUrl depend on the current user – filled by the service
        CreateMap<Event, EventDetailsDto>()
            .ForMember(dest => dest.MapUrl, opt => opt.MapFrom(src => MapsSearchUrl + Uri.EscapeDataString(src.Address)))
            .ForMember(dest => dest.RsvpCounts, opt => opt.Ignore())
            .ForMember(dest => dest.MyRsvp, opt => opt.Ignore())
            .ForMember(dest => dest.CanManage, opt => opt.Ignore())
            .ForMember(dest => dest.InvitationUrl, opt => opt.Ignore());

        CreateMap<EventParticipant, ParticipantDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName));

        CreateMap<EventUpdate, EventUpdateDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event.Title))
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author.FullName));

        CreateMap<EventAttachment, AttachmentDto>()
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.OriginalFileName))
            .ForMember(dest => dest.Size, opt => opt.MapFrom(src => src.SizeBytes))
            .ForMember(dest => dest.Url, opt => opt.MapFrom(src => DomainRules.AttachmentContentUrl(src.Id)))
            .ForMember(dest => dest.UploadedBy, opt => opt.MapFrom(src => src.UploadedBy.FullName));
    }
}
