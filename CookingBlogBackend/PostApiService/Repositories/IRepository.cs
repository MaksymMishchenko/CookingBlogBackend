using System.Linq.Expressions;

namespace PostApiService.Repositories
{
    public interface IRepository<T> where T : class, IEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        IQueryable<T> AsQueryable();
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<int> GetTotalCountAsync();
    }
}