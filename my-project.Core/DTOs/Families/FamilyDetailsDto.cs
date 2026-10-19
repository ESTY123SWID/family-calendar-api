namespace my_project.Core.DTOs.Families;

public class FamilyDetailsDto : FamilyDto
{
    // Filled for family managers only
    public string? JoinCode { get; set; }

    // UTC
    public DateTime CreatedAt { get; set; }
}
