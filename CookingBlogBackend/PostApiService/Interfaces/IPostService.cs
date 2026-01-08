using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface IPostService
    {
        Task<Result<PagedResult<PostListDto>>> GetPostsWithTotalPostCountAsync(
            int pageNumber,
            int pageSize,
            CancellationToken ct = default);
        Task<Result<PagedSearchResult<SearchPostListDto>>> SearchPostsWithTotalCountAsync
            (string query, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);
        Task<Result<PostAdminDetailsDto>> GetPostByIdAsync(int postId, CancellationToken ct = default);
        Task<Result<PostAdminDetailsDto>> AddPostAsync(PostCreateDto postDto, CancellationToken ct = default);
        Task<Result<PostAdminDetailsDto>> UpdatePostAsync
            (int postId, PostUpdateDto postDto, CancellationToken ct = default);
        Task<Result> DeletePostAsync(int postId, CancellationToken ct = default);
    }
}
