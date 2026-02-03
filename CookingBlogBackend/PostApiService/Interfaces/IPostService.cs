using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface IPostService
    {
        Task<Result<PagedResult<PostListDto>>> GetActivePostsPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken ct = default);
        Task<Result<PagedResult<AdminPostListDto>>> GetAdminPostsPagedAsync(
            bool? isActive = null,
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken ct = default);
        Task<Result<PagedSearchResult<SearchPostListDto>>> SearchActivePostsPagedAsync
            (string query, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);
        Task<Result<PagedResult<PostListDto>>> GetActivePostsByCategoryPagedAsync
            (string slug, int pageNumber, int pageSize, CancellationToken ct);
        Task<Result<PostAdminDetailsDto>> GetPostByIdAsync(int postId, CancellationToken ct = default);
        Task<Result<PostDetailsDto>> GetActivePostBySlugAsync(PostRequestBySlug dto, CancellationToken ct = default);
        Task<Result<PostAdminDetailsDto>> AddPostAsync(PostCreateDto postDto, CancellationToken ct = default);
        Task<Result<PostAdminDetailsDto>> UpdatePostAsync
            (int postId, PostUpdateDto postDto, CancellationToken ct = default);
        Task<Result> DeletePostAsync(int postId, CancellationToken ct = default);
    }
}
