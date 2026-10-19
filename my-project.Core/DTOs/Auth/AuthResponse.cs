using my_project.Core.DTOs.Users;

namespace my_project.Core.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;

    // UTC
    public DateTime ExpiresAt { get; set; }

    public UserDto User { get; set; } = null!;
}
