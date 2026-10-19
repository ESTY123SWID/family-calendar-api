namespace my_project.Core.DTOs.Tasks;

public class TaskProgressDto
{
    public int Total { get; set; }

    public int Done { get; set; }

    // Not Done yet
    public int Open { get; set; }

    // Not Done and DueDate has passed
    public int Overdue { get; set; }

    public IReadOnlyList<CategoryProgressDto> ByCategory { get; set; } = Array.Empty<CategoryProgressDto>();
}
