namespace PostApiService.Models.Dto.Response
{
    public record CommentDto(
        int Id,
        string Author,
        string Content,
        DateTime CreatedAt,
        string UserId
    );
}
