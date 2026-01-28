namespace PostApiService.Models.Dto.Response
{
    public record PostDetailsDto(
        int Id,
        string Title,
        string Description,
        string Content,
        string Author,
        string ImageUrl,
        string Slug,
        string? MetaTitle,
        string? MetaDescription,
        string Category,
        string CategorySlug,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        int CommentCount
    );   
}
