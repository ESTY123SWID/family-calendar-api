namespace my_project.Core.DTOs.Common;

// The single error format of the whole API
public class ErrorResponse
{
    public int Status { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Detail { get; set; }

    public string? CorrelationId { get; set; }

    // Validation errors per field, otherwise null
    public IDictionary<string, string[]>? Errors { get; set; }
}
