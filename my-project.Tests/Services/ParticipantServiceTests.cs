using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using my_project.Core.Common;
using my_project.Core.DTOs.Participants;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Repositories;
using my_project.Core.Interfaces.Services;
using my_project.Service.Services;
using my_project.Tests.Helpers;

namespace my_project.Tests.Services;

public class ParticipantServiceTests
{
    private const int FamilyId = 1;
    private const int MemberId = 2;
    private const int EventId = 7;

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<IEventParticipantRepository> _participants = new();
    private readonly Mock<IFamilyAccessService> _access = new();
    private readonly TestClock _clock = new();
    private readonly Event _event = new() { Id = EventId, FamilyId = FamilyId, OrganizerId = 1, Status = EventStatus.Scheduled };
    private readonly EventParticipant _participant = new() { EventId = EventId, UserId = MemberId, RsvpStatus = RsvpStatus.Pending };

    public ParticipantServiceTests()
    {
        _unitOfWork.Setup(unitOfWork => unitOfWork.Events).Returns(_events.Object);
        _unitOfWork.Setup(unitOfWork => unitOfWork.EventParticipants).Returns(_participants.Object);
        _events.Setup(events => events.GetByIdAsync(EventId, It.IsAny<CancellationToken>())).ReturnsAsync(_event);
        _participants.Setup(participants => participants.GetAsync(EventId, MemberId, It.IsAny<CancellationToken>())).ReturnsAsync(_participant);
        _access.Setup(access => access.EnsureMemberAsync(FamilyId, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Ok());
    }

    private ParticipantService CreateService(int currentUserId = MemberId)
    {
        return new ParticipantService(
            _unitOfWork.Object,
            _access.Object,
            new TestCurrentUser(currentUserId),
            _clock,
            TestMapper.Create(),
            Mock.Of<INotificationService>(),
            NullLogger<ParticipantService>.Instance);
    }

    [Fact]
    public async Task Rsvp_Coming_SavesAnswerGuestsAndTime()
    {
        var result = await CreateService().RsvpAsync(EventId, new RsvpRequest { Status = RsvpStatus.Coming, GuestsCount = 3 });

        Assert.True(result.IsSuccess);
        Assert.Equal(RsvpStatus.Coming, _participant.RsvpStatus);
        Assert.Equal(3, _participant.GuestsCount);
        Assert.Equal(_clock.UtcNow, _participant.RespondedAt);
    }

    [Fact]
    public async Task Rsvp_NotComing_ResetsTheGuests()
    {
        _participant.GuestsCount = 4;

        await CreateService().RsvpAsync(EventId, new RsvpRequest { Status = RsvpStatus.NotComing, GuestsCount = 4 });

        Assert.Equal(0, _participant.GuestsCount);
    }

    [Fact]
    public async Task Rsvp_Pending_IsNotAnAnswer()
    {
        var result = await CreateService().RsvpAsync(EventId, new RsvpRequest { Status = RsvpStatus.Pending });

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Rsvp_MemberWhoWasNotInvited_ReturnsNotFound()
    {
        var result = await CreateService(currentUserId: 3).RsvpAsync(EventId, new RsvpRequest { Status = RsvpStatus.Coming });

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Rsvp_CancelledEvent_ReturnsValidation()
    {
        _event.Status = EventStatus.Cancelled;

        var result = await CreateService().RsvpAsync(EventId, new RsvpRequest { Status = RsvpStatus.Coming });

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Rsvp_UserOfAnotherFamily_ReturnsForbidden()
    {
        _access.Setup(access => access.EnsureMemberAsync(FamilyId, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Forbidden("not a member"));

        var result = await CreateService(currentUserId: 9).RsvpAsync(EventId, new RsvpRequest { Status = RsvpStatus.Coming });

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }
}
