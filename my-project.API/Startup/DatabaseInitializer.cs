using Microsoft.EntityFrameworkCore;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Data;

namespace my_project.API.Startup;

// Runs once when the server starts, before it accepts requests:
// 1. Database:MigrateOnStartup = true -> applies the pending migrations (the schema and the demo data),
//    so the system runs on a new database without any manual step.
// 2. Creates the first system admin from configuration (SeedAdmin:Email / SeedAdmin:Password),
//    so no admin password is ever written in the code or in a file that goes to Git.
// Both steps are safe to repeat: applied migrations are skipped and an existing admin is left as is.
public static class DatabaseInitializer
{
    private const int MinAdminPasswordLength = 8;

    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        if (app.Configuration.GetValue("Database:MigrateOnStartup", false))
        {
            var context = services.GetRequiredService<AppDbContext>();
            var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

            if (pending.Count > 0)
            {
                logger.LogInformation("Applying {Count} migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
                await context.Database.MigrateAsync(cancellationToken);
            }
            else
            {
                logger.LogInformation("The database is up to date");
            }
        }

        await SeedAdminAsync(app.Configuration, services, logger, cancellationToken);
    }

    private static async Task SeedAdminAsync(IConfiguration configuration, IServiceProvider services, ILogger logger,
        CancellationToken cancellationToken)
    {
        var email = configuration["SeedAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["SeedAdmin:Password"];
        var fullName = configuration["SeedAdmin:FullName"]?.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            logger.LogInformation("SeedAdmin is not configured – no system admin is created");
            return;
        }

        if (password.Length < MinAdminPasswordLength)
        {
            logger.LogWarning("SeedAdmin:Password is shorter than {Length} characters – the admin is not created", MinAdminPasswordLength);
            return;
        }

        var unitOfWork = services.GetRequiredService<IUnitOfWork>();

        var existing = await unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            if (existing.Role != SystemRole.SystemAdmin)
            {
                logger.LogWarning("User {UserId} (the SeedAdmin email) exists but is not a system admin – left unchanged", existing.Id);
            }
            else
            {
                logger.LogDebug("System admin {UserId} from SeedAdmin already exists", existing.Id);
            }

            return;
        }

        var admin = new User
        {
            FullName = string.IsNullOrEmpty(fullName) ? "מנהל מערכת" : fullName,
            Email = email,
            PasswordHash = services.GetRequiredService<IPasswordHasher>().Hash(password),
            Role = SystemRole.SystemAdmin,
            CreatedAt = DateTime.UtcNow,
            NotificationSettings = new NotificationSettings()
        };

        await unitOfWork.Users.AddAsync(admin, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("System admin {UserId} was created from SeedAdmin", admin.Id);
    }
}
