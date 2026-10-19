using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using my_project.Core.Common;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Interfaces.Repositories;
using my_project.Core.Models;
using my_project.Data.Extensions;

namespace my_project.Data.Repositories;

public class FamilyRepository : Repository<Family>, IFamilyRepository
{
    // Computed inside the SQL query. ManagerName = the manager who joined first
    private static readonly Expression<Func<Family, AdminFamilyListItem>> ToAdminListItem = family =>
        new AdminFamilyListItem(
            family,
            family.Members
                .Where(member => member.Role == FamilyRole.Manager)
                .OrderBy(member => member.JoinedAt)
                .Select(member => member.User.FullName)
                .FirstOrDefault(),
            family.Members.Count,
            family.Events.Count);

    public FamilyRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<Family?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public async Task<Family?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(family => family.JoinCode == joinCode, cancellationToken);
    }

    public async Task<bool> JoinCodeExistsAsync(string joinCode, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(family => family.JoinCode == joinCode, cancellationToken);
    }

    public async Task<IReadOnlyList<Family>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(family => family.Members.Any(member => member.UserId == userId))
            .OrderBy(family => family.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FamilyListItem>> GetListForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await Context.FamilyMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId)
            .OrderBy(member => member.Family.Name)
            .Select(member => new FamilyListItem(member.Family, member.Role, member.Family.Members.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<FamilyListItem?> GetListItemForUserAsync(
        int familyId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await Context.FamilyMembers
            .AsNoTracking()
            .Where(member => member.FamilyId == familyId && member.UserId == userId)
            .Select(member => new FamilyListItem(member.Family, member.Role, member.Family.Members.Count))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<AdminFamilyListItem>> GetAdminPagedAsync(
        AdminQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var families = DbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = LikePattern.Contains(query.Search.Trim());
            families = families.Where(family =>
                EF.Functions.ILike(family.Name, pattern) || EF.Functions.ILike(family.JoinCode, pattern));
        }

        var ordered = query.SortBy?.ToLowerInvariant() switch
        {
            "createdat" => families.OrderByDirection(family => family.CreatedAt, query.SortDirection),
            _ => families.OrderByDirection(family => family.Name, query.SortDirection)
        };

        return await ordered
            .ThenBy(family => family.Id)
            .Select(ToAdminListItem)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<AdminFamilyListItem?> GetAdminItemAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(family => family.Id == id)
            .Select(ToAdminListItem)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(cancellationToken);
    }
}
