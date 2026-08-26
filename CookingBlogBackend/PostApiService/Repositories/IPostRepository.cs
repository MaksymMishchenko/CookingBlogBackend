namespace PostApiService.Repositories
{
    public interface IPostRepository : IRepository<Post>
    {
        IQueryable<Post> GetPublicFilteredPosts(string? search, bool? onlyActive, string? categorySlug);
        Task<bool> IsPostActiveAsync(int postId, CancellationToken ct);
    }
}
