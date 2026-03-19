using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface ICommentService
    {
        Task<Result<CommentScrollResponse<CommentDto>>> GetCommentsByPostIdAsync(
            int postId,
            int? lastId = null,
            int pageSize = 5,
            CancellationToken ct = default);
        Task<Result<CommentCreatedDto>> AddCommentAsync(int postId, string content, int? parentId, CancellationToken ct = default);
        Task<Result<CommentUpdatedDto>> UpdateCommentAsync(int commentId, string content, CancellationToken ct = default);
        Task<Result> DeleteCommentAsync(int commentId, CancellationToken ct = default);
    }
}
