namespace PostApiService.Repositories
{
    public interface IPostImageRepository : IRepository<PostImage>
    {
        Task<List<PostImage>> GetByPostIdAsync(int postId, CancellationToken ct = default);
        Task<List<PostImage>> GetUnattachedByUrlsAsync(IEnumerable<string> urls, CancellationToken ct = default);
        Task<List<PostImage>> GetOrphanedImagesAsync(DateTime olderThanUtc, CancellationToken ct = default);
        Task DeleteRangeAsync(IEnumerable<PostImage> images, CancellationToken ct = default);
    }
}
