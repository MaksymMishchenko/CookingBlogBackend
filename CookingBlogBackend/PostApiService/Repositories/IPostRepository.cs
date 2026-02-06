namespace PostApiService.Repositories
{
    public interface IPostRepository : IRepository<Post>
    {
        IQueryable<Post> GetFilteredPosts(string? search, bool? onlyActive, string? categorySlug);
        Task<bool> IsAvailableForCommentingAsync(int postId, CancellationToken ct);
    }
}
