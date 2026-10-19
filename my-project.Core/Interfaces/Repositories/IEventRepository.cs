using my_project.Core.Common;
using my_project.Core.Entities;
using my_project.Core.Models;

namespace my_project.Core.Interfaces.Repositories;

public interface IEventRepository : IRepository<Event>
{
    // Tracked, ready for update
    Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // Optional UTC range filter on StartAt, ordered by StartAt
    Task<IReadOnlyList<Event>> GetByFamilyIdAsync(
        int familyId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default);

    // Sets the Version the client last saw, so that saving fails with
    // ConcurrencyConflictException if someone else changed the event since
    void SetOriginalVersion(Event entity, Guid version);

    // Read-only. Includes the Organizer and the Participants with their User
    Task<Event?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    // Filtering, sorting and paging run in the database
    Task<PagedResult<EventListItem>> GetPagedForFamilyAsync(
        int familyId,
        EventQueryParameters query,
        CancellationToken cancellationToken = default);

    // The next scheduled event of the family, starting after nowUtc, that the user is invited to
    Task<EventListItem?> GetNextForUserAsync(
        int familyId,
        int userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    // Scheduled future events of the family where the RSVP of the user is still Pending
    Task<IReadOnlyList<EventListItem>> GetPendingRsvpForUserAsync(
        int familyId,
        int userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    // Read-only, for reminders. Scheduled events starting in the range,
    // including Participants → User → NotificationSettings
    Task<IReadOnlyList<Event>> GetScheduledWithParticipantsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    // startingAfterUtc set = only scheduled events that start after it
    Task<int> CountAsync(DateTime? startingAfterUtc = null, CancellationToken cancellationToken = default);
}
