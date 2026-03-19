using System.Text.Json.Serialization;

namespace PostApiService.Models.Dto.Response
{
    public record CommentCreatedDto(
         int Id,
         string Author,
         string Content,
         DateTime CreatedAt,
         string UserId,
         [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        int? ParentId = null,
         string? ReplyToUserName = null,
         bool IsEditedByAdmin = false
    );
}
