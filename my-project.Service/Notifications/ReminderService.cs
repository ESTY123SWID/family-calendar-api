using Microsoft.Extensions.Logging;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Exceptions;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Interfaces.Services;
using my_project.Core.Models;
using my_project.Service.Common;

namespace my_project.Service.Notifications;

// Runs periodically from ReminderBackgroundService. Every reminder is sent at most once:
// it is claimed in SentNotification before sending and released if sending fails (see TrySendAsync)
public class ReminderService : IReminderService
{
    // The largest DaysBeforeEvent a user can choose
    private const int MaxDaysBeforeEvent = 14;

    // A task reminder is sent when the due date is less than this far away
    private static readonly TimeSpan TaskReminderWindow = TimeSpan.FromHours(24);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly EmailTemplates _templates;
    private readonly LocalTimeConverter _localTime;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(
        IUnitOfWork unitOfWork,
        IEmailSender emailSender,
        IClock clock,
        EmailTemplates templates,
        LocalTimeConverter localTime,
        ILogger<ReminderService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
        _clock = clock;
        _templates = templates;
        _localTime = localTime;
        _logger = logger;
    }

    public async Task<int> ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;

        // Two queries load everything that may need a reminder, a third loads what was already sent
        var events = await _unitOfWork.Events.GetScheduledWithParticipantsAsync(
            now, now.AddDays(MaxDaysBeforeEvent + 1), cancellationToken);
        var tasks = await _unitOfWork.FamilyTasks.GetDueForRemindersAsync(now.Add(TaskReminderWindow), cancellationToken);

        var alreadySent = (await _unitOfWork.SentNotifications.GetForTargetsAsync(
                events.Select(evt => evt.Id),
                tasks.Select(task => task.Id),
                cancellationToken))
            .Select(sent => new ReminderKey(sent.UserId, sent.Type, sent.EventId, sent.TaskId))
            .ToHashSet();

        _logger.LogDebug("Reminder run: checking {EventCount} events and {TaskCount} tasks", events.Count, tasks.Count);

        var candidates = CollectEventReminders(events, now).Concat(CollectTaskReminders(tasks, now));

        var sentCount = 0;
        foreach (var reminder in candidates)
        {
            if (alreadySent.Contains(reminder.Key))
            {
                continue;
            }

            if (await TrySendAsync(reminder, now, cancellationToken))
            {
                alreadySent.Add(reminder.Key);
                sentCount++;
            }
        }

        if (sentCount > 0)
        {
            _logger.LogInformation("Sent {Count} reminders", sentCount);
        }

        return sentCount;
    }

    private IEnumerable<Reminder> CollectEventReminders(IEnumerable<Event> events, DateTime now)
    {
        var today = _localTime.LocalDate(now);

        foreach (var evt in events)
        {
            // Counted by local calendar date, so "today" means today in Israel, not in UTC
            var daysUntil = (_localTime.LocalDate(evt.StartAt) - today).Days;

            foreach (var participant in evt.Participants)
            {
                var user = participant.User;
                var settings = user.NotificationSettings;

                if (settings is null || !user.IsActive || participant.RsvpStatus == RsvpStatus.NotComing)
                {
                    continue;
                }

                if (daysUntil == 0 && settings.EventDayReminder)
                {
                    yield return new Reminder(
                        new ReminderKey(user.Id, NotificationType.EventDayReminder, evt.Id, null),
                        _templates.EventDayReminder(user, evt));
                }

                if (daysUntil >= 1 && daysUntil <= settings.DaysBeforeEvent && settings.EventReminders)
                {
                    yield return new Reminder(
                        new ReminderKey(user.Id, NotificationType.EventReminder, evt.Id, null),
                        _templates.EventReminder(user, evt));
                }

                if (participant.RsvpStatus == RsvpStatus.Pending
                    && daysUntil <= settings.DaysBeforeEvent
                    && settings.InvitationNotices)
                {
                    yield return new Reminder(
                        new ReminderKey(user.Id, NotificationType.RsvpReminder, evt.Id, null),
                        _templates.RsvpReminder(user, evt));
                }
            }
        }
    }

    private IEnumerable<Reminder> CollectTaskReminders(IEnumerable<FamilyTask> tasks, DateTime now)
    {
        foreach (var task in tasks)
        {
            var user = task.Assignee;
            var settings = user?.NotificationSettings;

            if (user is null || settings is null || !user.IsActive)
            {
                continue;
            }

            if (task.DueDate < now)
            {
                if (settings.TaskChangeNotices)
                {
                    yield return new Reminder(
                        new ReminderKey(user.Id, NotificationType.TaskOverdue, null, task.Id),
                        _templates.TaskOverdue(user, task));
                }
            }
            else if (settings.TaskReminders)
            {
                yield return new Reminder(
                    new ReminderKey(user.Id, NotificationType.TaskReminder, null, task.Id),
                    _templates.TaskReminder(user, task));
            }
        }
    }

    // 1. Claim: the reminder is recorded in SentNotification BEFORE it is sent. The unique index on
    //    (UserId, Type, EventId, TaskId) lets only one run claim it, so two overlapping runs
    //    (or two servers) never send the same email twice.
    // 2. Send. If sending fails, the claim is released, so the next run retries the reminder.
    // Saved per email, so a crash in the middle of a run does not cause duplicates either
    private async Task<bool> TrySendAsync(Reminder reminder, DateTime now, CancellationToken cancellationToken)
    {
        var claim = new SentNotification
        {
            UserId = reminder.Key.UserId,
            Type = reminder.Key.Type,
            EventId = reminder.Key.EventId,
            TaskId = reminder.Key.TaskId,
            SentAt = now
        };

        await _unitOfWork.SentNotifications.AddAsync(claim, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateValueException)
        {
            // Another run claimed (and is sending) this reminder a moment ago.
            // Removing an Added entity only stops tracking it, so the next save does not retry it
            _unitOfWork.SentNotifications.Remove(claim);
            return false;
        }

        try
        {
            await _emailSender.SendAsync(reminder.Message, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            // The server is stopping – the reminder was not sent, so it must not stay claimed
            await ReleaseClaimAsync(claim, reminder.Key);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to send {Type} reminder to user {UserId}; it will be retried on the next run",
                reminder.Key.Type, reminder.Key.UserId);

            await ReleaseClaimAsync(claim, reminder.Key);
            return false;
        }
    }

    private async Task ReleaseClaimAsync(SentNotification claim, ReminderKey key)
    {
        try
        {
            _unitOfWork.SentNotifications.Remove(claim);

            // Not tied to the run's token: a claim left behind would mean the reminder is never sent
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Could not release the {Type} reminder of user {UserId}; it will not be retried",
                key.Type, key.UserId);
        }
    }

    private sealed record ReminderKey(int UserId, NotificationType Type, int? EventId, int? TaskId);

    private sealed record Reminder(ReminderKey Key, EmailMessage Message);
}
