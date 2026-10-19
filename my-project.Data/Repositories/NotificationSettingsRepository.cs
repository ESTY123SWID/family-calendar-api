using Microsoft.EntityFrameworkCore;
using my_project.Core.Entities;
using my_project.Core.Interfaces.Repositories;

namespace my_project.Data.Repositories;

public class NotificationSettingsRepository : Repository<NotificationSettings>, INotificationSettingsRepository
{
    public NotificationSettingsRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<NotificationSettings?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([userId], cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationSettings>> GetByUserIdsAsync(
        IEnumerable<int> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            return Array.Empty<NotificationSettings>();
        }

        return await DbSet
            .AsNoTracking()
            .Include(settings => settings.User)
            .Where(settings => ids.Contains(settings.UserId))
            .ToListAsync(cancellationToken);
    }
}
