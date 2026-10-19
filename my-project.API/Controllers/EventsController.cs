using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my_project.Core.Common;
using my_project.Core.DTOs.Common;
using my_project.Core.DTOs.Events;
using my_project.Core.Interfaces.Services;

namespace my_project.API.Controllers;

[Route("api")]
[Authorize]
public class EventsController : ApiControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    // ?from&to&status&search&sortBy&sortDirection&page&pageSize
    [HttpGet("families/{familyId:int}/events")]
    [ProducesResponseType(typeof(PagedResult<EventSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForFamily(
        [FromRoute] int familyId,
        [FromQuery] EventQueryParameters query,
        CancellationToken cancellationToken)
    {
        return FromResult(await _eventService.GetPagedAsync(familyId, query, cancellationToken));
    }

    // Any family member; the creator becomes the organizer
    [HttpPost("families/{familyId:int}/events")]
    [ProducesResponseType(typeof(EventDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromRoute] int familyId, [FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventService.CreateAsync(familyId, request, cancellationToken);

        return CreatedFromResult(result, nameof(GetById), evt => new { id = evt.Id });
    }

    [HttpGet("events/{id:int}")]
    [ProducesResponseType(typeof(EventDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        return FromResult(await _eventService.GetByIdAsync(id, cancellationToken));
    }

    // Organizer or family manager. A stale Version → 409
    [HttpPut("events/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateEventRequest request, CancellationToken cancellationToken)
    {
        return FromResult(await _eventService.UpdateAsync(id, request, cancellationToken));
    }

    // Cancels the event (Status = Cancelled), no physical delete
    [HttpDelete("events/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel([FromRoute] int id, CancellationToken cancellationToken)
    {
        return FromResult(await _eventService.CancelAsync(id, cancellationToken));
    }
}
