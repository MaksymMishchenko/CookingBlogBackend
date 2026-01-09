using PostApiService.Models.Dto.Response;

namespace PostApiService.Helper
{
    public static class CommentMappingExtensions
    {
        public static CommentDto ToDto(this Comment comment, string authorName)
        {
            return new CommentDto(
                Id: comment.Id,
                Author: authorName,
                Content: comment.Content,
                CreatedAt: comment.CreatedAt,
                UserId: comment.UserId
            );
        }
    }
}
