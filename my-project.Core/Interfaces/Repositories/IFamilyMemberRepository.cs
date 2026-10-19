using my_project.Core.Entities;
using my_project.Core.Enums;

namespace my_project.Core.Interfaces.Repositories;

public interface IFamilyMemberRepository : IRepository<FamilyMember>
{
    // Tracked, ready for update / delete
    Task<FamilyMember?> GetAsync(int familyId, int userId, CancellationToken cancellationToken = default);

    // Includes the User of each member
    Task<IReadOnlyList<FamilyMember>> GetByFamilyIdAsync(int familyId, CancellationToken cancellationToken = default);

    Task<bool> IsMemberAsync(int familyId, int userId, CancellationToken cancellationToken = default);

    // Null if the user is not a member of the family
    Task<FamilyRole?> GetRoleAsync(int familyId, int userId, CancellationToken cancellationToken = default);

    // Used to prevent removing or demoting the last manager (409)
    Task<int> CountManagersAsync(int familyId, CancellationToken cancellationToken = default);
}
