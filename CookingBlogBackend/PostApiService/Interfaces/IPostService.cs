using PostApiService.Models;

namespace PostApiService.Interfaces
{
    public interface IPostService
    {
        Task<(List<Post> Posts, int TotalCount)> GetPostsWithTotalAsync(
            int pageNumber,
            int pageSize,
            int commentPageNumber = 1,
            int commentsPerPage = 10,
            bool includeComments = true,
            CancellationToken cancellationToken = default);
        Task<Post> GetPostByIdAsync(int postId, bool includeComments = true);
        Task<Post> AddPostAsync(Post post);
        Task<Post> UpdatePostAsync(int postId, Post post);
        Task DeletePostAsync(int postId);        
    }
}
