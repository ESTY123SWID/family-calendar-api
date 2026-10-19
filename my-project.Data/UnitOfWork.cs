using Microsoft.EntityFrameworkCore;
using Npgsql;
using my_project.Core.Exceptions;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Repositories;

namespace my_project.Data;

// One save for all the repositories of a request. Every repository is injected (all Scoped),
// so they share the request's single AppDbContext and its change tracker
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(
        AppDbContext context,
        IUserRepository users,
        IFamilyRepository families,
        IFamilyMemberRepository familyMembers,
        IEventRepository events,
        IEventParticipantRepository eventParticipants,
        IEventUpdateRepository eventUpdates,
        IFamilyTaskRepository familyTasks,
        INotificationSettingsRepository notificationSettings,
        IEventAttachmentRepository eventAttachments,
        ISentNotificationRepository sentNotifications)
    {
        _context = context;
        Users = users;
        Families = families;
        FamilyMembers = familyMembers;
        Events = events;
        EventParticipants = eventParticipants;
        EventUpdates = eventUpdates;
        FamilyTasks = familyTasks;
        NotificationSettings = notificationSettings;
        EventAttachments = eventAttachments;
        SentNotifications = sentNotifications;
    }

    public IUserRepository Users { get; }
    public IFamilyRepository Families { get; }
    public IFamilyMemberRepository FamilyMembers { get; }
    public IEventRepository Events { get; }
    public IEventParticipantRepository EventParticipants { get; }
    public IEventUpdateRepository EventUpdates { get; }
    public IFamilyTaskRepository FamilyTasks { get; }
    public INotificationSettingsRepository NotificationSettings { get; }
    public IEventAttachmentRepository EventAttachments { get; }
    public ISentNotificationRepository SentNotifications { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "The record was changed by another user. Reload it and try again.",
                exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
        {
            throw new DuplicateValueException(
                $"A unique constraint was violated ({postgres.ConstraintName}).",
                exception);
        }
    }
}
