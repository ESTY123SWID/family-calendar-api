using AutoMapper;
using my_project.Core.Common;
using my_project.Core.DTOs.Dashboard;
using my_project.Core.DTOs.Events;
using my_project.Core.DTOs.Tasks;
using my_project.Core.DTOs.Updates;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Interfaces.Services;
using my_project.Service.Common;

namespace my_project.Service.Services;

public class DashboardService : IDashboardService
{
    private const int LatestUpdatesCount = 5;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IFamilyAccessService _access;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IMapper _mapper;

    public DashboardService(
        IUnitOfWork unitOfWork,
        IFamilyAccessService access,
        ICurrentUser currentUser,
        IClock clock,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _access = access;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<ServiceResult<DashboardDto>> GetAsync(int familyId, CancellationToken cancellationToken = default)
    {
        var me = _currentUser.UserId;

        var access = await _access.EnsureMemberAsync(familyId, me, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<DashboardDto>.FromFailure(access);
        }

        var now = _clock.UtcNow;

        // Sequential on purpose: one DbContext does not support parallel queries
        var nextEvent = await _unitOfWork.Events.GetNextForUserAsync(familyId, me, now, cancellationToken);
        var pendingRsvps = await _unitOfWork.Events.GetPendingRsvpForUserAsync(familyId, me, now, cancellationToken);
        var myOpenTasks = await _unitOfWork.FamilyTasks.GetForUserAsync(familyId, me, excludeDone: true, cancellationToken: cancellationToken);
        var latestUpdates = await _unitOfWork.EventUpdates.GetLatestForFamilyAsync(familyId, LatestUpdatesCount, cancellationToken);

        var taskDtos = myOpenTasks
            .Select(task =>
            {
                var dto = _mapper.Map<TaskDto>(task);
                dto.IsOverdue = DomainRules.IsOverdue(task, now);
                return dto;
            })
            .ToList();

        return ServiceResult<DashboardDto>.Ok(new DashboardDto
        {
            NextEvent = nextEvent is null ? null : _mapper.Map<EventSummaryDto>(nextEvent),
            MyOpenTasks = taskDtos,
            PendingRsvps = _mapper.Map<List<EventSummaryDto>>(pendingRsvps),
            LatestUpdates = _mapper.Map<List<EventUpdateDto>>(latestUpdates)
        });
    }
}
