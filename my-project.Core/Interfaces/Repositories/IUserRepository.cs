using my_project.Core.Common;
using my_project.Core.Entities;
using my_project.Core.Models;

namespace my_project.Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    // True if both users are members of at least one common family
    Task<bool> ShareFamilyAsync(int userId, int otherUserId, CancellationToken cancellationToken = default);

    // System admin only. SortBy: name (default), createdAt
    Task<PagedResult<AdminUserListItem>> GetPagedAsync(
        AdminQueryParameters query,
        CancellationToken cancellationToken = default);

    // isActive null = all users
    Task<int> CountAsync(bool? isActive = null, CancellationToken cancellationToken = default);
}
