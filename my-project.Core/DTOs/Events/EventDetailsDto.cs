using my_project.Core.DTOs.Participants;
using my_project.Core.DTOs.Users;
using my_project.Core.Enums;

namespace my_project.Core.DTOs.Events;

public class EventDetailsDto
{
    public int Id { get; set; }

    public int FamilyId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    // UTC
    public DateTime StartAt { get; set; }

    // UTC
    public DateTime? EndAt { get; set; }

    public string? LocationName { get; set; }

    public string Address { get; set; } = string.Empty;

    // Google Maps link built from Address
    public string MapUrl { get; set; } = string.Empty;

    public string? Directions { get; set; }

    public EventStatus Status { get; set; }

    public UserSummaryDto Organizer { get; set; } = null!;

    public RsvpSummaryDto RsvpCounts { get; set; } = new();

    // The answer of the current user; null if they are not invited
    public RsvpStatus? MyRsvp { get; set; }

    // True if the current user is the organizer or a family manager
    public bool CanManage { get; set; }

    // Download link of the invitation attachment, if one was uploaded
    public string? InvitationUrl { get; set; }

    // UTC
    public DateTime CreatedAt { get; set; }

    // UTC
    public DateTime? UpdatedAt { get; set; }

    // Sent back on update for concurrency checking
    public Guid Version { get; set; }
}
