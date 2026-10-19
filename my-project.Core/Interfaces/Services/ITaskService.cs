using my_project.Core.Common;
using my_project.Core.DTOs.Tasks;
using my_project.Core.Enums;

namespace my_project.Core.Interfaces.Services;

public interface ITaskService
{
    Task<ServiceResult<PagedResult<TaskDto>>> GetPagedAsync(
        int eventId,
        TaskQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TaskProgressDto>> GetProgressAsync(int eventId, CancellationToken cancellationToken = default);

    // Tasks assigned to the current user in the family events
    Task<ServiceResult<IReadOnlyList<TaskDto>>> GetMineAsync(
        int familyId,
        FamilyTaskStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TaskDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // Organizer or family manager
    Task<ServiceResult<TaskDto>> CreateAsync(int eventId, CreateTaskRequest request, CancellationToken cancellationToken = default);

    // Organizer or family manager. A stale Version → Conflict
    Task<ServiceResult> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken = default);

    // Organizer or family manager
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    // ⭐ A member volunteers for a free task (for themselves only);
    // the organizer / family manager may assign anyone in the family.
    // Someone else took the task first → Conflict
    Task<ServiceResult> AssignAsync(int id, AssignTaskRequest request, CancellationToken cancellationToken = default);

    // The assignee, the organizer or a family manager. The task goes back to Open
    Task<ServiceResult> UnassignAsync(int id, CancellationToken cancellationToken = default);

    // The assignee, the organizer or a family manager
    Task<ServiceResult> UpdateStatusAsync(int id, UpdateTaskStatusRequest request, CancellationToken cancellationToken = default);
}
