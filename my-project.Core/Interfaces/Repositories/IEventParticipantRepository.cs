using my_project.Core.Entities;

namespace my_project.Core.Interfaces.Repositories;

public interface IEventParticipantRepository : IRepository<EventParticipant>
{
    // Tracked, ready for update
    Task<EventParticipant?> GetAsync(int eventId, int userId, CancellationToken cancellationToken = default);

    // Includes the User of each participant
    Task<IReadOnlyList<EventParticipant>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<EventParticipant> participants, CancellationToken cancellationToken = default);
}
