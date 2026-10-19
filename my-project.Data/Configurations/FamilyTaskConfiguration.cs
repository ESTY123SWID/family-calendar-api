using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my_project.Core.Entities;

namespace my_project.Data.Configurations;

public class FamilyTaskConfiguration : IEntityTypeConfiguration<FamilyTask>
{
    public void Configure(EntityTypeBuilder<FamilyTask> builder)
    {
        builder.HasKey(task => task.Id);

        builder.Property(task => task.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(task => task.Description)
            .HasMaxLength(1000);

        // ⭐ Protects volunteering for a free task
        builder.Property(task => task.Version)
            .IsConcurrencyToken();

        builder.HasOne(task => task.Event)
            .WithMany(evt => evt.Tasks)
            .HasForeignKey(task => task.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(task => task.Assignee)
            .WithMany(user => user.AssignedTasks)
            .HasForeignKey(task => task.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
