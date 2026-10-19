using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using my_project.Core.Interfaces;
using my_project.Data;
using my_project.Data.Repositories;
using my_project.Service.Services;
using Moq;
using my_project.Core.Interfaces.Services;

namespace my_project.Tests.Helpers;

// An in-memory SQLite database that lives as long as this object (the connection stays open).
// It is created from the real AppDbContext model, including the demo data (HasData).
// Each CreateContext() is a separate DbContext – like two separate HTTP requests
public sealed class SqliteDatabase : IDisposable
{
    public SqliteDatabase()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public SqliteConnection Connection { get; }

    public DbContextOptions<AppDbContext> Options => new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite(Connection)
        .Options;

    public AppDbContext CreateContext() => new(Options);

    // The real UnitOfWork with all the real repositories over the given context
    public static IUnitOfWork CreateUnitOfWork(AppDbContext context)
    {
        return new UnitOfWork(
            context,
            new UserRepository(context),
            new FamilyRepository(context),
            new FamilyMemberRepository(context),
            new EventRepository(context),
            new EventParticipantRepository(context),
            new EventUpdateRepository(context),
            new FamilyTaskRepository(context),
            new NotificationSettingsRepository(context),
            new EventAttachmentRepository(context),
            new SentNotificationRepository(context));
    }

    // A full TaskService over a context (the real access rules; notifications are not sent)
    public static TaskService CreateTaskService(AppDbContext context, int currentUserId)
    {
        var unitOfWork = CreateUnitOfWork(context);

        return new TaskService(
            unitOfWork,
            new FamilyAccessService(unitOfWork),
            new TestCurrentUser(currentUserId),
            new TestClock(),
            TestMapper.Create(),
            Mock.Of<INotificationService>(),
            NullLogger<TaskService>.Instance);
    }

    public void Dispose()
    {
        Connection.Dispose();
    }
}
