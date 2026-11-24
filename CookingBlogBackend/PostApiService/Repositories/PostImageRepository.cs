namespace PostApiService.Repositories
{
    public class PostImageRepository : Repository<PostImage>, IPostImageRepository
    {
        public PostImageRepository(ApplicationDbContext context) : base(context) { }

        public Task<List<PostImage>> GetByPostIdAsync(int postId, CancellationToken ct = default)
        {
            return _dbSet
                .Where(img => img.PostId == postId)
                .ToListAsync(ct);
        }

        public Task<List<PostImage>> GetUnattachedByUrlsAsync(IEnumerable<string> urls, CancellationToken ct = default)
        {
            var urlList = urls
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (urlList.Count == 0)
            {
                return Task.FromResult(new List<PostImage>());
            }

            return _dbSet
                .Where(img => img.PostId == null && urlList.Contains(img.Url))
                .ToListAsync(ct);
        }

        public Task<List<PostImage>> GetOrphanedImagesAsync(DateTime olderThanUtc, CancellationToken ct = default)
        {
            return _dbSet
                .Where(img => img.PostId == null && img.CreatedAt <= olderThanUtc)
                .OrderBy(img => img.CreatedAt)
                .ToListAsync(ct);
        }

        public Task DeleteRangeAsync(IEnumerable<PostImage> images, CancellationToken ct = default)
        {
            _dbSet.RemoveRange(images);
            return Task.CompletedTask;
        }
    }
}
