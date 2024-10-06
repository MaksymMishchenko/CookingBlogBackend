using PostApiService.Models;

namespace PostApiService.Interfaces
{
    public interface ICommentService
    {        
        Task AddCommentAsync(int postId, Comment comment);
        Task EditCommentAsync(Comment comment);
        Task DeleteCommentAsync(int commentId);
    }
}
