using my_project.Core.Common;
using my_project.Core.DTOs.Dashboard;

namespace my_project.Core.Interfaces.Services;

public interface IDashboardService
{
    Task<ServiceResult<DashboardDto>> GetAsync(int familyId, CancellationToken cancellationToken = default);
}
