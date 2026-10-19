using my_project.Core.Entities;
using my_project.Core.Enums;

namespace my_project.Core.Models;

// A family as seen by one of its members
public record FamilyListItem(Family Family, FamilyRole MyRole, int MembersCount);
