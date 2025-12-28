using PostApiService.Models.Dto;

namespace PostApiService.Interfaces
{
    public interface IPostService
    {
        Task<(List<PostListDto> Posts, int TotalPostCount)> GetPostsWithTotalPostCountAsync(
            int pageNumber,
            int pageSize,
            CancellationToken ct = default);
        Task<(List<SearchPostListDto> SearchPostList, int SearchTotalPosts)> SearchPostsWithTotalCountAsync
            (string query, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);
        Task<Post> GetPostByIdAsync(int postId, bool includeComments = true, CancellationToken ct = default);
        Task<Post> AddPostAsync(Post post, CancellationToken ct = default);
        Task<Post> UpdatePostAsync(int postId, Post post, CancellationToken ct = default);
        Task DeletePostAsync(int postId, CancellationToken ct = default);
    }
}
