using my_project.Core.Entities;

namespace my_project.Core.Models;

public record AdminFamilyListItem(Family Family, string? ManagerName, int MembersCount, int EventsCount);
