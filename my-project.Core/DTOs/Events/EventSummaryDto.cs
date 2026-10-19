using my_project.Core.Enums;

namespace my_project.Core.DTOs.Events;

// An event in a list
public class EventSummaryDto
{
    public int Id { get; set; }

    public int FamilyId { get; set; }

    public string Title { get; set; } = string.Empty;

    // UTC
    public DateTime StartAt { get; set; }

    // UTC
    public DateTime? EndAt { get; set; }

    public string? LocationName { get; set; }

    public string Address { get; set; } = string.Empty;

    public EventStatus Status { get; set; }

    public int OrganizerId { get; set; }

    public string OrganizerName { get; set; } = string.Empty;

    // Participants who answered Coming
    public int ComingCount { get; set; }

    // Tasks that are not Done
    public int OpenTasksCount { get; set; }
}
