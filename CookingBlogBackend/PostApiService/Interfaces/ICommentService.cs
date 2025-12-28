namespace PostApiService.Interfaces
{
    public interface ICommentService
    {
        Task AddCommentAsync(int postId, Comment comment, CancellationToken ct = default);
        Task UpdateCommentAsync(int commentId, EditCommentModel comment, CancellationToken ct = default);
        Task DeleteCommentAsync(int commentId, CancellationToken ct = default);
    }
}
