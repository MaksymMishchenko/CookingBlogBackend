using PostApiService.Models.Constants;
using PostApiService.Models.Dto.Response;
using System.Linq.Expressions;

namespace PostApiService.Helper
{
    public static class PostMappingExtensions
    {
        public static Expression<Func<Post, PostListDto>> ToDtoExpression => p => new PostListDto(
            p.Id,
            p.Title,
            p.Slug,
            p.Author,
            p.Category.Name ?? ContentConstants.DefaultCategory,
            p.CreatedAt,
            p.Description,
            p.Comments.Count
        );
    }
}
