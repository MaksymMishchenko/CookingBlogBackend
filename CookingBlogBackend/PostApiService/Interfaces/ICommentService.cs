using PostApiService.Models;

namespace PostApiService.Interfaces
{
    public interface ICommentService
    {        
        Task AddCommentAsync(int postId, Comment comment);
        Task UpdateCommentAsync(int commentId, EditCommentModel comment);
        Task DeleteCommentAsync(int commentId);
    }
}
