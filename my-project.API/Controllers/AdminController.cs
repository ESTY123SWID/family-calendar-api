using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my_project.Core.Common;
using my_project.Core.DTOs.Admin;
using my_project.Core.DTOs.Common;
using my_project.Core.DTOs.Events;
using my_project.Core.Enums;
using my_project.Core.Interfaces.Services;

namespace my_project.API.Controllers;

// System admin only. A regular user gets 403 on every endpoint
[Route("api/[controller]")]
[Authorize(Roles = nameof(SystemRole.SystemAdmin))]
public class AdminController : ApiControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(AdminStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        return FromResult(await _adminService.GetStatsAsync(cancellationToken));
    }

    // ?search&sortBy&sortDirection&page&pageSize
    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] AdminQueryParameters query, CancellationToken cancellationToken)
    {
        return FromResult(await _adminService.GetUsersAsync(query, cancellationToken));
    }

    // Block / unblock / change system role. Blocking or demoting yourself → 409
    [HttpPatch("users/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUser([FromRoute] int id, [FromBody] AdminUpdateUserRequest request, CancellationToken cancellationToken)
    {
        return FromResult(await _adminService.UpdateUserAsync(id, request, cancellationToken));
    }

    // ?search&sortBy&sortDirection&page&pageSize
    [HttpGet("families")]
    [ProducesResponseType(typeof(PagedResult<AdminFamilyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFamilies([FromQuery] AdminQueryParameters query, CancellationToken cancellationToken)
    {
        return FromResult(await _adminService.GetFamiliesAsync(query, cancellationToken));
    }

    [HttpGet("families/{id:int}")]
    [ProducesResponseType(typeof(AdminFamilyDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFamily([FromRoute] int id, CancellationToken cancellationToken)
    {
        return FromResult(await _adminService.GetFamilyAsync(id, cancellationToken));
    }

    // Read-only. ?from&to&status&search&sortBy&sortDirection&page&pageSize (as GET api/families/{familyId}/events)
    [HttpGet("families/{id:int}/events")]
    [ProducesResponseType(typeof(PagedResult<EventSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFamilyEvents([FromRoute] int id, [FromQuery] EventQueryParameters query, CancellationToken cancellationToken)
    {
        return FromResult(await _adminService.GetFamilyEventsAsync(id, query, cancellationToken));
    }

    [HttpDelete("families/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFamily([FromRoute] int id, CancellationToken cancellationToken)
    {
        return FromResult(await _adminService.DeleteFamilyAsync(id, cancellationToken));
    }
}
