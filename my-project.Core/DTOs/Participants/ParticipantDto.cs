using my_project.Core.Enums;

namespace my_project.Core.DTOs.Participants;

public class ParticipantDto
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public RsvpStatus RsvpStatus { get; set; }

    public int GuestsCount { get; set; }

    // UTC
    public DateTime InvitedAt { get; set; }

    // UTC
    public DateTime? RespondedAt { get; set; }
}
