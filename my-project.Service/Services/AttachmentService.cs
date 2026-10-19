using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using my_project.Core.Common;
using my_project.Core.DTOs.Attachments;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Interfaces.Services;
using my_project.Service.Common;
using my_project.Service.Options;

namespace my_project.Service.Services;

public class AttachmentService : IAttachmentService
{
    private const int MaxFileNameLength = 255;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IFamilyAccessService _access;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly IFileStorage _fileStorage;
    private readonly UploadSettings _uploadSettings;
    private readonly ILogger<AttachmentService> _logger;

    public AttachmentService(
        IUnitOfWork unitOfWork,
        IFamilyAccessService access,
        ICurrentUser currentUser,
        IClock clock,
        IMapper mapper,
        IFileStorage fileStorage,
        IOptions<UploadSettings> uploadSettings,
        ILogger<AttachmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _access = access;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _fileStorage = fileStorage;
        _uploadSettings = uploadSettings.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<IReadOnlyList<AttachmentDto>>> GetForEventAsync(
        int eventId,
        AttachmentKind? kind = null,
        CancellationToken cancellationToken = default)
    {
        var evt = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return ServiceResult<IReadOnlyList<AttachmentDto>>.NotFound(ErrorMessages.EventNotFound);
        }

        var access = await _access.EnsureMemberAsync(evt.FamilyId, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<IReadOnlyList<AttachmentDto>>.FromFailure(access);
        }

        var attachments = await _unitOfWork.EventAttachments.GetByEventIdAsync(eventId, kind, cancellationToken);

        return ServiceResult<IReadOnlyList<AttachmentDto>>.Ok(_mapper.Map<List<AttachmentDto>>(attachments));
    }

    public async Task<ServiceResult<AttachmentDto>> UploadAsync(
        int eventId,
        UploadAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var me = _currentUser.UserId;

        var evt = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return ServiceResult<AttachmentDto>.NotFound(ErrorMessages.EventNotFound);
        }

        // Image / Document: any member. Invitation: organizer or family manager only
        var access = request.Kind == AttachmentKind.Invitation
            ? await _access.EnsureCanManageEventAsync(evt, me, cancellationToken)
            : await _access.EnsureMemberAsync(evt.FamilyId, me, cancellationToken);

        if (!access.IsSuccess)
        {
            return ServiceResult<AttachmentDto>.FromFailure(access);
        }

        var (validation, fileType) = await ValidateFileAsync(request, cancellationToken);
        if (validation is not null)
        {
            return ServiceResult<AttachmentDto>.Validation(validation);
        }

        var uploader = await _unitOfWork.Users.GetByIdAsync(me, cancellationToken);
        if (uploader is null)
        {
            return ServiceResult<AttachmentDto>.NotFound(ErrorMessages.UserNotFound);
        }

        // Only one invitation per event: the new upload replaces the previous one
        var previousInvitation = request.Kind == AttachmentKind.Invitation
            ? await _unitOfWork.EventAttachments.GetInvitationAsync(eventId, cancellationToken)
            : null;

        var storedFileName = await _fileStorage.SaveAsync(request.Content, request.FileName, cancellationToken);

        var attachment = new EventAttachment
        {
            EventId = eventId,
            UploadedById = me,
            UploadedBy = uploader,
            OriginalFileName = Truncate(Path.GetFileName(request.FileName), MaxFileNameLength),
            StoredFileName = storedFileName,
            // Decided by the server from the (validated) extension, not by the client
            ContentType = fileType!.ContentType,
            SizeBytes = request.SizeBytes,
            Kind = request.Kind,
            UploadedAt = _clock.UtcNow
        };

        if (previousInvitation is not null)
        {
            _unitOfWork.EventAttachments.Remove(previousInvitation);
        }

        await _unitOfWork.EventAttachments.AddAsync(attachment, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // The row was not saved – do not leave the new file orphaned on disk
            await TryDeleteFileAsync(storedFileName, CancellationToken.None);
            throw;
        }

        if (previousInvitation is not null)
        {
            await TryDeleteFileAsync(previousInvitation.StoredFileName, cancellationToken);
        }

        _logger.LogInformation(
            "Attachment {AttachmentId} ({Kind}) uploaded to event {EventId} by user {UserId}",
            attachment.Id, attachment.Kind, eventId, me);

