using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface IPublicPostService
    {
        Task<Result<object>> GetPostsPagedAsync(PublicPostQueryDto postQuery, CancellationToken ct = default);

        Task<Result<PostDetailsDto>> GetPostBySlugAsync(PostRequestBySlug dto, CancellationToken ct = default);
    }
}
