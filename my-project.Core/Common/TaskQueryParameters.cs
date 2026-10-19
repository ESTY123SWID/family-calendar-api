using my_project.Core.Enums;

namespace my_project.Core.Common;

// SortBy: status (default), priority, dueDate, title, createdAt
public class TaskQueryParameters : PagingParameters
{
    public TaskCategory? Category { get; set; }

    public FamilyTaskStatus? Status { get; set; }

    public int? AssigneeId { get; set; }

    // Only tasks that are free for volunteering (no assignee)
    public bool OpenOnly { get; set; }
}
