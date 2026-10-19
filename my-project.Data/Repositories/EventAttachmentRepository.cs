using Microsoft.EntityFrameworkCore;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Interfaces.Repositories;

namespace my_project.Data.Repositories;

public class EventAttachmentRepository : Repository<EventAttachment>, IEventAttachmentRepository
{
    public EventAttachmentRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<EventAttachment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<EventAttachment>> GetByEventIdAsync(
        int eventId,
        AttachmentKind? kind = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(attachment => attachment.UploadedBy)
            .Where(attachment => attachment.EventId == eventId);

        if (kind.HasValue)
        {
            query = query.Where(attachment => attachment.Kind == kind.Value);
        }

        return await query
            .OrderByDescending(attachment => attachment.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<EventAttachment?> GetInvitationAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(
            attachment => attachment.EventId == eventId && attachment.Kind == AttachmentKind.Invitation,
            cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetStoredFileNamesForFamilyAsync(
        int familyId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(attachment => attachment.Event.FamilyId == familyId)
            .Select(attachment => attachment.StoredFileName)
            .ToListAsync(cancellationToken);
    }
}
