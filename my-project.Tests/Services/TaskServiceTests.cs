using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using my_project.Core.Common;
using my_project.Core.DTOs.Tasks;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Exceptions;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Repositories;
using my_project.Core.Interfaces.Services;
using my_project.Service.Services;
using my_project.Tests.Helpers;

namespace my_project.Tests.Services;

// TaskService with mocked dependencies: the permission rules and the handling of the limited resource
// (a free task that only one member may take)
public class TaskServiceTests
{
    private const int FamilyId = 1;
    private const int OrganizerId = 10;
    private const int MemberId = 20;
    private const int OtherMemberId = 30;
    private const int OutsiderId = 99;
    private const int TaskId = 5;

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IFamilyTaskRepository> _tasks = new();
    private readonly Mock<IFamilyAccessService> _access = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly TestCurrentUser _currentUser = new(MemberId);

    private readonly Event _event = new() { Id = 3, FamilyId = FamilyId, OrganizerId = OrganizerId, Status = EventStatus.Scheduled };
    private readonly FamilyTask _task;

    public TaskServiceTests()
    {
        _task = new FamilyTask
        {
            Id = TaskId, EventId = _event.Id, Event = _event, Title = "להביא מיחם",
            Status = FamilyTaskStatus.Open, AssigneeId = null, Version = Guid.NewGuid()
        };

        _unitOfWork.Setup(unitOfWork => unitOfWork.FamilyTasks).Returns(_tasks.Object);
        _tasks.Setup(tasks => tasks.GetWithEventAsync(TaskId, It.IsAny<CancellationToken>())).ReturnsAsync(_task);

        // Members of the family: the organizer, MemberId and OtherMemberId. Only the organizer manages the event
        _access.Setup(access => access.IsMemberAsync(FamilyId, It.IsIn(OrganizerId, MemberId, OtherMemberId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _access.Setup(access => access.IsMemberAsync(FamilyId, OutsiderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _access.Setup(access => access.CanManageEventAsync(_event, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event evt, int userId, CancellationToken _) => userId == evt.OrganizerId);
        _access.Setup(access => access.EnsureMemberAsync(FamilyId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, int userId, CancellationToken _) =>
                userId == OutsiderId ? ServiceResult.Forbidden("not a member") : ServiceResult.Ok());
    }

    private TaskService CreateService()
    {
        return new TaskService(
            _unitOfWork.Object,
            _access.Object,
            _currentUser,
            new TestClock(),
            TestMapper.Create(),
            _notifications.Object,
            NullLogger<TaskService>.Instance);
    }

    [Fact]
    public async Task Assign_FreeTaskToMyself_AssignsAndSaves()
    {
        var result = await CreateService().AssignAsync(TaskId, new AssignTaskRequest { UserId = MemberId });

        Assert.True(result.IsSuccess);
        Assert.Equal(MemberId, _task.AssigneeId);
        Assert.Equal(FamilyTaskStatus.Assigned, _task.Status);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifications.Verify(notifications => notifications.NotifyTaskChangedAsync(_task, MemberId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Assign_TaskAlreadyTakenBySomeoneElse_ReturnsConflictWithoutSaving()
    {
        _task.AssigneeId = OtherMemberId;
        _task.Status = FamilyTaskStatus.Assigned;

        var result = await CreateService().AssignAsync(TaskId, new AssignTaskRequest { UserId = MemberId });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(OtherMemberId, _task.AssigneeId);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Assign_WhenAnotherRequestSavedFirst_ReturnsConflict()
    {
        // Both requests saw a free task; the UPDATE of this one finds a different Version → 0 rows
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("changed", new Exception()));

        var result = await CreateService().AssignAsync(TaskId, new AssignTaskRequest { UserId = MemberId });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _notifications.Verify(notifications => notifications.NotifyTaskChangedAsync(
            It.IsAny<FamilyTask>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Assign_UnknownTask_ReturnsNotFound()
    {
        var result = await CreateService().AssignAsync(404, new AssignTaskRequest { UserId = MemberId });

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Assign_MemberAssigningSomeoneElse_ReturnsForbidden()
    {
        var result = await CreateService().AssignAsync(TaskId, new AssignTaskRequest { UserId = OtherMemberId });

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Null(_task.AssigneeId);
    }

    [Fact]
    public async Task Assign_UserFromAnotherFamily_ReturnsForbidden()
    {
        _currentUser.UserId = OutsiderId;

        var result = await CreateService().AssignAsync(TaskId, new AssignTaskRequest { UserId = OutsiderId });

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Assign_OrganizerAssigningAMember_Succeeds()
    {
        _currentUser.UserId = OrganizerId;

        var result = await CreateService().AssignAsync(TaskId, new AssignTaskRequest { UserId = OtherMemberId });

        Assert.True(result.IsSuccess);
        Assert.Equal(OtherMemberId, _task.AssigneeId);
    }

    [Fact]
    public async Task Assign_OrganizerAssigningANonMember_ReturnsValidation()
    {
        _currentUser.UserId = OrganizerId;

        var result = await CreateService().AssignAsync(TaskId, new AssignTaskRequest { UserId = OutsiderId });

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Assign_OnCancelledEvent_ReturnsValidation()
    {
        _event.Status = EventStatus.Cancelled;

        var result = await CreateService().AssignAsync(TaskId, new AssignTaskRequest { UserId = MemberId });

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task UpdateStatus_ByMemberWhoIsNotTheAssignee_ReturnsForbidden()
    {
        _task.AssigneeId = OtherMemberId;
        _task.Status = FamilyTaskStatus.Assigned;

        var result = await CreateService().UpdateStatusAsync(TaskId, new UpdateTaskStatusRequest { Status = FamilyTaskStatus.Done });

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(FamilyTaskStatus.Assigned, _task.Status);
    }

    [Fact]
    public async Task UpdateStatus_ByTheAssignee_Succeeds()
    {
        _task.AssigneeId = MemberId;
        _task.Status = FamilyTaskStatus.Assigned;

        var result = await CreateService().UpdateStatusAsync(TaskId, new UpdateTaskStatusRequest { Status = FamilyTaskStatus.Done });

        Assert.True(result.IsSuccess);
        Assert.Equal(FamilyTaskStatus.Done, _task.Status);
    }

    [Fact]
    public async Task UpdateStatus_ToOpen_ReturnsValidation()
    {
        _task.AssigneeId = MemberId;
        _task.Status = FamilyTaskStatus.Assigned;

        var result = await CreateService().UpdateStatusAsync(TaskId, new UpdateTaskStatusRequest { Status = FamilyTaskStatus.Open });

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Update_WithStaleVersion_ReturnsConflict()
    {
        _currentUser.UserId = OrganizerId;
        _access.Setup(access => access.EnsureCanManageEventAsync(_event, OrganizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Ok());
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("changed", new Exception()));

        var staleVersion = Guid.NewGuid();
        var request = new UpdateTaskRequest
        {
            Title = "כותרת חדשה", Category = TaskCategory.Food, Priority = TaskPriority.High, Version = staleVersion
        };

        var result = await CreateService().UpdateAsync(TaskId, request);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        // The Version the client sent is the one checked in the WHERE clause
        _tasks.Verify(tasks => tasks.SetOriginalVersion(_task, staleVersion), Times.Once);
    }

    [Fact]
    public async Task Update_ByMemberWhoIsNotTheOrganizer_ReturnsForbidden()
    {
        _access.Setup(access => access.EnsureCanManageEventAsync(_event, MemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Forbidden("manager only"));

        var request = new UpdateTaskRequest
        {
            Title = "x", Category = TaskCategory.Food, Priority = TaskPriority.Low, Version = _task.Version
        };

        var result = await CreateService().UpdateAsync(TaskId, request);

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_UserFromAnotherFamily_ReturnsForbidden()
    {
        _currentUser.UserId = OutsiderId;

        var result = await CreateService().GetByIdAsync(TaskId);

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Null(result.Data);
    }
}
