using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my_project.Core.DTOs.Common;
using my_project.Core.DTOs.Notifications;
using my_project.Core.DTOs.Users;
using my_project.Core.Interfaces.Services;

namespace my_project.API.Controllers;

[Route("api/users")]
[Authorize]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly INotificationSettingsService _notificationSettingsService;

    public UsersController(IUserService userService, INotificationSettingsService notificationSettingsService)
    {
        _userService = userService;
        _notificationSettingsService = notificationSettingsService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        return FromResult(await _userService.GetMeAsync(cancellationToken));
    }

    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        return FromResult(await _userService.UpdateMeAsync(request, cancellationToken));
    }

    // Allowed for the user, members of a common family and the system admin
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        return FromResult(await _userService.GetByIdAsync(id, cancellationToken));
    }

    [HttpGet("me/notification-settings")]
    [ProducesResponseType(typeof(NotificationSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotificationSettings(CancellationToken cancellationToken)
    {
        return FromResult(await _notificationSettingsService.GetMineAsync(cancellationToken));
    }

    [HttpPut("me/notification-settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMyNotificationSettings(
        [FromBody] UpdateNotificationSettingsRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _notificationSettingsService.UpdateMineAsync(request, cancellationToken));
    }
}
