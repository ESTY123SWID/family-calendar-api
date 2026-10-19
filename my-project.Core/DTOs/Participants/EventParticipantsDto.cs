namespace my_project.Core.DTOs.Participants;

// The participant list of an event together with its RSVP counts
public class EventParticipantsDto
{
    public IReadOnlyList<ParticipantDto> Items { get; set; } = Array.Empty<ParticipantDto>();

    // Always counts all participants, even when the list is filtered
    public RsvpSummaryDto Summary { get; set; } = new();
}
