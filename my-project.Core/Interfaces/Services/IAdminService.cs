using my_project.Core.Common;
using my_project.Core.DTOs.Admin;
using my_project.Core.DTOs.Events;

namespace my_project.Core.Interfaces.Services;

// System admin only
public interface IAdminService
{
    Task<ServiceResult<AdminStatsDto>> GetStatsAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<AdminUserDto>>> GetUsersAsync(
        AdminQueryParameters query,
        CancellationToken cancellationToken = default);

    // Block / unblock / change system role. Blocking or demoting yourself → Conflict
    Task<ServiceResult> UpdateUserAsync(int id, AdminUpdateUserRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<AdminFamilyDto>>> GetFamiliesAsync(
        AdminQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminFamilyDetailsDto>> GetFamilyAsync(int id, CancellationToken cancellationToken = default);

    // Read-only list of any family's events (the members' endpoint requires membership)
    Task<ServiceResult<PagedResult<EventSummaryDto>>> GetFamilyEventsAsync(
        int id,
        EventQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteFamilyAsync(int id, CancellationToken cancellationToken = default);
}
