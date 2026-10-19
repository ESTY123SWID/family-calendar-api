namespace my_project.Core.DTOs.Admin;

public class AdminFamilyDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string JoinCode { get; set; } = string.Empty;

    // One of the family managers (a family may have several)
    public string? ManagerName { get; set; }

    public int MembersCount { get; set; }

    public int EventsCount { get; set; }

    // UTC
    public DateTime CreatedAt { get; set; }
}
