using Microsoft.EntityFrameworkCore;

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

        public async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public IQueryable<T> AsQueryable() => _dbSet.AsQueryable();

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
    }
}
