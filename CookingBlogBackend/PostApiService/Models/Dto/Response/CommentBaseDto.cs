namespace PostApiService.Models.Dto.Response
{
    public abstract record CommentBaseDto(
       int Id,
       string Author,
       string Content,
       DateTime CreatedAt,
       string UserId,
       int? ParentId = null
    );
}
