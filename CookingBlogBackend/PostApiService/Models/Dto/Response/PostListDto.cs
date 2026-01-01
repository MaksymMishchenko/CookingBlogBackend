namespace PostApiService.Models.Dto.Response
{
    public record PostListDto(
        int Id,
        string Title,
        string Slug,
        string Author,
        string Category,
        DateTime CreatedAt,
        string Description,
        int CommentsCount
    );
}
