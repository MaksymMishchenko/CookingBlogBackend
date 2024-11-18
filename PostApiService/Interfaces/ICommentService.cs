using PostApiService.Models;

namespace PostApiService.Interfaces
{
    public interface ICommentService
    {        
        Task<bool> AddCommentAsync(int postId, Comment comment);
        Task<bool> EditCommentAsync(Comment comment);
        Task<bool> DeleteCommentAsync(int commentId);
    }
}
