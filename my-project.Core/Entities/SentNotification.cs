using my_project.Core.Enums;

namespace my_project.Core.Entities;

// Log of automatic emails, so the same reminder is not sent twice.
// Internal to the system only, no endpoint.
// Unique index on (UserId, Type, EventId, TaskId)
public class SentNotification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public NotificationType Type { get; set; }

    public int? EventId { get; set; }

    public int? TaskId { get; set; }

    // UTC
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
