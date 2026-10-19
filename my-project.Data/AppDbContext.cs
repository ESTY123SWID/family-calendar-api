using Microsoft.EntityFrameworkCore;
using my_project.Core.Entities;
using my_project.Data.Converters;
using my_project.Data.Seed;

namespace my_project.Data;

public class AppDbContext : DbContext
{
    private const string UpdatedAtProperty = "UpdatedAt";
    private const string VersionProperty = "Version";

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Family> Families => Set<Family>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
    public DbSet<EventUpdate> EventUpdates => Set<EventUpdate>();
    public DbSet<FamilyTask> FamilyTasks => Set<FamilyTask>();
    public DbSet<NotificationSettings> NotificationSettings => Set<NotificationSettings>();
    public DbSet<EventAttachment> EventAttachments => Set<EventAttachment>();
    public DbSet<SentNotification> SentNotifications => Set<SentNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Demo data (the "SeedData" migration)
        SeedData.Apply(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All dates are stored in UTC and read back as DateTimeKind.Utc
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampModifiedEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampModifiedEntities();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // For every modified entity: refresh UpdatedAt and issue a new concurrency Version.
    // The original Version stays in the UPDATE's WHERE clause, so a stale write fails
    private void StampModifiedEntities()
    {
        var modifiedEntries = ChangeTracker.Entries()
            .Where(entry => entry.State == EntityState.Modified);

        foreach (var entry in modifiedEntries)
        {
            if (entry.Metadata.FindProperty(UpdatedAtProperty) is not null)
            {
                entry.Property(UpdatedAtProperty).CurrentValue = DateTime.UtcNow;
            }

            if (entry.Metadata.FindProperty(VersionProperty) is { IsConcurrencyToken: true })
            {
                entry.Property(VersionProperty).CurrentValue = Guid.NewGuid();
            }
        }
    }
}
