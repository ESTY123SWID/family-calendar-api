using Microsoft.EntityFrameworkCore;
using my_project.Core.Common;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Interfaces.Repositories;
using my_project.Core.Models;
using my_project.Data.Extensions;

namespace my_project.Data.Repositories;

public class FamilyTaskRepository : Repository<FamilyTask>, IFamilyTaskRepository
{
    public FamilyTaskRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<FamilyTask?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<FamilyTask>> GetByEventIdAsync(
        int eventId,
        TaskCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(task => task.Assignee)
            .Where(task => task.EventId == eventId);

        if (category.HasValue)
        {
            query = query.Where(task => task.Category == category.Value);
        }

        return await query
            .OrderBy(task => task.Status)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FamilyTask>> GetByAssigneeIdAsync(int assigneeId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(task => task.AssigneeId == assigneeId)
            .OrderBy(task => task.DueDate)
            .ToListAsync(cancellationToken);
    }

    public void SetOriginalVersion(FamilyTask entity, Guid version)
    {
        Context.Entry(entity).Property(task => task.Version).OriginalValue = version;
    }

    public async Task<FamilyTask?> GetWithEventAsync(int id, CancellationToken cancellationToken = default)
    {
        // ⭐ Tracked on purpose: the loaded Version goes into the WHERE clause of the UPDATE
        return await DbSet
            .Include(task => task.Event)
            .Include(task => task.Assignee)
            .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
    }

    public async Task<PagedResult<FamilyTask>> GetPagedForEventAsync(
        int eventId,
        TaskQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var tasks = DbSet
            .AsNoTracking()
            .Include(task => task.Assignee)
            .Where(task => task.EventId == eventId);

        if (query.Category.HasValue)
        {
            tasks = tasks.Where(task => task.Category == query.Category.Value);
        }

        if (query.Status.HasValue)
        {
            tasks = tasks.Where(task => task.Status == query.Status.Value);
        }

        if (query.AssigneeId.HasValue)
        {
            tasks = tasks.Where(task => task.AssigneeId == query.AssigneeId.Value);
        }

        if (query.OpenOnly)
        {
            tasks = tasks.Where(task => task.AssigneeId == null);
        }

        var ordered = query.SortBy?.ToLowerInvariant() switch
        {
            "priority" => tasks.OrderByDirection(task => task.Priority, query.SortDirection),
            "duedate" => tasks.OrderByDirection(task => task.DueDate, query.SortDirection),
            "title" => tasks.OrderByDirection(task => task.Title, query.SortDirection),
            "createdat" => tasks.OrderByDirection(task => task.CreatedAt, query.SortDirection),
            _ => tasks.OrderByDirection(task => task.Status, query.SortDirection)
                .ThenByDescending(task => task.Priority)
        };

        return await ordered
            .ThenBy(task => task.Id)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<IReadOnlyList<FamilyTask>> GetForUserAsync(
        int familyId,
        int userId,
        FamilyTaskStatus? status = null,
        bool excludeDone = false,
        CancellationToken cancellationToken = default)
    {
        var tasks = DbSet
            .AsNoTracking()
            .Include(task => task.Event)
            .Include(task => task.Assignee)
            .Where(task => task.AssigneeId == userId && task.Event.FamilyId == familyId);

        if (status.HasValue)
        {
            tasks = tasks.Where(task => task.Status == status.Value);
        }

        if (excludeDone)
        {
            tasks = tasks.Where(task => task.Status != FamilyTaskStatus.Done);
        }

        // Tasks without a due date come last
        return await tasks
            .OrderBy(task => task.DueDate == null)
            .ThenBy(task => task.DueDate)
            .ThenBy(task => task.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FamilyTask>> GetUnfinishedAssignedInFamilyAsync(
        int familyId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(task => task.AssigneeId == userId
                && task.Event.FamilyId == familyId
                && task.Status != FamilyTaskStatus.Done)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskStatusCount>> GetStatusCountsAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(task => task.EventId == eventId)
            .GroupBy(task => new { task.Category, task.Status })
            .Select(group => new TaskStatusCount(group.Key.Category, group.Key.Status, group.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountOverdueAsync(int eventId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(
            task => task.EventId == eventId
                && task.Status != FamilyTaskStatus.Done
                && task.DueDate != null
                && task.DueDate < nowUtc,
            cancellationToken);
    }

    public async Task<int> CountAsync(bool openOnly = false, CancellationToken cancellationToken = default)
    {
        return openOnly
            ? await DbSet.CountAsync(task => task.Status != FamilyTaskStatus.Done, cancellationToken)
            : await DbSet.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FamilyTask>> GetDueForRemindersAsync(
        DateTime dueBeforeUtc,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(task => task.Event)
            .Include(task => task.Assignee)
                .ThenInclude(user => user!.NotificationSettings)
            .Where(task => task.AssigneeId != null
                && task.Status != FamilyTaskStatus.Done
                && task.DueDate != null
                && task.DueDate < dueBeforeUtc
                && task.Event.Status == EventStatus.Scheduled)
            .ToListAsync(cancellationToken);
    }
}
