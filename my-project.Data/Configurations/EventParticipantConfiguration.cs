using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my_project.Core.Entities;

namespace my_project.Data.Configurations;

public class EventParticipantConfiguration : IEntityTypeConfiguration<EventParticipant>
{
    public void Configure(EntityTypeBuilder<EventParticipant> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_EventParticipants_GuestsCount",
            "\"GuestsCount\" BETWEEN 0 AND 20"));

        builder.HasKey(participant => new { participant.EventId, participant.UserId });

        builder.HasOne(participant => participant.Event)
            .WithMany(evt => evt.Participants)
            .HasForeignKey(participant => participant.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(participant => participant.User)
            .WithMany(user => user.EventParticipations)
            .HasForeignKey(participant => participant.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
