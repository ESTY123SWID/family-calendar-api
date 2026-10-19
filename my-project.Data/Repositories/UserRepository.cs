using Microsoft.EntityFrameworkCore;
using my_project.Core.Common;
using my_project.Core.Entities;
using my_project.Core.Interfaces.Repositories;
using my_project.Core.Models;
using my_project.Data.Extensions;

namespace my_project.Data.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<bool> ShareFamilyAsync(int userId, int otherUserId, CancellationToken cancellationToken = default)
    {
        return await Context.FamilyMembers.AnyAsync(
            member => member.UserId == userId
                && Context.FamilyMembers.Any(other =>
                    other.FamilyId == member.FamilyId && other.UserId == otherUserId),
            cancellationToken);
    }

    public async Task<PagedResult<AdminUserListItem>> GetPagedAsync(
        AdminQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var users = DbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = LikePattern.Contains(query.Search.Trim());
            users = users.Where(user =>
                EF.Functions.ILike(user.FullName, pattern) || EF.Functions.ILike(user.Email, pattern));
        }

        var ordered = query.SortBy?.ToLowerInvariant() switch
        {
            "createdat" => users.OrderByDirection(user => user.CreatedAt, query.SortDirection),
            _ => users.OrderByDirection(user => user.FullName, query.SortDirection)
        };

        return await ordered
            .ThenBy(user => user.Id)
            .Select(user => new AdminUserListItem(user, user.FamilyMemberships.Count))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<int> CountAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        return isActive.HasValue
            ? await DbSet.CountAsync(user => user.IsActive == isActive.Value, cancellationToken)
            : await DbSet.CountAsync(cancellationToken);
    }
}
