using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my_project.Core.DTOs.Common;
using my_project.Core.DTOs.Participants;
using my_project.Core.Enums;
using my_project.Core.Interfaces.Services;

namespace my_project.API.Controllers;

[Route("api/events/{id:int}/participants")]
[Authorize]
public class ParticipantsController : ApiControllerBase
{
    private readonly IParticipantService _participantService;

    public ParticipantsController(IParticipantService participantService)
    {
        _participantService = participantService;
    }

    // The list (optionally filtered by ?rsvpStatus) and the RSVP counts
    [HttpGet]
    [ProducesResponseType(typeof(EventParticipantsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll([FromRoute] int id, [FromQuery] RsvpStatus? rsvpStatus, CancellationToken cancellationToken)
    {
        return FromResult(await _participantService.GetForEventAsync(id, rsvpStatus, cancellationToken));
    }

    // Organizer or family manager. Returns only the newly invited participants
    [HttpPost]
    [ProducesResponseType(typeof(IReadOnlyList<ParticipantDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Invite([FromRoute] int id, [FromBody] InviteParticipantsRequest request, CancellationToken cancellationToken)
    {
        var result = await _participantService.InviteAsync(id, request, cancellationToken);

        return CreatedFromResult(result, nameof(GetAll), _ => new { id });
    }

    // The current user answers for themselves only
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rsvp([FromRoute] int id, [FromBody] RsvpRequest request, CancellationToken cancellationToken)
    {
        return FromResult(await _participantService.RsvpAsync(id, request, cancellationToken));
    }

    // Organizer or family manager
    [HttpDelete("{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove([FromRoute] int id, [FromRoute] int userId, CancellationToken cancellationToken)
    {
        return FromResult(await _participantService.RemoveAsync(id, userId, cancellationToken));
    }
}
