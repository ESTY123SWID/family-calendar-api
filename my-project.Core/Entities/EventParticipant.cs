using my_project.Core.Enums;

namespace my_project.Core.Entities;

// Join entity: User ↔ Event (many-to-many). Composite key (EventId, UserId)
public class EventParticipant
{
    // FK → Event (Cascade)
    public int EventId { get; set; }

    // FK → User (Restrict). Must be a member of the same family
    public int UserId { get; set; }

    public RsvpStatus RsvpStatus { get; set; } = RsvpStatus.Pending;

    // 0–20, how many people come with them
    public int GuestsCount { get; set; }

    // UTC
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;

    // UTC
    public DateTime? RespondedAt { get; set; }

    public Event Event { get; set; } = null!;
    public User User { get; set; } = null!;
}
