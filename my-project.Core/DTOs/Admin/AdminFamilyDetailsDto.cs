using my_project.Core.DTOs.Families;

namespace my_project.Core.DTOs.Admin;

public class AdminFamilyDetailsDto : AdminFamilyDto
{
    public IReadOnlyList<FamilyMemberDto> Members { get; set; } = Array.Empty<FamilyMemberDto>();
}
