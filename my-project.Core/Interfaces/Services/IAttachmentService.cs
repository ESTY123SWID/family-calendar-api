using my_project.Core.Common;
using my_project.Core.DTOs.Attachments;
using my_project.Core.Enums;

namespace my_project.Core.Interfaces.Services;

public interface IAttachmentService
{
    Task<ServiceResult<IReadOnlyList<AttachmentDto>>> GetForEventAsync(
        int eventId,
        AttachmentKind? kind = null,
        CancellationToken cancellationToken = default);

    // Image / Document: any family member.
    // Invitation: organizer or family manager, one per event (replaces the previous one)
    Task<ServiceResult<AttachmentDto>> UploadAsync(
        int eventId,
        UploadAttachmentRequest request,
        CancellationToken cancellationToken = default);

    // Any member of the event family. The caller disposes the returned stream
    Task<ServiceResult<AttachmentContentDto>> GetContentAsync(int id, CancellationToken cancellationToken = default);

    // The uploader, the organizer or a family manager
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
