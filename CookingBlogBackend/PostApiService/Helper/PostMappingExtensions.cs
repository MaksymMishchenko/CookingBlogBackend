using PostApiService.Models.Constants;
using PostApiService.Models.Dto.Requests;
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
            p.Category.Slug ?? ContentConstants.DefaultSlugCategory,
            p.CreatedAt,
            p.UpdatedAt,
            p.Description,
            p.Comments.Count
        );

        public static Expression<Func<Post, PostAdminDetailsDto>> ToAdminDetailsDto => p => new PostAdminDetailsDto(
            p.Id,
            p.Title,
            p.Description,
            p.Content,
            p.Author,
            p.ImageUrl,
            p.Slug,
            p.MetaTitle,
            p.MetaDescription,
            p.CategoryId,
            p.CreatedAt,
            p.UpdatedAt
        );

        public static IQueryable<PostDetailsDto> ToDetailsDtoExpression(this IQueryable<Post> query)
        {
            return query.Select(p => new PostDetailsDto(
                p.Id,
                p.Title,
                p.Description,
                p.Content,
                p.Author,
                p.ImageUrl,
                p.Slug,
                p.MetaTitle,
                p.MetaDescription,
                p.Category.Name ?? ContentConstants.DefaultCategory,
                p.Category.Slug ?? ContentConstants.DefaultSlugCategory,
                p.CreatedAt,
                p.UpdatedAt,
                p.Comments.Count
            ));
        }

        public static PostAdminDetailsDto MapToAdminDto(this Post p) =>
        new PostAdminDetailsDto(
            p.Id,
            p.Title,
            p.Description,
            p.Content,
            p.Author,
            p.ImageUrl,
            p.Slug,
            p.MetaTitle,
            p.MetaDescription,
            p.CategoryId,
            p.CreatedAt,
            p.UpdatedAt
        );

        public static Post ToEntity(this PostCreateDto dto, string sanitizedContent)
        {
            var title = dto.Title.StripHtml();
            var description = dto.Description.StripHtml();

            return new Post
            {
                Title = title,
                Description = description,
                Content = sanitizedContent,
                Author = dto.Author.StripHtml(),
                ImageUrl = dto.ImageUrl,
                Slug = dto.Slug.StripHtml(),
                CategoryId = dto.CategoryId,

                MetaTitle = string.IsNullOrWhiteSpace(dto.MetaTitle)
                    ? title
                    : dto.MetaTitle.StripHtml(),

                MetaDescription = string.IsNullOrWhiteSpace(dto.MetaDescription)
                    ? (description.Length > 200 ? description[..197] + "..." : description)
                    : dto.MetaDescription.StripHtml(),

                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
        }        

        public static void UpdateEntity(this PostUpdateDto dto, Post entity, string sanitizedContent)
        {
            entity.Title = dto.Title.StripHtml();
            entity.Description = dto.Description.StripHtml();
            entity.Content = sanitizedContent;
            entity.Author = dto.Author.StripHtml();
            entity.ImageUrl = dto.ImageUrl;
            entity.Slug = dto.Slug.StripHtml();
            entity.CategoryId = dto.CategoryId;

            entity.MetaTitle = string.IsNullOrWhiteSpace(dto.MetaTitle)
                ? dto.Title.StripHtml()
                : dto.MetaTitle.StripHtml();

            entity.MetaDescription = string.IsNullOrWhiteSpace(dto.MetaDescription)
                ? (dto.Description.StripHtml().Length > 200
                    ? dto.Description.StripHtml()[..197] + "..."
                    : dto.Description.StripHtml())
                : dto.MetaDescription.StripHtml();
        }
    }
}
