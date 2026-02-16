using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface IPostService
    {
        Task<Result<object>> GetPostsPagedAsync(PostQueryDto postQuery, CancellationToken ct = default);

        Task<Result<PagedResult<AdminPostListDto>>> GetAdminPostsPagedAsync(
            PostAdminQueryDto postQuery, CancellationToken ct = default);

        Task<Result<PostAdminDetailsDto>> GetPostByIdAsync(int postId, CancellationToken ct = default);

        Task<Result<PostDetailsDto>> GetPostBySlugAsync(PostRequestBySlug dto, CancellationToken ct = default);

        Task<Result<PostAdminDetailsDto>> AddPostAsync(PostCreateDto postDto, CancellationToken ct = default);

        Task<Result<PostAdminDetailsDto>> UpdatePostAsync
            (int postId, PostUpdateDto postDto, CancellationToken ct = default);

        Task<Result> DeletePostAsync(int postId, CancellationToken ct = default);
    }
}
