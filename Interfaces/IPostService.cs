using PostApiService.Models;

namespace PostApiService.Interfaces
{
    public interface IPostService
    {
        Task<List<Post>> GetAllPosts();
        Task AddPostAsync(Post post);
        Task EditPostAsync(Post post);
        Task DeletePostAsync(int postId);
    }
}
