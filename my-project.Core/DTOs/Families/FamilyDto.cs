using my_project.Core.Enums;

namespace my_project.Core.DTOs.Families;

public class FamilyDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // The role of the current user in this family
    public FamilyRole MyRole { get; set; }

    public int MembersCount { get; set; }
}
