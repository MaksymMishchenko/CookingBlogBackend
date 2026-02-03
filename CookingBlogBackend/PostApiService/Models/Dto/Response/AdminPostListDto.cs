namespace PostApiService.Models.Dto.Response
{
    public record AdminPostListDto(
        int Id,
        string Title,
        string Author,
        string CategoryName,
        DateTime CreatedAt,
        bool IsActive,
        int CommentCount
    );
}
