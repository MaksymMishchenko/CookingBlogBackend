namespace PostApiService.Models.Dto.Response
{
    public record PostAdminDetailsDto(
        int Id,
        string Title,
        string Description,
        string Content,
        string Author,
        string ImageUrl,
        string Slug,
        string? MetaTitle,
        string? MetaDescription,
        int CategoryId,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}
