using my_project.Core.Enums;

namespace my_project.Core.DTOs.Attachments;

public class AttachmentDto
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public AttachmentKind Kind { get; set; }

    public long Size { get; set; }

    // Download link
    public string Url { get; set; } = string.Empty;

    public int UploadedById { get; set; }

    public string UploadedBy { get; set; } = string.Empty;

    // UTC
    public DateTime UploadedAt { get; set; }
}
