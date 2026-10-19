using my_project.Core.Common;
using my_project.Core.Entities;
using my_project.Core.Models;

namespace my_project.Core.Interfaces.Repositories;

public interface IFamilyRepository : IRepository<Family>
{
    Task<Family?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Family?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);

    Task<bool> JoinCodeExistsAsync(string joinCode, CancellationToken cancellationToken = default);

    // Families the user is a member of
    Task<IReadOnlyList<Family>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    // Families the user is a member of, with their role and the members count, ordered by name
    Task<IReadOnlyList<FamilyListItem>> GetListForUserAsync(int userId, CancellationToken cancellationToken = default);

    // Null if the family does not exist or the user is not a member of it
    Task<FamilyListItem?> GetListItemForUserAsync(int familyId, int userId, CancellationToken cancellationToken = default);

    // System admin only. SortBy: name (default), createdAt
    Task<PagedResult<AdminFamilyListItem>> GetAdminPagedAsync(
        AdminQueryParameters query,
        CancellationToken cancellationToken = default);

    // System admin only
    Task<AdminFamilyListItem?> GetAdminItemAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
