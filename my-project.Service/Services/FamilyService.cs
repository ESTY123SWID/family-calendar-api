using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.Logging;
using my_project.Core.Common;
using my_project.Core.DTOs.Families;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Exceptions;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Interfaces.Services;
using my_project.Core.Models;
using my_project.Service.Common;

namespace my_project.Service.Services;

public class FamilyService : IFamilyService
{
    private const int JoinCodePrefixMaxLength = 6;
    private const string JoinCodeFallbackPrefix = "FAM";
    private const int JoinCodeMaxAttempts = 10;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IFamilyAccessService _access;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly FamilyDeleter _familyDeleter;
    private readonly ILogger<FamilyService> _logger;

    public FamilyService(
        IUnitOfWork unitOfWork,
        IFamilyAccessService access,
        ICurrentUser currentUser,
        IClock clock,
        IMapper mapper,
        FamilyDeleter familyDeleter,
        ILogger<FamilyService> logger)
    {
        _unitOfWork = unitOfWork;
        _access = access;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _familyDeleter = familyDeleter;
        _logger = logger;
    }

    public async Task<ServiceResult<IReadOnlyList<FamilyDto>>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var families = await _unitOfWork.Families.GetListForUserAsync(_currentUser.UserId, cancellationToken);

        return ServiceResult<IReadOnlyList<FamilyDto>>.Ok(_mapper.Map<List<FamilyDto>>(families));
    }

    public async Task<ServiceResult<FamilyDetailsDto>> GetDetailsAsync(int familyId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Families.GetListItemForUserAsync(familyId, _currentUser.UserId, cancellationToken);

        if (item is null)
        {
            var access = await _access.EnsureMemberAsync(familyId, _currentUser.UserId, cancellationToken);
            return ServiceResult<FamilyDetailsDto>.FromFailure(access);
        }

        return ServiceResult<FamilyDetailsDto>.Ok(ToDetailsDto(item));
    }

    public async Task<ServiceResult<FamilyDetailsDto>> CreateAsync(
        CreateFamilyRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var name = request.Name.Trim();

        var family = new Family
        {
            Name = name,
            JoinCode = await GenerateUniqueJoinCodeAsync(name, cancellationToken),
            CreatedById = _currentUser.UserId,
            CreatedAt = now
        };

        // The creator becomes the family manager, in the same save
        family.Members.Add(new FamilyMember
        {
            UserId = _currentUser.UserId,
            Role = FamilyRole.Manager,
            JoinedAt = now
        });

        await _unitOfWork.Families.AddAsync(family, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Family {FamilyId} created by user {UserId}", family.Id, _currentUser.UserId);

        return ServiceResult<FamilyDetailsDto>.Ok(ToDetailsDto(new FamilyListItem(family, FamilyRole.Manager, 1)));
    }

    public async Task<ServiceResult> UpdateAsync(
        int familyId,
        UpdateFamilyRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await _access.EnsureFamilyManagerAsync(familyId, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access;
        }

        var family = await _unitOfWork.Families.GetByIdAsync(familyId, cancellationToken);
        if (family is null)
        {
            return ServiceResult.NotFound(ErrorMessages.FamilyNotFound);
        }

        family.Name = request.Name.Trim();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int familyId, CancellationToken cancellationToken = default)
    {
        var access = await _access.EnsureFamilyManagerAsync(familyId, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access;
        }

        var family = await _unitOfWork.Families.GetByIdAsync(familyId, cancellationToken);
        if (family is null)
        {
            return ServiceResult.NotFound(ErrorMessages.FamilyNotFound);
        }

        await _familyDeleter.DeleteAsync(family, cancellationToken);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<FamilyDto>> JoinAsync(JoinFamilyRequest request, CancellationToken cancellationToken = default)
    {
        var me = _currentUser.UserId;
        var family = await _unitOfWork.Families.GetByJoinCodeAsync(NormalizeJoinCode(request.JoinCode), cancellationToken);

        if (family is null)
        {
            return ServiceResult<FamilyDto>.NotFound(ErrorMessages.JoinCodeNotFound);
        }

        if (await _access.IsMemberAsync(family.Id, me, cancellationToken))
        {
            return ServiceResult<FamilyDto>.Conflict(ErrorMessages.AlreadyMember);
        }

        await _unitOfWork.FamilyMembers.AddAsync(
            new FamilyMember
            {
                FamilyId = family.Id,
                UserId = me,
                Role = FamilyRole.Member,
                JoinedAt = _clock.UtcNow
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} joined family {FamilyId}", me, family.Id);

        var item = await _unitOfWork.Families.GetListItemForUserAsync(family.Id, me, cancellationToken);
        return ServiceResult<FamilyDto>.Ok(_mapper.Map<FamilyDto>(item));
    }

    public async Task<ServiceResult<IReadOnlyList<FamilyMemberDto>>> GetMembersAsync(
        int familyId,
        CancellationToken cancellationToken = default)
    {
        var access = await _access.EnsureMemberAsync(familyId, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<IReadOnlyList<FamilyMemberDto>>.FromFailure(access);
        }

        var members = await _unitOfWork.FamilyMembers.GetByFamilyIdAsync(familyId, cancellationToken);

        return ServiceResult<IReadOnlyList<FamilyMemberDto>>.Ok(_mapper.Map<List<FamilyMemberDto>>(members));
    }

    public async Task<ServiceResult> ChangeMemberRoleAsync(
        int familyId,
        int userId,
        ChangeMemberRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await _access.EnsureFamilyManagerAsync(familyId, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access;
        }

        var member = await _unitOfWork.FamilyMembers.GetAsync(familyId, userId, cancellationToken);
        if (member is null)
        {
            return ServiceResult.NotFound(ErrorMessages.MemberNotFound);
        }

        var newRole = request.Role!.Value;
        if (member.Role == newRole)
        {
            return ServiceResult.Ok();
        }

        if (member.Role == FamilyRole.Manager && await IsLastManagerAsync(familyId, cancellationToken))
        {
            return ServiceResult.Conflict(ErrorMessages.LastManager);
        }

        member.Role = newRole;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} in family {FamilyId} is now {Role}", userId, familyId, newRole);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveMemberAsync(int familyId, int userId, CancellationToken cancellationToken = default)
    {
        if (userId == _currentUser.UserId)
        {
            return await LeaveAsync(familyId, cancellationToken);
        }

        var access = await _access.EnsureFamilyManagerAsync(familyId, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access;
        }

        var member = await _unitOfWork.FamilyMembers.GetAsync(familyId, userId, cancellationToken);
        if (member is null)
        {
            return ServiceResult.NotFound(ErrorMessages.MemberNotFound);
        }

        return await RemoveAsync(member, cancellationToken);
    }

    public async Task<ServiceResult> LeaveAsync(int familyId, CancellationToken cancellationToken = default)
    {
        var membership = await _access.GetMembershipAsync(familyId, _currentUser.UserId, cancellationToken);
        if (membership is null)
        {
            return await _access.EnsureMemberAsync(familyId, _currentUser.UserId, cancellationToken);
        }

        return await RemoveAsync(membership, cancellationToken);
    }

    public async Task<ServiceResult<JoinCodeDto>> RegenerateJoinCodeAsync(
        int familyId,
        CancellationToken cancellationToken = default)
    {
        var access = await _access.EnsureFamilyManagerAsync(familyId, _currentUser.UserId, cancellationToken);
        if (!access.IsSuccess)
        {
            return ServiceResult<JoinCodeDto>.FromFailure(access);
        }

        var family = await _unitOfWork.Families.GetByIdAsync(familyId, cancellationToken);
        if (family is null)
        {
            return ServiceResult<JoinCodeDto>.NotFound(ErrorMessages.FamilyNotFound);
        }

        family.JoinCode = await GenerateUniqueJoinCodeAsync(family.Name, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Join code of family {FamilyId} was regenerated", familyId);

        return ServiceResult<JoinCodeDto>.Ok(new JoinCodeDto { JoinCode = family.JoinCode });
    }

    private async Task<ServiceResult> RemoveAsync(FamilyMember member, CancellationToken cancellationToken)
    {
        if (member.Role == FamilyRole.Manager && await IsLastManagerAsync(member.FamilyId, cancellationToken))
        {
            return ServiceResult.Conflict(ErrorMessages.LastManager);
        }

        // Unfinished tasks of the leaving member go back to Open, so others can volunteer for them.
        // Done tasks keep their assignee as history
        var tasksToRelease = await _unitOfWork.FamilyTasks.GetUnfinishedAssignedInFamilyAsync(
            member.FamilyId, member.UserId, cancellationToken);

        foreach (var task in tasksToRelease)
        {
            task.AssigneeId = null;
            task.Status = FamilyTaskStatus.Open;
        }

        // Membership removal and task release are saved together (one transaction)
        _unitOfWork.FamilyMembers.Remove(member);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // One of the tasks was changed at the same moment – nothing was saved
            _logger.LogWarning(
                "Concurrency conflict while removing user {UserId} from family {FamilyId}",
                member.UserId, member.FamilyId);
            return ServiceResult.Conflict(ErrorMessages.ConcurrentChangeTryAgain);
        }

        _logger.LogInformation(
            "User {UserId} left / was removed from family {FamilyId}; {Count} tasks released",
            member.UserId, member.FamilyId, tasksToRelease.Count);

        return ServiceResult.Ok();
    }

    private async Task<bool> IsLastManagerAsync(int familyId, CancellationToken cancellationToken)
    {
        return await _unitOfWork.FamilyMembers.CountManagersAsync(familyId, cancellationToken) <= 1;
    }

    private FamilyDetailsDto ToDetailsDto(FamilyListItem item)
    {
        var dto = _mapper.Map<FamilyDetailsDto>(item);

        // The join code lets anyone in – only managers see it
        dto.JoinCode = item.MyRole == FamilyRole.Manager ? item.Family.JoinCode : null;

        return dto;
    }

    // e.g. "Levi" → "LEVI-4821". Non-Latin names (e.g. Hebrew) get the "FAM" prefix
    private async Task<string> GenerateUniqueJoinCodeAsync(string familyName, CancellationToken cancellationToken)
    {
        var prefix = BuildJoinCodePrefix(familyName);

        for (var attempt = 0; attempt < JoinCodeMaxAttempts; attempt++)
        {
            var code = $"{prefix}-{RandomNumberGenerator.GetInt32(1000, 10000)}";

            if (!await _unitOfWork.Families.JoinCodeExistsAsync(code, cancellationToken))
            {
                return code;
            }
        }

        throw new InvalidOperationException($"Could not generate a unique join code after {JoinCodeMaxAttempts} attempts.");
    }

    private static string BuildJoinCodePrefix(string familyName)
    {
        var prefix = new StringBuilder();

        foreach (var character in familyName.ToUpperInvariant())
        {
            if (character is >= 'A' and <= 'Z')
            {
                prefix.Append(character);

                if (prefix.Length == JoinCodePrefixMaxLength)
                {
                    break;
                }
            }
        }

        return prefix.Length > 0 ? prefix.ToString() : JoinCodeFallbackPrefix;
    }

    private static string NormalizeJoinCode(string joinCode)
    {
        return joinCode.Trim().ToUpperInvariant();
    }
}
