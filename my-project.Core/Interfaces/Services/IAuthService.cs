using my_project.Core.Common;
using my_project.Core.DTOs.Auth;

namespace my_project.Core.Interfaces.Services;

public interface IAuthService
{
    // Email already registered → Conflict
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    // Unknown email / wrong password / blocked user → Unauthorized, with the same message
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
