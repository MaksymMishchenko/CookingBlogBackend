namespace PostApiService.Models.Dto.Response
{
    public record PostListDto(
        int Id,
        string Title,
        string Slug,
        string Author,
        string Category,
        string CategorySlug,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string Description,
        int CommentsCount
    );
}
