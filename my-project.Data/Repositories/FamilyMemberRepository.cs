using Microsoft.EntityFrameworkCore;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Interfaces.Repositories;

namespace my_project.Data.Repositories;

public class FamilyMemberRepository : Repository<FamilyMember>, IFamilyMemberRepository
{
    public FamilyMemberRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<FamilyMember?> GetAsync(int familyId, int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([familyId, userId], cancellationToken);
    }

    public async Task<IReadOnlyList<FamilyMember>> GetByFamilyIdAsync(int familyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(member => member.User)
            .Where(member => member.FamilyId == familyId)
            .OrderBy(member => member.User.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsMemberAsync(int familyId, int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            member => member.FamilyId == familyId && member.UserId == userId,
            cancellationToken);
    }

    public async Task<FamilyRole?> GetRoleAsync(int familyId, int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(member => member.FamilyId == familyId && member.UserId == userId)
            .Select(member => (FamilyRole?)member.Role)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountManagersAsync(int familyId, CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(
            member => member.FamilyId == familyId && member.Role == FamilyRole.Manager,
            cancellationToken);
    }
}
