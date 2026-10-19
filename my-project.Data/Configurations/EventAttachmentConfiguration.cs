using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my_project.Core.Entities;
using my_project.Core.Enums;

namespace my_project.Data.Configurations;

public class EventAttachmentConfiguration : IEntityTypeConfiguration<EventAttachment>
{
    private const long MaxSizeBytes = 5 * 1024 * 1024;

    public void Configure(EntityTypeBuilder<EventAttachment> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_EventAttachments_SizeBytes",
            $"\"SizeBytes\" BETWEEN 0 AND {MaxSizeBytes}"));

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(attachment => attachment.StoredFileName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(attachment => attachment.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(attachment => attachment.EventId);

        // At most one Invitation per event
        builder.HasIndex(attachment => attachment.EventId, "IX_EventAttachments_EventId_Invitation")
            .IsUnique()
            .HasFilter($"\"Kind\" = {(int)AttachmentKind.Invitation}");

        builder.HasOne(attachment => attachment.Event)
            .WithMany(evt => evt.Attachments)
            .HasForeignKey(attachment => attachment.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(attachment => attachment.UploadedBy)
            .WithMany(user => user.UploadedAttachments)
            .HasForeignKey(attachment => attachment.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
