using my_project.Core.Entities;

namespace my_project.Core.Interfaces.Repositories;

public interface INotificationSettingsRepository : IRepository<NotificationSettings>
{
    // Tracked, ready for update
    Task<NotificationSettings?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    // Read-only. Includes the User (for the email address)
    Task<IReadOnlyList<NotificationSettings>> GetByUserIdsAsync(
        IEnumerable<int> userIds,
        CancellationToken cancellationToken = default);
}
