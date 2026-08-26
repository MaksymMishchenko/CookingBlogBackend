namespace PostApiService.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context) { }

        public async Task<string?> GetNameBySlugAsync(string? slug, CancellationToken ct)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(c => c.Slug == slug)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<string?> GetNameByIdAsync(int? id, CancellationToken ct)
        {
            if (!id.HasValue)
                return null;

            return await _dbSet
                .AsNoTracking()
                .Where(c => c.Id == id.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);
        }
    }
}
