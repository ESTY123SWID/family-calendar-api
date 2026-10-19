using Microsoft.EntityFrameworkCore;
using my_project.Core.DTOs.Tasks;
using my_project.Core.Common;
using my_project.Data;
using my_project.Tests.Helpers;

namespace my_project.Tests.Data;

// Runs only when TEST_POSTGRES_CONNECTION is set to an EMPTY test database
// (never the real database – the test applies all the migrations to it).
// Without it the tests are reported as Skipped
public sealed class PostgresFactAttribute : FactAttribute
{
    public const string VariableName = "TEST_POSTGRES_CONNECTION";

    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(VariableName)))
        {
            Skip = $"PostgreSQL is not available: set {VariableName} to an empty test database to run this test";
        }
    }
}

public class PostgresMigrationTests
{
    private static DbContextOptions<AppDbContext> Options() => new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable(PostgresFactAttribute.VariableName))
        .Options;

    [PostgresFact]
    public async Task Migrations_ApplyOnPostgres_AndConcurrencyReturns409()
    {
        await using (var context = new AppDbContext(Options()))
        {
            await context.Database.MigrateAsync();
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            Assert.Equal(6, await context.Users.CountAsync(user => user.Email.EndsWith("@family.demo")));
        }

        await using var davidContext = new AppDbContext(Options());
        await using var mosheContext = new AppDbContext(Options());
        await davidContext.FamilyTasks.Include(task => task.Event).SingleAsync(task => task.Id == 3);
        await mosheContext.FamilyTasks.Include(task => task.Event).SingleAsync(task => task.Id == 3);

        var david = await SqliteDatabase.CreateTaskService(davidContext, 2).AssignAsync(3, new AssignTaskRequest { UserId = 2 });
        var moshe = await SqliteDatabase.CreateTaskService(mosheContext, 3).AssignAsync(3, new AssignTaskRequest { UserId = 3 });

        Assert.True(david.IsSuccess);
        Assert.Equal(ErrorType.Conflict, moshe.ErrorType);
    }
}
