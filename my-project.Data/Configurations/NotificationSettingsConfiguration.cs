using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my_project.Core.Entities;

namespace my_project.Data.Configurations;

public class NotificationSettingsConfiguration : IEntityTypeConfiguration<NotificationSettings>
{
    public void Configure(EntityTypeBuilder<NotificationSettings> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_NotificationSettings_DaysBeforeEvent",
            "\"DaysBeforeEvent\" BETWEEN 1 AND 14"));

        builder.HasKey(settings => settings.UserId);

        builder.HasOne(settings => settings.User)
            .WithOne(user => user.NotificationSettings)
            .HasForeignKey<NotificationSettings>(settings => settings.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
