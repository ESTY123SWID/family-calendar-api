using my_project.Core.Entities;

namespace my_project.Core.Interfaces.Repositories;

public interface IEventUpdateRepository : IRepository<EventUpdate>
{
    Task<EventUpdate?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // Includes the Author, newest first
    Task<IReadOnlyList<EventUpdate>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default);

    // Latest updates of all the family events. Includes the Author and the Event, newest first
    Task<IReadOnlyList<EventUpdate>> GetLatestForFamilyAsync(
        int familyId,
        int count,
        CancellationToken cancellationToken = default);
}
