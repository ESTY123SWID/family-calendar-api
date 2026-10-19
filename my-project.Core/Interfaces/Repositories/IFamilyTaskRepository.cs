using my_project.Core.Common;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Models;

namespace my_project.Core.Interfaces.Repositories;

public interface IFamilyTaskRepository : IRepository<FamilyTask>
{
    // Tracked, ready for update
    Task<FamilyTask?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // Includes the Assignee. Optional filter by category (food / shopping / equipment lists)
    Task<IReadOnlyList<FamilyTask>> GetByEventIdAsync(
        int eventId,
        TaskCategory? category = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FamilyTask>> GetByAssigneeIdAsync(int assigneeId, CancellationToken cancellationToken = default);

    // Sets the Version the client last saw, so that saving fails with
    // ConcurrencyConflictException if someone else changed the task since
    void SetOriginalVersion(FamilyTask entity, Guid version);

    // ⭐ Tracked (the update must carry the loaded Version). Includes the Event and the Assignee
    Task<FamilyTask?> GetWithEventAsync(int id, CancellationToken cancellationToken = default);

    // Includes the Assignee. Filtering, sorting and paging run in the database
    Task<PagedResult<FamilyTask>> GetPagedForEventAsync(
        int eventId,
        TaskQueryParameters query,
        CancellationToken cancellationToken = default);

    // Tasks assigned to the user in all the family events. Includes the Event and the Assignee.
    // excludeDone = only tasks that are not Done
    Task<IReadOnlyList<FamilyTask>> GetForUserAsync(
        int familyId,
        int userId,
        FamilyTaskStatus? status = null,
        bool excludeDone = false,
        CancellationToken cancellationToken = default);

    // Tracked, ready for update. Tasks of the family events assigned to the user that are not Done
    // (released when the user leaves or is removed from the family)
    Task<IReadOnlyList<FamilyTask>> GetUnfinishedAssignedInFamilyAsync(
        int familyId,
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskStatusCount>> GetStatusCountsAsync(int eventId, CancellationToken cancellationToken = default);

    // Tasks that are not Done and whose DueDate is before nowUtc
    Task<int> CountOverdueAsync(int eventId, DateTime nowUtc, CancellationToken cancellationToken = default);

    // openOnly = only tasks that are not Done
    Task<int> CountAsync(bool openOnly = false, CancellationToken cancellationToken = default);

    // Read-only, for reminders. Assigned tasks of scheduled events that are not Done and are due before dueBeforeUtc.
    // Includes the Event and Assignee → NotificationSettings
    Task<IReadOnlyList<FamilyTask>> GetDueForRemindersAsync(DateTime dueBeforeUtc, CancellationToken cancellationToken = default);
}
