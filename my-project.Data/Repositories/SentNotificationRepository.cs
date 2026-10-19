using Microsoft.EntityFrameworkCore;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Interfaces.Repositories;

namespace my_project.Data.Repositories;

public class SentNotificationRepository : Repository<SentNotification>, ISentNotificationRepository
{
    public SentNotificationRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<bool> ExistsAsync(
        int userId,
        NotificationType type,
        int? eventId,
        int? taskId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            notification => notification.UserId == userId
                && notification.Type == type
                && notification.EventId == eventId
                && notification.TaskId == taskId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<SentNotification>> GetForTargetsAsync(
        IEnumerable<int> eventIds,
        IEnumerable<int> taskIds,
        CancellationToken cancellationToken = default)
    {
        var eventIdList = eventIds.Distinct().ToList();
        var taskIdList = taskIds.Distinct().ToList();

        if (eventIdList.Count == 0 && taskIdList.Count == 0)
        {
            return Array.Empty<SentNotification>();
        }

        return await DbSet
            .AsNoTracking()
            .Where(notification =>
                (notification.EventId != null && eventIdList.Contains(notification.EventId.Value))
                || (notification.TaskId != null && taskIdList.Contains(notification.TaskId.Value)))
            .ToListAsync(cancellationToken);
    }
}
