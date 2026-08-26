namespace PostApiService.Repositories
{
    public interface IPostRepository : IRepository<Post>
    {
        IQueryable<Post> GetPublicFilteredPosts(string? search, bool? onlyActive, string? categorySlug);

        IQueryable<Post> GetAdminFilteredPosts(string? search, bool? onlyActive, int? categoryId);

        Task<bool> IsPostActiveAsync(int postId, CancellationToken ct);
    }
}
