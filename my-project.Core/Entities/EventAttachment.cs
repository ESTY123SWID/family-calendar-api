using my_project.Core.Enums;

namespace my_project.Core.Entities;

// File metadata only – the file itself is stored on disk
public class EventAttachment
{
    public int Id { get; set; }

    // FK → Event (Cascade)
    public int EventId { get; set; }

    // FK → User (Restrict)
    public int UploadedById { get; set; }

    // Required, max 255
    public string OriginalFileName { get; set; } = string.Empty;

    // Required, max 100. GUID + extension
    public string StoredFileName { get; set; } = string.Empty;

    // Required, max 100
    public string ContentType { get; set; } = string.Empty;

    // Max 5MB
    public long SizeBytes { get; set; }

    // Invitation: one per event (a new upload replaces the previous one)
    public AttachmentKind Kind { get; set; }

    // UTC
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Event Event { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
}
