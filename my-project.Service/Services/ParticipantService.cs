using AutoMapper;
using Microsoft.Extensions.Logging;
using my_project.Core.Common;
using my_project.Core.DTOs.Participants;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Interfaces.Services;
using my_project.Service.Common;

namespace my_project.Service.Services;

public class ParticipantService : IParticipantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFamilyAccessService _access;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly INotificationService _notifications;
    private readonly ILogger<ParticipantService> _logger;

    public ParticipantService(
        IUnitOfWork unitOfWork,
        IFamilyAccessService access,
        ICurrentUser currentUser,
        IClock clock,
        IMapper mapper,
        INotificationService notifications,
        ILogger<ParticipantService> logger)
    {
        _unitOfWork = unitOfWork;
        _access = access;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<ServiceResult<EventParticipantsDto>> GetForEventAsync(
        int eventId,
        RsvpStatus? rsvpStatus = null,
        CancellationToken cancellationToken = default)
    {
        var evt = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return ServiceResult<EventParticipantsDto>.NotFound(ErrorMessages.EventNotFound);
        }

        var access = await _access.EnsureMemberAsync(evt.FamilyId, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<EventParticipantsDto>.FromFailure(access);
        }

        var participants = await _unitOfWork.EventParticipants.GetByEventIdAsync(eventId, cancellationToken);

        var filtered = rsvpStatus.HasValue
            ? participants.Where(participant => participant.RsvpStatus == rsvpStatus.Value)
            : participants;

        return ServiceResult<EventParticipantsDto>.Ok(new EventParticipantsDto
        {
            Items = _mapper.Map<List<ParticipantDto>>(filtered),
            Summary = DomainRules.BuildRsvpSummary(participants)
        });
    }

    public async Task<ServiceResult<IReadOnlyList<ParticipantDto>>> InviteAsync(
        int eventId,
        InviteParticipantsRequest request,
        CancellationToken cancellationToken = default)
    {
        var evt = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return ServiceResult<IReadOnlyList<ParticipantDto>>.NotFound(ErrorMessages.EventNotFound);
        }

        var access = await _access.EnsureCanManageEventAsync(evt, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<IReadOnlyList<ParticipantDto>>.FromFailure(access);
        }

        if (evt.Status == EventStatus.Cancelled)
        {
            return ServiceResult<IReadOnlyList<ParticipantDto>>.Validation(ErrorMessages.EventCancelled);
        }

        var members = (await _unitOfWork.FamilyMembers.GetByFamilyIdAsync(evt.FamilyId, cancellationToken))
            .ToDictionary(member => member.UserId);

        var requestedIds = request.UserIds.Distinct().ToList();
        if (requestedIds.Any(userId => !members.ContainsKey(userId)))
        {
            return ServiceResult<IReadOnlyList<ParticipantDto>>.Validation(ErrorMessages.InviteesNotMembers);
        }

        // Already invited users are skipped, not an error
        var existingIds = (await _unitOfWork.EventParticipants.GetByEventIdAsync(eventId, cancellationToken))
            .Select(participant => participant.UserId)
            .ToHashSet();

        var now = _clock.UtcNow;
        var newParticipants = requestedIds
            .Where(userId => !existingIds.Contains(userId))
            .Select(userId => new EventParticipant
            {
                EventId = eventId,
                UserId = userId,
                RsvpStatus = RsvpStatus.Pending,
                InvitedAt = now
            })
            .ToList();

        if (newParticipants.Count > 0)
        {
            await _unitOfWork.EventParticipants.AddRangeAsync(newParticipants, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("{Count} participants invited to event {EventId}", newParticipants.Count, eventId);

            await _notifications.NotifyInvitedAsync(evt, newParticipants.Select(participant => participant.UserId), cancellationToken);
        }

        var result = newParticipants
            .Select(participant =>
            {
                var dto = _mapper.Map<ParticipantDto>(participant);
                dto.FullName = members[participant.UserId].User.FullName;
                return dto;
            })
            .ToList();

        return ServiceResult<IReadOnlyList<ParticipantDto>>.Ok(result);
    }

    public async Task<ServiceResult> RemoveAsync(int eventId, int userId, CancellationToken cancellationToken = default)
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

        if (userId == evt.OrganizerId)
        {
            return ServiceResult.Validation(ErrorMessages.CannotRemoveOrganizer);
        }

        var participant = await _unitOfWork.EventParticipants.GetAsync(eventId, userId, cancellationToken);
        if (participant is null)
        {
            return ServiceResult.NotFound(ErrorMessages.ParticipantNotFound);
        }

        _unitOfWork.EventParticipants.Remove(participant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RsvpAsync(int eventId, RsvpRequest request, CancellationToken cancellationToken = default)
    {
        var me = _currentUser.UserId;

        var evt = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return ServiceResult.NotFound(ErrorMessages.EventNotFound);
        }

        var access = await _access.EnsureMemberAsync(evt.FamilyId, me, cancellationToken);
        if (!access.IsSuccess)
        {
            return access;
        }

        if (evt.Status == EventStatus.Cancelled)
        {
            return ServiceResult.Validation(ErrorMessages.EventCancelled);
        }

        var status = request.Status!.Value;
        if (status == RsvpStatus.Pending)
        {
            return ServiceResult.Validation(ErrorMessages.PendingIsNotAnswer);
        }

        // Every participant answers for themselves only
        var participant = await _unitOfWork.EventParticipants.GetAsync(eventId, me, cancellationToken);
        if (participant is null)
        {
            return ServiceResult.NotFound(ErrorMessages.NotInvited);
        }

        participant.RsvpStatus = status;
        participant.GuestsCount = status == RsvpStatus.NotComing ? 0 : request.GuestsCount;
        participant.RespondedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok();
    }
}
