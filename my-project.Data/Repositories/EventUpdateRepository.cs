using Microsoft.EntityFrameworkCore;
using my_project.Core.Entities;
using my_project.Core.Interfaces.Repositories;

namespace my_project.Data.Repositories;

public class EventUpdateRepository : Repository<EventUpdate>, IEventUpdateRepository
{
    public EventUpdateRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<EventUpdate?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<EventUpdate>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(update => update.Author)
            .Where(update => update.EventId == eventId)
            .OrderByDescending(update => update.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EventUpdate>> GetLatestForFamilyAsync(
        int familyId,
        int count,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(update => update.Author)
            .Include(update => update.Event)
            .Where(update => update.Event.FamilyId == familyId)
            .OrderByDescending(update => update.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
