using System.Text.Json.Serialization;

namespace PostApiService.Models.Dto.Response
{
    public record CommentDto(
       int Id,
       string Content,
       string Author,
       DateTime CreatedAt,
       string UserId,       
       [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
       int? ParentId = null,
       string? ReplyToUserName = null,
       bool IsEditedByAdmin = false
    );
}
