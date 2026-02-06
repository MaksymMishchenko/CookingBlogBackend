namespace PostApiService.Repositories
{
    public class PostRepository : Repository<Post>, IPostRepository
    {
        public PostRepository(ApplicationDbContext context) : base(context) { }

        public IQueryable<Post> GetFilteredPosts(string? search, bool? onlyActive, string? categorySlug)
        {
            var query = _dbSet.AsNoTracking().Include(p => p.Category).AsQueryable();

            if (onlyActive.HasValue)
                query = query.Where(p => p.IsActive == onlyActive.Value);

            if (!string.IsNullOrWhiteSpace(categorySlug))
                query = query.Where(p => p.Category.Slug == categorySlug);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(q) ||
                    p.Description.ToLower().Contains(q) ||
                    p.Content.ToLower().Contains(q));
            }

            return query.OrderByDescending(p => p.CreatedAt);
        }

        public async Task<bool> IsAvailableForCommentingAsync(int postId, CancellationToken ct)
        {
            return await _dbSet.AnyAsync(p => p.Id == postId && p.IsActive, ct);
        }
    }
}
