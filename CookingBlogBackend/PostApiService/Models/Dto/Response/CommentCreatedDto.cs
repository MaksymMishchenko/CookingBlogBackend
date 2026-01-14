namespace PostApiService.Models.Dto.Response
{
    public record CommentCreatedDto(
        int Id,
        string Author,
        string Content,
        DateTime CreatedAt,
        string UserId       
    ) : CommentBaseDto(Id, Author, Content, CreatedAt, UserId);
}
