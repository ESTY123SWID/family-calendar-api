using AutoMapper;
using my_project.Core.Common;
using my_project.Core.DTOs.Updates;
using my_project.Core.Entities;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Interfaces.Services;
using my_project.Service.Common;

namespace my_project.Service.Services;

public class EventUpdateService : IEventUpdateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFamilyAccessService _access;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly INotificationService _notifications;

    public EventUpdateService(
        IUnitOfWork unitOfWork,
        IFamilyAccessService access,
        ICurrentUser currentUser,
        IClock clock,
        IMapper mapper,
        INotificationService notifications)
    {
        _unitOfWork = unitOfWork;
        _access = access;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _notifications = notifications;
    }

    public async Task<ServiceResult<IReadOnlyList<EventUpdateDto>>> GetForEventAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        var evt = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return ServiceResult<IReadOnlyList<EventUpdateDto>>.NotFound(ErrorMessages.EventNotFound);
        }

        var access = await _access.EnsureMemberAsync(evt.FamilyId, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<IReadOnlyList<EventUpdateDto>>.FromFailure(access);
        }

        var updates = await _unitOfWork.EventUpdates.GetByEventIdAsync(eventId, cancellationToken);

        return ServiceResult<IReadOnlyList<EventUpdateDto>>.Ok(_mapper.Map<List<EventUpdateDto>>(updates));
    }

    public async Task<ServiceResult<EventUpdateDto>> CreateAsync(
        int eventId,
        CreateEventUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var evt = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return ServiceResult<EventUpdateDto>.NotFound(ErrorMessages.EventNotFound);
        }

        var access = await _access.EnsureCanManageEventAsync(evt, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<EventUpdateDto>.FromFailure(access);
        }

        var author = await _unitOfWork.Users.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (author is null)
        {
            return ServiceResult<EventUpdateDto>.NotFound(ErrorMessages.UserNotFound);
        }

        var update = new EventUpdate
        {
            EventId = eventId,
            AuthorId = author.Id,
            Author = author,
            Content = request.Content.Trim(),
            CreatedAt = _clock.UtcNow
        };

        await _unitOfWork.EventUpdates.AddAsync(update, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyEventUpdatePostedAsync(evt, update, cancellationToken);

        return ServiceResult<EventUpdateDto>.Ok(_mapper.Map<EventUpdateDto>(update));
    }

    public async Task<ServiceResult> DeleteAsync(int eventId, int updateId, CancellationToken cancellationToken = default)
    {
        var evt = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return ServiceResult.NotFound(ErrorMessages.EventNotFound);
        }

        var access = await _access.EnsureCanManageEventAsync(evt, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access;
        }

        // The update must belong to this event (no deleting through another event URL)
        var update = await _unitOfWork.EventUpdates.GetByIdAsync(updateId, cancellationToken);
        if (update is null || update.EventId != eventId)
        {
            return ServiceResult.NotFound(ErrorMessages.UpdateNotFound);
        }

        _unitOfWork.EventUpdates.Remove(update);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok();
    }
}
