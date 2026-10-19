

using my_project.Core.Enums;

namespace my_project.Core.Entities;

public class Event
{
    public int Id { get; set; }

    // FK → Family (Cascade)
    public int FamilyId { get; set; }

    // FK → User (Restrict). The user who created the event
    public int OrganizerId { get; set; }

    // Required, max 150
    public string Title { get; set; } = string.Empty;

    // Max 2000
    public string? Description { get; set; }

    // UTC
    public DateTime StartAt { get; set; }

    // UTC, must be after StartAt

    public DateTime? EndAt { get; set; }

    // Max 150, e.g. "At grandma and grandpa's"
    public string? LocationName { get; set; }

    // Required, max 300. The map link is built from it in the DTO
    public string Address { get; set; } = string.Empty;

    // Max 1000, directions for getting there
    public string? Directions { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Scheduled;

    // UTC
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // UTC, updated automatically
    public DateTime? UpdatedAt { get; set; }

    // Concurrency token – two people editing at once → 409
    public Guid Version { get; set; } = Guid.NewGuid();

    public Family Family { get; set; } = null!;
    public User Organizer { get; set; } = null!;
    public ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();
    public ICollection<EventUpdate> Updates { get; set; } = new List<EventUpdate>();
    public ICollection<FamilyTask> Tasks { get; set; } = new List<FamilyTask>();
    public ICollection<EventAttachment> Attachments { get; set; } = new List<EventAttachment>();
}
