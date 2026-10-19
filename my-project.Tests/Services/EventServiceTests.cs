using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using my_project.Core.Common;
using my_project.Core.DTOs.Events;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Exceptions;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Repositories;
using my_project.Core.Interfaces.Services;
using my_project.Service.Services;
using my_project.Tests.Helpers;

namespace my_project.Tests.Services;

public class EventServiceTests
{
    private const int FamilyId = 1;
    private const int OrganizerId = 1;
    private const int MemberId = 2;
    private const int OutsiderId = 9;

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<IFamilyMemberRepository> _members = new();
    private readonly Mock<IFamilyAccessService> _access = new();
    private readonly TestCurrentUser _currentUser = new(OrganizerId);
    private readonly TestClock _clock = new();
    private readonly Event _event;

    public EventServiceTests()
    {
        _event = new Event
        {
            Id = 7, FamilyId = FamilyId, OrganizerId = OrganizerId, Title = "שבת", Address = "ירושלים",
            StartAt = _clock.UtcNow.AddDays(10), Status = EventStatus.Scheduled, Version = Guid.NewGuid()
        };

        _unitOfWork.Setup(unitOfWork => unitOfWork.Events).Returns(_events.Object);
        _unitOfWork.Setup(unitOfWork => unitOfWork.FamilyMembers).Returns(_members.Object);
        _events.Setup(events => events.GetByIdAsync(_event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_event);

        _members.Setup(members => members.GetByFamilyIdAsync(FamilyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FamilyMember>
            {
                new() { FamilyId = FamilyId, UserId = OrganizerId, Role = FamilyRole.Manager },
                new() { FamilyId = FamilyId, UserId = MemberId, Role = FamilyRole.Member }
            });

        _access.Setup(access => access.EnsureMemberAsync(FamilyId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, int userId, CancellationToken _) =>
                userId == OutsiderId ? ServiceResult.Forbidden("not a member") : ServiceResult.Ok());
        _access.Setup(access => access.EnsureCanManageEventAsync(_event, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event evt, int userId, CancellationToken _) =>
                userId == evt.OrganizerId ? ServiceResult.Ok() : ServiceResult.Forbidden("manager only"));
    }

    private EventService CreateService()
    {
        return new EventService(
            _unitOfWork.Object,
            _access.Object,
            _currentUser,
            _clock,
            TestMapper.Create(),
            Mock.Of<INotificationService>(),
            NullLogger<EventService>.Instance);
    }

    private CreateEventRequest NewEventRequest(DateTime startAt, DateTime? endAt = null) => new()
    {
        Title = "יום הולדת", Address = "תל אביב", StartAt = startAt, EndAt = endAt, InviteAll = true
    };

    private UpdateEventRequest UpdateRequest(Guid version) => new()
    {
        Title = "שבת משפחתית", Address = "ירושלים", StartAt = _event.StartAt, Version = version
    };

    [Fact]
    public async Task Create_StartInThePast_ReturnsValidation()
    {
        var result = await CreateService().CreateAsync(FamilyId, NewEventRequest(_clock.UtcNow.AddHours(-1)));

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _events.Verify(events => events.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_EndBeforeStart_ReturnsValidation()
    {
        var start = _clock.UtcNow.AddDays(3);

        var result = await CreateService().CreateAsync(FamilyId, NewEventRequest(start, start.AddHours(-2)));

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Create_InviteeFromAnotherFamily_ReturnsValidation()
    {
        var request = NewEventRequest(_clock.UtcNow.AddDays(3));
        request.InviteAll = false;
        request.InviteeIds = [MemberId, OutsiderId];

        var result = await CreateService().CreateAsync(FamilyId, request);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Create_ByUserOfAnotherFamily_ReturnsForbidden()
    {
        _currentUser.UserId = OutsiderId;

        var result = await CreateService().CreateAsync(FamilyId, NewEventRequest(_clock.UtcNow.AddDays(3)));

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Create_InviteAll_AddsEveryMemberAndTheOrganizerIsComing()
    {
        Event? added = null;
        _events.Setup(events => events.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback((Event evt, CancellationToken _) => added = evt);

        await CreateService().CreateAsync(FamilyId, NewEventRequest(_clock.UtcNow.AddDays(3)));

        Assert.NotNull(added);
        Assert.Equal(2, added.Participants.Count);
        Assert.Equal(RsvpStatus.Coming, added.Participants.Single(p => p.UserId == OrganizerId).RsvpStatus);
        Assert.Equal(RsvpStatus.Pending, added.Participants.Single(p => p.UserId == MemberId).RsvpStatus);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ByMemberWhoIsNotTheOrganizer_ReturnsForbidden()
    {
        _currentUser.UserId = MemberId;

        var result = await CreateService().UpdateAsync(_event.Id, UpdateRequest(_event.Version));

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Update_WhenSomeoneElseSavedFirst_ReturnsConflict()
    {
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("changed"));
        var staleVersion = Guid.NewGuid();

        var result = await CreateService().UpdateAsync(_event.Id, UpdateRequest(staleVersion));

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _events.Verify(events => events.SetOriginalVersion(_event, staleVersion), Times.Once);
    }

    [Fact]
    public async Task Update_CancelledEvent_ReturnsValidation()
    {
        _event.Status = EventStatus.Cancelled;

        var result = await CreateService().UpdateAsync(_event.Id, UpdateRequest(_event.Version));

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Cancel_ByOrganizer_SetsCancelled_AndTwiceIsNotAnError()
    {
        var service = CreateService();

        Assert.True((await service.CancelAsync(_event.Id)).IsSuccess);
        Assert.Equal(EventStatus.Cancelled, _event.Status);
        Assert.True((await service.CancelAsync(_event.Id)).IsSuccess);

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_UnknownEvent_ReturnsNotFound()
    {
        var result = await CreateService().CancelAsync(12345);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
