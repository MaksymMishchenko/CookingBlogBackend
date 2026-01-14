namespace PostApiService.Repositories
{
    public interface IPostRepository : IRepository<Post>
    {
        Task<bool> IsAvailableForCommentingAsync(int postId, CancellationToken ct);
    }
}
