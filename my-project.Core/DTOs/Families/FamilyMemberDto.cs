using my_project.Core.Enums;

namespace my_project.Core.DTOs.Families;

public class FamilyMemberDto
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public FamilyRole Role { get; set; }

    // UTC
    public DateTime JoinedAt { get; set; }
}
