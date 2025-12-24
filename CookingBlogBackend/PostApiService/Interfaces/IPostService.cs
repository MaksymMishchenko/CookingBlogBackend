using PostApiService.Models;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Responses;

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
        Task<PostAdminDetailsDto> GetPostByIdAsync(int postId);
        Task<PostAdminDetailsDto> AddPostAsync(PostCreateDto postDto);
        Task<PostAdminDetailsDto> UpdatePostAsync(int postId, Post post);
        Task DeletePostAsync(int postId);
    }
}
