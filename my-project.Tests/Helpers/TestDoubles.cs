using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using my_project.Core.Interfaces.Infrastructure;

namespace my_project.Tests.Helpers;

// The logged-in user of a test (instead of the JWT claims)
public class TestCurrentUser : ICurrentUser
{
    public TestCurrentUser(int userId, bool isSystemAdmin = false)
    {
        UserId = userId;
        IsSystemAdmin = isSystemAdmin;
    }

    public int UserId { get; set; }

    public string Email => $"user{UserId}@test.local";

    public bool IsSystemAdmin { get; set; }
}

// A fixed "now", so date rules give the same result on every run
public class TestClock : IClock
{
    public static readonly DateTime DefaultNow = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    public DateTime UtcNow { get; set; } = DefaultNow;
}

public static class TestMapper
{
    // The real AutoMapper profiles of the Service layer
    public static IMapper Create()
    {
        return new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(typeof(my_project.Service.DependencyInjection).Assembly)
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();
    }
}
