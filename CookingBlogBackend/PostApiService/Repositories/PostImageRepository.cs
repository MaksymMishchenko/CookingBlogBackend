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
            var urlList = urls.ToList();
            if (urlList.Count == 0)
            {
                return Task.FromResult(new List<PostImage>());
            }

            return _dbSet
                .Where(img => img.PostId == null && urlList.Contains(img.Url))
                .ToListAsync(ct);
        }
    }
}
