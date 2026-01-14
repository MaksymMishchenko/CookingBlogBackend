using PostApiService.Models.Dto.Response;

namespace PostApiService.Helper
{
    public static class CommentMappingExtensions
    {
        public static CommentCreatedDto ToCreatedDto(this Comment comment, string authorName)
        {
            return new CommentCreatedDto(
                Id: comment.Id,
                Author: authorName,
                Content: comment.Content,
                CreatedAt: comment.CreatedAt,
                UserId: comment.UserId               
            );
        }

        public static CommentUpdatedDto ToUpdatedDto(this Comment comment, string authorName)
        {
            return new CommentUpdatedDto(
                Id: comment.Id,
                Author: authorName,
                Content: comment.Content,
                CreatedAt: comment.CreatedAt,
                UserId: comment.UserId,
                IsEditedByAdmin: comment.IsEditedByAdmin
            );
        }
    }
}
