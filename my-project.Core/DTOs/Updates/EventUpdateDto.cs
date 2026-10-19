namespace my_project.Core.DTOs.Updates;

public class EventUpdateDto
{
    public int Id { get; set; }

    public int EventId { get; set; }

    // Filled when the update is shown outside its event (dashboard)
    public string? EventTitle { get; set; }

    public int AuthorId { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    // UTC
    public DateTime CreatedAt { get; set; }
}
