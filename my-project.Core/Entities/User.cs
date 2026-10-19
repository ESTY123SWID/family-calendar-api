using my_project.Core.Enums;

namespace my_project.Core.Entities;

public class User
{
    public int Id { get; set; }

    // Required, max 100
    public string FullName { get; set; } = string.Empty;

    // Required, max 200, unique
    public string Email { get; set; } = string.Empty;

    // Required. Never exposed in a DTO and never written to logs
    public string PasswordHash { get; set; } = string.Empty;

    public SystemRole Role { get; set; } = SystemRole.User;

    // false = blocked, login returns 401
    public bool IsActive { get; set; } = true;

    // UTC
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public NotificationSettings? NotificationSettings { get; set; }
    public ICollection<FamilyMember> FamilyMemberships { get; set; } = new List<FamilyMember>();
    public ICollection<Family> CreatedFamilies { get; set; } = new List<Family>();
    public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
    public ICollection<EventParticipant> EventParticipations { get; set; } = new List<EventParticipant>();
    public ICollection<EventUpdate> EventUpdates { get; set; } = new List<EventUpdate>();
    public ICollection<FamilyTask> AssignedTasks { get; set; } = new List<FamilyTask>();
    public ICollection<EventAttachment> UploadedAttachments { get; set; } = new List<EventAttachment>();
}
