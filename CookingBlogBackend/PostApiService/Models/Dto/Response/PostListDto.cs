namespace PostApiService.Models.Dto.Response
{
    public record PostListDto(
        int Id,
        string Title,
        string Slug,
        string Author,
        string Category,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string Description,
        int CommentsCount
    );
}
