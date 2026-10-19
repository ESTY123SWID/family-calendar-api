using my_project.Core.DTOs.Events;
using my_project.Core.DTOs.Tasks;
using my_project.Core.DTOs.Updates;

namespace my_project.Core.DTOs.Dashboard;

// The home page of the current user within one family
public class DashboardDto
{
    // The next scheduled event the user is invited to, or null
    public EventSummaryDto? NextEvent { get; set; }

    // Tasks assigned to the user that are not Done
    public IReadOnlyList<TaskDto> MyOpenTasks { get; set; } = Array.Empty<TaskDto>();

    // Upcoming events the user has not answered yet
    public IReadOnlyList<EventSummaryDto> PendingRsvps { get; set; } = Array.Empty<EventSummaryDto>();

    public IReadOnlyList<EventUpdateDto> LatestUpdates { get; set; } = Array.Empty<EventUpdateDto>();
}
