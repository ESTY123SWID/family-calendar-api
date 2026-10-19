using my_project.Core.Entities;
using my_project.Core.Enums;

namespace my_project.Core.Interfaces.Repositories;

public interface IEventAttachmentRepository : IRepository<EventAttachment>
{
    // Tracked, ready for delete
    Task<EventAttachment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // Includes the UploadedBy user. Optional filter by kind
    Task<IReadOnlyList<EventAttachment>> GetByEventIdAsync(
        int eventId,
        AttachmentKind? kind = null,
        CancellationToken cancellationToken = default);

    // The single Invitation attachment of the event, if any (tracked)
    Task<EventAttachment?> GetInvitationAsync(int eventId, CancellationToken cancellationToken = default);

    // Stored file names of all the family attachments, so the files can be deleted with the family
    Task<IReadOnlyList<string>> GetStoredFileNamesForFamilyAsync(int familyId, CancellationToken cancellationToken = default);
}
