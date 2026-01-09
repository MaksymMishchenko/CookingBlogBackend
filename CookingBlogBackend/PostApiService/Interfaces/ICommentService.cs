using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface ICommentService
    {
        Task<Result<CommentDto>> AddCommentAsync(int postId, string content, CancellationToken ct = default);
        Task UpdateCommentAsync(int commentId, EditCommentModel comment, CancellationToken ct = default);
        Task DeleteCommentAsync(int commentId, CancellationToken ct = default);
    }
}
