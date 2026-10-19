using my_project.Core.Enums;

namespace my_project.Core.Entities;

public class FamilyTask
{
    public int Id { get; set; }

    // FK → Event (Cascade)
    public int EventId { get; set; }

    // Required, max 150
    public string Title { get; set; } = string.Empty;

    // Max 1000
    public string? Description { get; set; }

    public TaskCategory Category { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public FamilyTaskStatus Status { get; set; } = FamilyTaskStatus.Open;

    // UTC
    public DateTime? DueDate { get; set; }

    // FK → User (SetNull). null = free for volunteering
    public int? AssigneeId { get; set; }

    // UTC
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // UTC
    public DateTime? UpdatedAt { get; set; }

    // ⭐ Concurrency token – protects volunteering for a free task.
    // Included in the UPDATE's WHERE clause; the second request gets
    // DbUpdateConcurrencyException → 409
    public Guid Version { get; set; } = Guid.NewGuid();

    public Event Event { get; set; } = null!;
    public User? Assignee { get; set; }
}
