using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace PostApiService.Repositories
{
    public class Repository<T> : IRepository<T> where T : class, IEntity
    {
        private readonly DbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(DbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<List<T>> GetAllAsync() => await _dbSet.ToListAsync();

        public async Task<T> AddAsync(T entity, CancellationToken token = default)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public IQueryable<T> AsQueryable() => _dbSet.AsQueryable().AsNoTracking();

        public async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate,
                                   CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsQueryable()
                .AsNoTracking()
                .AnyAsync(predicate, cancellationToken);
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .CountAsync();
        }
    }
}
