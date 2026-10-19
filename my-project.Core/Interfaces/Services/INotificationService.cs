using my_project.Core.Entities;

namespace my_project.Core.Interfaces.Services;

// Immediate emails sent by other services after a change was saved.
// Respects NotificationSettings. A sending failure is logged and never fails the request
public interface INotificationService
{
    // To the participants, except the user who made the change
    Task NotifyEventChangedAsync(Event evt, int changedByUserId, CancellationToken cancellationToken = default);

    // To the participants, except the user who cancelled
    Task NotifyEventCancelledAsync(Event evt, int cancelledByUserId, CancellationToken cancellationToken = default);

    // To the participants, except the author
    Task NotifyEventUpdatePostedAsync(Event evt, EventUpdate update, CancellationToken cancellationToken = default);

    Task NotifyInvitedAsync(Event evt, IEnumerable<int> userIds, CancellationToken cancellationToken = default);

    // To the assignee, when the task was changed by another user
    Task NotifyTaskChangedAsync(FamilyTask task, int changedByUserId, CancellationToken cancellationToken = default);
}
