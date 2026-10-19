using my_project.Core.Entities;

namespace my_project.Core.Models;

// An event with the counts shown in lists, computed inside the database query
public record EventListItem(Event Event, string OrganizerName, int ComingCount, int OpenTasksCount);
    