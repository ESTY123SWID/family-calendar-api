using Microsoft.EntityFrameworkCore;
using my_project.Core.Common;
using my_project.Core.DTOs.Tasks;
using my_project.Core.Enums;
using my_project.Core.Exceptions;
using my_project.Tests.Helpers;

namespace my_project.Tests.Data;

// ⭐ The limited resource with a real database (in-memory SQLite) and two separate DbContexts –
// exactly like two family members who press "I'll take it" at the same moment.
// The real UnitOfWork, repositories and TaskService run; only the database engine is SQLite instead of PostgreSQL
// (the concurrency check is EF's, the same SQL "UPDATE ... WHERE Id = @id AND Version = @version" on both)
public class TaskConcurrencyTests : IDisposable
{
    // From the seed: task 2 "להביא מיחם ופלטה" in event 1 of family לוי, open (no assignee)
    private const int FreeTaskId = 2;
    private const int David = 2;
    private const int Moshe = 3;

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task TwoMembersTakeTheSameFreeTask_OnlyTheFirstWins_TheSecondGets409()
    {
        await using var davidContext = _database.CreateContext();
        await using var mosheContext = _database.CreateContext();

        // Both requests read the task while it is still free
        var davidSees = await davidContext.FamilyTasks.Include(task => task.Event).SingleAsync(task => task.Id == FreeTaskId);
        var mosheSees = await mosheContext.FamilyTasks.Include(task => task.Event).SingleAsync(task => task.Id == FreeTaskId);
        Assert.Null(davidSees.AssigneeId);
        Assert.Null(mosheSees.AssigneeId);
        Assert.Equal(davidSees.Version, mosheSees.Version);
        var versionBefore = davidSees.Version;

        // David saves first
        var davidResult = await SqliteDatabase.CreateTaskService(davidContext, David)
            .AssignAsync(FreeTaskId, new AssignTaskRequest { UserId = David });

        // Moshe still holds the old Version → his UPDATE matches 0 rows
        var mosheResult = await SqliteDatabase.CreateTaskService(mosheContext, Moshe)
            .AssignAsync(FreeTaskId, new AssignTaskRequest { UserId = Moshe });

        Assert.True(davidResult.IsSuccess);
        Assert.Equal(ErrorType.Conflict, mosheResult.ErrorType);

        // The database holds David's assignment only, with a new Version
        await using var check = _database.CreateContext();
        var stored = await check.FamilyTasks.AsNoTracking().SingleAsync(task => task.Id == FreeTaskId);
        Assert.Equal(David, stored.AssigneeId);
        Assert.Equal(FamilyTaskStatus.Assigned, stored.Status);
        Assert.NotEqual(versionBefore, stored.Version);
    }

    [Fact]
    public async Task SecondMemberWhoReadsAfterTheSave_GetsTaskAlreadyTaken409()
    {
        await using (var first = _database.CreateContext())
        {
            var result = await SqliteDatabase.CreateTaskService(first, David).AssignAsync(FreeTaskId, new AssignTaskRequest { UserId = David });
            Assert.True(result.IsSuccess);
        }

        await using var second = _database.CreateContext();
        var late = await SqliteDatabase.CreateTaskService(second, Moshe).AssignAsync(FreeTaskId, new AssignTaskRequest { UserId = Moshe });

        Assert.Equal(ErrorType.Conflict, late.ErrorType);
    }

    [Fact]
    public async Task UnitOfWork_TranslatesEfConcurrencyErrorToConcurrencyConflictException()
    {
        await using var contextA = _database.CreateContext();
        await using var contextB = _database.CreateContext();
        var unitOfWorkA = SqliteDatabase.CreateUnitOfWork(contextA);
        var unitOfWorkB = SqliteDatabase.CreateUnitOfWork(contextB);

        var taskA = await unitOfWorkA.FamilyTasks.GetByIdAsync(FreeTaskId);
        var taskB = await unitOfWorkB.FamilyTasks.GetByIdAsync(FreeTaskId);

        taskA!.Title = "שינוי של A";
        await unitOfWorkA.SaveChangesAsync();

        taskB!.Title = "שינוי של B";
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => unitOfWorkB.SaveChangesAsync());
    }

    [Fact]
    public async Task EditingWithTheVersionTheClientSaw_StaleVersionIsRejected()
    {
        Guid versionTheClientSaw;
        await using (var read = _database.CreateContext())
        {
            versionTheClientSaw = (await read.FamilyTasks.AsNoTracking().SingleAsync(task => task.Id == FreeTaskId)).Version;
        }

        // Someone else edits the task in the meantime
        await using (var other = _database.CreateContext())
        {
            var unitOfWork = SqliteDatabase.CreateUnitOfWork(other);
            var task = await unitOfWork.FamilyTasks.GetByIdAsync(FreeTaskId);
            task!.Description = "עודכן על ידי מישהו אחר";
            await unitOfWork.SaveChangesAsync();
        }

        // The client sends the Version it saw (as UpdateTaskRequest.Version)
        await using var context = _database.CreateContext();
        var uow = SqliteDatabase.CreateUnitOfWork(context);
        var stale = await uow.FamilyTasks.GetByIdAsync(FreeTaskId);
        uow.FamilyTasks.SetOriginalVersion(stale!, versionTheClientSaw);
        stale!.Title = "עריכה על גרסה ישנה";

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => uow.SaveChangesAsync());
    }
}
