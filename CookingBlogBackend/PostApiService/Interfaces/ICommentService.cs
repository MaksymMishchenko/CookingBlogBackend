using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface ICommentService
    {
        Task<Result<CommentDto>> AddCommentAsync(int postId, string content, CancellationToken ct = default);
        Task<Result<CommentDto>> UpdateCommentAsync(int commentId, string content, CancellationToken ct = default);
        Task<Result> DeleteCommentAsync(int commentId, CancellationToken ct = default);
    }
}
