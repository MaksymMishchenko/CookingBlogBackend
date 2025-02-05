using PostApiService.Models;

namespace PostApiService.Interfaces
{
    public interface ICommentService
    {        
        Task AddCommentAsync(int postId, Comment comment);
        Task<bool> UpdateCommentAsync(int commentId, EditCommentModel comment);
        Task DeleteCommentAsync(int commentId);
    }
}
