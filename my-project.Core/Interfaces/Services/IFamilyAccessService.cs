using my_project.Core.Common;
using my_project.Core.Entities;

namespace my_project.Core.Interfaces.Services;

// The single place for family-level permission checks. Used by all other services
public interface IFamilyAccessService
{
    // Null if the user is not a member of the family
    Task<FamilyMember?> GetMembershipAsync(int familyId, int userId, CancellationToken cancellationToken = default);

    Task<bool> IsMemberAsync(int familyId, int userId, CancellationToken cancellationToken = default);

    Task<bool> IsFamilyManagerAsync(int familyId, int userId, CancellationToken cancellationToken = default);

    // The event organizer (while still a family member) or a family manager
    Task<bool> CanManageEventAsync(Event evt, int userId, CancellationToken cancellationToken = default);

    // Ok, or NotFound (no such family) / Forbidden (not a member)
    Task<ServiceResult> EnsureMemberAsync(int familyId, int userId, CancellationToken cancellationToken = default);

    // Ok, or NotFound (no such family) / Forbidden (not a member or not a manager)
    Task<ServiceResult> EnsureFamilyManagerAsync(int familyId, int userId, CancellationToken cancellationToken = default);

    // Ok, or Forbidden (not a member, or neither the organizer nor a family manager)
    Task<ServiceResult> EnsureCanManageEventAsync(Event evt, int userId, CancellationToken cancellationToken = default);
}
