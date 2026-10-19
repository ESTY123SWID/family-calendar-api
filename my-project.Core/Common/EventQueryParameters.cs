using my_project.Core.Enums;

namespace my_project.Core.Common;

// SortBy: startAt (default), title, createdAt
public class EventQueryParameters : PagingParameters
{
    // UTC, filters on StartAt
    public DateTime? From { get; set; }

    // UTC, filters on StartAt
    public DateTime? To { get; set; }

    public EventStatus? Status { get; set; }

    // Searched in Title, LocationName and Address
    public string? Search { get; set; }
}
