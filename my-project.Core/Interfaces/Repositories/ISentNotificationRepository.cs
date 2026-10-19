using my_project.Core.Entities;
using my_project.Core.Enums;

namespace my_project.Core.Interfaces.Repositories;

public interface ISentNotificationRepository : IRepository<SentNotification>
{
    // Used to avoid sending the same reminder twice
    Task<bool> ExistsAsync(
        int userId,
        NotificationType type,
        int? eventId,
        int? taskId,
        CancellationToken cancellationToken = default);

    // All notifications already sent for these events or tasks, loaded in one query
    Task<IReadOnlyList<SentNotification>> GetForTargetsAsync(
        IEnumerable<int> eventIds,
        IEnumerable<int> taskIds,
        CancellationToken cancellationToken = default);
}
