using my_project.Core.Common;
using my_project.Core.DTOs.Families;

namespace my_project.Core.Interfaces.Services;

public interface IFamilyService
{
    Task<ServiceResult<IReadOnlyList<FamilyDto>>> GetMineAsync(CancellationToken cancellationToken = default);

    // JoinCode is filled for managers only
    Task<ServiceResult<FamilyDetailsDto>> GetDetailsAsync(int familyId, CancellationToken cancellationToken = default);

    // The creator becomes the family manager
    Task<ServiceResult<FamilyDetailsDto>> CreateAsync(CreateFamilyRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(int familyId, UpdateFamilyRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(int familyId, CancellationToken cancellationToken = default);

    // Wrong code → NotFound. Already a member → Conflict
    Task<ServiceResult<FamilyDto>> JoinAsync(JoinFamilyRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<FamilyMemberDto>>> GetMembersAsync(int familyId, CancellationToken cancellationToken = default);

    // Demoting the last manager → Conflict
    Task<ServiceResult> ChangeMemberRoleAsync(
        int familyId,
        int userId,
        ChangeMemberRoleRequest request,
        CancellationToken cancellationToken = default);

    // A manager removes a member, or a user removes themselves (same as Leave).
    // Removing the last manager → Conflict
    Task<ServiceResult> RemoveMemberAsync(int familyId, int userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> LeaveAsync(int familyId, CancellationToken cancellationToken = default);

    Task<ServiceResult<JoinCodeDto>> RegenerateJoinCodeAsync(int familyId, CancellationToken cancellationToken = default);
}
