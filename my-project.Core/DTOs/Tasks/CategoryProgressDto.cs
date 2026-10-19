using my_project.Core.Enums;

namespace my_project.Core.DTOs.Tasks;

public class CategoryProgressDto
{
    public TaskCategory Category { get; set; }

    public int Total { get; set; }

    public int Done { get; set; }
}
