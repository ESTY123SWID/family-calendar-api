using my_project.Core.DTOs.Users;

namespace my_project.Core.DTOs.Admin;

public class AdminUserDto : UserDto
{
    public bool IsActive { get; set; }

    public int FamiliesCount { get; set; }

    // UTC
    public DateTime CreatedAt { get; set; }
}
