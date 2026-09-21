namespace PostApiService.Repositories
{
    public interface IPostRepository : IRepository<Post>
    {
        IQueryable<Post> GetPublicFilteredPosts(string? search, bool? onlyActive, string? categorySlug);

        IQueryable<Post> GetAdminFilteredAndSortedPosts(string? search, bool? onlyActive, int? categoryId, string? sortBy, string? sortDirection);

        Task<bool> IsPostActiveAsync(int postId, CancellationToken ct);

        Task<Post?> GetByIdWithAuthorsAsync(int id, CancellationToken ct = default);
    }
}
