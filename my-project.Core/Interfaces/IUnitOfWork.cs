using my_project.Core.Interfaces.Repositories;

namespace my_project.Core.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IFamilyRepository Families { get; }
    IFamilyMemberRepository FamilyMembers { get; }
    IEventRepository Events { get; }
    IEventParticipantRepository EventParticipants { get; }
    IEventUpdateRepository EventUpdates { get; }
    IFamilyTaskRepository FamilyTasks { get; }
    INotificationSettingsRepository NotificationSettings { get; }
    IEventAttachmentRepository EventAttachments { get; }
    ISentNotificationRepository SentNotifications { get; }

    // Throws ConcurrencyConflictException when a Version no longer matches
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
