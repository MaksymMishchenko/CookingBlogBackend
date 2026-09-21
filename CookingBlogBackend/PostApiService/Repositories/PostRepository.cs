namespace PostApiService.Repositories
{
    public class PostRepository : Repository<Post>, IPostRepository
    {
        public PostRepository(ApplicationDbContext context) : base(context) { }

        private IQueryable<Post> ApplyCommonFilters(string? search, bool? onlyActive)
        {
            var query = _dbSet.AsNoTracking().Include(p => p.Category).AsQueryable();

            if (onlyActive.HasValue)
                query = query.Where(p => p.IsActive == onlyActive.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(q) ||
                    p.Description.ToLower().Contains(q) ||
                    p.Content.ToLower().Contains(q));
            }

            return query;
        }

        public IQueryable<Post> GetPublicFilteredPosts(string? search, bool? onlyActive, string? categorySlug)
        {
            var query = ApplyCommonFilters(search, onlyActive);

            if (!string.IsNullOrWhiteSpace(categorySlug))
                query = query.Where(p => p.Category.Slug == categorySlug);

            return query.OrderByDescending(p => p.CreatedAt);
        }
        public IQueryable<Post> GetAdminFilteredAndSortedPosts
            (string? search, bool? onlyActive, int? categoryId, string? sortBy, string? sortDirection)
        {
            var query = ApplyCommonFilters(search, onlyActive);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);           

            query = (sortBy, sortDirection?.ToLower()) switch
            {
                ("title", "asc") => query.OrderBy(p => p.Title),
                ("title", "desc") => query.OrderByDescending(p => p.Title),
                ("createdAt", "asc") => query.OrderBy(p => p.CreatedAt),
                ("createdAt", "desc") => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            return query;
        }

        public async Task<bool> IsPostActiveAsync(int postId, CancellationToken ct)
        {
            return await _dbSet.AnyAsync(p => p.Id == postId && p.IsActive, ct);
        }

        public async Task<Post?> GetByIdWithAuthorsAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(p => p.Author)                
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }
    }
}