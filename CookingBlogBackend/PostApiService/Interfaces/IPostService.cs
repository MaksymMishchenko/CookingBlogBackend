using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface IPostService
    {
        Task<Result<object>> GetPostsPagedAsync(PostQueryDto postQuery, CancellationToken ct = default);

        Task<Result<PostDetailsDto>> GetPostBySlugAsync(PostRequestBySlug dto, CancellationToken ct = default);
    }
}
