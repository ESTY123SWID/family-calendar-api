using Moq;
using my_project.Core.Common;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Repositories;
using my_project.Service.Services;

namespace my_project.Tests.Services;

// The single place of the family permission rules – and of the isolation between families
public class FamilyAccessServiceTests
{
    private const int FamilyId = 1;
    private const int ManagerId = 1;
    private const int MemberId = 2;
    private const int OutsiderId = 4;

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IFamilyMemberRepository> _members = new();
    private readonly Mock<IFamilyRepository> _families = new();
    private readonly FamilyAccessService _service;

    public FamilyAccessServiceTests()
    {
        _unitOfWork.Setup(unitOfWork => unitOfWork.FamilyMembers).Returns(_members.Object);
        _unitOfWork.Setup(unitOfWork => unitOfWork.Families).Returns(_families.Object);

        _members.Setup(members => members.GetRoleAsync(FamilyId, ManagerId, It.IsAny<CancellationToken>())).ReturnsAsync(FamilyRole.Manager);
        _members.Setup(members => members.GetRoleAsync(FamilyId, MemberId, It.IsAny<CancellationToken>())).ReturnsAsync(FamilyRole.Member);
        _members.Setup(members => members.GetRoleAsync(FamilyId, OutsiderId, It.IsAny<CancellationToken>())).ReturnsAsync((FamilyRole?)null);
        _members.Setup(members => members.IsMemberAsync(FamilyId, It.IsIn(ManagerId, MemberId), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _families.Setup(families => families.GetByIdAsync(FamilyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Family { Id = FamilyId, Name = "לוי", JoinCode = "LEVI-1" });

        _service = new FamilyAccessService(_unitOfWork.Object);
    }

    [Fact]
    public async Task EnsureMember_Member_IsOk()
    {
        var result = await _service.EnsureMemberAsync(FamilyId, MemberId);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EnsureMember_UserOfAnotherFamily_IsForbidden()
    {
        var result = await _service.EnsureMemberAsync(FamilyId, OutsiderId);

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task EnsureMember_FamilyThatDoesNotExist_IsNotFound()
    {
        var result = await _service.EnsureMemberAsync(777, MemberId);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Theory]
    [InlineData(ManagerId, ErrorType.None)]
    [InlineData(MemberId, ErrorType.Forbidden)]
    [InlineData(OutsiderId, ErrorType.Forbidden)]
    public async Task EnsureFamilyManager_OnlyTheManagerPasses(int userId, ErrorType expected)
    {
        var result = await _service.EnsureFamilyManagerAsync(FamilyId, userId);

        Assert.Equal(expected, result.ErrorType);
    }

    [Theory]
    [InlineData(ManagerId, true)]   // family manager
    [InlineData(MemberId, true)]    // the organizer
    [InlineData(OutsiderId, false)] // an organizer who left the family no longer manages the event
    public async Task CanManageEvent_ManagerOrOrganizerWhoIsStillAMember(int userId, bool expected)
    {
        var evt = new Event { Id = 1, FamilyId = FamilyId, OrganizerId = userId == ManagerId ? MemberId : userId };

        Assert.Equal(expected, await _service.CanManageEventAsync(evt, userId));
    }

    [Fact]
    public async Task EnsureCanManageEvent_PlainMemberWhoIsNotTheOrganizer_IsForbidden()
    {
        var evt = new Event { Id = 1, FamilyId = FamilyId, OrganizerId = ManagerId };

        var result = await _service.EnsureCanManageEventAsync(evt, MemberId);

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }
}
