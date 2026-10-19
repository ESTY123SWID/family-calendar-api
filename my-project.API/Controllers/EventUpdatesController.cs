using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my_project.Core.DTOs.Common;
using my_project.Core.DTOs.Updates;
using my_project.Core.Interfaces.Services;

namespace my_project.API.Controllers;

[Route("api/events/{id:int}/updates")]
[Authorize]
public class EventUpdatesController : ApiControllerBase
{
    private readonly IEventUpdateService _eventUpdateService;

    public EventUpdatesController(IEventUpdateService eventUpdateService)
    {
        _eventUpdateService = eventUpdateService;
    }

    // Newest first
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventUpdateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll([FromRoute] int id, CancellationToken cancellationToken)
    {
        return FromResult(await _eventUpdateService.GetForEventAsync(id, cancellationToken));
    }

    // Organizer or family manager
    [HttpPost]
    [ProducesResponseType(typeof(EventUpdateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromRoute] int id, [FromBody] CreateEventUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventUpdateService.CreateAsync(id, request, cancellationToken);

        return CreatedFromResult(result, nameof(GetAll), _ => new { id });
    }

    // Organizer or family manager
    [HttpDelete("{updateId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, [FromRoute] int updateId, CancellationToken cancellationToken)
    {
        return FromResult(await _eventUpdateService.DeleteAsync(id, updateId, cancellationToken));
    }
}