        return ServiceResult<AttachmentDto>.Ok(_mapper.Map<AttachmentDto>(attachment));
    }

    public async Task<ServiceResult<AttachmentContentDto>> GetContentAsync(int id, CancellationToken cancellationToken = default)
    {
        var attachment = await _unitOfWork.EventAttachments.GetByIdAsync(id, cancellationToken);
        if (attachment is null)
        {
            return ServiceResult<AttachmentContentDto>.NotFound(ErrorMessages.AttachmentNotFound);
        }

        var evt = await _unitOfWork.Events.GetByIdAsync(attachment.EventId, cancellationToken);
        if (evt is null)
        {
            return ServiceResult<AttachmentContentDto>.NotFound(ErrorMessages.EventNotFound);
        }

        var access = await _access.EnsureMemberAsync(evt.FamilyId, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<AttachmentContentDto>.FromFailure(access);
        }

        Stream content;
        try
        {
            content = await _fileStorage.OpenReadAsync(attachment.StoredFileName, cancellationToken);
        }
        catch (FileNotFoundException exception)
        {
            // The row exists but the file is gone – a data problem worth an Error in the log
            _logger.LogError(exception, "File {StoredFileName} of attachment {AttachmentId} is missing", attachment.StoredFileName, id);
            return ServiceResult<AttachmentContentDto>.NotFound(ErrorMessages.AttachmentNotFound);
        }

        return ServiceResult<AttachmentContentDto>.Ok(new AttachmentContentDto
        {
            Content = content,
            ContentType = attachment.ContentType,
            FileName = attachment.OriginalFileName
        });
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var me = _currentUser.UserId;

        var attachment = await _unitOfWork.EventAttachments.GetByIdAsync(id, cancellationToken);
        if (attachment is null)
        {
            return ServiceResult.NotFound(ErrorMessages.AttachmentNotFound);
        }

        var evt = await _unitOfWork.Events.GetByIdAsync(attachment.EventId, cancellationToken);
        if (evt is null)
        {
            return ServiceResult.NotFound(ErrorMessages.EventNotFound);
        }

        var access = await _access.EnsureMemberAsync(evt.FamilyId, me, cancellationToken);
        if (!access.IsSuccess)
        {
            return access;
        }

        // The uploader, or the organizer / family manager
        if (attachment.UploadedById != me && !await _access.CanManageEventAsync(evt, me, cancellationToken))
        {
            return ServiceResult.Forbidden(ErrorMessages.AttachmentDeleteForbidden);
        }

        _unitOfWork.EventAttachments.Remove(attachment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await TryDeleteFileAsync(attachment.StoredFileName, cancellationToken);

        return ServiceResult.Ok();
    }

    // Returns an error message (or null) and, for a valid file, its type.
    // Checks the size, the extension, and that the content really is a file of that type –
    // a renamed .exe is rejected even if it is called "photo.jpg"
    private async Task<(string? Error, FileTypes.FileType? FileType)> ValidateFileAsync(
        UploadAttachmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SizeBytes <= 0)
        {
            return (ErrorMessages.FileEmpty, null);
        }

        if (request.SizeBytes > _uploadSettings.MaxSizeBytes)
        {
            return (string.Format(ErrorMessages.FileTooLarge, _uploadSettings.MaxSizeMb), null);
        }

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!_uploadSettings.AllowedExtensions.Contains(extension) || !FileTypes.TryGet(extension, out var fileType))
        {
            return (string.Format(ErrorMessages.FileTypeNotAllowed, string.Join(", ", _uploadSettings.AllowedExtensions)), null);
        }

        if (!await MatchesSignatureAsync(request.Content, fileType, cancellationToken))
        {
            return (string.Format(ErrorMessages.FileContentMismatch, extension), null);
        }

        return (null, fileType);
    }

    // Reads the first bytes and rewinds, so the whole file is still saved afterwards
    private static async Task<bool> MatchesSignatureAsync(
        Stream content,
        FileTypes.FileType fileType,
        CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
        {
            throw new ArgumentException("The upload stream must be seekable so its content can be verified.", nameof(content));
        }

        var header = new byte[FileTypes.HeaderLength];
        var start = content.Position;
        var read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        content.Position = start;

        return fileType.MatchesSignature(header.AsSpan(0, read));
    }

    private async Task TryDeleteFileAsync(string storedFileName, CancellationToken cancellationToken)
    {
        try
        {
            await _fileStorage.DeleteAsync(storedFileName, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Failed to delete file {StoredFileName}", storedFileName);
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
