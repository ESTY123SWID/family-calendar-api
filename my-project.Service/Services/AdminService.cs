using AutoMapper;
using Microsoft.Extensions.Logging;
using my_project.Core.Common;
using my_project.Core.DTOs.Admin;
using my_project.Core.DTOs.Events;
using my_project.Core.DTOs.Families;
using my_project.Core.Enums;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Interfaces.Services;
using my_project.Service.Common;

namespace my_project.Service.Services;

// The controller is protected by [Authorize(Roles = "SystemAdmin")];
// every method checks again here (defense in depth)
public class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly FamilyDeleter _familyDeleter;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        IMapper mapper,
        FamilyDeleter familyDeleter,
        ILogger<AdminService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _familyDeleter = familyDeleter;
        _logger = logger;
    }

    public async Task<ServiceResult<AdminStatsDto>> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsSystemAdmin)
        {
            return ServiceResult<AdminStatsDto>.Forbidden(ErrorMessages.AdminOnly);
        }

        // Sequential on purpose: one DbContext does not support parallel queries
        var stats = new AdminStatsDto
        {
            TotalUsers = await _unitOfWork.Users.CountAsync(cancellationToken: cancellationToken),
            BlockedUsers = await _unitOfWork.Users.CountAsync(isActive: false, cancellationToken),
            TotalFamilies = await _unitOfWork.Families.CountAsync(cancellationToken),
            TotalEvents = await _unitOfWork.Events.CountAsync(cancellationToken: cancellationToken),
            UpcomingEvents = await _unitOfWork.Events.CountAsync(_clock.UtcNow, cancellationToken),
            TotalTasks = await _unitOfWork.FamilyTasks.CountAsync(cancellationToken: cancellationToken),
            OpenTasks = await _unitOfWork.FamilyTasks.CountAsync(openOnly: true, cancellationToken)
        };

        return ServiceResult<AdminStatsDto>.Ok(stats);
    }

    public async Task<ServiceResult<PagedResult<AdminUserDto>>> GetUsersAsync(
        AdminQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsSystemAdmin)
        {
            return ServiceResult<PagedResult<AdminUserDto>>.Forbidden(ErrorMessages.AdminOnly);
        }

        var page = await _unitOfWork.Users.GetPagedAsync(query, cancellationToken);

        return ServiceResult<PagedResult<AdminUserDto>>.Ok(new PagedResult<AdminUserDto>(
            _mapper.Map<List<AdminUserDto>>(page.Items),
            page.Page,
            page.PageSize,
            page.TotalCount));
    }

    public async Task<ServiceResult> UpdateUserAsync(
        int id,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsSystemAdmin)
        {
            return ServiceResult.Forbidden(ErrorMessages.AdminOnly);
        }

        // The admin must not lock themselves out of the system
        var changesOwnAccess = id == _currentUser.UserId
            && (request.IsActive == false || request.Role == SystemRole.User);

        if (changesOwnAccess)
        {
            return ServiceResult.Conflict(ErrorMessages.CannotChangeOwnAdminAccount);
        }

        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return ServiceResult.NotFound(ErrorMessages.UserNotFound);
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        if (request.Role.HasValue)
        {
            user.Role = request.Role.Value;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {AdminId} updated user {UserId}: IsActive={IsActive}, Role={Role}",
            _currentUser.UserId, id, user.IsActive, user.Role);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<PagedResult<AdminFamilyDto>>> GetFamiliesAsync(
        AdminQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsSystemAdmin)
        {
            return ServiceResult<PagedResult<AdminFamilyDto>>.Forbidden(ErrorMessages.AdminOnly);
        }

        var page = await _unitOfWork.Families.GetAdminPagedAsync(query, cancellationToken);

        return ServiceResult<PagedResult<AdminFamilyDto>>.Ok(new PagedResult<AdminFamilyDto>(
            _mapper.Map<List<AdminFamilyDto>>(page.Items),
            page.Page,
            page.PageSize,
            page.TotalCount));
    }

    public async Task<ServiceResult<AdminFamilyDetailsDto>> GetFamilyAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsSystemAdmin)
        {
            return ServiceResult<AdminFamilyDetailsDto>.Forbidden(ErrorMessages.AdminOnly);
        }

        var item = await _unitOfWork.Families.GetAdminItemAsync(id, cancellationToken);
        if (item is null)
        {
            return ServiceResult<AdminFamilyDetailsDto>.NotFound(ErrorMessages.FamilyNotFound);
        }

        var members = await _unitOfWork.FamilyMembers.GetByFamilyIdAsync(id, cancellationToken);

        var dto = _mapper.Map<AdminFamilyDetailsDto>(item);
        dto.Members = _mapper.Map<List<FamilyMemberDto>>(members);

        return ServiceResult<AdminFamilyDetailsDto>.Ok(dto);
    }

    public async Task<ServiceResult<PagedResult<EventSummaryDto>>> GetFamilyEventsAsync(
        int id,
        EventQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsSystemAdmin)
        {
            return ServiceResult<PagedResult<EventSummaryDto>>.Forbidden(ErrorMessages.AdminOnly);
        }

        var family = await _unitOfWork.Families.GetByIdAsync(id, cancellationToken);
        if (family is null)
        {
            return ServiceResult<PagedResult<EventSummaryDto>>.NotFound(ErrorMessages.FamilyNotFound);
        }

        query.From = query.From.ToUtc();
        query.To = query.To.ToUtc();

        var page = await _unitOfWork.Events.GetPagedForFamilyAsync(id, query, cancellationToken);

        return ServiceResult<PagedResult<EventSummaryDto>>.Ok(new PagedResult<EventSummaryDto>(
            _mapper.Map<List<EventSummaryDto>>(page.Items),
            page.Page,
            page.PageSize,
            page.TotalCount));
    }

    public async Task<ServiceResult> DeleteFamilyAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsSystemAdmin)
        {
            return ServiceResult.Forbidden(ErrorMessages.AdminOnly);
        }

        var family = await _unitOfWork.Families.GetByIdAsync(id, cancellationToken);
        if (family is null)
        {
            return ServiceResult.NotFound(ErrorMessages.FamilyNotFound);
        }

        await _familyDeleter.DeleteAsync(family, cancellationToken);

        _logger.LogInformation("Admin {AdminId} deleted family {FamilyId}", _currentUser.UserId, id);

        return ServiceResult.Ok();
    }
}
