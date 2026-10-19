namespace my_project.Core.DTOs.Attachments;

// A file ready to be streamed to the client. The caller disposes Content
public class AttachmentContentDto
{
    public Stream Content { get; set; } = Stream.Null;

    public string ContentType { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
}
