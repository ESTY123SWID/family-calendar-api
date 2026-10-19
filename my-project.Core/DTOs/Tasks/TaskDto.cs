using my_project.Core.Enums;

namespace my_project.Core.DTOs.Tasks;

public class TaskDto
{
    public int Id { get; set; }

    public int EventId { get; set; }

    // Filled when the task is shown outside its event ("my tasks", dashboard)
    public string? EventTitle { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskCategory Category { get; set; }

    public TaskPriority Priority { get; set; }

    public FamilyTaskStatus Status { get; set; }

    // UTC
    public DateTime? DueDate { get; set; }

    // null = free for volunteering
    public int? AssigneeId { get; set; }

    public string? AssigneeName { get; set; }

    // DueDate has passed and the task is not Done
    public bool IsOverdue { get; set; }

    // UTC
    public DateTime CreatedAt { get; set; }

    // UTC
    public DateTime? UpdatedAt { get; set; }

    // Sent back on update for concurrency checking
    public Guid Version { get; set; }
}
