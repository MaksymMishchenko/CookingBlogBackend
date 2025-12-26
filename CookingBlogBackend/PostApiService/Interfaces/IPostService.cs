using PostApiService.Models.Dto;

namespace PostApiService.Interfaces
{
    public interface IPostService
    {
        Task<(List<PostListDto> Posts, int TotalPostCount)> GetPostsWithTotalPostCountAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
        Task<(List<SearchPostListDto> SearchPostList, int SearchTotalPosts)> SearchPostsWithTotalCountAsync
            (string query, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
        Task<Post> GetPostByIdAsync(int postId, bool includeComments = true);
        Task<Post> AddPostAsync(Post post);
        Task<Post> UpdatePostAsync(int postId, Post post);
        Task DeletePostAsync(int postId);
    }
}
