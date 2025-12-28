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

        public async Task<List<T>> GetAllAsync
            (CancellationToken ct = default) => await _dbSet.AsNoTracking().ToListAsync(ct);
        public async Task<int> GetTotalCountAsync(CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .CountAsync(ct);
        }       

        public async Task<T?> GetByIdAsync
            (int id, CancellationToken ct = default) => await _dbSet.FindAsync(id, ct);

        public async Task<bool> AnyAsync
            (Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        {
            return await _dbSet
                .AsQueryable()
                .AsNoTracking()
                .AnyAsync(predicate, ct);
        }

        public Task AddAsync(T entity, CancellationToken ct = default)
        {
            _context.Entry(entity).State = EntityState.Added;

            return Task.CompletedTask;
        }

        public Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            _context.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(T entity, CancellationToken ct = default)
        {
            _context.Entry(entity).State = EntityState.Deleted;

            return Task.CompletedTask;
        }

        public IQueryable<T> AsQueryable() => _dbSet.AsQueryable().AsNoTracking();

        public IQueryable<T> GetFilteredQueryable(Expression<Func<T, bool>> predicate)
        {
            return _dbSet
                .AsQueryable()
                .AsNoTracking()
                .Where(predicate);
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _context.SaveChangesAsync(ct);
        }
    }
}
