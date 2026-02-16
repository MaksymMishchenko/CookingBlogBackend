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
    }
}
