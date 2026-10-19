using Microsoft.EntityFrameworkCore;
using my_project.Core.Entities;
using my_project.Core.Interfaces.Repositories;

namespace my_project.Data.Repositories;

public class EventParticipantRepository : Repository<EventParticipant>, IEventParticipantRepository
{
    public EventParticipantRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<EventParticipant?> GetAsync(int eventId, int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([eventId, userId], cancellationToken);
    }

    public async Task<IReadOnlyList<EventParticipant>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(participant => participant.User)
            .Where(participant => participant.EventId == eventId)
            .OrderBy(participant => participant.User.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<EventParticipant> participants, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(participants, cancellationToken);
    }
}
