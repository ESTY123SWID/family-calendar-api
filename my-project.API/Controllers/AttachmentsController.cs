using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my_project.API.Filters;
using my_project.API.Models;
using my_project.Core.DTOs.Attachments;
using my_project.Core.DTOs.Common;
using my_project.Core.Enums;
using my_project.Core.Interfaces.Services;

namespace my_project.API.Controllers;

[Route("api")]
[Authorize]
public class AttachmentsController : ApiControllerBase
{
    private readonly IAttachmentService _attachmentService;

    public AttachmentsController(IAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    // ?kind=Image|Document|Invitation
    [HttpGet("events/{id:int}/attachments")]
    [ProducesResponseType(typeof(IReadOnlyList<AttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent([FromRoute] int id, [FromQuery] AttachmentKind? kind, CancellationToken cancellationToken)
    {
        return FromResult(await _attachmentService.GetForEventAsync(id, kind, cancellationToken));
    }

    // Image / Document: any member. Invitation: organizer or family manager (replaces the previous one).
    // Only a Stream and the file details are passed on – the service does not know ASP.NET
    [HttpPost("events/{id:int}/attachments")]
    [Consumes("multipart/form-data")]
    [UploadSizeLimit]
    [ProducesResponseType(typeof(AttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upload([FromRoute] int id, [FromForm] UploadAttachmentForm form, CancellationToken cancellationToken)
    {
        await using var content = form.File.OpenReadStream();

        var request = new UploadAttachmentRequest
        {
            Content = content,
            FileName = form.File.FileName,
            SizeBytes = form.File.Length,
            Kind = form.Kind!.Value
        };

        var result = await _attachmentService.UploadAsync(id, request, cancellationToken);

        return CreatedFromResult(result, nameof(GetContent), attachment => new { id = attachment.Id });
    }

    // Streams the file itself (not JSON)
    [HttpGet("attachments/{id:int}/content")]
    [Produces("application/octet-stream", "application/json")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContent([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _attachmentService.GetContentAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromResult(result);
        }

        // The browser must use the Content-Type the server chose, never guess another one from the bytes
        Response.Headers.XContentTypeOptions = "nosniff";

        // FileStreamResult disposes the stream after sending it.
        // The file name makes it a download (Content-Disposition: attachment), not a page opened in the browser
        var file = result.Data!;
        return File(file.Content, file.ContentType, file.FileName);
    }

    // The uploader, the organizer or a family manager
    [HttpDelete("attachments/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        return FromResult(await _attachmentService.DeleteAsync(id, cancellationToken));
    }
}
