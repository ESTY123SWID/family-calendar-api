using my_project.Core.Enums;

namespace my_project.Core.DTOs.Users;

// Never contains PasswordHash
public class UserDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public SystemRole Role { get; set; }
}
