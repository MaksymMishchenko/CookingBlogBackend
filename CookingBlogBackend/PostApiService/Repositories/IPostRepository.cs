namespace PostApiService.Repositories
{
    public interface IPostRepository : IRepository<Post>
    {
        IQueryable<Post> GetFilteredPosts(string? search, bool? onlyActive, string? categorySlug);
        Task<bool> IsPostActiveAsync(int postId, CancellationToken ct);
    }
}
