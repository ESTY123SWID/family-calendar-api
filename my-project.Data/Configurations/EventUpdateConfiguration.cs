using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my_project.Core.Entities;

namespace my_project.Data.Configurations;

public class EventUpdateConfiguration : IEntityTypeConfiguration<EventUpdate>
{
    public void Configure(EntityTypeBuilder<EventUpdate> builder)
    {
        builder.HasKey(update => update.Id);

        builder.Property(update => update.Content)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasOne(update => update.Event)
            .WithMany(evt => evt.Updates)
            .HasForeignKey(update => update.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(update => update.Author)
            .WithMany(user => user.EventUpdates)
            .HasForeignKey(update => update.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
