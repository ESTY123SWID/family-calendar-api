using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using my_project.Core.Common;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Interfaces.Repositories;
using my_project.Core.Models;
using my_project.Data.Extensions;

namespace my_project.Data.Repositories;

public class EventRepository : Repository<Event>, IEventRepository
{
    // The list counts are computed inside the SQL query (no N+1)
    private static readonly Expression<Func<Event, EventListItem>> ToListItem = evt =>
        new EventListItem(
            evt,
            evt.Organizer.FullName,
            evt.Participants.Count(participant => participant.RsvpStatus == RsvpStatus.Coming),
            evt.Tasks.Count(task => task.Status != FamilyTaskStatus.Done));

    public EventRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetByFamilyIdAsync(
        int familyId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(evt => evt.FamilyId == familyId);

        if (fromUtc.HasValue)
        {
            query = query.Where(evt => evt.StartAt >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(evt => evt.StartAt <= toUtc.Value);
        }

        return await query
            .OrderBy(evt => evt.StartAt)
            .ToListAsync(cancellationToken);
    }

    public void SetOriginalVersion(Event entity, Guid version)
    {
        Context.Entry(entity).Property(evt => evt.Version).OriginalValue = version;
    }

    public async Task<Event?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(evt => evt.Organizer)
            .Include(evt => evt.Participants)
                .ThenInclude(participant => participant.User)
            .FirstOrDefaultAsync(evt => evt.Id == id, cancellationToken);
    }

    public async Task<PagedResult<EventListItem>> GetPagedForFamilyAsync(
        int familyId,
        EventQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var events = DbSet
            .AsNoTracking()
            .Where(evt => evt.FamilyId == familyId);

        if (query.From.HasValue)
        {
            events = events.Where(evt => evt.StartAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            events = events.Where(evt => evt.StartAt <= query.To.Value);
        }

        if (query.Status.HasValue)
        {
            events = events.Where(evt => evt.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = LikePattern.Contains(query.Search.Trim());
            events = events.Where(evt =>
                EF.Functions.ILike(evt.Title, pattern)
                || (evt.LocationName != null && EF.Functions.ILike(evt.LocationName, pattern))
                || EF.Functions.ILike(evt.Address, pattern));
        }

        var ordered = query.SortBy?.ToLowerInvariant() switch
        {
            "title" => events.OrderByDirection(evt => evt.Title, query.SortDirection),
            "createdat" => events.OrderByDirection(evt => evt.CreatedAt, query.SortDirection),
            _ => events.OrderByDirection(evt => evt.StartAt, query.SortDirection)
        };

        return await ordered
            .ThenBy(evt => evt.Id)
            .Select(ToListItem)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<EventListItem?> GetNextForUserAsync(
        int familyId,
        int userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(evt => evt.FamilyId == familyId
                && evt.Status == EventStatus.Scheduled
                && evt.StartAt > nowUtc
                && evt.Participants.Any(participant => participant.UserId == userId))
            .OrderBy(evt => evt.StartAt)
            .Select(ToListItem)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EventListItem>> GetPendingRsvpForUserAsync(
        int familyId,
        int userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(evt => evt.FamilyId == familyId
                && evt.Status == EventStatus.Scheduled
                && evt.StartAt > nowUtc
                && evt.Participants.Any(participant =>
                    participant.UserId == userId && participant.RsvpStatus == RsvpStatus.Pending))
            .OrderBy(evt => evt.StartAt)
            .Select(ToListItem)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetScheduledWithParticipantsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(evt => evt.Participants)
                .ThenInclude(participant => participant.User)
                    .ThenInclude(user => user.NotificationSettings)
            .Where(evt => evt.Status == EventStatus.Scheduled
                && evt.StartAt >= fromUtc
                && evt.StartAt <= toUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(DateTime? startingAfterUtc = null, CancellationToken cancellationToken = default)
    {
        return startingAfterUtc.HasValue
            ? await DbSet.CountAsync(
                evt => evt.Status == EventStatus.Scheduled && evt.StartAt > startingAfterUtc.Value,
                cancellationToken)
            : await DbSet.CountAsync(cancellationToken);
    }
}
