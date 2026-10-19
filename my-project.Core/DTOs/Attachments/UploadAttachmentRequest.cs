using my_project.Core.Enums;

namespace my_project.Core.DTOs.Attachments;

// Built by the controller from the multipart form, so the service does not depend on ASP.NET.
// There is no ContentType: the service decides it from the validated extension, never from the client
public class UploadAttachmentRequest
{
    // Must be seekable – the service reads the first bytes to verify the file type, then rewinds
    public Stream Content { get; set; } = Stream.Null;

    public string FileName { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public AttachmentKind Kind { get; set; }
}
