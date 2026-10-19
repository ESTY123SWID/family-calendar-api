using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my_project.Core.DTOs.Common;
using my_project.Core.DTOs.Dashboard;
using my_project.Core.DTOs.Families;
using my_project.Core.Interfaces.Services;

namespace my_project.API.Controllers;

[Route("api/families")]
[Authorize]
public class FamiliesController : ApiControllerBase
{
    private readonly IFamilyService _familyService;
    private readonly IDashboardService _dashboardService;

    public FamiliesController(IFamilyService familyService, IDashboardService dashboardService)
    {
        _familyService = familyService;
        _dashboardService = dashboardService;
    }

    // The families of the current user
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FamilyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        return FromResult(await _familyService.GetMineAsync(cancellationToken));
    }

    // The creator becomes the family manager
    [HttpPost]
    [ProducesResponseType(typeof(FamilyDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFamilyRequest request, CancellationToken cancellationToken)
    {
        var result = await _familyService.CreateAsync(request, cancellationToken);

        return CreatedFromResult(result, nameof(GetById), family => new { familyId = family.Id });
    }

    [HttpGet("{familyId:int}")]
    [ProducesResponseType(typeof(FamilyDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int familyId, CancellationToken cancellationToken)
    {
        return FromResult(await _familyService.GetDetailsAsync(familyId, cancellationToken));
    }

    [HttpPut("{familyId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] int familyId, [FromBody] UpdateFamilyRequest request, CancellationToken cancellationToken)
    {
        return FromResult(await _familyService.UpdateAsync(familyId, request, cancellationToken));
    }

    [HttpDelete("{familyId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int familyId, CancellationToken cancellationToken)
    {
        return FromResult(await _familyService.DeleteAsync(familyId, cancellationToken));
    }

    // Join a family with its code (sent in the body, not in the URL)
    [HttpPost("memberships")]
    [ProducesResponseType(typeof(FamilyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Join([FromBody] JoinFamilyRequest request, CancellationToken cancellationToken)
    {
        var result = await _familyService.JoinAsync(request, cancellationToken);

        return CreatedFromResult(result, nameof(GetById), family => new { familyId = family.Id });
    }

    [HttpGet("{familyId:int}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<FamilyMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMembers([FromRoute] int familyId, CancellationToken cancellationToken)
    {
        return FromResult(await _familyService.GetMembersAsync(familyId, cancellationToken));
    }

    [HttpPatch("{familyId:int}/members/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeMemberRole(
        [FromRoute] int familyId,
        [FromRoute] int userId,
        [FromBody] ChangeMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _familyService.ChangeMemberRoleAsync(familyId, userId, request, cancellationToken));
    }

    // A manager removes a member, or a user removes themselves (leaving the family)
    [HttpDelete("{familyId:int}/members/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveMember([FromRoute] int familyId, [FromRoute] int userId, CancellationToken cancellationToken)
    {
        return FromResult(await _familyService.RemoveMemberAsync(familyId, userId, cancellationToken));
    }

    [HttpPut("{familyId:int}/join-code")]
    [ProducesResponseType(typeof(JoinCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegenerateJoinCode([FromRoute] int familyId, CancellationToken cancellationToken)
    {
        return FromResult(await _familyService.RegenerateJoinCodeAsync(familyId, cancellationToken));
    }

    [HttpGet("{familyId:int}/dashboard")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDashboard([FromRoute] int familyId, CancellationToken cancellationToken)
    {
        return FromResult(await _dashboardService.GetAsync(familyId, cancellationToken));
    }
}
