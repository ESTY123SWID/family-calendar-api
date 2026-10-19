using AutoMapper;
using Microsoft.Extensions.Logging;
using my_project.Core.Common;
using my_project.Core.DTOs.Events;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Exceptions;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Interfaces.Services;
using my_project.Service.Common;

namespace my_project.Service.Services;

public class EventService : IEventService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFamilyAccessService _access;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly INotificationService _notifications;
    private readonly ILogger<EventService> _logger;

    public EventService(
        IUnitOfWork unitOfWork,
        IFamilyAccessService access,
        ICurrentUser currentUser,
        IClock clock,
        IMapper mapper,
        INotificationService notifications,
        ILogger<EventService> logger)
    {
        _unitOfWork = unitOfWork;
        _access = access;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedResult<EventSummaryDto>>> GetPagedAsync(
        int familyId,
        EventQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var access = await _access.EnsureMemberAsync(familyId, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<PagedResult<EventSummaryDto>>.FromFailure(access);
        }

        query.From = query.From.ToUtc();
        query.To = query.To.ToUtc();

        var page = await _unitOfWork.Events.GetPagedForFamilyAsync(familyId, query, cancellationToken);

        var result = new PagedResult<EventSummaryDto>(
            _mapper.Map<List<EventSummaryDto>>(page.Items),
            page.Page,
            page.PageSize,
            page.TotalCount);

        return ServiceResult<PagedResult<EventSummaryDto>>.Ok(result);
    }

    public async Task<ServiceResult<EventDetailsDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var evt = await _unitOfWork.Events.GetWithDetailsAsync(id, cancellationToken);
        if (evt is null)
        {
            return ServiceResult<EventDetailsDto>.NotFound(ErrorMessages.EventNotFound);
        }

        var membership = await _access.GetMembershipAsync(evt.FamilyId, _currentUser.UserId, cancellationToken);
        if (membership is null)
        {
            return ServiceResult<EventDetailsDto>.Forbidden(ErrorMessages.NotFamilyMember);
        }

        var me = _currentUser.UserId;
        var dto = _mapper.Map<EventDetailsDto>(evt);

        dto.RsvpCounts = DomainRules.BuildRsvpSummary(evt.Participants);
        dto.MyRsvp = evt.Participants.FirstOrDefault(participant => participant.UserId == me)?.RsvpStatus;
        dto.CanManage = membership.Role == FamilyRole.Manager || evt.OrganizerId == me;

        var invitation = await _unitOfWork.EventAttachments.GetInvitationAsync(evt.Id, cancellationToken);
        dto.InvitationUrl = invitation is null ? null : DomainRules.AttachmentContentUrl(invitation.Id);

        return ServiceResult<EventDetailsDto>.Ok(dto);
    }

    public async Task<ServiceResult<EventDetailsDto>> CreateAsync(
        int familyId,
        CreateEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var me = _currentUser.UserId;

        var access = await _access.EnsureMemberAsync(familyId, me, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<EventDetailsDto>.FromFailure(access);
        }

        var now = _clock.UtcNow;
        var startAt = request.StartAt!.Value.ToUtc();
        var endAt = request.EndAt.ToUtc();

        if (startAt <= now)
        {
            return ServiceResult<EventDetailsDto>.Validation(ErrorMessages.StartInPast);
        }

        if (endAt.HasValue && endAt.Value <= startAt)
        {
            return ServiceResult<EventDetailsDto>.Validation(ErrorMessages.EndBeforeStart);
        }

        var members = await _unitOfWork.FamilyMembers.GetByFamilyIdAsync(familyId, cancellationToken);
        var memberIds = members.Select(member => member.UserId).ToHashSet();

        var inviteeIds = request.InviteAll
            ? memberIds
            : (request.InviteeIds ?? []).ToHashSet();

        if (!inviteeIds.IsSubsetOf(memberIds))
        {
            return ServiceResult<EventDetailsDto>.Validation(ErrorMessages.InviteesNotMembers);
        }

        // The organizer always takes part, and is Coming
        inviteeIds.Add(me);

        var evt = new Event
        {
            FamilyId = familyId,
            OrganizerId = me,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            StartAt = startAt,
            EndAt = endAt,
            LocationName = request.LocationName?.Trim(),
            Address = request.Address.Trim(),
            Directions = request.Directions?.Trim(),
            CreatedAt = now
        };

        foreach (var userId in inviteeIds)
        {
            var isOrganizer = userId == me;

            evt.Participants.Add(new EventParticipant
            {
                UserId = userId,
                RsvpStatus = isOrganizer ? RsvpStatus.Coming : RsvpStatus.Pending,
                InvitedAt = now,
                RespondedAt = isOrganizer ? now : null
            });
        }

        // Event + participants in a single save
        await _unitOfWork.Events.AddAsync(evt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Event {EventId} created in family {FamilyId} by user {UserId} with {Count} participants",
            evt.Id, familyId, me, inviteeIds.Count);

        await _notifications.NotifyInvitedAsync(evt, inviteeIds.Where(userId => userId != me), cancellationToken);

        return await GetByIdAsync(evt.Id, cancellationToken);
    }

    public async Task<ServiceResult> UpdateAsync(int id, UpdateEventRequest request, CancellationToken cancellationToken = default)
    {
        var evt = await _unitOfWork.Events.GetByIdAsync(id, cancellationToken);
        if (evt is null)
        {
            return ServiceResult.NotFound(ErrorMessages.EventNotFound);
        }

        var access = await _access.EnsureCanManageEventAsync(evt, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access;
        }

        if (evt.Status == EventStatus.Cancelled)
        {
            return ServiceResult.Validation(ErrorMessages.EventCancelled);
        }

        var startAt = request.StartAt!.Value.ToUtc();
        var endAt = request.EndAt.ToUtc();

        if (endAt.HasValue && endAt.Value <= startAt)
        {
            return ServiceResult.Validation(ErrorMessages.EndBeforeStart);
        }

        // The Version the client saw goes into the WHERE clause of the UPDATE
        _unitOfWork.Events.SetOriginalVersion(evt, request.Version!.Value);

        evt.Title = request.Title.Trim();
        evt.Description = request.Description?.Trim();
        evt.StartAt = startAt;
        evt.EndAt = endAt;
        evt.LocationName = request.LocationName?.Trim();
        evt.Address = request.Address.Trim();
        evt.Directions = request.Directions?.Trim();

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Concurrency conflict while updating event {EventId}", id);
            return ServiceResult.Conflict(ErrorMessages.EventChangedByOther);
        }

        await _notifications.NotifyEventChangedAsync(evt, _currentUser.UserId, cancellationToken);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var evt = await _unitOfWork.Events.GetByIdAsync(id, cancellationToken);
        if (evt is null)
        {
            return ServiceResult.NotFound(ErrorMessages.EventNotFound);
        }

        var access = await _access.EnsureCanManageEventAsync(evt, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access;
        }

        // Cancelling twice is not an error
        if (evt.Status == EventStatus.Cancelled)
        {
            return ServiceResult.Ok();
        }

        evt.Status = EventStatus.Cancelled;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Concurrency conflict while cancelling event {EventId}", id);
            return ServiceResult.Conflict(ErrorMessages.EventChangedByOther);
        }

        _logger.LogInformation("Event {EventId} cancelled by user {UserId}", id, _currentUser.UserId);

        await _notifications.NotifyEventCancelledAsync(evt, _currentUser.UserId, cancellationToken);

        return ServiceResult.Ok();
    }
}
