using PostApiService.Models;

namespace PostApiService.Interfaces
{
    public interface IPostService
    {
        Task<List<Post>> GetAllPostsAsync(int pageNumber,
            int pageSize,
            int commentPageNumber = 1,
            int commentsPerPage = 10,
            bool includeComments = true);
        Task<Post> GetPostByIdAsync(int postId);
        Task<(bool Success, int PostId)> AddPostAsync(Post post);
        Task<(bool Success, int PostId)> EditPostAsync(Post post);
        Task<bool> DeletePostAsync(int postId);
    }
}
