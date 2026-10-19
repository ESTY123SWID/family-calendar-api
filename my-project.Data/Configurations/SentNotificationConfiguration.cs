using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my_project.Core.Entities;

namespace my_project.Data.Configurations;

public class SentNotificationConfiguration : IEntityTypeConfiguration<SentNotification>
{
    public void Configure(EntityTypeBuilder<SentNotification> builder)
    {
        builder.HasKey(notification => notification.Id);

        // By default PostgreSQL treats NULLs as distinct in a unique index.
        // AreNullsDistinct(false) (NULLS NOT DISTINCT, PostgreSQL 15+) makes rows with
        // a null EventId / TaskId deduplicated too
        builder.HasIndex(notification => new
            {
                notification.UserId,
                notification.Type,
                notification.EventId,
                notification.TaskId
            })
            .IsUnique()
            .AreNullsDistinct(false);
    }
}
