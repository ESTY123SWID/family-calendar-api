using Microsoft.EntityFrameworkCore;
using my_project.Core.Enums;
using my_project.Service.Security;
using my_project.Tests.Helpers;

namespace my_project.Tests.Data;

// The demo data (HasData) is created with the database and is usable as is
public class SeedDataTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Seed_CreatesUsersFamiliesEventsAndTasks()
    {
        await using var context = _database.CreateContext();

        Assert.Equal(6, await context.Users.CountAsync());
        Assert.Equal(2, await context.Families.CountAsync());
        Assert.Equal(4, await context.Events.CountAsync());
        Assert.Equal(9, await context.FamilyTasks.CountAsync());
        Assert.Equal(6, await context.NotificationSettings.CountAsync());
        Assert.True(await context.FamilyTasks.AnyAsync(task => task.AssigneeId == null && task.Status == FamilyTaskStatus.Open));
        Assert.True(await context.Events.AnyAsync(evt => evt.Status == EventStatus.Cancelled));
    }

    [Fact]
    public async Task Seed_HasNoSystemAdmin_TheAdminComesFromConfigurationOnly()
    {
        await using var context = _database.CreateContext();

        Assert.False(await context.Users.AnyAsync(user => user.Role == SystemRole.SystemAdmin));
    }

    [Fact]
    public async Task Seed_DemoPasswordWorksWithTheRealHasher()
    {
        await using var context = _database.CreateContext();
        var sara = await context.Users.SingleAsync(user => user.Email == "sara@family.demo");

        var hasher = new PasswordHasher();
        Assert.True(hasher.Verify(sara.PasswordHash, "Demo1234!"));
        Assert.False(hasher.Verify(sara.PasswordHash, "wrong"));
    }

    [Fact]
    public async Task Seed_EveryFamilyHasAManager_AndJoinCodesAreUnique()
    {
        await using var context = _database.CreateContext();

        foreach (var familyId in await context.Families.Select(family => family.Id).ToListAsync())
        {
            Assert.True(await context.FamilyMembers.AnyAsync(m => m.FamilyId == familyId && m.Role == FamilyRole.Manager));
        }

        var codes = await context.Families.Select(family => family.JoinCode).ToListAsync();
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public async Task Seed_ModelIsStable_TwoBuildsGiveTheSameData()
    {
        // HasData must not use Guid.NewGuid() / DateTime.UtcNow, otherwise every migration would "change" the seed
        await using var first = _database.CreateContext();
        using var secondDatabase = new SqliteDatabase();
        await using var second = secondDatabase.CreateContext();

        var firstVersions = await first.FamilyTasks.OrderBy(t => t.Id).Select(t => t.Version).ToListAsync();
        var secondVersions = await second.FamilyTasks.OrderBy(t => t.Id).Select(t => t.Version).ToListAsync();

        Assert.Equal(firstVersions, secondVersions);
    }
}
