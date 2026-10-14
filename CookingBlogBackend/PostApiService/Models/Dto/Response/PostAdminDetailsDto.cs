namespace PostApiService.Models.Dto.Response
{
    public record PostAdminDetailsDto(
        int Id,
        string Title,
        string Description,
        string Content,
        string AuthorId,
        string Author,        
        string Slug,
        string? MetaTitle,
        string? MetaDescription,
        int CategoryId,
        bool isActive,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string PreviewImageUrl,
        string PreviewImageAlt
    );
}
