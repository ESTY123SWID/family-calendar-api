namespace my_project.Core.Interfaces.Repositories;

// Write operations shared by all repositories.
// Changes are persisted only by IUnitOfWork.SaveChangesAsync
public interface IRepository<TEntity> where TEntity : class
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
