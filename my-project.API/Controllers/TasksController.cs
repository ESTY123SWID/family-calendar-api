using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my_project.Core.Common;
using my_project.Core.DTOs.Common;
using my_project.Core.DTOs.Tasks;
using my_project.Core.Enums;
using my_project.Core.Interfaces.Services;

namespace my_project.API.Controllers;

[Route("api")]
[Authorize]
public class TasksController : ApiControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    // ?category&status&assigneeId&openOnly&sortBy&sortDirection&page&pageSize
    [HttpGet("events/{id:int}/tasks")]
    [ProducesResponseType(typeof(PagedResult<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(
        [FromRoute] int id,
        [FromQuery] TaskQueryParameters query,
        CancellationToken cancellationToken)
    {
        return FromResult(await _taskService.GetPagedAsync(id, query, cancellationToken));
    }

    [HttpGet("events/{id:int}/tasks/progress")]
    [ProducesResponseType(typeof(TaskProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProgress([FromRoute] int id, CancellationToken cancellationToken)
    {
        return FromResult(await _taskService.GetProgressAsync(id, cancellationToken));
    }

    // Organizer or family manager. A new task is free for volunteering
    [HttpPost("events/{id:int}/tasks")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromRoute] int id, [FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await _taskService.CreateAsync(id, request, cancellationToken);

        return CreatedFromResult(result, nameof(GetById), task => new { id = task.Id });
    }

    // Tasks assigned to the current user in all the family events
    [HttpGet("families/{familyId:int}/tasks/mine")]
    [ProducesResponseType(typeof(IReadOnlyList<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMine(
        [FromRoute] int familyId,
        [FromQuery] FamilyTaskStatus? status,
        CancellationToken cancellationToken)
    {
        return FromResult(await _taskService.GetMineAsync(familyId, status, cancellationToken));
    }

    [HttpGet("tasks/{id:int}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        return FromResult(await _taskService.GetByIdAsync(id, cancellationToken));
    }

    // Organizer or family manager. A stale Version → 409
    [HttpPut("tasks/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        return FromResult(await _taskService.UpdateAsync(id, request, cancellationToken));
    }

    // Organizer or family manager
    [HttpDelete("tasks/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        return FromResult(await _taskService.DeleteAsync(id, cancellationToken));
    }

    // ⭐ The limited resource. A member volunteers (own id only); the organizer / manager may assign anyone.
    // Two members taking the same free task at once: the first gets 204, the second 409
    [HttpPut("tasks/{id:int}/assignee")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Assign([FromRoute] int id, [FromBody] AssignTaskRequest request, CancellationToken cancellationToken)
    {
        return FromResult(await _taskService.AssignAsync(id, request, cancellationToken));
    }

    // The assignee, the organizer or a family manager. The task goes back to Open
    [HttpDelete("tasks/{id:int}/assignee")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Unassign([FromRoute] int id, CancellationToken cancellationToken)
    {
        return FromResult(await _taskService.UnassignAsync(id, cancellationToken));
    }

    // The assignee, the organizer or a family manager
    [HttpPatch("tasks/{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus([FromRoute] int id, [FromBody] UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        return FromResult(await _taskService.UpdateStatusAsync(id, request, cancellationToken));
    }
}
