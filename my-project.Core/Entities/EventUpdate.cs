namespace my_project.Core.Entities;

public class EventUpdate
{
    public int Id { get; set; }

    // FK → Event (Cascade)
    public int EventId { get; set; }

    // FK → User (Restrict)
    public int AuthorId { get; set; }

    // Required, max 1000
    public string Content { get; set; } = string.Empty;

    // UTC
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Event Event { get; set; } = null!;
    public User Author { get; set; } = null!;
}
