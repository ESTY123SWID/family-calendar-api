using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my_project.Core.Entities;

namespace my_project.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Events_EndAt_AfterStartAt",
            "\"EndAt\" IS NULL OR \"EndAt\" > \"StartAt\""));

        builder.HasKey(evt => evt.Id);

        builder.Property(evt => evt.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(evt => evt.Description)
            .HasMaxLength(2000);

        builder.Property(evt => evt.LocationName)
            .HasMaxLength(150);

        builder.Property(evt => evt.Address)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(evt => evt.Directions)
            .HasMaxLength(1000);

        builder.Property(evt => evt.Version)
            .IsConcurrencyToken();

        builder.HasOne(evt => evt.Family)
            .WithMany(family => family.Events)
            .HasForeignKey(evt => evt.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(evt => evt.Organizer)
            .WithMany(user => user.OrganizedEvents)
            .HasForeignKey(evt => evt.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
