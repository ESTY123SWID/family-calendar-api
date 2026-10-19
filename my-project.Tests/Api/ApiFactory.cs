using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using my_project.Data;
using my_project.Tests.Helpers;

namespace my_project.Tests.Api;

// The real API (Program.cs: middlewares, JWT, controllers, services) in memory, over a SQLite database
// with the demo data. Environment "Testing": no User Secrets, so no real connection string or key is used.
// Emails and reminders are off – nothing is sent
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin-Test-Only-1";

    private readonly SqliteDatabase _database = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // A placeholder: the real AppDbContext registration is replaced below
        builder.UseSetting("ConnectionStrings:Default", "Host=unused");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Jwt:Key", "test-only-signing-key-0123456789-abcdefghijklmnop");
        builder.UseSetting("Email:Enabled", "false");
        builder.UseSetting("Reminders:Enabled", "false");
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173");
        // The startup admin seeder runs against the test database
        builder.UseSetting("SeedAdmin:Email", AdminEmail);
        builder.UseSetting("SeedAdmin:Password", AdminPassword);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_database.Connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _database.Dispose();
        }
    }
}

internal static class ServiceCollectionTestExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var descriptor in services.Where(descriptor => descriptor.ServiceType == typeof(T)).ToList())
        {
            services.Remove(descriptor);
        }
    }
}
