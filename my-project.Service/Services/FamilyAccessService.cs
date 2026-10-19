using my_project.Core.Common;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Services;
using my_project.Service.Common;

namespace my_project.Service.Services;

public class FamilyAccessService : IFamilyAccessService
{
    private readonly IUnitOfWork _unitOfWork;

    public FamilyAccessService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FamilyMember?> GetMembershipAsync(int familyId, int userId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.FamilyMembers.GetAsync(familyId, userId, cancellationToken);
    }

    public async Task<bool> IsMemberAsync(int familyId, int userId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.FamilyMembers.IsMemberAsync(familyId, userId, cancellationToken);
    }

    public async Task<bool> IsFamilyManagerAsync(int familyId, int userId, CancellationToken cancellationToken = default)
    {
        var role = await _unitOfWork.FamilyMembers.GetRoleAsync(familyId, userId, cancellationToken);
        return role == FamilyRole.Manager;
    }

    public async Task<bool> CanManageEventAsync(Event evt, int userId, CancellationToken cancellationToken = default)
    {
        var role = await _unitOfWork.FamilyMembers.GetRoleAsync(evt.FamilyId, userId, cancellationToken);

        // An organizer who left the family no longer manages the event
        return role == FamilyRole.Manager
            || (role.HasValue && evt.OrganizerId == userId);
    }

    public async Task<ServiceResult> EnsureMemberAsync(int familyId, int userId, CancellationToken cancellationToken = default)
    {
        if (await IsMemberAsync(familyId, userId, cancellationToken))
        {
            return ServiceResult.Ok();
        }

        return await FamilyNotFoundOrForbiddenAsync(familyId, ErrorMessages.NotFamilyMember, cancellationToken);
    }

    public async Task<ServiceResult> EnsureFamilyManagerAsync(int familyId, int userId, CancellationToken cancellationToken = default)
    {
        var role = await _unitOfWork.FamilyMembers.GetRoleAsync(familyId, userId, cancellationToken);

        return role switch
        {
            FamilyRole.Manager => ServiceResult.Ok(),
            FamilyRole.Member => ServiceResult.Forbidden(ErrorMessages.FamilyManagerOnly),
            _ => await FamilyNotFoundOrForbiddenAsync(familyId, ErrorMessages.NotFamilyMember, cancellationToken)
        };
    }

    public async Task<ServiceResult> EnsureCanManageEventAsync(Event evt, int userId, CancellationToken cancellationToken = default)
    {
        var role = await _unitOfWork.FamilyMembers.GetRoleAsync(evt.FamilyId, userId, cancellationToken);

        if (role is null)
        {
            return ServiceResult.Forbidden(ErrorMessages.NotFamilyMember);
        }

        return role == FamilyRole.Manager || evt.OrganizerId == userId
            ? ServiceResult.Ok()
            : ServiceResult.Forbidden(ErrorMessages.EventManagerOnly);
    }

    // A non-member gets 404 only when the family really does not exist
    private async Task<ServiceResult> FamilyNotFoundOrForbiddenAsync(
        int familyId,
        string forbiddenMessage,
        CancellationToken cancellationToken)
    {
        var family = await _unitOfWork.Families.GetByIdAsync(familyId, cancellationToken);

        return family is null
            ? ServiceResult.NotFound(ErrorMessages.FamilyNotFound)
            : ServiceResult.Forbidden(forbiddenMessage);
    }
}
