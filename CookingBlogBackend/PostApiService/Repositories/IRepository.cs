using System.Linq.Expressions;

namespace PostApiService.Repositories
{
    public interface IRepository<T> where T : class, IEntity
    {
        Task<List<T>> GetAllAsync(CancellationToken ct = default);
        Task<int> GetTotalCountAsync(CancellationToken ct = default);
        Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(T entity, CancellationToken ct = default);
        Task DeleteAsync(T entity, CancellationToken ct = default);               
       
        IQueryable<T> AsQueryable();
        IQueryable<T> GetFilteredQueryable(Expression<Func<T, bool>> predicate);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}