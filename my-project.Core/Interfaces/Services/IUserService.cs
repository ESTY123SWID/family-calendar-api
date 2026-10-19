using my_project.Core.Common;
using my_project.Core.DTOs.Users;

namespace my_project.Core.Interfaces.Services;

public interface IUserService
{
    Task<ServiceResult<UserDto>> GetMeAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateMeAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);

    // Allowed for the user, members of a common family and the system admin
    Task<ServiceResult<UserDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
