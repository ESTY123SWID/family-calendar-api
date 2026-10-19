using AutoMapper;
using Microsoft.Extensions.Logging;
using my_project.Core.Common;
using my_project.Core.DTOs.Tasks;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Exceptions;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Interfaces.Services;
using my_project.Service.Common;

namespace my_project.Service.Services;

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFamilyAccessService _access;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly INotificationService _notifications;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        IUnitOfWork unitOfWork,
        IFamilyAccessService access,
        ICurrentUser currentUser,
        IClock clock,
        IMapper mapper,
        INotificationService notifications,
        ILogger<TaskService> logger)
    {
        _unitOfWork = unitOfWork;
        _access = access;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedResult<TaskDto>>> GetPagedAsync(
        int eventId,
        TaskQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var evt = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return ServiceResult<PagedResult<TaskDto>>.NotFound(ErrorMessages.EventNotFound);
        }

        var access = await _access.EnsureMemberAsync(evt.FamilyId, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<PagedResult<TaskDto>>.FromFailure(access);
        }

        var page = await _unitOfWork.FamilyTasks.GetPagedForEventAsync(eventId, query, cancellationToken);

        return ServiceResult<PagedResult<TaskDto>>.Ok(new PagedResult<TaskDto>(
            ToDtos(page.Items),
            page.Page,
            page.PageSize,
            page.TotalCount));
    }

    public async Task<ServiceResult<TaskProgressDto>> GetProgressAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var evt = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return ServiceResult<TaskProgressDto>.NotFound(ErrorMessages.EventNotFound);
        }

        var access = await _access.EnsureMemberAsync(evt.FamilyId, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<TaskProgressDto>.FromFailure(access);
        }

        // Grouped counts come from the database; only the small grouped result is summed here
        var counts = await _unitOfWork.FamilyTasks.GetStatusCountsAsync(eventId, cancellationToken);
        var overdue = await _unitOfWork.FamilyTasks.CountOverdueAsync(eventId, _clock.UtcNow, cancellationToken);

        var total = counts.Sum(count => count.Count);
        var done = counts.Where(count => count.Status == FamilyTaskStatus.Done).Sum(count => count.Count);

        var byCategory = counts
            .GroupBy(count => count.Category)
            .OrderBy(group => group.Key)
            .Select(group => new CategoryProgressDto
            {
                Category = group.Key,
                Total = group.Sum(count => count.Count),
                Done = group.Where(count => count.Status == FamilyTaskStatus.Done).Sum(count => count.Count)
            })
            .ToList();

        return ServiceResult<TaskProgressDto>.Ok(new TaskProgressDto
        {
            Total = total,
            Done = done,
            Open = total - done,
            Overdue = overdue,
            ByCategory = byCategory
        });
    }

    public async Task<ServiceResult<IReadOnlyList<TaskDto>>> GetMineAsync(
        int familyId,
        FamilyTaskStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var access = await _access.EnsureMemberAsync(familyId, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<IReadOnlyList<TaskDto>>.FromFailure(access);
        }

        var tasks = await _unitOfWork.FamilyTasks.GetForUserAsync(
            familyId, _currentUser.UserId, status, cancellationToken: cancellationToken);

        return ServiceResult<IReadOnlyList<TaskDto>>.Ok(ToDtos(tasks));
    }

    public async Task<ServiceResult<TaskDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var (task, failure) = await LoadForMemberAsync(id, cancellationToken);
        if (task is null)
        {
            return ServiceResult<TaskDto>.FromFailure(failure!);
        }

        return ServiceResult<TaskDto>.Ok(ToDto(task));
    }

    public async Task<ServiceResult<TaskDto>> CreateAsync(
        int eventId,
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var evt = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return ServiceResult<TaskDto>.NotFound(ErrorMessages.EventNotFound);
        }

        var access = await _access.EnsureCanManageEventAsync(evt, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<TaskDto>.FromFailure(access);
        }

        if (evt.Status == EventStatus.Cancelled)
        {
            return ServiceResult<TaskDto>.Validation(ErrorMessages.EventCancelled);
        }

        // A new task is always free for volunteering
        var task = new FamilyTask
        {
            EventId = eventId,
            Event = evt,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Category = request.Category!.Value,
            Priority = request.Priority,
            DueDate = request.DueDate.ToUtc(),
            Status = FamilyTaskStatus.Open,
            CreatedAt = _clock.UtcNow
        };

        await _unitOfWork.FamilyTasks.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Task {TaskId} created in event {EventId}", task.Id, eventId);

        return ServiceResult<TaskDto>.Ok(ToDto(task));
    }

    public async Task<ServiceResult> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var (task, failure) = await LoadForManagerAsync(id, cancellationToken);
        if (task is null)
        {
            return failure!;
        }

        // The Version the client saw goes into the WHERE clause → two editors at once = 409
        _unitOfWork.FamilyTasks.SetOriginalVersion(task, request.Version!.Value);

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.Category = request.Category!.Value;
        task.Priority = request.Priority!.Value;
        task.DueDate = request.DueDate.ToUtc();

        if (!await TrySaveAsync(id, cancellationToken))
        {
            return ServiceResult.Conflict(ErrorMessages.TaskChangedByOther);
        }

        await _notifications.NotifyTaskChangedAsync(task, _currentUser.UserId, cancellationToken);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var (task, failure) = await LoadForManagerAsync(id, cancellationToken, allowCancelledEvent: true);
        if (task is null)
        {
            return failure!;
        }

        _unitOfWork.FamilyTasks.Remove(task);

        if (!await TrySaveAsync(id, cancellationToken))
        {
            return ServiceResult.Conflict(ErrorMessages.TaskChangedByOther);
        }

        _logger.LogInformation("Task {TaskId} deleted by user {UserId}", id, _currentUser.UserId);

        return ServiceResult.Ok();
    }

    // ⭐ The limited resource: a free task that several family members may try to take at the same moment
    public async Task<ServiceResult> AssignAsync(int id, AssignTaskRequest request, CancellationToken cancellationToken = default)
    {
        var me = _currentUser.UserId;
        var targetUserId = request.UserId!.Value;

        // Tracked: the Version loaded here goes into the WHERE clause of the UPDATE
        var task = await _unitOfWork.FamilyTasks.GetWithEventAsync(id, cancellationToken);
        if (task is null)
        {
            return ServiceResult.NotFound(ErrorMessages.TaskNotFound);
        }

        var evt = task.Event;
        if (!await _access.IsMemberAsync(evt.FamilyId, me, cancellationToken))
        {
            return ServiceResult.Forbidden(ErrorMessages.NotFamilyMember);
        }

        var canManage = await _access.CanManageEventAsync(evt, me, cancellationToken);

        // A member volunteers only for themselves; the organizer / family manager may assign anyone
        if (targetUserId != me && !canManage)
        {
            return ServiceResult.Forbidden(ErrorMessages.VolunteerSelfOnly);
        }

        if (evt.Status == EventStatus.Cancelled)
        {
            return ServiceResult.Validation(ErrorMessages.EventCancelled);
        }

        if (targetUserId != me && !await _access.IsMemberAsync(evt.FamilyId, targetUserId, cancellationToken))
        {
            return ServiceResult.Validation(ErrorMessages.AssigneeNotMember);
        }

        // Already assigned to the same user – nothing to change
        if (task.AssigneeId == targetUserId)
        {
            return ServiceResult.Ok();
        }

        // Taken before this request was read – a member cannot take it over
        if (task.AssigneeId is not null && !canManage)
        {
            return ServiceResult.Conflict(ErrorMessages.TaskAlreadyTaken);
        }

        task.AssigneeId = targetUserId;
        task.Status = FamilyTaskStatus.Assigned;

        try
        {
            // UPDATE ... WHERE Id = @id AND Version = @loadedVersion
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Both requests saw a free task, but the other UPDATE changed the Version first
            _logger.LogWarning("Task {TaskId} was taken by someone else", id);
            return ServiceResult.Conflict(ErrorMessages.TaskTakenConcurrently);
        }

        _logger.LogInformation("Task {TaskId} assigned to user {AssigneeId} by user {UserId}", id, targetUserId, me);

        await _notifications.NotifyTaskChangedAsync(task, me, cancellationToken);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UnassignAsync(int id, CancellationToken cancellationToken = default)
    {
        var (task, failure) = await LoadForAssigneeOrManagerAsync(id, cancellationToken);
        if (task is null)
        {
            return failure!;
        }

        // Removing an assignee that is not there is not an error
        if (task.AssigneeId is null)
        {
            return ServiceResult.Ok();
        }

        task.AssigneeId = null;
        task.Status = FamilyTaskStatus.Open;

        if (!await TrySaveAsync(id, cancellationToken))
        {
            return ServiceResult.Conflict(ErrorMessages.TaskChangedByOther);
        }

        _logger.LogInformation("Task {TaskId} released by user {UserId}", id, _currentUser.UserId);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateStatusAsync(
        int id,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var (task, failure) = await LoadForAssigneeOrManagerAsync(id, cancellationToken);
        if (task is null)
        {
            return failure!;
        }

        var status = request.Status!.Value;

        // Open always means "no assignee" – that transition goes through UnassignAsync
        if (status == FamilyTaskStatus.Open)
        {
            return ServiceResult.Validation(ErrorMessages.OpenStatusViaUnassign);
        }

        if (task.AssigneeId is null)
        {
            return ServiceResult.Validation(ErrorMessages.TaskNotAssigned);
        }

        if (task.Status == status)
        {
            return ServiceResult.Ok();
        }

        task.Status = status;

        if (!await TrySaveAsync(id, cancellationToken))
        {
            return ServiceResult.Conflict(ErrorMessages.TaskChangedByOther);
        }

        await _notifications.NotifyTaskChangedAsync(task, _currentUser.UserId, cancellationToken);

        return ServiceResult.Ok();
    }

    // Loads the task (tracked, with its Event) and checks that the current user is a family member
    private async Task<(FamilyTask? Task, ServiceResult? Failure)> LoadForMemberAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var task = await _unitOfWork.FamilyTasks.GetWithEventAsync(id, cancellationToken);
        if (task is null)
        {
            return (null, ServiceResult.NotFound(ErrorMessages.TaskNotFound));
        }

        var access = await _access.EnsureMemberAsync(task.Event.FamilyId, _currentUser.UserId, cancellationToken);

        return access.IsSuccess ? (task, null) : (null, access);
    }

    // Organizer or family manager. Changes are blocked on a cancelled event (deleting is allowed)
    private async Task<(FamilyTask? Task, ServiceResult? Failure)> LoadForManagerAsync(
        int id,
        CancellationToken cancellationToken,
        bool allowCancelledEvent = false)
    {
        var task = await _unitOfWork.FamilyTasks.GetWithEventAsync(id, cancellationToken);
        if (task is null)
        {
            return (null, ServiceResult.NotFound(ErrorMessages.TaskNotFound));
        }

        var access = await _access.EnsureCanManageEventAsync(task.Event, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return (null, access);
        }

        if (!allowCancelledEvent && task.Event.Status == EventStatus.Cancelled)
        {
            return (null, ServiceResult.Validation(ErrorMessages.EventCancelled));
        }

        return (task, null);
    }

    // The assignee, the organizer or a family manager. Blocked on a cancelled event
    private async Task<(FamilyTask? Task, ServiceResult? Failure)> LoadForAssigneeOrManagerAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var (task, failure) = await LoadForMemberAsync(id, cancellationToken);
        if (task is null)
        {
            return (null, failure);
        }

        var me = _currentUser.UserId;
        if (task.AssigneeId != me && !await _access.CanManageEventAsync(task.Event, me, cancellationToken))
        {
            return (null, ServiceResult.Forbidden(ErrorMessages.TaskAssigneeOrManagerOnly));
        }

        if (task.Event.Status == EventStatus.Cancelled)
        {
            return (null, ServiceResult.Validation(ErrorMessages.EventCancelled));
        }

        return (task, null);
    }

    // False when someone else changed the task since it was loaded (409)
    private async Task<bool> TrySaveAsync(int taskId, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Concurrency conflict on task {TaskId}", taskId);
            return false;
        }
    }

    private TaskDto ToDto(FamilyTask task)
    {
        var dto = _mapper.Map<TaskDto>(task);
        dto.IsOverdue = DomainRules.IsOverdue(task, _clock.UtcNow);
        return dto;
    }

    private List<TaskDto> ToDtos(IEnumerable<FamilyTask> tasks)
    {
        return tasks.Select(ToDto).ToList();
    }
}
