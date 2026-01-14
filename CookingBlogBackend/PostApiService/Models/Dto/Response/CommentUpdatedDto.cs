namespace PostApiService.Models.Dto.Response
{
    public record CommentUpdatedDto(
        int Id,
        string Author,
        string Content,
        DateTime CreatedAt,
        string UserId,
        bool IsEditedByAdmin
    ) : CommentBaseDto(Id, Author, Content, CreatedAt, UserId);
}
