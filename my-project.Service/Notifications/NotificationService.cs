using Microsoft.Extensions.Logging;
using my_project.Core.Entities;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Interfaces.Services;
using my_project.Core.Models;

namespace my_project.Service.Notifications;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;
    private readonly EmailTemplates _templates;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        IEmailSender emailSender,
        EmailTemplates templates,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
        _templates = templates;
        _logger = logger;
    }

    public async Task NotifyEventChangedAsync(Event evt, int changedByUserId, CancellationToken cancellationToken = default)
    {
        var recipients = await GetParticipantIdsAsync(evt.Id, changedByUserId, cancellationToken);

        await SendAsync(
            recipients,
            settings => settings.EventChangeNotices,
            user => _templates.EventChanged(user, evt),
            cancellationToken);
    }

    public async Task NotifyEventCancelledAsync(Event evt, int cancelledByUserId, CancellationToken cancellationToken = default)
    {
        var recipients = await GetParticipantIdsAsync(evt.Id, cancelledByUserId, cancellationToken);

        await SendAsync(
            recipients,
            settings => settings.EventChangeNotices,
            user => _templates.EventCancelled(user, evt),
            cancellationToken);
    }

    public async Task NotifyEventUpdatePostedAsync(Event evt, EventUpdate update, CancellationToken cancellationToken = default)
    {
        var recipients = await GetParticipantIdsAsync(evt.Id, update.AuthorId, cancellationToken);

        await SendAsync(
            recipients,
            settings => settings.EventChangeNotices,
            user => _templates.EventUpdatePosted(user, evt, update),
            cancellationToken);
    }

    public async Task NotifyInvitedAsync(Event evt, IEnumerable<int> userIds, CancellationToken cancellationToken = default)
    {
        await SendAsync(
            userIds,
            settings => settings.InvitationNotices,
            user => _templates.Invitation(user, evt),
            cancellationToken);
    }

    public async Task NotifyTaskChangedAsync(FamilyTask task, int changedByUserId, CancellationToken cancellationToken = default)
    {
        // Only the assignee is notified, and only about changes made by someone else
        if (task.AssigneeId is not { } assigneeId || assigneeId == changedByUserId)
        {
            return;
        }

        await SendAsync(
            [assigneeId],
            settings => settings.TaskChangeNotices,
            user => _templates.TaskChanged(user, task),
            cancellationToken);
    }

    private async Task<IReadOnlyList<int>> GetParticipantIdsAsync(
        int eventId,
        int excludedUserId,
        CancellationToken cancellationToken)
    {
        var participants = await _unitOfWork.EventParticipants.GetByEventIdAsync(eventId, cancellationToken);

        return participants
            .Select(participant => participant.UserId)
            .Where(userId => userId != excludedUserId)
            .ToList();
    }

    // Loads all the recipients' settings in one query, then sends one email per user who opted in.
    // A failed email is logged and skipped – it never fails the request that triggered it
    private async Task SendAsync(
        IEnumerable<int> userIds,
        Func<NotificationSettings, bool> wantsNotification,
        Func<User, EmailMessage> buildMessage,
        CancellationToken cancellationToken)
    {
        var settingsList = await _unitOfWork.NotificationSettings.GetByUserIdsAsync(userIds, cancellationToken);

        foreach (var settings in settingsList.Where(settings => settings.User.IsActive && wantsNotification(settings)))
        {
            try
            {
                await _emailSender.SendAsync(buildMessage(settings.User), cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Failed to send a notification email to user {UserId}", settings.UserId);
            }
        }
    }
}
