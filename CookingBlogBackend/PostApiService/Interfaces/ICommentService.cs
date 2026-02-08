using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface ICommentService
    {
        Task<Result<PagedResult<CommentDto>>> GetCommentsByPostIdAsync(
            int postId,
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken ct = default);
        Task<Result<CommentCreatedDto>> AddCommentAsync(int postId, string content, CancellationToken ct = default);
        Task<Result<CommentUpdatedDto>> UpdateCommentAsync(int commentId, string content, CancellationToken ct = default);
        Task<Result> DeleteCommentAsync(int commentId, CancellationToken ct = default);
    }
}
