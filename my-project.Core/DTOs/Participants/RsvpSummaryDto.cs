namespace my_project.Core.DTOs.Participants;

public class RsvpSummaryDto
{
    public int Coming { get; set; }

    public int NotComing { get; set; }

    public int Maybe { get; set; }

    public int Pending { get; set; }

    // Guests brought by participants who answered Coming
    public int TotalGuests { get; set; }
}
