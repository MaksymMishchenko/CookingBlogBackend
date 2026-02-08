namespace PostApiService.Models.Dto.Response
{
    public record CommentDto(
        int Id,
        string Content,
        string Author,
        DateTime CreatedAt,
        string UserId,
        bool IsEditedByAdmin = false
    );
}
