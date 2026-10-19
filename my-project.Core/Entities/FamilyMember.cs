using my_project.Core.Enums;

namespace my_project.Core.Entities;

// Join entity: User ↔ Family (many-to-many). Composite key (FamilyId, UserId)
public class FamilyMember
{
    // FK → Family (Cascade)
    public int FamilyId { get; set; }

    // FK → User (Cascade)
    public int UserId { get; set; }

    public FamilyRole Role { get; set; } = FamilyRole.Member;

    // UTC
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;
    public User User { get; set; } = null!;
}
