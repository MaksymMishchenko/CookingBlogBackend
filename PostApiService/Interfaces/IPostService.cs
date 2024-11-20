using PostApiService.Models;

namespace PostApiService.Interfaces
{
    public interface IPostService
    {
        Task<List<Post>> GetAllPostsAsync();
        Task<Post> GetPostByIdAsync(int postId);
        Task<(bool Success, int PostId)> AddPostAsync(Post post);
        Task<(bool Success, int PostId)> EditPostAsync(Post post);
        Task<bool> DeletePostAsync(int postId);
    }
}
