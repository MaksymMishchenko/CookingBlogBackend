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
            p.CreatedAt,
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
            p.CreatedAt
        );

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
            p.CreatedAt
        );

        public static Post ToEntity(this PostCreateDto dto)
        {
            return new Post
            {
                Title = dto.Title,
                Description = dto.Description,
                Content = dto.Content,
                Author = dto.Author,
                ImageUrl = dto.ImageUrl,
                Slug = dto.Slug,
                CategoryId = dto.CategoryId,
                MetaTitle = string.IsNullOrWhiteSpace(dto.MetaTitle)
                    ? dto.Title
                    : dto.MetaTitle,
                MetaDescription = string.IsNullOrWhiteSpace(dto.MetaDescription)
                    ? (dto.Description.Length > 200 ? dto.Description[..197] + "..." : dto.Description)
                    : dto.MetaDescription,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
        }

        public static PostAdminDetailsDto ToDto(this Post post)
        {
            return new PostAdminDetailsDto(
                post.Id,
                post.Title,
               post.Description,
                post.Content,
                post.Author,
                post.ImageUrl,
                post.Slug,
                string.IsNullOrWhiteSpace(post.MetaTitle)
                    ? post.Title
                    : post.MetaTitle,
                string.IsNullOrWhiteSpace(post.MetaDescription)
                    ? (post.Description.Length > 200 ? post.Description[..197] + "..." : post.Description)
                    : post.MetaDescription,
                post.CategoryId,
                DateTime.UtcNow
            );
        }

        public static void UpdateEntity(this PostUpdateDto dto, Post existingPost)
        {
            existingPost.Title = dto.Title;
            existingPost.Description = dto.Description;
            existingPost.Content = dto.Content;
            existingPost.ImageUrl = dto.ImageUrl;
            existingPost.MetaTitle = dto.MetaTitle;
            existingPost.MetaDescription = dto.MetaDescription;
            existingPost.Slug = dto.Slug;
        }
    }
}
