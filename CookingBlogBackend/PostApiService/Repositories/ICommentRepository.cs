namespace PostApiService.Repositories
{
    public interface ICommentRepository : IRepository<Comment>
    {        
        Task<bool> HasRepliesAsync(int commentId, CancellationToken ct = default);

        Task<Comment?> GetWithUserAsync(int id, CancellationToken ct = default);

        IQueryable<Comment> GetQueryByPostId(int postId, CancellationToken ct = default);
    }
}
